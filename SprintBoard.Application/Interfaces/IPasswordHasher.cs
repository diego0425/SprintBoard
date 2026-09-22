namespace SprintBoard.Application.Interfaces
{
    /// <summary>
    /// Defines password hashing and verification operations
    /// required by the authentication workflow.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Creates a secure hash for a plain-text password.
        /// </summary>
        /// <param name="password">
        /// Plain-text password to hash.
        /// </param>
        /// <returns>
        /// Secure password hash suitable for persistence.
        /// </returns>
        string Hash(string password);

        /// <summary>
        /// Verifies a plain-text password against a stored hash.
        /// </summary>
        /// <param name="hashedPassword">
        /// Previously stored password hash.
        /// </param>
        /// <param name="providedPassword">
        /// Plain-text password supplied during authentication.
        /// </param>
        /// <returns>
        /// The result of the password verification operation.
        /// </returns>
        PasswordVerificationOutcome Verify(
            string hashedPassword,
            string providedPassword);
    }

    /// <summary>
    /// Represents the result of a password verification operation.
    /// </summary>
    public enum PasswordVerificationOutcome
    {
        /// <summary>
        /// The provided password does not match the stored hash.
        /// </summary>
        Failed,

        /// <summary>
        /// The provided password matches the stored hash.
        /// </summary>
        Success,

        /// <summary>
        /// The password matches, but the stored hash should
        /// be regenerated using the current hashing configuration.
        /// </summary>
        SuccessRehashNeeded
    }
}