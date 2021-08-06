namespace ShugiShugi
{
    using ShugiShugi.Common;

    /// <summary>
    /// <c>Program</c>
    /// </summary>
    class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
        {
            EtwLogger.Tracer.DebugMessage("Started");

            var coreObject = new CoreObject();
            
            coreObject.Function1();
        }
    }
}
