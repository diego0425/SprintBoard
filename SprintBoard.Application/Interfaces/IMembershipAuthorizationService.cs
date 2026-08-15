namespace SprintBoard.Application.Interfaces
{
    /// <summary>
    /// Defines authorization checks for board, card, and checklist task access.
    /// </summary>
    public interface IMembershipAuthorizationService
    {
        /// <summary>
        /// Ensures that a user belongs to a board.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="userId">
        /// The user identifier whose membership is being verified.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the board.
        /// </exception>
        Task EnsureBoardMemberAsync(Guid boardId, Guid userId);
        /// <summary>
        /// Ensures that a user owns a board.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="userId">
        /// The user identifier whose ownership is being verified.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not the board owner.
        /// </exception>
        Task EnsureBoardOwnerAsync(Guid boardId, Guid userId);
        /// <summary>
        /// Ensures that a user has owner or administrator privileges on a board.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="userId">
        /// The user identifier whose role is being verified.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is neither the owner nor an administrator.
        /// </exception>
        Task EnsureBoardOwnerOrAdminAsync(Guid boardId, Guid userId);

        /// <summary>
        /// Ensures that a user belongs to the board that contains a card.
        /// </summary>
        /// <param name="cardId">
        /// The card identifier used to resolve the parent board.
        /// </param>
        /// <param name="userId">
        /// The user identifier whose membership is being verified.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the card does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the parent board.
        /// </exception>
        Task EnsureCardBoardMemberAsync(Guid cardId, Guid userId);
        /// <summary>
        /// Ensures that a user belongs to the board that contains a checklist task.
        /// </summary>
        /// <param name="taskId">
        /// The checklist task identifier used to resolve the parent card and board.
        /// </param>
        /// <param name="userId">
        /// The user identifier whose membership is being verified.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the checklist task or parent card does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the parent board.
        /// </exception>
        Task EnsureCardTaskBoardMemberAsync(Guid taskId, Guid userId);
    }
}
