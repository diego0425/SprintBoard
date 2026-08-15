using System.Security.Cryptography;
using System.Text;
using SprintBoard.Application.DTOs.Auth;
using SprintBoard.Application.Interfaces;
using SprintBoard.Domain.Entities;

namespace SprintBoard.Application.Services
{
    /// <summary>
    /// Coordinates user registration and credential validation.
    /// </summary>
    public sealed class AuthService
    {
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthService"/> class.
        /// </summary>
        /// <param name="userRepository">
        /// Repository used to query existing accounts and persist newly registered users.
        /// </param>
        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Validates a registration request, creates a new user account, and persists it.
        /// </summary>
        /// <param name="request">
        /// The registration data containing identity and password information for the new account.
        /// </param>
        /// <returns>
        /// The newly created user entity.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when required registration data is missing, the password is too short, or the password confirmation does not match.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the supplied email address is already in use.
        /// </exception>
        public async Task<User> RegisterAsync(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new ArgumentException("Full name cannot be empty.");

            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ArgumentException("Username cannot be empty.");

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email cannot be empty.");

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                throw new ArgumentException("Password must be at least 8 characters.");

            if (request.Password != request.RepeatPassword)
                throw new ArgumentException("Passwords do not match.");

            var email = request.Email.Trim().ToLowerInvariant();

            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser is not null)
                throw new InvalidOperationException("Email already in use.");

            var passwordHash = HashPassword(request.Password);

            var user = new User(request.FullName, request.Username, email, passwordHash);
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return user;
        }

        /// <summary>
        /// Validates user credentials and returns the authenticated user.
        /// </summary>
        /// <param name="request">
        /// The login request containing the email address and password to validate.
        /// </param>
        /// <returns>
        /// The authenticated user entity.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the email address or password is missing.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the account does not exist or the password is invalid.
        /// </exception>
        public async Task<User> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Email and password are required.");

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _userRepository.GetByEmailAsync(email);

            if (user is null)
                throw new InvalidOperationException("Invalid credentials.");

            var passwordHash = HashPassword(request.Password);

            if (!string.Equals(user.PasswordHash, passwordHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid credentials.");

            return user;
        }

        /// <summary>
        /// Computes the SHA-256 hash used by the current authentication workflow.
        /// </summary>
        /// <param name="password">
        /// The plain-text password to hash.
        /// </param>
        /// <returns>
        /// The hexadecimal representation of the password hash.
        /// </returns>
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(hashBytes);
        }
    }
}
