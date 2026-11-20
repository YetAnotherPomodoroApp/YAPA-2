using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Specifics
{
    public class AvaloniaThreading : IThreading
    {
        public void RunOnUiThread(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.UIThread.Invoke(action);
            }
        }
    }

    public class Timer : ITimer
    {
        private readonly DispatcherTimer _timer;

        public Timer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => Tick?.Invoke();
        }

        public event Action Tick;

        public void Start() => _timer.Start();

        public void Stop() => _timer.Stop();
    }

    public class SettingManager : ISettingManager
    {
        private bool _restartNeeded;
        private string _newVersion;

        public bool RestartNeeded
        {
            get => _restartNeeded;
            set { _restartNeeded = value; OnPropertyChanged(); }
        }

        public string NewVersion
        {
            get => _newVersion;
            set { _newVersion = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
