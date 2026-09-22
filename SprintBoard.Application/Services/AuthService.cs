using System.Security.Cryptography;
using System.Text;
using SprintBoard.Application.DTOs.Auth;
using SprintBoard.Application.Interfaces;
using SprintBoard.Domain.Entities;

namespace SprintBoard.Application.Services
{
    /// <summary>
    /// Coordinates user registration, credential validation,
    /// and legacy password-hash migration.
    /// </summary>
    public sealed class AuthService
    {
        private const int Sha256HashLength =
            64;

        private readonly IUserRepository
            _userRepository;

        private readonly IPasswordHasher
            _passwordHasher;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="AuthService"/> class.
        /// </summary>
        /// <param name="userRepository">
        /// Repository used to query and persist user accounts.
        /// </param>
        /// <param name="passwordHasher">
        /// Password hashing abstraction used for secure password
        /// creation and verification.
        /// </param>
        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher)
        {
            _userRepository =
                userRepository;

            _passwordHasher =
                passwordHasher;
        }

        /// <summary>
        /// Validates a registration request, securely hashes the
        /// password, creates the user account, and persists it.
        /// </summary>
        /// <param name="request">
        /// Registration data for the new account.
        /// </param>
        /// <returns>
        /// The newly created user entity.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when required registration data is missing,
        /// the password is too short, or confirmation does not match.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the supplied email address is already in use.
        /// </exception>
        public async Task<User> RegisterAsync(
            RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(
                    request.FullName))
            {
                throw new ArgumentException(
                    "Full name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.Username))
            {
                throw new ArgumentException(
                    "Username cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.Email))
            {
                throw new ArgumentException(
                    "Email cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.Password) ||
                request.Password.Length < 8)
            {
                throw new ArgumentException(
                    "Password must be at least 8 characters.");
            }

            if (request.Password !=
                request.RepeatPassword)
            {
                throw new ArgumentException(
                    "Passwords do not match.");
            }

            var email =
                request.Email
                    .Trim()
                    .ToLowerInvariant();

            var existingUser =
                await _userRepository
                    .GetByEmailAsync(email);

            if (existingUser is not null)
            {
                throw new InvalidOperationException(
                    "Email already in use.");
            }

            var passwordHash =
                _passwordHasher.Hash(
                    request.Password);

            var user =
                new User(
                    request.FullName,
                    request.Username,
                    email,
                    passwordHash);

            await _userRepository
                .AddAsync(user);

            await _userRepository
                .SaveChangesAsync();

            return user;
        }

        /// <summary>
        /// Validates user credentials and transparently upgrades
        /// legacy or outdated password hashes when authentication succeeds.
        /// </summary>
        /// <param name="request">
        /// Login request containing the email address and password.
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
        public async Task<User> LoginAsync(
            LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(
                    request.Email) ||
                string.IsNullOrWhiteSpace(
                    request.Password))
            {
                throw new ArgumentException(
                    "Email and password are required.");
            }

            var email =
                request.Email
                    .Trim()
                    .ToLowerInvariant();

            var user =
                await _userRepository
                    .GetByEmailAsync(email);

            if (user is null)
            {
                throw new InvalidOperationException(
                    "Invalid credentials.");
            }

            if (IsLegacySha256Hash(
                    user.PasswordHash))
            {
                if (!VerifyLegacySha256Password(
                        user.PasswordHash,
                        request.Password))
                {
                    throw new InvalidOperationException(
                        "Invalid credentials.");
                }

                await UpgradePasswordHashAsync(
                    user,
                    request.Password);

                return user;
            }

            var verificationResult =
                _passwordHasher.Verify(
                    user.PasswordHash,
                    request.Password);

            if (verificationResult ==
                PasswordVerificationOutcome.Failed)
            {
                throw new InvalidOperationException(
                    "Invalid credentials.");
            }

            if (verificationResult ==
                PasswordVerificationOutcome
                    .SuccessRehashNeeded)
            {
                await UpgradePasswordHashAsync(
                    user,
                    request.Password);
            }

            return user;
        }

        /// <summary>
        /// Determines whether a stored hash matches the hexadecimal
        /// SHA-256 format used by older SprintBoard accounts.
        /// </summary>
        private static bool IsLegacySha256Hash(
            string passwordHash)
        {
            return passwordHash.Length ==
                    Sha256HashLength &&
                passwordHash.All(
                    Uri.IsHexDigit);
        }

        /// <summary>
        /// Verifies a password against the legacy SHA-256 format
        /// using a fixed-time comparison.
        /// </summary>
        private static bool VerifyLegacySha256Password(
            string storedHash,
            string password)
        {
            if (!IsLegacySha256Hash(
                    storedHash))
            {
                return false;
            }

            var storedHashBytes =
                Convert.FromHexString(
                    storedHash);

            var candidateHashBytes =
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        password));

            return CryptographicOperations
                .FixedTimeEquals(
                    storedHashBytes,
                    candidateHashBytes);
        }

        /// <summary>
        /// Replaces the stored password hash using the current secure
        /// hashing configuration and persists the upgraded value.
        /// </summary>
        private async Task UpgradePasswordHashAsync(
            User user,
            string password)
        {
            var upgradedHash =
                _passwordHasher.Hash(
                    password);

            user.ChangePassword(
                upgradedHash);

            await _userRepository
                .SaveChangesAsync();
        }
    }
}