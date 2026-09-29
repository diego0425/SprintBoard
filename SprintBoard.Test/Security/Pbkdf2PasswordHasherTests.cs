using SprintBoard.Application.Interfaces;
using SprintBoard.Infrastructure.Security;
using Xunit;

namespace SprintBoard.Test.Security
{
    /// <summary>
    /// Contains security-focused tests for the
    /// <see cref="Pbkdf2PasswordHasher"/>.
    /// </summary>
    public sealed class Pbkdf2PasswordHasherTests
    {
        private readonly Pbkdf2PasswordHasher
            _passwordHasher =
                new();

        /// <summary>
        /// Verifies that an empty password cannot be hashed.
        /// </summary>
        [Fact]
        public void Hash_ShouldThrowArgumentException_WhenPasswordIsEmpty()
        {
            // Arrange
            const string password =
                "";

            // Act
            var exception =
                Assert.Throws<ArgumentException>(
                    () =>
                        _passwordHasher.Hash(
                            password));

            // Assert
            Assert.Equal(
                "Password cannot be empty. (Parameter 'password')",
                exception.Message);
        }

        /// <summary>
        /// Verifies that a valid password produces a non-empty
        /// hash that does not expose the original password.
        /// </summary>
        [Fact]
        public void Hash_ShouldCreateSecureHash_WhenPasswordIsValid()
        {
            // Arrange
            const string password =
                "Password123";

            // Act
            var hash =
                _passwordHasher.Hash(
                    password);

            // Assert
            Assert.False(
                string.IsNullOrWhiteSpace(
                    hash));

            Assert.NotEqual(
                password,
                hash);

            Assert.DoesNotContain(
                password,
                hash);
        }

        /// <summary>
        /// Verifies that hashing the same password multiple times
        /// produces different hashes because a random salt is used.
        /// </summary>
        [Fact]
        public void Hash_ShouldCreateDifferentHashes_WhenPasswordIsRepeated()
        {
            // Arrange
            const string password =
                "Password123";

            // Act
            var firstHash =
                _passwordHasher.Hash(
                    password);

            var secondHash =
                _passwordHasher.Hash(
                    password);

            // Assert
            Assert.NotEqual(
                firstHash,
                secondHash);
        }

        /// <summary>
        /// Verifies that a password is successfully verified
        /// against a hash created from the same password.
        /// </summary>
        [Fact]
        public void Verify_ShouldReturnSuccess_WhenPasswordMatches()
        {
            // Arrange
            const string password =
                "Password123";

            var hash =
                _passwordHasher.Hash(
                    password);

            // Act
            var result =
                _passwordHasher.Verify(
                    hash,
                    password);

            // Assert
            Assert.True(
                result is
                    PasswordVerificationOutcome.Success or
                    PasswordVerificationOutcome.SuccessRehashNeeded);
        }

        /// <summary>
        /// Verifies that verification fails when the supplied
        /// password does not match the stored password hash.
        /// </summary>
        [Fact]
        public void Verify_ShouldReturnFailed_WhenPasswordIsIncorrect()
        {
            // Arrange
            const string correctPassword =
                "Password123";

            const string wrongPassword =
                "WrongPassword123";

            var hash =
                _passwordHasher.Hash(
                    correctPassword);

            // Act
            var result =
                _passwordHasher.Verify(
                    hash,
                    wrongPassword);

            // Assert
            Assert.Equal(
                PasswordVerificationOutcome.Failed,
                result);
        }

        /// <summary>
        /// Verifies that malformed password hashes are rejected
        /// instead of propagating hashing implementation exceptions.
        /// </summary>
        [Fact]
        public void Verify_ShouldReturnFailed_WhenHashIsMalformed()
        {
            // Arrange
            const string malformedHash =
                "this-is-not-a-valid-password-hash";

            const string password =
                "Password123";

            // Act
            var result =
                _passwordHasher.Verify(
                    malformedHash,
                    password);

            // Assert
            Assert.Equal(
                PasswordVerificationOutcome.Failed,
                result);
        }

        /// <summary>
        /// Verifies that verification immediately fails when
        /// the stored password hash is empty.
        /// </summary>
        [Fact]
        public void Verify_ShouldReturnFailed_WhenHashIsEmpty()
        {
            // Arrange
            const string hash =
                "";

            const string password =
                "Password123";

            // Act
            var result =
                _passwordHasher.Verify(
                    hash,
                    password);

            // Assert
            Assert.Equal(
                PasswordVerificationOutcome.Failed,
                result);
        }

        /// <summary>
        /// Verifies that verification immediately fails when
        /// the supplied password is empty.
        /// </summary>
        [Fact]
        public void Verify_ShouldReturnFailed_WhenPasswordIsEmpty()
        {
            // Arrange
            var hash =
                _passwordHasher.Hash(
                    "Password123");

            const string password =
                "";

            // Act
            var result =
                _passwordHasher.Verify(
                    hash,
                    password);

            // Assert
            Assert.Equal(
                PasswordVerificationOutcome.Failed,
                result);
        }

        /// <summary>
        /// Verifies that two independently generated hashes for the
        /// same password can both successfully verify that password.
        /// </summary>
        [Fact]
        public void Verify_ShouldValidateBothSaltedHashes_WhenPasswordIsSame()
        {
            // Arrange
            const string password =
                "Password123";

            var firstHash =
                _passwordHasher.Hash(
                    password);

            var secondHash =
                _passwordHasher.Hash(
                    password);

            // Act
            var firstResult =
                _passwordHasher.Verify(
                    firstHash,
                    password);

            var secondResult =
                _passwordHasher.Verify(
                    secondHash,
                    password);

            // Assert
            Assert.NotEqual(
                firstHash,
                secondHash);

            Assert.NotEqual(
                PasswordVerificationOutcome.Failed,
                firstResult);

            Assert.NotEqual(
                PasswordVerificationOutcome.Failed,
                secondResult);
        }
    }
}