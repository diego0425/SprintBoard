using Microsoft.AspNetCore.Http;
using SprintBoard.api.Middlewares;
using Xunit;

namespace SprintBoard.Test.Middlewares
{
    /// <summary>
    /// Contains tests for the
    /// <see cref="CorrelationIdMiddleware"/>.
    /// </summary>
    public sealed class CorrelationIdMiddlewareTests
    {
        /// <summary>
        /// Verifies that a new correlation identifier is generated
        /// when the incoming request does not provide one.
        /// </summary>
        [Fact]
        public async Task Invoke_ShouldGenerateCorrelationId_WhenHeaderIsMissing()
        {
            // Arrange
            var nextCalled = false;

            RequestDelegate next =
                context =>
                {
                    nextCalled = true;

                    Assert.False(
                        string.IsNullOrWhiteSpace(
                            context.TraceIdentifier));

                    return Task.CompletedTask;
                };

            var middleware =
                new CorrelationIdMiddleware(
                    next);

            var context =
                new DefaultHttpContext();

            // Act
            await middleware.Invoke(
                context);

            // Assert
            Assert.True(nextCalled);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    context.TraceIdentifier));

            Assert.Equal(
                context.TraceIdentifier,
                context.Response.Headers[
                    CorrelationIdMiddleware
                        .HeaderName]
                    .ToString());

            Assert.Equal(
                32,
                context.TraceIdentifier.Length);
        }

        /// <summary>
        /// Verifies that a valid client-provided correlation
        /// identifier is preserved throughout the request.
        /// </summary>
        [Fact]
        public async Task Invoke_ShouldPreserveCorrelationId_WhenHeaderIsValid()
        {
            // Arrange
            const string correlationId =
                "sprintboard-request-123";

            RequestDelegate next =
                context =>
                {
                    Assert.Equal(
                        correlationId,
                        context.TraceIdentifier);

                    return Task.CompletedTask;
                };

            var middleware =
                new CorrelationIdMiddleware(
                    next);

            var context =
                new DefaultHttpContext();

            context.Request.Headers[
                CorrelationIdMiddleware
                    .HeaderName] =
                correlationId;

            // Act
            await middleware.Invoke(
                context);

            // Assert
            Assert.Equal(
                correlationId,
                context.TraceIdentifier);

            Assert.Equal(
                correlationId,
                context.Response.Headers[
                    CorrelationIdMiddleware
                        .HeaderName]
                    .ToString());
        }

        /// <summary>
        /// Verifies that an invalid correlation identifier
        /// supplied by the client is replaced with a safe one.
        /// </summary>
        [Fact]
        public async Task Invoke_ShouldGenerateNewCorrelationId_WhenHeaderIsInvalid()
        {
            // Arrange
            var invalidCorrelationId =
                new string(
                    'a',
                    65);

            RequestDelegate next =
                _ =>
                    Task.CompletedTask;

            var middleware =
                new CorrelationIdMiddleware(
                    next);

            var context =
                new DefaultHttpContext();

            context.Request.Headers[
                CorrelationIdMiddleware
                    .HeaderName] =
                invalidCorrelationId;

            // Act
            await middleware.Invoke(
                context);

            // Assert
            Assert.NotEqual(
                invalidCorrelationId,
                context.TraceIdentifier);

            Assert.Equal(
                32,
                context.TraceIdentifier.Length);

            Assert.Equal(
                context.TraceIdentifier,
                context.Response.Headers[
                    CorrelationIdMiddleware
                        .HeaderName]
                    .ToString());
        }
    }
}