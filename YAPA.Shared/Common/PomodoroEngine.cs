using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using YAPA.Shared.Contracts;
using System.Collections.Generic;

namespace YAPA.Shared.Common
{
    public class PomodoroEngineSnapshot
    {
        public PomodoroProfile PomodoroProfile { get; set; }
        public string ProfileName { get; set; }
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
                NotifyPropertyChanged(nameof(IsRunning));
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
            ResetTo(Phase == PomodoroPhase.Break || Phase == PomodoroPhase.WorkEnded ? Current.NextPomodoro : Current);

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

            EverythingChanged();
        }

        private readonly Pomodoro _pom1;

        private Pomodoro _current;

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
            _timer.Tick += _timer_Tick;

            var pom4 = new Pomodoro(_settings) { Index = 4 };
            var pom3 = new Pomodoro(_settings) { Index = 3, NextPomodoro = pom4 };
            var pom2 = new Pomodoro(_settings) { Index = 2, NextPomodoro = pom3 };
            _pom1 = new Pomodoro(_settings) { Index = 1, NextPomodoro = pom2 };
            pom4.NextPomodoro = _pom1;

            _startDate = _endDate = _dateTime.DateTimeUtc();

            OnPomodoroCompleted += PomodoroEngine_OnPomodoroCompleted;

            Current = _pom1;

            globalSettings.PropertyChanged += _globalSettings_PropertyChanged;

            var todayStart = _dateTime.DateTimeUtc().Date;
            var todayEnd = _dateTime.DateTimeUtc().Date.AddDays(1).AddSeconds(-1);
            _completedTodayBeforeStarting = repository?.Pomodoros?.Count(x => todayStart <= x.DateTime && x.DateTime <= todayEnd) ?? 0;
        }

        private void _globalSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == $"{nameof(PomodoroEngine)}.{nameof(_settings.WorkTime)}"
                || e.PropertyName == $"{nameof(PomodoroEngine)}.{nameof(_settings.BreakTime)}"
                || e.PropertyName == $"{nameof(PomodoroEngine)}.{nameof(_settings.LongBreakTime)}"
                || e.PropertyName == $"{nameof(PomodoroEngine)}.{nameof(_settings.CountBackwards)}"
                || e.PropertyName == $"{nameof(PomodoroEngine)}.{nameof(_settings.ActiveProfile)}"
                || e.PropertyName == $"{nameof(PomodoroEngine)}.{nameof(_settings.Profiles)}")
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
        private async void PomodoroEngine_OnPomodoroBreakCompleted()
        {
            var delayBeforeStarting = 1.5;
            if (!_settings.AutoStartWork)
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
                PomodoroEngine_OnPomodoroBreakCompleted();
            }
        }

        public PomodoroEngineSnapshot GetSnapshot()
        {
            var snapshot = new PomodoroEngineSnapshot
            {
                PomodoroProfile = _settings.Profiles[_settings.ActiveProfile],
                ProfileName = _settings.ActiveProfile,
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

            if (string.IsNullOrEmpty(snapshot.ProfileName))
            {
                var standartProfile = "Snapshot";

                var currentProfiles = _settings.Profiles;
                currentProfiles[standartProfile] = snapshot.PomodoroProfile;

                _settings.Profiles = currentProfiles;
                _settings.ActiveProfile = standartProfile;
            }
            else
            {
                var currentProfiles = _settings.Profiles;
                currentProfiles[snapshot.ProfileName] = snapshot.PomodoroProfile;

                _settings.Profiles = currentProfiles;
                _settings.ActiveProfile = snapshot.ProfileName;
            }

            _elapsedInPause = snapshot.PausedTime;

            if (snapshot.Phase == PomodoroPhase.Work ||
                snapshot.Phase == PomodoroPhase.Break ||
                (snapshot.Phase == PomodoroPhase.WorkEnded && _settings.AutoStartBreak))
            {
                _startDate = _endDate = snapshot.StartDate;
                _timer.Start();
            }

            Phase = snapshot.Phase;

            EverythingChanged();
        }

        private void EverythingChanged()
        {
            NotifyPropertyChanged(nameof(Phase));
            NotifyPropertyChanged(nameof(Counter));
            NotifyPropertyChanged(nameof(IsRunning));
            NotifyPropertyChanged(nameof(Elapsed));
            NotifyPropertyChanged(nameof(Remaining));
            NotifyPropertyChanged(nameof(DisplayValue));
            NotifyPropertyChanged(nameof(Index));
            NotifyPropertyChanged(nameof(WorkTime));
            NotifyPropertyChanged(nameof(BreakTime));
            NotifyPropertyChanged(nameof(DisplayValue));
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

        private static readonly Dictionary<string, PomodoroProfile> DefaultProfile;
        private static string DefaultProfileName = "Pomodoro";

        static PomodoroEngineSettings()
        {
            DefaultProfile = new Dictionary<string, PomodoroProfile>
            {
                [DefaultProfileName] = new PomodoroProfile
                {
                    AutoStartBreak = false,
                    BreakTime = 5 * 60,
                    LongBreakTime = 15 * 60,
                    WorkTime = 25 * 60,
                    PomodorosBeforeLongBreak = 4
                }
            };
        }

        public Dictionary<string, PomodoroProfile> Profiles
        {
            get => _settings.Get(nameof(Profiles), DefaultProfile);
            set => _settings.Update(nameof(Profiles), value);
        }

        public string ActiveProfile
        {
            get => _settings.Get(nameof(ActiveProfile), DefaultProfileName);
            set => _settings.Update(nameof(ActiveProfile), value);
        }

        public int WorkTime
        {
            get => Profiles[ActiveProfile].WorkTime;
            set
            {
                var temp = new Dictionary<string, PomodoroProfile>(Profiles);
                var profile = temp[ActiveProfile];
                profile.WorkTime = value;
                Profiles = temp;
            }
        }

        public int BreakTime
        {
            get => Profiles[ActiveProfile].BreakTime;
            set
            {
                var temp = new Dictionary<string, PomodoroProfile>(Profiles);
                var profile = temp[ActiveProfile];
                profile.BreakTime = value;
                Profiles = temp;
            }
        }

        public int LongBreakTime
        {
            get => Profiles[ActiveProfile].LongBreakTime;
            set
            {
                var temp = new Dictionary<string, PomodoroProfile>(Profiles);
                var profile = temp[ActiveProfile];
                profile.LongBreakTime = value;
                Profiles = temp;
            }
        }

        public bool AutoStartBreak
        {
            get => Profiles[ActiveProfile].AutoStartBreak;
            set
            {
                var temp = new Dictionary<string, PomodoroProfile>(Profiles);
                var profile = temp[ActiveProfile];
                profile.AutoStartBreak = value;
                Profiles = temp;
            }
        }

        public bool AutoStartWork
        {
            get => Profiles[ActiveProfile].AutoStartWork;
            set
            {
                var temp = new Dictionary<string, PomodoroProfile>(Profiles);
                var profile = temp[ActiveProfile];
                profile.AutoStartWork = value;
                Profiles = temp;
            }
        }

        public string FontFamily
        {
            get => _settings.Get(nameof(FontFamily), "Segoe UI Light.ttf");
            set => _settings.Update(nameof(FontFamily), value);
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

        public string ReleaseNotes
        {
            get => _settings.Get(nameof(ReleaseNotes), string.Empty, true);
            set => _settings.Update(nameof(ReleaseNotes), value, true);
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

    public class PomodoroProfile
    {
        public int WorkTime { get; set; }
        public int BreakTime { get; set; }
        public int LongBreakTime { get; set; }
        public bool AutoStartBreak { get; set; }
        public bool AutoStartWork { get; set; }
        public int PomodorosBeforeLongBreak { get; set; }
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
