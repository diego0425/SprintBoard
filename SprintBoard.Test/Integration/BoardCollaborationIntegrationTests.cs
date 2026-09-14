using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SprintBoard.Application.DTOs.Auth;
using SprintBoard.Application.DTOs.Board;
using SprintBoard.Domain.Enums;
using Xunit;

namespace SprintBoard.Test.Integration
{
    /// <summary>
    /// Contains integration tests for collaborative board flows
    /// involving multiple authenticated users.
    /// </summary>
    public sealed class BoardCollaborationIntegrationTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        /// <summary>
        /// Initializes the integration tests using the isolated
        /// SprintBoard application factory.
        /// </summary>
        public BoardCollaborationIntegrationTests(
            CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // ============================================================
        // MEMBERSHIP AUTHORIZATION
        // ============================================================

        /// <summary>
        /// Verifies that an authenticated user who is not a member
        /// cannot retrieve another user's board.
        /// </summary>
        [Fact]
        public async Task Outsider_ShouldReceiveForbidden_WhenAccessingAnotherUsersBoard()
        {
            // Arrange
            var owner =
                await RegisterUserAsync();

            var outsider =
                await RegisterUserAsync();

            using var ownerClient =
                owner.Client;

            using var outsiderClient =
                outsider.Client;

            var board =
                await CreateBoardAsync(
                    ownerClient);

            // Act
            var response =
                await outsiderClient.GetAsync(
                    $"/api/v1/boards/{board.Id}",
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);
        }

        // ============================================================
        // CREATE INVITATION
        // ============================================================

        /// <summary>
        /// Verifies that a board owner can create an invitation
        /// for another registered user through the real HTTP API.
        /// </summary>
        [Fact]
        public async Task Owner_ShouldCreateInvitation_ForRegisteredUser()
        {
            // Arrange
            var owner =
                await RegisterUserAsync();

            var member =
                await RegisterUserAsync();

            using var ownerClient =
                owner.Client;

            using var memberClient =
                member.Client;

            var board =
                await CreateBoardAsync(
                    ownerClient);

            // Act
            var invitation =
                await CreateInvitationAsync(
                    ownerClient,
                    board.Id,
                    member.Email);

            // Assert
            Assert.NotEqual(
                Guid.Empty,
                invitation.Id);

            Assert.Equal(
                board.Id,
                invitation.BoardId);

            Assert.Equal(
                member.Email,
                invitation.Email);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    invitation.Token));

            Assert.True(
                invitation.ExpiresAt >
                DateTime.UtcNow);
        }

        // ============================================================
        // ACCEPT INVITATION
        // ============================================================

        /// <summary>
        /// Verifies that an invited user can accept a board
        /// invitation through the real invitation endpoint.
        /// </summary>
        [Fact]
        public async Task InvitedUser_ShouldAcceptInvitation()
        {
            // Arrange
            var scenario =
                await CreateInvitationScenarioAsync();

            using var ownerClient =
                scenario.Owner.Client;

            using var memberClient =
                scenario.Member.Client;

            // Act
            var response =
                await memberClient.PostAsJsonAsync(
                    "/api/v1/invitations/accept",
                    new
                    {
                        Token =
                            scenario.Invitation.Token
                    },
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.NoContent,
                response.StatusCode);

            /*
             * The strongest confirmation is not merely HTTP 204.
             * The user must now actually have access to the board.
             */
            var boardResponse =
                await memberClient.GetAsync(
                    $"/api/v1/boards/{scenario.Board.Id}",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                boardResponse.StatusCode);
        }

        // ============================================================
        // MEMBER BOARD LIST
        // ============================================================

        /// <summary>
        /// Verifies that an accepted board membership causes the board
        /// to appear in the invited user's board collection.
        /// </summary>
        [Fact]
        public async Task AcceptedMember_ShouldSeeBoardInMyBoards()
        {
            // Arrange
            var scenario =
                await CreateAcceptedMemberScenarioAsync();

            using var ownerClient =
                scenario.Owner.Client;

            using var memberClient =
                scenario.Member.Client;

            // Act
            var response =
                await memberClient.GetAsync(
                    "/api/v1/boards",
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var boards =
                await response.Content
                    .ReadFromJsonAsync<
                        List<BoardResponse>>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(boards);

            Assert.Contains(
                boards!,
                board =>
                    board.Id ==
                    scenario.Board.Id);
        }

        // ============================================================
        // MEMBER LIST / ROLE
        // ============================================================

        /// <summary>
        /// Verifies that an accepted user appears in the board member
        /// list with the default Member role.
        /// </summary>
        [Fact]
        public async Task BoardMembers_ShouldContainAcceptedUserWithMemberRole()
        {
            // Arrange
            var scenario =
                await CreateAcceptedMemberScenarioAsync();

            using var ownerClient =
                scenario.Owner.Client;

            using var memberClient =
                scenario.Member.Client;

            // Act
            var response =
                await ownerClient.GetAsync(
                    $"/api/v1/boards/{scenario.Board.Id}/members",
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var members =
                await response.Content
                    .ReadFromJsonAsync<
                        List<BoardMemberResponse>>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(members);

            var acceptedMember =
                Assert.Single(
                    members!,
                    member =>
                        member.Username ==
                        scenario.Member.Username);

            Assert.Equal(
                BoardRole.Member,
                acceptedMember.Role);
        }

        // ============================================================
        // MEMBER PERMISSIONS
        // ============================================================

        /// <summary>
        /// Verifies that a regular board member cannot delete a board
        /// owned by another user.
        /// </summary>
        [Fact]
        public async Task Member_ShouldReceiveForbidden_WhenDeletingBoard()
        {
            // Arrange
            var scenario =
                await CreateAcceptedMemberScenarioAsync();

            using var ownerClient =
                scenario.Owner.Client;

            using var memberClient =
                scenario.Member.Client;

            // Act
            var response =
                await memberClient.DeleteAsync(
                    $"/api/v1/boards/{scenario.Board.Id}",
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);

            /*
             * Confirm that the forbidden operation did not
             * accidentally remove the board.
             */
            var ownerResponse =
                await ownerClient.GetAsync(
                    $"/api/v1/boards/{scenario.Board.Id}",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                ownerResponse.StatusCode);
        }

        // ============================================================
        // ROLE MANAGEMENT
        // ============================================================

        /// <summary>
        /// Verifies that the board owner can promote an accepted
        /// Member to the Admin role through the real HTTP API.
        /// </summary>
        [Fact]
        public async Task Owner_ShouldPromoteMemberToAdmin()
        {
            // Arrange
            var scenario =
                await CreateAcceptedMemberScenarioAsync();

            using var ownerClient =
                scenario.Owner.Client;

            using var memberClient =
                scenario.Member.Client;

            var membersBeforeResponse =
                await ownerClient.GetAsync(
                    $"/api/v1/boards/{scenario.Board.Id}/members",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                membersBeforeResponse.StatusCode);

            var membersBefore =
                await membersBeforeResponse.Content
                    .ReadFromJsonAsync<
                        List<BoardMemberResponse>>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(membersBefore);

            var memberBefore =
                Assert.Single(
                    membersBefore!,
                    member =>
                        member.Username ==
                        scenario.Member.Username);

            Assert.Equal(
                BoardRole.Member,
                memberBefore.Role);

            // Act
            var response =
                await ownerClient.PatchAsJsonAsync(
                    $"/api/v1/boards/{scenario.Board.Id}/members/role",
                    new ChangeBoardMemberRoleRequest
                    {
                        MemberUserId =
                            memberBefore.UserId,

                        NewRole =
                            (int)BoardRole.Admin
                    },
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.NoContent,
                response.StatusCode);

            var membersAfterResponse =
                await ownerClient.GetAsync(
                    $"/api/v1/boards/{scenario.Board.Id}/members",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                membersAfterResponse.StatusCode);

            var membersAfter =
                await membersAfterResponse.Content
                    .ReadFromJsonAsync<
                        List<BoardMemberResponse>>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(membersAfter);

            var promotedMember =
                Assert.Single(
                    membersAfter!,
                    member =>
                        member.UserId ==
                        memberBefore.UserId);

            Assert.Equal(
                BoardRole.Admin,
                promotedMember.Role);
        }

        // ============================================================
        // ADMIN INVITATIONS
        // ============================================================

        /// <summary>
        /// Verifies that an administrator can invite another user
        /// to a board through the real HTTP API.
        /// </summary>
        [Fact]
        public async Task Admin_ShouldCreateInvitation()
        {
            // Arrange
            var scenario =
                await CreateAcceptedMemberScenarioAsync();

            using var ownerClient =
                scenario.Owner.Client;

            using var adminClient =
                scenario.Member.Client;

            await PromoteMemberToAdminAsync(
                ownerClient,
                scenario.Board.Id,
                scenario.Member.Username);

            var invitedUser =
                await RegisterUserAsync();

            using var invitedUserClient =
                invitedUser.Client;

            // Act
            var invitation =
                await CreateInvitationAsync(
                    adminClient,
                    scenario.Board.Id,
                    invitedUser.Email);

            // Assert
            Assert.NotEqual(
                Guid.Empty,
                invitation.Id);

            Assert.Equal(
                scenario.Board.Id,
                invitation.BoardId);

            Assert.Equal(
                invitedUser.Email,
                invitation.Email);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    invitation.Token));
        }

        // ============================================================
        // ADMIN REMOVES MEMBER
        // ============================================================

        /// <summary>
        /// Verifies that an administrator can remove a regular Member
        /// and that the removed user immediately loses board access.
        /// </summary>
        [Fact]
        public async Task Admin_ShouldRemoveRegularMember()
        {
            // Arrange
            var owner =
                await RegisterUserAsync();

            using var ownerClient =
                owner.Client;

            var board =
                await CreateBoardAsync(
                    ownerClient);

            var admin =
                await AddMemberAsync(
                    ownerClient,
                    board.Id);

            var regularMember =
                await AddMemberAsync(
                    ownerClient,
                    board.Id);

            using var adminClient =
                admin.Client;

            using var memberClient =
                regularMember.Client;

            await PromoteMemberToAdminAsync(
                ownerClient,
                board.Id,
                admin.Username);

            var memberMembership =
                await GetBoardMemberAsync(
                    ownerClient,
                    board.Id,
                    regularMember.Username);

            // Act
            var response =
                await adminClient.DeleteAsync(
                    $"/api/v1/boards/{board.Id}/members/{memberMembership.UserId}",
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.NoContent,
                response.StatusCode);

            /*
             * Confirm that membership removal actually changed
             * authorization, not merely returned HTTP 204.
             */
            var boardResponse =
                await memberClient.GetAsync(
                    $"/api/v1/boards/{board.Id}",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.Forbidden,
                boardResponse.StatusCode);

            /*
             * The board itself must still exist for the owner.
             */
            var ownerResponse =
                await ownerClient.GetAsync(
                    $"/api/v1/boards/{board.Id}",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                ownerResponse.StatusCode);
        }

        // ============================================================
        // ADMIN CANNOT REMOVE ADMIN
        // ============================================================

        /// <summary>
        /// Verifies that an administrator cannot remove another
        /// administrator from the board.
        /// </summary>
        [Fact]
        public async Task Admin_ShouldReceiveForbidden_WhenRemovingAnotherAdmin()
        {
            // Arrange
            var owner =
                await RegisterUserAsync();

            using var ownerClient =
                owner.Client;

            var board =
                await CreateBoardAsync(
                    ownerClient);

            var firstAdmin =
                await AddMemberAsync(
                    ownerClient,
                    board.Id);

            var secondAdmin =
                await AddMemberAsync(
                    ownerClient,
                    board.Id);

            using var firstAdminClient =
                firstAdmin.Client;

            using var secondAdminClient =
                secondAdmin.Client;

            await PromoteMemberToAdminAsync(
                ownerClient,
                board.Id,
                firstAdmin.Username);

            await PromoteMemberToAdminAsync(
                ownerClient,
                board.Id,
                secondAdmin.Username);

            var secondAdminMembership =
                await GetBoardMemberAsync(
                    ownerClient,
                    board.Id,
                    secondAdmin.Username);

            // Act
            var response =
                await firstAdminClient.DeleteAsync(
                    $"/api/v1/boards/{board.Id}/members/{secondAdminMembership.UserId}",
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);

            /*
             * Confirm that the forbidden removal caused no
             * membership mutation.
             */
            var boardResponse =
                await secondAdminClient.GetAsync(
                    $"/api/v1/boards/{board.Id}",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                boardResponse.StatusCode);

            var memberAfter =
                await GetBoardMemberAsync(
                    ownerClient,
                    board.Id,
                    secondAdmin.Username);

            Assert.Equal(
                BoardRole.Admin,
                memberAfter.Role);
        }

        // ============================================================
        // MEMBER LEAVES BOARD
        // ============================================================

        /// <summary>
        /// Verifies that a regular member can leave a board and
        /// immediately loses access to that board.
        /// </summary>
        [Fact]
        public async Task Member_ShouldLeaveBoard()
        {
            // Arrange
            var scenario =
                await CreateAcceptedMemberScenarioAsync();

            using var ownerClient =
                scenario.Owner.Client;

            using var memberClient =
                scenario.Member.Client;

            // Act
            var response =
                await memberClient.DeleteAsync(
                    $"/api/v1/boards/{scenario.Board.Id}/members/me",
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.NoContent,
                response.StatusCode);

            var memberBoardResponse =
                await memberClient.GetAsync(
                    $"/api/v1/boards/{scenario.Board.Id}",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.Forbidden,
                memberBoardResponse.StatusCode);

            /*
             * Leaving the board must not affect the board itself
             * or the owner's access.
             */
            var ownerBoardResponse =
                await ownerClient.GetAsync(
                    $"/api/v1/boards/{scenario.Board.Id}",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                ownerBoardResponse.StatusCode);
        }

        // ============================================================
        // OWNER CANNOT LEAVE BOARD
        // ============================================================

        /// <summary>
        /// Verifies that the board owner cannot leave their own board
        /// and must delete the board instead.
        /// </summary>
        [Fact]
        public async Task Owner_ShouldReceiveConflict_WhenLeavingOwnBoard()
        {
            // Arrange
            var owner =
                await RegisterUserAsync();

            using var ownerClient =
                owner.Client;

            var board =
                await CreateBoardAsync(
                    ownerClient);

            // Act
            var response =
                await ownerClient.DeleteAsync(
                    $"/api/v1/boards/{board.Id}/members/me",
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.Conflict,
                response.StatusCode);

            var body =
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

            Assert.Contains(
                "The board owner cannot leave the board",
                body);

            /*
             * Confirm that the owner's membership was preserved.
             */
            var boardResponse =
                await ownerClient.GetAsync(
                    $"/api/v1/boards/{board.Id}",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                boardResponse.StatusCode);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Registers a unique user through the actual authentication
        /// endpoint and returns an authenticated HTTP client.
        /// </summary>
        private async Task<RegisteredUser>
            RegisterUserAsync()
        {
            var client =
                CreateClient();

            var suffix =
                Guid.NewGuid()
                    .ToString("N");

            var username =
                $"u{suffix[..12]}";

            var email =
                $"{suffix}@example.com";

            var request =
                new RegisterRequest
                {
                    FullName =
                        "Integration Test User",

                    Username =
                        username,

                    Email =
                        email,

                    Password =
                        "Password123!",

                    RepeatPassword =
                        "Password123!"
                };

            var response =
                await client.PostAsJsonAsync(
                    "/api/v1/auth/register",
                    request,
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var authResponse =
                await response.Content
                    .ReadFromJsonAsync<AuthResponse>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(authResponse);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    authResponse!.AccessToken));

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    authResponse.AccessToken);

            return new RegisteredUser(
                client,
                email,
                username);
        }

        /// <summary>
        /// Creates a board through the authenticated HTTP API.
        /// </summary>
        private static async Task<BoardResponse>
            CreateBoardAsync(
                HttpClient client)
        {
            var request =
                new CreateBoardRequest
                {
                    Name =
                        $"Board-{Guid.NewGuid():N}"
                };

            var response =
                await client.PostAsJsonAsync(
                    "/api/v1/boards",
                    request,
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);

            var board =
                await response.Content
                    .ReadFromJsonAsync<BoardResponse>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(board);

            return board!;
        }

        /// <summary>
        /// Creates a board invitation through the owner's authenticated
        /// HTTP client.
        /// </summary>
        private static async Task<BoardInvitationResponse>
            CreateInvitationAsync(
                HttpClient ownerClient,
                Guid boardId,
                string email)
        {
            var response =
                await ownerClient.PostAsJsonAsync(
                    $"/api/v1/boards/{boardId}/invitations",
                    new CreateBoardInvitationRequest
                    {
                        Email =
                            email
                    },
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var invitation =
                await response.Content
                    .ReadFromJsonAsync<
                        BoardInvitationResponse>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(invitation);

            return invitation!;
        }

        /// <summary>
        /// Creates two users, a board owned by the first user,
        /// and a pending invitation for the second user.
        /// </summary>
        private async Task<InvitationScenario>
            CreateInvitationScenarioAsync()
        {
            var owner =
                await RegisterUserAsync();

            var member =
                await RegisterUserAsync();

            var board =
                await CreateBoardAsync(
                    owner.Client);

            var invitation =
                await CreateInvitationAsync(
                    owner.Client,
                    board.Id,
                    member.Email);

            return new InvitationScenario(
                owner,
                member,
                board,
                invitation);
        }

        /// <summary>
        /// Creates a complete collaboration scenario in which the
        /// invited user has already accepted the board invitation.
        /// </summary>
        private async Task<AcceptedMemberScenario>
            CreateAcceptedMemberScenarioAsync()
        {
            var scenario =
                await CreateInvitationScenarioAsync();

            var response =
                await scenario.Member.Client
                    .PostAsJsonAsync(
                        "/api/v1/invitations/accept",
                        new
                        {
                            Token =
                                scenario.Invitation.Token
                        },
                        TestContext.Current
                            .CancellationToken);

            Assert.Equal(
                HttpStatusCode.NoContent,
                response.StatusCode);

            return new AcceptedMemberScenario(
                scenario.Owner,
                scenario.Member,
                scenario.Board);
        }

        /// <summary>
        /// Registers a new user, invites them to the specified board,
        /// accepts the invitation, and returns the authenticated user.
        /// </summary>
        private async Task<RegisteredUser>
            AddMemberAsync(
                HttpClient ownerClient,
                Guid boardId)
        {
            var member =
                await RegisterUserAsync();

            var invitation =
                await CreateInvitationAsync(
                    ownerClient,
                    boardId,
                    member.Email);

            var response =
                await member.Client.PostAsJsonAsync(
                    "/api/v1/invitations/accept",
                    new
                    {
                        Token =
                            invitation.Token
                    },
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.NoContent,
                response.StatusCode);

            return member;
        }

        /// <summary>
        /// Retrieves a specific board member by username through
        /// the real members endpoint.
        /// </summary>
        private static async Task<BoardMemberResponse>
            GetBoardMemberAsync(
                HttpClient client,
                Guid boardId,
                string username)
        {
            var response =
                await client.GetAsync(
                    $"/api/v1/boards/{boardId}/members",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var members =
                await response.Content
                    .ReadFromJsonAsync<
                        List<BoardMemberResponse>>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(members);

            return Assert.Single(
                members!,
                member =>
                    member.Username ==
                    username);
        }

        /// <summary>
        /// Promotes the specified board member to the Admin role
        /// through the real role-management endpoint.
        /// </summary>
        private static async Task
            PromoteMemberToAdminAsync(
                HttpClient ownerClient,
                Guid boardId,
                string username)
        {
            var member =
                await GetBoardMemberAsync(
                    ownerClient,
                    boardId,
                    username);

            var response =
                await ownerClient.PatchAsJsonAsync(
                    $"/api/v1/boards/{boardId}/members/role",
                    new ChangeBoardMemberRoleRequest
                    {
                        MemberUserId =
                            member.UserId,

                        NewRole =
                            (int)BoardRole.Admin
                    },
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.NoContent,
                response.StatusCode);
        }

        /// <summary>
        /// Creates an HTTP client connected to the isolated
        /// SprintBoard test application.
        /// </summary>
        private HttpClient CreateClient()
        {
            return _factory.CreateClient();
        }

        /// <summary>
        /// Represents an authenticated user participating in an
        /// integration-test collaboration scenario.
        /// </summary>
        private sealed record RegisteredUser(
            HttpClient Client,
            string Email,
            string Username);

        /// <summary>
        /// Represents a board collaboration scenario containing
        /// a pending invitation.
        /// </summary>
        private sealed record InvitationScenario(
            RegisteredUser Owner,
            RegisteredUser Member,
            BoardResponse Board,
            BoardInvitationResponse Invitation);

        /// <summary>
        /// Represents a collaboration scenario after the invited
        /// user has successfully joined the board.
        /// </summary>
        private sealed record AcceptedMemberScenario(
            RegisteredUser Owner,
            RegisteredUser Member,
            BoardResponse Board);
    }
}