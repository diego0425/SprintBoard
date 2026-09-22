using Microsoft.AspNetCore.Identity;
using SprintBoard.Application.Interfaces;

namespace SprintBoard.Infrastructure.Security
{
    /// <summary>
    /// Provides secure password hashing and verification
    /// using the ASP.NET Core Identity PBKDF2 implementation.
    /// </summary>
    public sealed class Pbkdf2PasswordHasher
        : IPasswordHasher
    {
        private readonly PasswordHasher<object>
            _passwordHasher;

        private readonly object _userMarker =
            new();

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="Pbkdf2PasswordHasher"/> class.
        /// </summary>
        public Pbkdf2PasswordHasher()
        {
            _passwordHasher =
                new PasswordHasher<object>();
        }

        /// <summary>
        /// Creates a salted PBKDF2 hash for the supplied password.
        /// </summary>
        /// <param name="password">
        /// Plain-text password to hash.
        /// </param>
        /// <returns>
        /// Encoded password hash containing the information
        /// required for future verification.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the supplied password is empty.
        /// </exception>
        public string Hash(
            string password)
        {
            if (string.IsNullOrWhiteSpace(
                    password))
            {
                throw new ArgumentException(
                    "Password cannot be empty.",
                    nameof(password));
            }

            return _passwordHasher
                .HashPassword(
                    _userMarker,
                    password);
        }

        /// <summary>
        /// Verifies a plain-text password against a previously
        /// generated PBKDF2 password hash.
        /// </summary>
        /// <param name="hashedPassword">
        /// Stored PBKDF2 password hash.
        /// </param>
        /// <param name="providedPassword">
        /// Plain-text password supplied for authentication.
        /// </param>
        /// <returns>
        /// Verification outcome describing whether the password
        /// matched and whether the hash should be regenerated.
        /// </returns>
        public PasswordVerificationOutcome Verify(
            string hashedPassword,
            string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(
                    hashedPassword) ||
                string.IsNullOrWhiteSpace(
                    providedPassword))
            {
                return PasswordVerificationOutcome
                    .Failed;
            }

            try
            {
                var result =
                    _passwordHasher
                        .VerifyHashedPassword(
                            _userMarker,
                            hashedPassword,
                            providedPassword);

                return result switch
                {
                    PasswordVerificationResult
                        .Success =>
                        PasswordVerificationOutcome
                            .Success,

                    PasswordVerificationResult
                        .SuccessRehashNeeded =>
                        PasswordVerificationOutcome
                            .SuccessRehashNeeded,

                    _ =>
                        PasswordVerificationOutcome
                            .Failed
                };
            }
            catch (FormatException)
            {
                return PasswordVerificationOutcome
                    .Failed;
            }
            catch (ArgumentException)
            {
                return PasswordVerificationOutcome
                    .Failed;
            }
        }
    }
}