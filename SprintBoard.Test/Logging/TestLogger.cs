using Microsoft.Extensions.Logging;

namespace SprintBoard.Test.Logging
{
    /// <summary>
    /// Represents a log entry captured during an automated test.
    /// </summary>
    public sealed record TestLogEntry(
        LogLevel Level,
        string Message,
        Exception? Exception);

    /// <summary>
    /// Captures application log entries in memory so automated
    /// tests can verify logging behavior without external sinks.
    /// </summary>
    /// <typeparam name="T">
    /// Category type associated with the logger.
    /// </typeparam>
    public sealed class TestLogger<T>
        : ILogger<T>
    {
        private readonly List<TestLogEntry>
            _entries = [];

        /// <summary>
        /// Gets all log entries captured by this logger.
        /// </summary>
        public IReadOnlyList<TestLogEntry>
            Entries =>
                _entries;

        /// <summary>
        /// Begins a logging scope.
        /// </summary>
        public IDisposable? BeginScope<TState>(
            TState state)
            where TState : notnull
        {
            return EmptyScope.Instance;
        }

        /// <summary>
        /// Indicates whether the supplied log level is enabled.
        /// </summary>
        public bool IsEnabled(
            LogLevel logLevel)
        {
            return true;
        }

        /// <summary>
        /// Captures a formatted log entry in memory.
        /// </summary>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string>
                formatter)
        {
            _entries.Add(
                new TestLogEntry(
                    logLevel,
                    formatter(
                        state,
                        exception),
                    exception));
        }

        /// <summary>
        /// Represents an empty disposable logging scope.
        /// </summary>
        private sealed class EmptyScope
            : IDisposable
        {
            /// <summary>
            /// Gets the singleton empty scope instance.
            /// </summary>
            public static EmptyScope Instance
            {
                get;
            } = new();

            /// <summary>
            /// Completes the empty logging scope.
            /// </summary>
            public void Dispose()
            {
            }
        }
    }
}