namespace ShugiShugi.Common
{
    using Microsoft.Diagnostics.Tracing;

    /// <summary>
    /// <c>EtwLogger</c>
    /// </summary>
    /// <seealso cref="Microsoft.Diagnostics.Tracing.EventSource" />
    [EventSource(Name = "ShugiShugi-Trace")]
    public sealed class EtwLogger : Microsoft.Diagnostics.Tracing.EventSource
    {
        public static EtwLogger Tracer = new EtwLogger();

        /// <summary>
        /// Function to write general 'debug' messages
        /// </summary>
        /// <param name="message">The message.</param>
        [Event(1)]
        public void DebugMessage(string message)
        {
            WriteEvent(1, message);
        }

        /// <summary>
        /// Function will write events about 'test start' and 'test stop' phases
        /// </summary>
        /// <param name="message">The message.</param>
        [Event(2)]
        public void ReportTestEvent(string message)
        {
            WriteEvent(2, message);
        }
    }
}
