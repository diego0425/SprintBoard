namespace SprintBoard.Application.Common
{
    /// <summary>
    /// Represents the SMTP and frontend URL settings required by the application email workflow.
    /// </summary>
    public sealed class EmailOptions
    {
        /// <summary>
        /// Gets or initializes the SMTP server host name.
        /// </summary>
        public string SmtpHost { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the SMTP server port.
        /// </summary>
        public int SmtpPort { get; init; }
        /// <summary>
        /// Gets or initializes the username used to authenticate with the SMTP server.
        /// </summary>
        public string SmtpUsername { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the password used to authenticate with the SMTP server.
        /// </summary>
        public string SmtpPassword { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the sender email address used for outgoing messages.
        /// </summary>
        public string From { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the frontend base URL used when building invitation links.
        /// </summary>
        public string FrontendBaseUrl { get; init; } = string.Empty;
    }
}
