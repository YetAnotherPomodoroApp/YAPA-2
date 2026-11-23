using System.Windows;
using System.Windows.Threading;
using NLog;
using YAPA.Shared.Common;

namespace YAPA.WPF.Specifics
{
    /// <summary>
    /// Keeps the application running when Windows can't provide the taskbar button progress bar.
    /// Without this the exception WPF throws while setting it up closes the application at startup.
    /// </summary>
    public static class TaskbarFailureGuard
    {
        public static void Handle(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (!KnownFailures.IsTaskbarIntegrationFailure(e.Exception))
            {
                return;
            }

            LogManager.GetLogger("YAPA2").Warn(e.Exception, "Taskbar progress is not available on this system, continuing without it");

            //Stop asking for it, otherwise it fails again every time the taskbar button is recreated
            var application = Application.Current;
            if (application != null)
            {
                foreach (Window window in application.Windows)
                {
                    try
                    {
                        window.TaskbarItemInfo = null;
                    }
                    catch
                    {
                        //Nothing more can be done for this window
                    }
                }
            }

            e.Handled = true;
        }
    }
}
