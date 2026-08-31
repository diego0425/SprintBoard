using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SprintBoard.api.Errors;
using SprintBoard.api.Middlewares;
using SprintBoard.Application.Exceptions;
using Xunit;

namespace SprintBoard.Test.Middlewares
{
    /// <summary>
    /// Contains tests for the <see cref="GlobalExceptionMiddleware"/>.
    /// </summary>
    public class GlobalExceptionMiddlewareTests
    {
        private readonly Mock<ILogger<GlobalExceptionMiddleware>>
            _loggerMock;

        /// <summary>
        /// Initializes the mocked dependencies used by the
        /// global exception middleware tests.
        /// </summary>
        public GlobalExceptionMiddlewareTests()
        {
            _loggerMock =
                new Mock<ILogger<GlobalExceptionMiddleware>>();
        }

        // ============================================================
        // SUCCESSFUL PIPELINE
        // ============================================================

        /// <summary>
        /// Verifies that the middleware continues the HTTP pipeline
        /// without modifying the response when no exception occurs.
        /// </summary>
        [Fact]
        public async Task Invoke_ShouldCallNext_WhenNoExceptionOccurs()
        {
            // Arrange
            var nextCalled = false;

            RequestDelegate next =
                context =>
                {
                    nextCalled = true;

                    context.Response.StatusCode =
                        StatusCodes.Status204NoContent;

                    return Task.CompletedTask;
                };

            var middleware =
                new GlobalExceptionMiddleware(
                    next,
                    _loggerMock.Object);

            var context =
                CreateHttpContext();

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.True(nextCalled);

            Assert.Equal(
                StatusCodes.Status204NoContent,
                context.Response.StatusCode);

            Assert.Equal(
                0,
                context.Response.Body.Length);
        }

        // ============================================================
        // ARGUMENT EXCEPTION
        // ============================================================

        /// <summary>
        /// Verifies that an ArgumentException is translated
        /// into an HTTP 400 Bad Request response.
        /// </summary>
        [Fact]
        public async Task Invoke_ShouldReturnBadRequest_WhenArgumentExceptionIsThrown()
        {
            // Arrange
            const string message =
                "Invalid request data.";

            var exception =
                new ArgumentException(message);

            var (middleware, context) =
                CreateMiddleware(exception);

            // Act
            await middleware.Invoke(context);

            // Assert
            var response =
                await ReadErrorResponseAsync(context);

            Assert.Equal(
                StatusCodes.Status400BadRequest,
                context.Response.StatusCode);

            Assert.Equal(
                StatusCodes.Status400BadRequest,
                response.StatusCode);

            Assert.Equal(
                message,
                response.Message);
        }

        // ============================================================
        // KEY NOT FOUND
        // ============================================================

        /// <summary>
        /// Verifies that a KeyNotFoundException is translated
        /// into an HTTP 404 Not Found response.
        /// </summary>
        [Fact]
        public async Task Invoke_ShouldReturnNotFound_WhenKeyNotFoundExceptionIsThrown()
        {
            // Arrange
            const string message =
                "Resource not found.";

            var exception =
                new KeyNotFoundException(message);

            var (middleware, context) =
                CreateMiddleware(exception);

            // Act
            await middleware.Invoke(context);

            // Assert
            var response =
                await ReadErrorResponseAsync(context);

            Assert.Equal(
                StatusCodes.Status404NotFound,
                context.Response.StatusCode);

            Assert.Equal(
                StatusCodes.Status404NotFound,
                response.StatusCode);

            Assert.Equal(
                message,
                response.Message);
        }

        // ============================================================
        // INVALID OPERATION
        // ============================================================

        /// <summary>
        /// Verifies that an InvalidOperationException is translated
        /// into an HTTP 409 Conflict response.
        /// </summary>
        [Fact]
        public async Task Invoke_ShouldReturnConflict_WhenInvalidOperationExceptionIsThrown()
        {
            // Arrange
            const string message =
                "Operation cannot be completed.";

            var exception =
                new InvalidOperationException(message);

            var (middleware, context) =
                CreateMiddleware(exception);

            // Act
            await middleware.Invoke(context);

            // Assert
            var response =
                await ReadErrorResponseAsync(context);

            Assert.Equal(
                StatusCodes.Status409Conflict,
                context.Response.StatusCode);

            Assert.Equal(
                StatusCodes.Status409Conflict,
                response.StatusCode);

            Assert.Equal(
                message,
                response.Message);
        }

        // ============================================================
        // UNAUTHORIZED
        // ============================================================

        /// <summary>
        /// Verifies that an UnauthorizedAccessException is translated
        /// into an HTTP 401 Unauthorized response.
        /// </summary>
        [Fact]
        public async Task Invoke_ShouldReturnUnauthorized_WhenUnauthorizedAccessExceptionIsThrown()
        {
            // Arrange
            const string message =
                "User is not authenticated.";

            var exception =
                new UnauthorizedAccessException(message);

            var (middleware, context) =
                CreateMiddleware(exception);

            // Act
            await middleware.Invoke(context);

            // Assert
            var response =
                await ReadErrorResponseAsync(context);

            Assert.Equal(
                StatusCodes.Status401Unauthorized,
                context.Response.StatusCode);

            Assert.Equal(
                StatusCodes.Status401Unauthorized,
                response.StatusCode);

            Assert.Equal(
                message,
                response.Message);
        }

        // ============================================================
        // FORBIDDEN
        // ============================================================

        /// <summary>
        /// Verifies that a ForbiddenAccessException is translated
        /// into an HTTP 403 Forbidden response.
        /// </summary>
        [Fact]
        public async Task Invoke_ShouldReturnForbidden_WhenForbiddenAccessExceptionIsThrown()
        {
            // Arrange
            const string message =
                "Access denied.";

            var exception =
                new ForbiddenAccessException(message);

            var (middleware, context) =
                CreateMiddleware(exception);

            // Act
            await middleware.Invoke(context);

            // Assert
            var response =
                await ReadErrorResponseAsync(context);

            Assert.Equal(
                StatusCodes.Status403Forbidden,
                context.Response.StatusCode);

            Assert.Equal(
                StatusCodes.Status403Forbidden,
                response.StatusCode);

            Assert.Equal(
                message,
                response.Message);
        }

        // ============================================================
        // UNEXPECTED EXCEPTION
        // ============================================================

        /// <summary>
        /// Verifies that an unexpected exception is translated into
        /// HTTP 500 without exposing internal exception information.
        /// </summary>
        [Fact]
        public async Task Invoke_ShouldReturnInternalServerError_WhenUnexpectedExceptionIsThrown()
        {
            // Arrange
            const string internalMessage =
                "Sensitive database failure.";

            var exception =
                new Exception(internalMessage);

            var (middleware, context) =
                CreateMiddleware(exception);

            // Act
            await middleware.Invoke(context);

            // Assert
            var response =
                await ReadErrorResponseAsync(context);

            Assert.Equal(
                StatusCodes.Status500InternalServerError,
                context.Response.StatusCode);

            Assert.Equal(
                StatusCodes.Status500InternalServerError,
                response.StatusCode);

            Assert.Equal(
                "An unexpected error occurred.",
                response.Message);

            Assert.DoesNotContain(
                internalMessage,
                response.Message);
        }

        // ============================================================
        // CONTENT TYPE
        // ============================================================

        /// <summary>
        /// Verifies that error responses produced by the middleware
        /// use the JSON content type.
        /// </summary>
        [Fact]
        public async Task Invoke_ShouldReturnJsonContentType_WhenExceptionOccurs()
        {
            // Arrange
            var exception =
                new ArgumentException(
                    "Invalid request.");

            var (middleware, context) =
                CreateMiddleware(exception);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(
                "application/json",
                context.Response.ContentType);
        }

        // ============================================================
        // TRACE IDENTIFIER
        // ============================================================

        /// <summary>
        /// Verifies that the current HTTP trace identifier is included
        /// in the standardized API error response.
        /// </summary>
        [Fact]
        public async Task Invoke_ShouldIncludeTraceIdentifierInErrorResponse()
        {
            // Arrange
            const string traceId =
                "sprintboard-test-trace-id";

            var exception =
                new InvalidOperationException(
                    "Conflict.");

            var (middleware, context) =
                CreateMiddleware(
                    exception,
                    traceId);

            // Act
            await middleware.Invoke(context);

            // Assert
            var response =
                await ReadErrorResponseAsync(context);

            Assert.Equal(
                traceId,
                context.TraceIdentifier);

            Assert.Equal(
                traceId,
                response.TraceId);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Creates a middleware instance whose next request delegate
        /// throws the specified exception.
        /// </summary>
        private (
            GlobalExceptionMiddleware Middleware,
            DefaultHttpContext Context)
            CreateMiddleware(
                Exception exception,
                string traceId = "test-trace-id")
        {
            RequestDelegate next =
                _ => Task.FromException(exception);

            var middleware =
                new GlobalExceptionMiddleware(
                    next,
                    _loggerMock.Object);

            var context =
                CreateHttpContext(traceId);

            return (
                middleware,
                context);
        }

        /// <summary>
        /// Creates an HTTP context with an in-memory response body
        /// and a predictable trace identifier.
        /// </summary>
        private static DefaultHttpContext CreateHttpContext(
            string traceId = "test-trace-id")
        {
            var context =
                new DefaultHttpContext
                {
                    TraceIdentifier = traceId
                };

            context.Response.Body =
                new MemoryStream();

            return context;
        }

        /// <summary>
        /// Reads and deserializes the standardized API error response
        /// written to the HTTP response body.
        /// </summary>
        private static async Task<ApiErrorResponse>
            ReadErrorResponseAsync(
                HttpContext context)
        {
            context.Response.Body.Position = 0;

            using var reader =
                new StreamReader(
                    context.Response.Body,
                    leaveOpen: true);

            var json =
                await reader.ReadToEndAsync();

            var response =
                JsonSerializer.Deserialize<ApiErrorResponse>(
                    json);

            Assert.NotNull(response);

            return response!;
        }
    }
}