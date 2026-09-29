using System.Text;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using SprintBoard.api.Errors;
using SprintBoard.api.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SprintBoard.api.Auth;
using SprintBoard.api.Middlewares;
using SprintBoard.api.Services;
using SprintBoard.Application.Common;
using SprintBoard.Application.DependencyInjection;
using SprintBoard.Application.Interfaces;
using SprintBoard.Infrastructure.DependencyInjection;
using SprintBoard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(
    (services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(
                builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "SprintBoard API",
            Version = "v1"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter: Bearer {your JWT token}"
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IInvitationLinkBuilder, InvitationLinkBuilder>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddHttpContextAccessor();

var jwtOptions = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException(
        "JWT signing key is missing.");
}

if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least 32 bytes.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
{
    throw new InvalidOperationException(
        "JWT issuer is missing.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException(
        "JWT audience is missing.");
}

if (jwtOptions.ExpiresMinutes <= 0)
{
    throw new InvalidOperationException(
        "JWT expiration must be greater than zero.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddDbContext<SprintBoardDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddDbContextCheck<SprintBoardDbContext>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);

var allowedOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }
        });
});

var authRateLimitOptions =
    builder.Configuration
        .GetSection(
            AuthRateLimitOptions.SectionName)
        .Get<AuthRateLimitOptions>()
    ?? new AuthRateLimitOptions();

if (authRateLimitOptions.LoginPermitLimit <= 0 ||
    authRateLimitOptions.LoginWindowSeconds <= 0 ||
    authRateLimitOptions.RegisterPermitLimit <= 0 ||
    authRateLimitOptions.RegisterWindowSeconds <= 0)
{
    throw new InvalidOperationException(
        "Authentication rate-limit configuration " +
        "must contain positive values.");
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.OnRejected =
        async (
            context,
            cancellationToken) =>
        {
            var httpContext =
                context.HttpContext;

            if (context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter,
                    out TimeSpan retryAfter))
            {
                httpContext.Response.Headers
                    .RetryAfter =
                    Math.Ceiling(
                            retryAfter.TotalSeconds)
                        .ToString(
                            CultureInfo.InvariantCulture);
            }

            var response =
                new ApiErrorResponse
                {
                    StatusCode =
                        StatusCodes
                            .Status429TooManyRequests,

                    Message =
                        "Too many requests. " +
                        "Please try again later.",

                    TraceId =
                        httpContext.TraceIdentifier
                };

            await httpContext.Response
                .WriteAsJsonAsync(
                    response,
                    cancellationToken);
        };

    options.AddPolicy(
        AuthRateLimitPolicies.Login,
        httpContext =>
            RateLimitPartition
                .GetFixedWindowLimiter(
                    partitionKey:
                        httpContext.Connection
                            .RemoteIpAddress?
                            .ToString()
                        ?? "unknown",

                    factory:
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit =
                                    authRateLimitOptions
                                        .LoginPermitLimit,

                                Window =
                                    TimeSpan.FromSeconds(
                                        authRateLimitOptions
                                            .LoginWindowSeconds),

                                QueueLimit =
                                    0,

                                AutoReplenishment =
                                    true
                            }));

    options.AddPolicy(
        AuthRateLimitPolicies.Register,
        httpContext =>
            RateLimitPartition
                .GetFixedWindowLimiter(
                    partitionKey:
                        httpContext.Connection
                            .RemoteIpAddress?
                            .ToString()
                        ?? "unknown",

                    factory:
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit =
                                    authRateLimitOptions
                                        .RegisterPermitLimit,

                                Window =
                                    TimeSpan.FromSeconds(
                                        authRateLimitOptions
                                            .RegisterWindowSeconds),

                                QueueLimit =
                                    0,

                                AutoReplenishment =
                                    true
                            }));
});

var knownProxyIp =
    builder.Configuration[
        "ReverseProxy:KnownProxyIp"];

IPAddress? knownProxyAddress =
    null;

if (!string.IsNullOrWhiteSpace(
        knownProxyIp))
{
    if (!IPAddress.TryParse(
            knownProxyIp,
            out knownProxyAddress))
    {
        throw new InvalidOperationException(
            "Reverse proxy IP address is invalid.");
    }
}

builder.Services.Configure<
    ForwardedHeadersOptions>(
    options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto;

        /*
         * SprintBoard currently has a single trusted
         * reverse proxy in front of the API.
         */
        options.ForwardLimit =
            1;

        if (knownProxyAddress is not null)
        {
            options.KnownProxies.Add(
                knownProxyAddress);
        }
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope =
        app.Services.CreateScope();

    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<
                SprintBoardDbContext>();

    dbContext.Database.Migrate();
}

app.UseForwardedHeaders();

app.UseStaticFiles();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} " +
        "responded {StatusCode} in " +
        "{Elapsed:0.0000} ms";

    options.GetLevel =
        (httpContext, elapsed, exception) =>
        {
            if (exception is not null ||
                httpContext.Response.StatusCode >= 500)
            {
                return LogEventLevel.Error;
            }

            if (httpContext.Response.StatusCode >= 400)
            {
                return LogEventLevel.Warning;
            }

            return LogEventLevel.Information;
        };

    options.EnrichDiagnosticContext =
        (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set(
                "CorrelationId",
                httpContext.TraceIdentifier);
        };
});

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate =
            healthCheck =>
                healthCheck.Tags.Contains("live")
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate =
            healthCheck =>
                healthCheck.Tags.Contains("ready")
    });

app.MapHealthChecks("/health");

app.MapControllers();
app.Run();

/// <summary>
/// Exposes the application entry point for integration testing
/// through WebApplicationFactory.
/// </summary>
public partial class Program
{
}
