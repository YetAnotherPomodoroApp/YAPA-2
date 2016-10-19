using System;
using System.ComponentModel;
using YAPA.Contracts;
using YAPA.WPF;

namespace YAPA.Shared
{

    public class PomodoroEnginePlugin : IPluginMeta
    {
        public string Title => "General";

        public Type Plugin => null;

        public Type Settings => typeof(PomodoroEngineSettings);

        public Type SettingEditWindow => typeof(PomodoroEngineSettingWindow);
    }

    public class PomodoroEngine : IPomodoroEngine, IPlugin
    {
        private readonly PomodoroEngineSettings _settings;
        private readonly ITimer _timer;

        public int Index => Current.Index;

        public int Elapsed => (int)(_endDate - _startDate).TotalSeconds;

        public int WorkTime => Current.WorkTime;

        public int BreakTime => Current.BreakTime;

        private PomodoroPhase _phase;
        public PomodoroPhase Phase
        {
            get
            {
                return _phase;
            }
            private set
            {
                if (_phase == value)
                {
                    return;
                }
                _phase = value;
                NotifyPropertyChanged(nameof(Phase));
            }
        }

        public event Func<bool> OnStarting;
        public event Action OnStarted;
        public event Action OnStopped;
        public event Action OnPomodoroCompleted;

        public bool IsRunning => Phase == PomodoroPhase.Break || Phase == PomodoroPhase.Work;

        private DateTime _startDate = DateTime.UtcNow;
        private DateTime _endDate = DateTime.UtcNow;

        public void Start()
        {
            var cancelStart = OnStarting?.Invoke();
            if (cancelStart.HasValue && cancelStart.Value)
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
            ResetTo(Current);

            OnStopped?.Invoke();
        }

        public void Reset()
        {
            ResetTo(_pom1);
        }

        private void ResetTo(Pomodoro pom)
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
            if (Current.Index != pom.Index)
            {
                Current = pom;
            }
        }

        private readonly Pomodoro _pom1, _pom2, _pom3, _pom4;

        private Pomodoro _current;
        private Pomodoro Current
        {
            get { return _current; }
            set
            {
                if (_current?.Index == value.Index)
                {
                    return;
                }
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

            _pom4 = new Pomodoro(_settings) { Index = 4 };
            _pom3 = new Pomodoro(_settings) { Index = 3, NextPomodoro = _pom4 };
            _pom2 = new Pomodoro(_settings) { Index = 2, NextPomodoro = _pom3 };
            _pom1 = new Pomodoro(_settings) { Index = 1, NextPomodoro = _pom2 };
            _pom4.NextPomodoro = _pom1;

            Current = _pom1;
        }

        private void _timer_Tick()
        {
            _endDate = DateTime.UtcNow;
            NotifyPropertyChanged(nameof(Elapsed));

            if (Phase == PomodoroPhase.Work && Elapsed / 60 >= WorkTime)
            {
                _timer.Stop();
                Phase = PomodoroPhase.WorkEnded;
                OnPomodoroCompleted?.Invoke();
            }

            if (Phase == PomodoroPhase.Break && Elapsed / 60 >= BreakTime)
            {
                _timer.Stop();
                Phase = PomodoroPhase.BreakEnded;
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
        private readonly ISettingsForPlugin _settings;

        public int WorkTime
        {
            get { return _settings.Get(nameof(WorkTime), 25); }
            set { _settings.Update(nameof(WorkTime), value); }
        }

        public int BreakTime
        {
            get { return _settings.Get(nameof(BreakTime), 5); }
            set { _settings.Update(nameof(BreakTime), value); }
        }

        public int LongBreakTime
        {
            get { return _settings.Get(nameof(LongBreakTime), 15); }
            set { _settings.Update(nameof(LongBreakTime), value); }
        }

        public PomodoroEngineSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForPlugin(nameof(PomodoroEngine));
        }

        public void DefereChanges()
        {
            _settings.DeferChanges();
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
