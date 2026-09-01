using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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

        /// <summary>
        /// Supplies configuration values before the SprintBoard
        /// application entry point is executed.
        /// </summary>
        /// <param name="builder">
        /// Host builder used to initialize the test application.
        /// </param>
        /// <returns>
        /// The initialized SprintBoard test host.
        /// </returns>
        protected override IHost CreateHost(
            IHostBuilder builder)
        {
            /*
             * This configuration is added BEFORE Program.cs executes.
             *
             * Therefore:
             *
             * builder.Configuration.GetSection("Jwt")
             *
             * already contains these values when SprintBoard
             * registers JwtOptions and JwtBearer.
             */
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
                                "60"
                        };

                    configuration.AddInMemoryCollection(
                        settings);
                });

            /*
             * Allow WebApplicationFactory to execute the real
             * SprintBoard Program.cs and build the application.
             */
            var host =
                base.CreateHost(builder);

            /*
             * At this point the custom SQLite DbContext has already
             * replaced SQL Server. Build the schema using the real
             * SprintBoard EF Core model.
             */
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
        /// Replaces production infrastructure after Program.cs
        /// registrations have been executed.
        /// </summary>
        /// <param name="builder">
        /// Web host builder used by the integration-test application.
        /// </param>
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(
                services =>
                {
                    ConfigureDatabase(
                        services);
                });
        }

        // ============================================================
        // DATABASE
        // ============================================================

        /// <summary>
        /// Replaces SQL Server with an isolated SQLite
        /// in-memory relational database.
        /// </summary>
        /// <param name="services">
        /// Application service collection.
        /// </param>
        private void ConfigureDatabase(
            IServiceCollection services)
        {
            /*
             * Remove the SprintBoardDbContext registration
             * created by the production application.
             */
            services.RemoveAll<
                SprintBoardDbContext>();

            services.RemoveAll<
                DbContextOptions<
                    SprintBoardDbContext>>();

            /*
             * Remove EF Core's SQL Server options configuration.
             *
             * Without this, EF Core may detect both SQL Server
             * and SQLite providers for the same DbContext.
             */
            services.RemoveAll<
                IDbContextOptionsConfiguration<
                    SprintBoardDbContext>>();

            /*
             * SQLite in-memory databases live only while
             * their connection remains open.
             */
            _connection =
                new SqliteConnection(
                    "Data Source=:memory:");

            _connection.Open();

            /*
             * Register SQLite as the relational database provider
             * used by integration tests.
             */
            services.AddDbContext<
                SprintBoardDbContext>(
                options =>
                    options.UseSqlite(
                        _connection));
        }

        // ============================================================
        // DISPOSE
        // ============================================================

        /// <summary>
        /// Releases the SQLite connection when the integration
        /// test application is disposed.
        /// </summary>
        /// <param name="disposing">
        /// Indicates whether managed resources should be disposed.
        /// </param>
        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                _connection?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}