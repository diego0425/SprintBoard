using SprintBoard.Domain.Entities;

namespace SprintBoard.Application.Interfaces
{
    /// <summary>
    /// Defines persistence operations required by the application layer for users.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Retrieves a user by identifier.
        /// </summary>
        /// <param name="userId">
        /// The user identifier.
        /// </param>
        /// <returns>
        /// The matching user, or <see langword="null"/> when not found.
        /// </returns>
        Task<User?> GetByIdAsync(Guid userId);
        /// <summary>
        /// Retrieves a user by email address.
        /// </summary>
        /// <param name="email">
        /// The normalized email address.
        /// </param>
        /// <returns>
        /// The matching user, or <see langword="null"/> when not found.
        /// </returns>
        Task<User?> GetByEmailAsync(string email);
        /// <summary>
        /// Retrieves a user by username.
        /// </summary>
        /// <param name="username">
        /// The normalized username.
        /// </param>
        /// <returns>
        /// The matching user, or <see langword="null"/> when not found.
        /// </returns>
        Task<User?> GetByUsernameAsync(string username);
        /// <summary>
        /// Stages a user for persistence.
        /// </summary>
        /// <param name="user">
        /// The user to add.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task AddAsync(User user);
        /// <summary>
        /// Persists all pending user changes.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task SaveChangesAsync();
    }
}
