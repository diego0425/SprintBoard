using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SprintBoard.Application.Interfaces;
using SprintBoard.Infrastructure.Email;
using SprintBoard.Infrastructure.Persistence;
using SprintBoard.Infrastructure.Persistence.Repositories;

namespace SprintBoard.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Provides dependency injection registrations for the Infrastructure layer.
    /// </summary>
    public static class InfrastructureDI
    {
        /// <summary>
        /// Registers the database context, repository implementations, and infrastructure services.
        /// </summary>
        /// <param name="services">
        /// The service collection that receives the Infrastructure layer registrations.
        /// </param>
        /// <param name="configuration">
        /// The application configuration used to resolve infrastructure settings such as the database connection string.
        /// </param>
        /// <returns>
        /// The same service collection so additional registrations can be chained.
        /// </returns>
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<SprintBoardDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("Default")));

            services.AddScoped<IBoardRepository, BoardRepository>();
            services.AddScoped<ICardRepository, CardRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IBoardMemberRepository, BoardMemberRepository>();
            services.AddScoped<IBoardInvitationRepository, BoardInvitationRepository>();
            services.AddScoped<ICardTaskRepository, CardTaskRepository>();
            services.AddScoped<IEmailService, SmtpEmailService>();

            return services;
        }
    }
}
