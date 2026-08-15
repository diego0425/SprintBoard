namespace SprintBoard.Application.Exceptions
{
    /// <summary>
    /// Represents an authorization failure in which an authenticated user lacks permission to perform an operation.
    /// </summary>
    public class ForbiddenAccessException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ForbiddenAccessException"/> class.
        /// </summary>
        /// <param name="message">
        /// A message describing why access to the requested operation was denied.
        /// </param>
        public ForbiddenAccessException(string message) : base(message)
        {
        }
    }
}
