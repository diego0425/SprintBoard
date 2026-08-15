using Microsoft.Extensions.Options;
using SprintBoard.Application.Common;
using SprintBoard.Application.Interfaces;

namespace SprintBoard.api.Services;

/// <summary>
/// Builds frontend URLs used to accept or decline board invitations.
/// </summary>
public sealed class InvitationLinkBuilder : IInvitationLinkBuilder
{
    private readonly EmailOptions _emailOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvitationLinkBuilder"/> class.
    /// </summary>
    /// <param name="emailOptions">
    /// Options accessor that provides the frontend base URL used to compose invitation links.
    /// </param>
    public InvitationLinkBuilder(IOptions<EmailOptions> emailOptions)
    {
        _emailOptions = emailOptions.Value;
    }

    /// <summary>
    /// Builds the frontend URL used to accept a board invitation.
    /// </summary>
    /// <param name="token">
    /// Invitation token that uniquely identifies the invitation to be accepted.
    /// </param>
    /// <returns>
    /// Fully qualified frontend URL that routes the invited user to the accept invitation flow.
    /// </returns>
    public string BuildAcceptInvitationLink(string token)
    {
        var frontendBaseUrl = _emailOptions.FrontendBaseUrl.TrimEnd('/');

        return $"{frontendBaseUrl}/invitations?action=accept&token={token}";
    }

    /// <summary>
    /// Builds the frontend URL used to decline a board invitation.
    /// </summary>
    /// <param name="token">
    /// Invitation token that uniquely identifies the invitation to be declined.
    /// </param>
    /// <returns>
    /// Fully qualified frontend URL that routes the invited user to the decline invitation flow.
    /// </returns>
    public string BuildDeclineInvitationLink(string token)
    {
        var frontendBaseUrl = _emailOptions.FrontendBaseUrl.TrimEnd('/');

        return $"{frontendBaseUrl}/invitations?action=decline&token={token}";
    }
}
