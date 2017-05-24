using System;
using System.ComponentModel;
using System.Threading.Tasks;
using YAPA.Contracts;

namespace YAPA.Shared
{
    public class PomodoroEngine : IPomodoroEngine, IPlugin
    {
        private readonly PomodoroEngineSettings _settings;
        private readonly ITimer _timer;

        public int Index => Current.Index;

        public int Elapsed => (int)(_endDate - _startDate).TotalSeconds;
        public int Remaining
        {
            get
            {
                var total = 0;
                switch (Phase)
                {
                    case PomodoroPhase.WorkEnded:
                    case PomodoroPhase.Work:
                        total = WorkTime;
                        break;
                    case PomodoroPhase.Break:
                    case PomodoroPhase.BreakEnded:
                        total = BreakTime;
                        break;
                }
                return total - Elapsed;
            }
        }

        public int DisplayValue => _settings.CountBackwards ? Remaining : Elapsed;

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

        /// <summary>
        /// Invoked when pomodoro work is completed
        /// </summary>
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

            switch (Phase)
            {
                case PomodoroPhase.NotStarted:
                    Phase = PomodoroPhase.Work;
                    break;
                case PomodoroPhase.BreakEnded:
                    Phase = PomodoroPhase.Work;
                    Current = Current.NextPomodoro;
                    break;
                case PomodoroPhase.WorkEnded:
                    Phase = PomodoroPhase.Break;
                    break;
                default:
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

            OnPomodoroCompleted += PomodoroEngine_OnPomodoroCompleted;

            Current = _pom1;
        }

        private async void PomodoroEngine_OnPomodoroCompleted()
        {
            if (_settings.AutoStartBreak)
            {
                await Task.Delay(TimeSpan.FromSeconds(1.5));
                if (IsRunning == false)
                {
                    Start();
                }
            }
        }

        private void _timer_Tick()
        {
            _endDate = DateTime.UtcNow;
            NotifyPropertyChanged(nameof(Elapsed));

            if (Phase == PomodoroPhase.Work && Elapsed >= WorkTime)
            {
                _timer.Stop();
                Phase = PomodoroPhase.WorkEnded;
                OnPomodoroCompleted?.Invoke();
            }

            if (Phase == PomodoroPhase.Break && Elapsed >= BreakTime)
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
        private readonly ISettingsForComponent _settings;

        public int WorkTime
        {
            get { return _settings.Get(nameof(WorkTime), 25 * 60); }
            set { _settings.Update(nameof(WorkTime), value); }
        }

        public int BreakTime
        {
            get { return _settings.Get(nameof(BreakTime), 5 * 60); }
            set { _settings.Update(nameof(BreakTime), value); }
        }

        public int LongBreakTime
        {
            get { return _settings.Get(nameof(LongBreakTime), 15 * 60); }
            set { _settings.Update(nameof(LongBreakTime), value); }
        }

        public bool AutoStartBreak
        {
            get { return _settings.Get(nameof(AutoStartBreak), false); }
            set { _settings.Update(nameof(AutoStartBreak), value); }
        }

        public bool CountBackwards
        {
            get { return _settings.Get(nameof(CountBackwards), false); }
            set { _settings.Update(nameof(CountBackwards), value); }
        }

        public bool DisableSoundNotifications
        {
            get { return _settings.Get(nameof(DisableSoundNotifications), false); }
            set { _settings.Update(nameof(DisableSoundNotifications), value); }
        }

        public double Volume
        {
            get { return _settings.Get(nameof(Volume), 0.5); }
            set { _settings.Update(nameof(Volume), value); }
        }

        public PomodoroEngineSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(nameof(PomodoroEngine));
        }

        public void DeferChanges()
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
