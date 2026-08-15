using Microsoft.EntityFrameworkCore;
using SprintBoard.Application.Interfaces;
using SprintBoard.Domain.Entities;

namespace SprintBoard.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Provides persistence operations for users.
    /// </summary>
    public sealed class UserRepository : IUserRepository
    {
        private readonly SprintBoardDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRepository"/> class.
        /// </summary>
        /// <param name="dbContext">
        /// The SprintBoard Entity Framework Core context used to execute queries and track persistence changes.
        /// </param>
        public UserRepository(SprintBoardDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Gets a user by its identifier.
        /// </summary>
        /// <param name="userId">
        /// The user identifier.
        /// </param>
        /// <returns>
        /// The matching user, or <see langword="null"/> when not found.
        /// </returns>
        public async Task<User?> GetByIdAsync(Guid userId)
            => await _dbContext.Users
                .FirstOrDefaultAsync(user => user.Id == userId);

        /// <summary>
        /// Gets a user by email address.
        /// </summary>
        /// <param name="email">
        /// The user email address.
        /// </param>
        /// <returns>
        /// The matching user, or <see langword="null"/> when not found.
        /// </returns>
        public async Task<User?> GetByEmailAsync(string email)
            => await _dbContext.Users
                .FirstOrDefaultAsync(user => user.Email == email);

        /// <summary>
        /// Gets a user by username.
        /// </summary>
        /// <param name="username">
        /// The username.
        /// </param>
        /// <returns>
        /// The matching user, or <see langword="null"/> when not found.
        /// </returns>
        public async Task<User?> GetByUsernameAsync(string username)
            => await _dbContext.Users
                .FirstOrDefaultAsync(user => user.Username == username);

        /// <summary>
        /// Adds a user to the current unit of work.
        /// </summary>
        /// <param name="user">
        /// The user to add.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public async Task AddAsync(User user)
            => await _dbContext.Users.AddAsync(user);

        /// <summary>
        /// Persists all pending changes to the database.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public async Task SaveChangesAsync()
            => await _dbContext.SaveChangesAsync();
    }
}
