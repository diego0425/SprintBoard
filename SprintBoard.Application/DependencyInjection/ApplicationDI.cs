using Microsoft.Extensions.DependencyInjection;
using SprintBoard.Application.Interfaces;
using SprintBoard.Application.Services;

namespace SprintBoard.Application.DependencyInjection
{
    /// <summary>
    /// Provides dependency injection registrations for the Application layer.
    /// </summary>
    public static class ApplicationDI
    {
        /// <summary>
        /// Registers application services and authorization abstractions in the dependency injection container.
        /// </summary>
        /// <param name="services">
        /// The service collection that receives the Application layer registrations.
        /// </param>
        /// <returns>
        /// The same service collection so additional registrations can be chained.
        /// </returns>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<BoardService>();
            services.AddScoped<CardService>();
            services.AddScoped<UserService>();
            services.AddScoped<AuthService>();
            services.AddScoped<CardTaskService>();
            services.AddScoped<InvitationService>();
            services.AddScoped<IMembershipAuthorizationService, MembershipAuthorizationService>();

            return services;
        }
    }
}
