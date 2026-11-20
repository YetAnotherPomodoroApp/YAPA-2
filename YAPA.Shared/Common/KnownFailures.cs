using System;

namespace YAPA.Shared.Common
{
    /// <summary>
    /// Failures of optional Windows features that the application can run without.
    /// </summary>
    public static class KnownFailures
    {
        /// <summary>
        /// Windows could not set up the taskbar button of a window (the progress bar on it).
        /// Seen where the shell does not implement the taskbar interface, for example with a replacement shell.
        /// </summary>
        public static bool IsTaskbarIntegrationFailure(Exception exception)
        {
            var stackTrace = exception?.StackTrace;

            return stackTrace != null && stackTrace.Contains("System.Windows.Window.ApplyTaskbarItemInfo");
        }
    }
}
