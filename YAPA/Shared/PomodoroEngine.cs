using System;
using System.ComponentModel;
using YAPA.Contracts;

namespace YAPA.Shared
{
    public class PomodoroEngine : IPomodoroEngine
    {
        private readonly PomodoroEngineSettings _settings;
        private readonly ITimer _timer;

        public int Index => Current.Index;

        public int Elapsed => (int)(_startDate - _endDate).TotalSeconds;

        public int WorkTime => Current.WorkTime;

        public int BreakTime => Current.BreakTime;

        public PomodoroPhase Phase { get; private set; }

        public event Func<bool> OnStarting;
        public event Action OnStarted;
        public event Action OnStopped;

        public bool IsRunning => Phase == PomodoroPhase.Break || Phase == PomodoroPhase.Work;

        private DateTime _startDate = DateTime.UtcNow;
        private DateTime _endDate = DateTime.UtcNow;

        public void Start()
        {
            var result = OnStarting?.Invoke();
            if (result.HasValue && result.Value)
            {
                return;
            }

            if (Phase == PomodoroPhase.BreakEnded || Phase == PomodoroPhase.NotStarted)
            {
                Phase = PomodoroPhase.Work;
            }
            else if (Phase == PomodoroPhase.WorkEnded)
            {
                Phase = PomodoroPhase.Break;
            }
            else
            {
                throw new InvalidOperationException($"Can't start pomodoro from phase: {Phase}");
            }

            _startDate = _endDate = DateTime.UtcNow;
            _timer.Start();

            OnStarted?.Invoke();
        }

        public void Stop()
        {
            switch (Phase)
            {
                case PomodoroPhase.Work:
                    //Reset current pomodoro
                    Phase = PomodoroPhase.WorkEnded; //Raise event that work ended
                    Phase = PomodoroPhase.NotStarted;
                    break;
                case PomodoroPhase.Break:
                    //go to next pomodoro
                    Phase = PomodoroPhase.BreakEnded; //Raise event that break ended
                    Phase = PomodoroPhase.NotStarted;
                    break;
                default:
                    throw new InvalidOperationException($"Can't stop pomodoro from phase: {Phase}");
            }

            _timer.Stop();

            OnStopped?.Invoke();
        }

        public void Reset()
        {
            _timer.Stop();

            switch (Phase)
            {
                case PomodoroPhase.Work:
                    Phase = PomodoroPhase.WorkEnded;
                    break;
                case PomodoroPhase.Break:
                    Phase = PomodoroPhase.BreakEnded;
                    break;
            }

            Phase = PomodoroPhase.NotStarted;
            _startDate = _endDate = DateTime.UtcNow;
            Current = pom1;
        }

        Pomodoro pom1, pom2, pom3, pom4;

        private Pomodoro _current;

        private Pomodoro Current
        {
            get { return _current; }
            set
            {
                _current = value;
                NotifyPropertyChanged("Index");
                NotifyPropertyChanged("WorkTime");
                NotifyPropertyChanged("BreakTime");
            }
        }

        public PomodoroEngine(PomodoroEngineSettings settings, ITimer timer)
        {
            _settings = settings;
            _timer = timer;
            _timer.Tick += _timer_Tick;

            pom4 = new Pomodoro(_settings) { Index = 4 };
            pom3 = new Pomodoro(_settings) { Index = 3, NextPomodoro = pom4 };
            pom2 = new Pomodoro(_settings) { Index = 2, NextPomodoro = pom3 };
            pom1 = new Pomodoro(_settings) { Index = 1, NextPomodoro = pom2 };
            pom4.NextPomodoro = pom1;

            Current = pom1;
        }

        private void _timer_Tick()
        {
            _endDate = DateTime.UtcNow;
            NotifyPropertyChanged("Elapsed");

            if (Phase == PomodoroPhase.Work && Elapsed / 60 >= WorkTime)
            {
                _timer.Stop();
            }

            if (Phase == PomodoroPhase.Break && Elapsed / 60 >= BreakTime)
            {
                _timer.Stop();
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    public class PomodoroEngineSettings : IPluginSettings
    {
        private readonly ISettings _settings;

        public int WorkTime
        {
            get { return _settings.Get("WorkTime", 25); }
            set { _settings.Update("WorkTime", value); }
        }

        public int BreakTime
        {
            get { return _settings.Get("BreakTime", 5); }
            set { _settings.Update("BreakTime", value); }
        }

        public int LongBreakTime
        {
            get { return _settings.Get("LongBreakTime", 15); }
            set { _settings.Update("LongBreakTime", value); }
        }

        public PomodoroEngineSettings(ISettings settings)
        {
            _settings = settings;
        }
    }

    internal class Pomodoro
    {
        private readonly PomodoroEngineSettings _settings;

        public int Index { get; set; }

        public int WorkTime => _settings.WorkTime;

        public int BreakTime => Index == 4 ? _settings.LongBreakTime : _settings.BreakTime;

        public Pomodoro NextPomodoro { get; set; }

        public Pomodoro(PomodoroEngineSettings settings)
        {
            _settings = settings;
        }
    }
}
