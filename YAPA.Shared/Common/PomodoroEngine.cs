using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using YAPA.Shared.Contracts;

namespace YAPA.Shared.Common
{
    public class PomodoroEngineSnapshot
    {
        public string EngineSettings { get; set; }
        public int PausedTime { get; set; }
        public DateTime StartDate { get; set; }
        public PomodoroPhase Phase { get; set; }
        public int PomodoroIndex { get; set; }
    }

    public class PomodoroEngine : IPomodoroEngine, IPlugin
    {
        private readonly PomodoroEngineSettings _settings;
        private readonly ITimer _timer;
        private readonly IDate _dateTime;
        private readonly IThreading _threading;
        private readonly IPomodoroRepository _repository;

        public int Index => Current.Index;

        private int _completedPomodorosThisSession;
        private int _completedTodayBeforeStarting;
        public int Counter
        {
            get
            {
                int counter;
                switch (_settings.Counter)
                {
                    case CounterEnum.PomodoroIndex:
                        counter = Index;
                        break;
                    case CounterEnum.CompletedToday:
                        counter = _completedTodayBeforeStarting + _completedPomodorosThisSession;
                        break;
                    case CounterEnum.CompletedThisSession:
                        counter = _completedPomodorosThisSession;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return counter;
            }
        }

        private int _elapsedInPause;
        public int Elapsed => Math.Min(_elapsedInPause + (int)(_endDate - _startDate).TotalSeconds, CurrentIntervalLength);
        public int Remaining => CurrentIntervalLength - Elapsed;

        public int CurrentIntervalLength
        {
            get
            {
                var length = 0;
                switch (Phase)
                {
                    case PomodoroPhase.NotStarted:
                    case PomodoroPhase.WorkEnded:
                    case PomodoroPhase.Work:
                    case PomodoroPhase.Pause:
                        length = WorkTime;
                        break;
                    case PomodoroPhase.Break:
                    case PomodoroPhase.BreakEnded:
                        length = BreakTime;
                        break;
                }
                return length;
            }
        }

        public int DisplayValue => _settings.CountBackwards ? Remaining : Elapsed;

        public int WorkTime => Current.WorkTime;

        public int BreakTime => Current.BreakTime;

        private PomodoroPhase _phase;
        public PomodoroPhase Phase
        {
            get => _phase;
            private set
            {
                if (_phase == value)
                {
                    return;
                }
                _phase = value;
                NotifyPropertyChanged(nameof(Phase));
                NotifyPropertyChanged(nameof(Counter));
            }
        }

        public event Func<bool> OnStarting;
        public event Action OnStarted;
        public event Action OnStopped;
        public event Action OnPaused;

        /// <summary>
        /// Invoked when pomodoro work is completed
        /// </summary>
        public event Action OnPomodoroCompleted;

        public bool IsRunning => Phase == PomodoroPhase.Break || Phase == PomodoroPhase.Work;

        private DateTime _startDate;
        private DateTime _endDate;

        public void Start()
        {
            var cancelStart = OnStarting?.Invoke();
            if (cancelStart.HasValue && cancelStart.Value)
            {
                return;
            }

            switch (Phase)
            {
                case PomodoroPhase.Pause:
                case PomodoroPhase.NotStarted:
                    Phase = PomodoroPhase.Work;
                    break;
                case PomodoroPhase.BreakEnded:
                    Phase = PomodoroPhase.Work;
                    Current = Current.NextPomodoro;
                    _elapsedInPause = 0;
                    break;
                case PomodoroPhase.WorkEnded:
                    Phase = PomodoroPhase.Break;
                    _elapsedInPause = 0;
                    break;
                default:
                    throw new InvalidOperationException($"Can't start pomodoro from phase: {Phase}");
            }

            _startDate = _endDate = _dateTime.DateTimeUtc();
            _timer.Start();

            OnStarted?.Invoke();
        }

        public void Stop()
        {
            ResetTo(Phase == PomodoroPhase.Break ? Current.NextPomodoro : Current);

            OnStopped?.Invoke();
        }

        public void Pause()
        {
            _timer.Stop();
            Phase = PomodoroPhase.Pause;
            _elapsedInPause = Elapsed;
            _startDate = _endDate = _dateTime.DateTimeUtc();
            OnPaused?.Invoke();
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
            _elapsedInPause = 0;

            _startDate = _endDate = _dateTime.DateTimeUtc();

            if (Current.Index != pom.Index)
            {
                Current = pom;
            }

            NotifyPropertyChanged(nameof(Elapsed));
            NotifyPropertyChanged(nameof(Remaining));
            NotifyPropertyChanged(nameof(DisplayValue));
        }

        private readonly Pomodoro _pom1;

        private Pomodoro _current;
        private ISettings _globalSettings;

        private Pomodoro Current
        {
            get => _current;
            set
            {
                if (_current?.Index == value.Index)
                {
                    return;
                }
                _current = value;
                NotifyPropertyChanged(nameof(Index));
                NotifyPropertyChanged(nameof(WorkTime));
                NotifyPropertyChanged(nameof(BreakTime));
            }
        }

        public PomodoroEngine(PomodoroEngineSettings settings, ITimer timer, IDate dateTime, IThreading threading, ISettings globalSettings, IPomodoroRepository repository)
        {
            _settings = settings;
            _timer = timer;
            _dateTime = dateTime;
            _threading = threading;
            _repository = repository;
            _globalSettings = globalSettings;
            _timer.Tick += _timer_Tick;

            var pom4 = new Pomodoro(_settings) { Index = 4 };
            var pom3 = new Pomodoro(_settings) { Index = 3, NextPomodoro = pom4 };
            var pom2 = new Pomodoro(_settings) { Index = 2, NextPomodoro = pom3 };
            _pom1 = new Pomodoro(_settings) { Index = 1, NextPomodoro = pom2 };
            pom4.NextPomodoro = _pom1;

            _startDate = _endDate = _dateTime.DateTimeUtc();

            OnPomodoroCompleted += PomodoroEngine_OnPomodoroCompleted;

            Current = _pom1;

            _globalSettings.PropertyChanged += _globalSettings_PropertyChanged;

            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1);
            _completedTodayBeforeStarting = _repository.Pomodoros.Count(x => todayStart <= x.DateTime && x.DateTime <= todayEnd);
        }

        private void _globalSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == $"{nameof(PomodoroEngine)}.{nameof(_settings.WorkTime)}"
                || e.PropertyName == $"{nameof(PomodoroEngine)}.{nameof(_settings.BreakTime)}"
                || e.PropertyName == $"{nameof(PomodoroEngine)}.{nameof(_settings.LongBreakTime)}"
                || e.PropertyName == $"{nameof(PomodoroEngine)}.{nameof(_settings.CountBackwards)}")
            {
                NotifyPropertyChanged(nameof(DisplayValue));
                NotifyPropertyChanged(nameof(WorkTime));
                NotifyPropertyChanged(nameof(BreakTime));
            }
            else if (e.PropertyName == $"{nameof(PomodoroEngine)}.{nameof(_settings.Counter)}")
            {
                NotifyPropertyChanged(nameof(Counter));
            }

        }

        private async void PomodoroEngine_OnPomodoroCompleted()
        {
            _completedPomodorosThisSession++;
            NotifyPropertyChanged(nameof(Counter));

            var delayBeforeStarting = 1.5;
            if (!_settings.AutoStartBreak)
            {
                return;
            }
            await Task.Delay(TimeSpan.FromSeconds(delayBeforeStarting));
            if (IsRunning == false)
            {
                var expectedWorkEndTime = _startDate.AddSeconds(WorkTime - _elapsedInPause);
                Start();
                _startDate = expectedWorkEndTime;
            }
        }

        private void _timer_Tick()
        {
            _endDate = _dateTime.DateTimeUtc();
            NotifyPropertyChanged(nameof(Elapsed));
            NotifyPropertyChanged(nameof(Remaining));
            NotifyPropertyChanged(nameof(DisplayValue));

            if (Phase == PomodoroPhase.Work && Elapsed >= WorkTime)
            {
                _timer.Stop();
                Phase = PomodoroPhase.WorkEnded;
                OnPomodoroCompleted?.Invoke();
            }
            else if (Phase == PomodoroPhase.Break && Elapsed >= BreakTime)
            {
                _timer.Stop();
                Phase = PomodoroPhase.BreakEnded;
            }
        }

        public PomodoroEngineSnapshot GetSnapshot()
        {
            var snapshot = new PomodoroEngineSnapshot
            {
                EngineSettings = _globalSettings.GetRawSettingsForComponent(nameof(PomodoroEngine)),
                PausedTime = Elapsed,
                Phase = Phase,
                StartDate = _dateTime.DateTimeUtc(),
                PomodoroIndex = Index
            };

            return snapshot;
        }

        public void LoadSnapshot(PomodoroEngineSnapshot snapshot)
        {
            _timer.Stop();

            _globalSettings.SetRawSettingsForComponent(nameof(PomodoroEngine), snapshot.EngineSettings);
            _elapsedInPause = snapshot.PausedTime;

            if (snapshot.Phase == PomodoroPhase.Work ||
                snapshot.Phase == PomodoroPhase.Break ||
                (snapshot.Phase == PomodoroPhase.WorkEnded && _settings.AutoStartBreak))
            {
                _timer.Start();
            }

            Phase = snapshot.Phase;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string property)
        {
            _threading.RunOnUiThread(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            });
        }
    }

    public class PomodoroEngineSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public int WorkTime
        {
            get => _settings.Get(nameof(WorkTime), 25 * 60);
            set => _settings.Update(nameof(WorkTime), value);
        }

        public int BreakTime
        {
            get => _settings.Get(nameof(BreakTime), 5 * 60);
            set => _settings.Update(nameof(BreakTime), value);
        }

        public int LongBreakTime
        {
            get => _settings.Get(nameof(LongBreakTime), 15 * 60);
            set => _settings.Update(nameof(LongBreakTime), value);
        }

        public bool AutoStartBreak
        {
            get => _settings.Get(nameof(AutoStartBreak), false);
            set => _settings.Update(nameof(AutoStartBreak), value);
        }

        public bool CountBackwards
        {
            get => _settings.Get(nameof(CountBackwards), false);
            set => _settings.Update(nameof(CountBackwards), value);
        }

        public bool DisableSoundNotifications
        {
            get => _settings.Get(nameof(DisableSoundNotifications), false);
            set => _settings.Update(nameof(DisableSoundNotifications), value);
        }

        public double Volume
        {
            get => _settings.Get(nameof(Volume), 0.5);
            set => _settings.Update(nameof(Volume), value);
        }

        public CounterEnum Counter
        {
            get => _settings.Get(nameof(Counter), CounterEnum.PomodoroIndex);
            set => _settings.Update(nameof(Counter), value);
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
