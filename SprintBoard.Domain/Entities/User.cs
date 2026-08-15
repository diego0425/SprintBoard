namespace SprintBoard.Domain.Entities;

/// <summary>
/// Represents a SprintBoard user account.
/// </summary>
public class User
{
    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the user's full name.
    /// </summary>
    public string FullName { get; private set; } = null!;

    /// <summary>
    /// Gets the normalized username.
    /// </summary>
    public string Username { get; private set; } = null!;

    /// <summary>
    /// Gets the normalized email address.
    /// </summary>
    public string Email { get; private set; } = null!;

    /// <summary>
    /// Gets the user's password hash.
    /// </summary>
    public string PasswordHash { get; private set; } = null!;

    /// <summary>
    /// Gets the optional profile image URL.
    /// </summary>
    public string? ProfileImageUrl { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the account was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the user's board memberships.
    /// </summary>
    public ICollection<BoardMember> BoardMemberships { get; private set; } = new List<BoardMember>();

    /// <summary>
    /// Initializes a user instance for Entity Framework Core.
    /// </summary>
    protected User()
    {
    }

    /// <summary>
    /// Initializes a new user account.
    /// </summary>
    /// <param name="fullName">
    /// The user's full name.
    /// </param>
    /// <param name="username">
    /// The username.
    /// </param>
    /// <param name="email">
    /// The email address.
    /// </param>
    /// <param name="passwordHash">
    /// The precomputed password hash.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when any required value is empty.
    /// </exception>
    public User(string fullName, string username, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        Id = Guid.NewGuid();
        FullName = fullName.Trim();
        Username = username.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's full name.
    /// </summary>
    /// <param name="fullName">
    /// The new full name.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the full name is empty.
    /// </exception>
    public void UpdateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        FullName = fullName.Trim();
    }

    /// <summary>
    /// Updates and normalizes the username.
    /// </summary>
    /// <param name="username">
    /// The new username.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the username is empty.
    /// </exception>
    public void UpdateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));

        Username = username.Trim();
    }

    /// <summary>
    /// Replaces the user's password hash.
    /// </summary>
    /// <param name="passwordHash">
    /// The new precomputed password hash.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the password hash is empty.
    /// </exception>
    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    /// <summary>
    /// Updates or removes the profile image URL.
    /// </summary>
    /// <param name="profileImageUrl">
    /// The new image URL, or an empty value to remove it.
    /// </param>
    public void UpdateProfileImage(string? profileImageUrl)
    {
        ProfileImageUrl = string.IsNullOrWhiteSpace(profileImageUrl)
            ? null
            : profileImageUrl.Trim();
    }
}
