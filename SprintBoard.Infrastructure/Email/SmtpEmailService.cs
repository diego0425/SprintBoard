using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SprintBoard.Application.Common;
using SprintBoard.Application.Interfaces;

namespace SprintBoard.Infrastructure.Email
{
    /// <summary>
    /// Sends application emails through an SMTP server.
    /// </summary>
    public sealed class SmtpEmailService : IEmailService
    {
        private readonly EmailOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmtpEmailService"/> class.
        /// </summary>
        /// <param name="options">
        /// Options provider containing the SMTP credentials, server settings, sender address, and related email configuration.
        /// </param>
        public SmtpEmailService(IOptions<EmailOptions> options)
        {
            _options = options.Value;
        }

        /// <summary>
        /// Sends a board invitation email containing accept and decline actions through the configured SMTP server.
        /// </summary>
        /// <param name="toEmail">
        /// The recipient email address.
        /// </param>
        /// <param name="boardName">
        /// The board name displayed in the message.
        /// </param>
        /// <param name="acceptInvitationLink">
        /// The URL the recipient can use to accept the invitation.
        /// </param>
        /// <param name="declineInvitationLink">
        /// The URL the recipient can use to decline the invitation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the recipient email address is empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when required SMTP configuration is missing or invalid.
        /// </exception>
        public async Task SendBoardInvitationAsync(
            string toEmail,
            string boardName,
            string acceptInvitationLink,
            string declineInvitationLink)
        {
            ValidateConfiguration(toEmail);

            var encodedBoardName = WebUtility.HtmlEncode(boardName);
            var encodedAcceptLink = WebUtility.HtmlEncode(acceptInvitationLink);
            var encodedDeclineLink = WebUtility.HtmlEncode(declineInvitationLink);
            var emailSubject = $"You're invited to join '{boardName}' on SprintBoard";
            var emailBody = BuildInvitationEmailBody(
                encodedBoardName,
                encodedAcceptLink,
                encodedDeclineLink);

            using var emailMessage = new MailMessage
            {
                From = new MailAddress(_options.From),
                Subject = emailSubject,
                Body = emailBody,
                IsBodyHtml = true
            };

            emailMessage.To.Add(toEmail);

            using var smtpClient = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                Credentials = new NetworkCredential(
                    _options.SmtpUsername,
                    _options.SmtpPassword),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(emailMessage);
        }

        /// <summary>
        /// Validates the recipient and required SMTP settings before a message is sent.
        /// </summary>
        /// <param name="recipientEmail">
        /// The email address that will receive the message.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when the recipient email address is empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the sender address, SMTP host, or SMTP port is not configured correctly.
        /// </exception>
        private void ValidateConfiguration(string recipientEmail)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
                throw new ArgumentException("Recipient email cannot be empty.", nameof(recipientEmail));

            if (string.IsNullOrWhiteSpace(_options.From))
                throw new InvalidOperationException("The sender email address is not configured.");

            if (string.IsNullOrWhiteSpace(_options.SmtpHost))
                throw new InvalidOperationException("The SMTP host is not configured.");

            if (_options.SmtpPort <= 0)
                throw new InvalidOperationException("The SMTP port is invalid.");
        }

        /// <summary>
        /// Builds the HTML body used for a board invitation email.
        /// </summary>
        /// <param name="boardName">
        /// The HTML-encoded board name.
        /// </param>
        /// <param name="acceptInvitationLink">
        /// The HTML-encoded acceptance URL.
        /// </param>
        /// <param name="declineInvitationLink">
        /// The HTML-encoded decline URL.
        /// </param>
        /// <returns>
        /// The complete HTML email body.
        /// </returns>
        private static string BuildInvitationEmailBody(
            string boardName,
            string acceptInvitationLink,
            string declineInvitationLink)
            => $"""
            <html>
              <body style="margin:0;padding:0;background-color:#f5f7fb;font-family:Arial,Helvetica,sans-serif;color:#1f2937;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f5f7fb;padding:32px 16px;">
                  <tr>
                    <td align="center">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 10px 30px rgba(15,23,42,0.08);">
                        <tr>
                          <td style="padding:24px 32px;background:#111827;color:#ffffff;">
                            <h1 style="margin:0;font-size:24px;line-height:1.2;">SprintBoard</h1>
                            <p style="margin:8px 0 0;font-size:14px;color:#d1d5db;">Board invitation</p>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:32px;">
                            <h2 style="margin:0 0 16px;font-size:22px;color:#111827;">
                              You’ve been invited to join <span style="color:#2563eb;">{boardName}</span>
                            </h2>
                            <p style="margin:0 0 16px;font-size:15px;line-height:1.6;color:#4b5563;">
                              Someone invited you to collaborate on this board in SprintBoard.
                              Use one of the actions below to respond to the invitation.
                            </p>
                            <table role="presentation" cellspacing="0" cellpadding="0" style="margin:24px 0;">
                              <tr>
                                <td style="padding-right:12px;">
                                  <a href="{acceptInvitationLink}" style="display:inline-block;padding:12px 20px;background:#16a34a;color:#ffffff;text-decoration:none;border-radius:10px;font-weight:600;font-size:14px;">
                                    Accept invitation
                                  </a>
                                </td>
                                <td>
                                  <a href="{declineInvitationLink}" style="display:inline-block;padding:12px 20px;background:#dc2626;color:#ffffff;text-decoration:none;border-radius:10px;font-weight:600;font-size:14px;">
                                    Decline invitation
                                  </a>
                                </td>
                              </tr>
                            </table>
                            <p style="margin:0 0 8px;font-size:14px;color:#6b7280;">
                              If the buttons do not work, copy and paste one of these links into your browser:
                            </p>
                            <p style="margin:0 0 8px;font-size:13px;line-height:1.6;word-break:break-all;">
                              <strong>Accept:</strong><br />
                              <a href="{acceptInvitationLink}" style="color:#2563eb;">{acceptInvitationLink}</a>
                            </p>
                            <p style="margin:0 0 24px;font-size:13px;line-height:1.6;word-break:break-all;">
                              <strong>Decline:</strong><br />
                              <a href="{declineInvitationLink}" style="color:#2563eb;">{declineInvitationLink}</a>
                            </p>
                            <p style="margin:0;font-size:13px;line-height:1.6;color:#9ca3af;">
                              If you were not expecting this invitation, you can safely ignore this email.
                            </p>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:20px 32px;background:#f9fafb;border-top:1px solid #e5e7eb;">
                            <p style="margin:0;font-size:12px;color:#9ca3af;">SprintBoard • Invitation email</p>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>
            """;
    }
}
