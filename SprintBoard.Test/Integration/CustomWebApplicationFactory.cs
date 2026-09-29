using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SprintBoard.Application.Interfaces;
using SprintBoard.Infrastructure.Persistence;

namespace SprintBoard.Test.Integration
{
    /// <summary>
    /// Creates an isolated SprintBoard application instance for
    /// integration testing using SQLite and test-only configuration.
    /// </summary>
    public sealed class CustomWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private const string TestJwtKey =
            "SprintBoard-Integration-Testing-Key-2026-Secure-123456789";

        private const string TestJwtIssuer =
            "SprintBoard.IntegrationTests";

        private const string TestJwtAudience =
            "SprintBoard.IntegrationTests.Client";

        private SqliteConnection? _connection;

        private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;

        /// <summary>
        /// Initializes the integration test factory using the
        /// default SprintBoard test configuration.
        /// </summary>
        public CustomWebApplicationFactory()
            : this(
                new Dictionary<string, string?>())
        {
        }

        /// <summary>
        /// Initializes the integration test factory with
        /// additional configuration overrides.
        /// </summary>
        /// <param name="configurationOverrides">
        /// Configuration values that should replace the
        /// default integration test settings.
        /// </param>
        internal CustomWebApplicationFactory(
            IReadOnlyDictionary<string, string?>
                configurationOverrides)
        {
            _configurationOverrides =
                configurationOverrides
                ?? throw new ArgumentNullException(
                    nameof(configurationOverrides));
        }

        /// <summary>
        /// Supplies configuration values before the SprintBoard
        /// application entry point is executed.
        /// </summary>
        protected override IHost CreateHost(
            IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(
                configuration =>
                {
                    var settings =
                        new Dictionary<string, string?>
                        {
                            ["Jwt:Key"] =
                                TestJwtKey,

                            ["Jwt:Issuer"] =
                                TestJwtIssuer,

                            ["Jwt:Audience"] =
                                TestJwtAudience,

                            ["Jwt:ExpiresMinutes"] =
                                "60",

                            ["RateLimiting:Auth:LoginPermitLimit"] =
                                "10000",

                            ["RateLimiting:Auth:LoginWindowSeconds"] =
                                "60",

                            ["RateLimiting:Auth:RegisterPermitLimit"] =
                                "10000",

                            ["RateLimiting:Auth:RegisterWindowSeconds"] =
                                "60",

                            ["FileStorage:PublicBaseUrl"] =
                                "http://localhost",
                        };

                    foreach (var configurationOverride in _configurationOverrides)
                    {
                        settings[configurationOverride.Key] = configurationOverride.Value;
                    }

                    configuration.AddInMemoryCollection(
                        settings);
                });

            var host =
                base.CreateHost(builder);

            using var scope =
                host.Services.CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<
                        SprintBoardDbContext>();

            dbContext.Database.EnsureCreated();

            return host;
        }

        /// <summary>
        /// Replaces production infrastructure after the normal
        /// application registrations have been executed.
        /// </summary>
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(
                services =>
                {
                    ConfigureDatabase(services);
                    ConfigureExternalServices(services);
                });
        }

        // ============================================================
        // DATABASE
        // ============================================================

        /// <summary>
        /// Replaces SQL Server with an isolated SQLite
        /// in-memory relational database.
        /// </summary>
        private void ConfigureDatabase(
            IServiceCollection services)
        {
            services.RemoveAll<
                SprintBoardDbContext>();

            services.RemoveAll<
                DbContextOptions<
                    SprintBoardDbContext>>();

            services.RemoveAll<
                IDbContextOptionsConfiguration<
                    SprintBoardDbContext>>();

            _connection =
                new SqliteConnection(
                    "Data Source=:memory:");

            _connection.Open();

            services.AddDbContext<
                SprintBoardDbContext>(
                options =>
                    options.UseSqlite(
                        _connection));
        }

        // ============================================================
        // EXTERNAL SERVICES
        // ============================================================

        /// <summary>
        /// Replaces external integrations that must not perform real
        /// network operations during automated integration tests.
        /// </summary>
        private static void ConfigureExternalServices(
            IServiceCollection services)
        {
            /*
             * Invitations should execute their real application and
             * persistence flow, but automated tests must never send
             * real external emails.
             */
            services.RemoveAll<IEmailService>();

            services.AddSingleton<
                IEmailService,
                NoOpEmailService>();
        }

        // ============================================================
        // DISPOSE
        // ============================================================

        /// <summary>
        /// Releases the SQLite connection when the integration
        /// test application is disposed.
        /// </summary>
        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                _connection?.Dispose();
            }

            base.Dispose(disposing);
        }

        // ============================================================
        // TEST DOUBLES
        // ============================================================

        /// <summary>
        /// Prevents integration tests from sending real emails while
        /// preserving the complete invitation application flow.
        /// </summary>
        private sealed class NoOpEmailService
            : IEmailService
        {
            /// <summary>
            /// Simulates successful invitation email delivery.
            /// </summary>
            public Task SendBoardInvitationAsync(
                string toEmail,
                string boardName,
                string acceptInvitationLink,
                string declineInvitationLink)
            {
                return Task.CompletedTask;
            }
        }
    }
}