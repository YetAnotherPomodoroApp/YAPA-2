using System;
using System.Windows;
using YAPA.Shared.Contracts;

namespace YAPA.WPF.Specifics
{
    public class WpfThreading : IThreading
    {
        public void RunOnUiThread(Action action)
        {
            // Application.Current is null once the app has shut down, but a timer
            // tick already queued on the dispatcher can still run (crash report
            // from 2.0.192: PomodoroEngine._timer_Tick while closing). Nothing
            // needs updating then, so skip instead of crashing.
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted)
            {
                return;
            }

            dispatcher.Invoke(action);
        }
    }
}
