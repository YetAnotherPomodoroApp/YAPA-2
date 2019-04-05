using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using YAPA.Shared;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;
using WindowState = System.Windows.WindowState;


namespace Motivational
{
    public partial class MainWindow : AbstractWindow, INotifyPropertyChanged
    {
        private readonly MotivationalThemeSettings _settings;
        private readonly PomodoroEngineSettings _baseSettings;
        private readonly IPomodoroRepository _pomodoroRepository;
        private readonly ISettings _globalSettings;


        public MainWindow(IMainViewModel viewModel, MotivationalThemeSettings settings, PomodoroEngineSettings baseSettings, IPomodoroRepository pomodoroRepository, ISettings globalSettings) :base(viewModel)
        {
            ViewModel = viewModel;
            _settings = settings;
            _baseSettings = baseSettings;
            _pomodoroRepository = pomodoroRepository;
            _globalSettings = globalSettings;
            InitializeComponent();
            globalSettings.PropertyChanged += _globalSettings_PropertyChanged;

            DataContext = this;

            // Initialize Pomodoro session
            ResetPomodoroPeriod();

            ViewModel.Engine.PropertyChanged += Engine_PropertyChanged;
            ViewModel.Engine.OnPomodoroCompleted += Engine_OnPomodoroCompleted;
            ViewModel.Engine.OnStarted += EngineOnOnStarted;

            UpdateCompletedPomodoroCount();

            UpdateTime();
        }

        private void EngineOnOnStarted()
        {
            if (CurrentPomodoroPeriod > 0)
                HidePeriodCompletedIndicator();

            CurrentQuote = null; // Reset quote
        }

        private void _globalSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.StartsWith("Motivational"))
            {
                RaisePropertyChanged(nameof(_settings.UseWhiteText));
                RaisePropertyChanged(nameof(TimerForegroundColor));
                RaisePropertyChanged(nameof(TimerShadowColor));
                RaisePropertyChanged(nameof(WindowBackgroundColor));
                RaisePropertyChanged(nameof(WindowBackground2Color));
                RaisePropertyChanged(nameof(WindowShadowColor));
                RaisePropertyChanged(nameof(WindowShadowOpacity));
                RaisePropertyChanged(nameof(WindowForegroundColor));

            }
        }

        private void UpdateTime()
        {
            var min = CurrentTimeValue / 60;
            var sec = CurrentTimeValue % 60;

            CurrentTimeMinutesText = String.Format("{0:00}", min);
            CurrentTimeSecondsText = String.Format("{0:00}", sec);
        }

        private async void UpdateCompletedPomodoroCount()
        {
            await Task.Run(() =>
            {
                var today = DateTime.Today.Date;
                CurrentPomodoroPeriod = _pomodoroRepository.Pomodoros.Where(x => x.DateTime == today).Select(a => a.Count).DefaultIfEmpty(0).Sum();
                RaisePropertyChanged(nameof(CurrentPomodoroPeriod));
            });
        }

        private void Engine_OnPomodoroCompleted()
        {
            CurrentPomodoroPeriod++;
            RaisePropertyChanged(nameof(CurrentPomodoroPeriod));

            DoCompletePeriod();
        }

        private void Engine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.Engine.Elapsed) || e.PropertyName == nameof(ViewModel.Engine.DisplayValue))
            {
                RaisePropertyChanged(nameof(CurrentTimeValue));
                RaisePropertyChanged(nameof(ProgressValue));
                UpdateTime();
            }
            else if (e.PropertyName == nameof(ViewModel.Engine.Phase))
            {
                RaisePropertyChanged(nameof(ProgressState));
                NotifyPeriodChanged();
            }
        }

        public double ProgressValue
        {
            get
            {
                var elapsed = ViewModel.Engine.Elapsed;
                var progress = 0d;
                switch (ViewModel.Engine.Phase)
                {
                    case PomodoroPhase.WorkEnded:
                    case PomodoroPhase.Work:
                        progress = (double)elapsed / (ViewModel.Engine.WorkTime);
                        break;
                    case PomodoroPhase.Break:
                    case PomodoroPhase.BreakEnded:
                        progress = (double)elapsed / (ViewModel.Engine.BreakTime);
                        break;
                }
                return progress;
            }
        }


        public string ProgressState
        {
            get
            {
                var progressState = "";
                switch (ViewModel.Engine.Phase)
                {
                    case PomodoroPhase.NotStarted:
                        break;
                    case PomodoroPhase.Work:
                        progressState = "Normal";
                        break;
                    case PomodoroPhase.Break:
                        progressState = "Paused";
                        break;
                    case PomodoroPhase.WorkEnded:
                    case PomodoroPhase.BreakEnded:
                        progressState = "Error";
                        break;
                }
                return progressState;
            }
        }

        public int CurrentTimeValue => ViewModel.Engine.DisplayValue;

        private void DoCompletePeriod()
        {
            CurrentTimeMinutesText = "00";
            CurrentTimeSecondsText = "00";

            ShowPeriodCompletedIndicator();
        }

        public int CurrentPomodoroPeriod { get; set; }


        /// <summary>
        /// Reset Pomodoro period
        /// </summary>
        private void ResetPomodoroPeriod()
        {
            CurrentTimeMinutesText = "00";
            CurrentTimeSecondsText = "00";
        }

        /// <summary>
        /// Period completed indicator animation instance
        /// </summary>
        private Storyboard _periodCompletedAnimationStoryboard = null;
        private Storyboard PeriodCompletedIndicator
        {
            get
            {
                if (null == _periodCompletedAnimationStoryboard)
                    _periodCompletedAnimationStoryboard = TryFindResource("PeriodCompletedIndicatorStoryboard") as Storyboard;

                return _periodCompletedAnimationStoryboard;
            }
        }

        private void ShowPeriodCompletedIndicator()
        {
            PeriodCompletedIndicator?.Begin();
        }

        /// <summary>
        /// Hide completed indicator
        /// </summary>
        private void HidePeriodCompletedIndicator()
        {
            PeriodCompletedIndicator?.Stop();
        }

        /// <summary>
        /// Notifies UI about changed period
        /// </summary>
        private void NotifyPeriodChanged()
        {
            RaisePropertyChanged("CurrentPomodoroPeriod");
            RaisePropertyChanged("CurrentPeriodText");
            RaisePropertyChanged("CurrentPeriodTextSource");
            RaisePropertyChanged("CurrentPeriodIcon");
            RaisePropertyChanged("ProgressState");
            RaisePropertyChanged("CanStop");
            RaisePropertyChanged("CanStart");
            RaisePropertyChanged("CanPause");
        }

        public bool CanStart => ViewModel.StartCommand.CanExecute(null);
        public bool CanStop => ViewModel.StopCommand.CanExecute(null);
        public bool CanPause => ViewModel.PauseCommand.CanExecute(null);


        /// <summary>
        /// Are we using light theme currently?
        /// </summary>
        public bool UseLightTheme => _settings.UseWhiteText;

        #region UI binded properties

        public SolidColorBrush AccentColor => Utils.HexToBrush("#FF0080");

        public double TimerForegroundOpacity => 0.3;

        public double TimerShadowOpacity => 0.6;

        public SolidColorBrush TimerForegroundColor => UseLightTheme ? Utils.HexToBrush(Const.COLOR_LIGHT_TIMER_FOREGROUND) : Utils.HexToBrush(Const.COLOR_DARK_TIMER_FOREGROUND);

        public Color TimerShadowColor => UseLightTheme ? Utils.HexToColor(Const.COLOR_LIGHT_TIMER_SHADOW) : Utils.HexToColor(Const.COLOR_DARK_TIMER_SHADOW);

        public SolidColorBrush WindowBackgroundColor => UseLightTheme ? Utils.HexToBrush(Const.COLOR_LIGHT_WINDOW_BACKGROUND) : Utils.HexToBrush(Const.COLOR_DARK_WINDOW_BACKGROUND);

        public SolidColorBrush WindowBackground2Color => UseLightTheme ? Utils.HexToBrush(Const.COLOR_LIGHT_WINDOW_BACKGROUND2) : Utils.HexToBrush(Const.COLOR_DARK_WINDOW_BACKGROUND2);

        public SolidColorBrush WindowForegroundColor => UseLightTheme ? Utils.HexToBrush(Const.COLOR_LIGHT_WINDOW_FOREGROUND) : Utils.HexToBrush(Const.COLOR_DARK_WINDOW_FOREGROUND);

        public Color WindowShadowColor => UseLightTheme ? Utils.HexToColor(Const.COLOR_LIGHT_WINDOW_SHADOW) : Utils.HexToColor(Const.COLOR_DARK_WINDOW_SHADOW);

        public double WindowShadowOpacity => UseLightTheme ? Const.COLOR_LIGHT_WINDOW_SHADOW_OPACITY : Const.COLOR_DARK_WINDOW_SHADOW_OPACITY;

        #endregion

        public int WorkTime => _baseSettings.WorkTime;

        public int BreakTime => _baseSettings.BreakTime;

        public int LongBreakTime => _baseSettings.LongBreakTime;

        public bool CountBackwards => _baseSettings.CountBackwards;

        public string CurrentTimeMinutesText
        {
            set
            {
                CurrentTimeMinutes.Text = value;
                CurrentTimeMinutesInsideWindow.Text = value;
            }
        }

        public string CurrentTimeSecondsText
        {
            set
            {
                CurrentTimeSeconds.Text = value;
                CurrentTimeSecondsInsideWindow.Text = value;
            }
        }

        /// <summary>
        /// Current quote text
        /// </summary>
        private Quote _currentQuote = null;
        public Quote CurrentQuote
        {
            get
            {
                if (null == _currentQuote)
                {
                    _currentQuote = Quotes.GetRandomQuote();
                }

                return _currentQuote;
            }
            set
            {
                _currentQuote = value;
            }
        }

        /// <summary>
        /// Current period motivation text
        /// </summary>
        public string CurrentPeriodText
        {
            get
            {
                switch (ViewModel.Engine.Phase)
                {
                    case PomodoroPhase.NotStarted:
                        return Localizations.General.app_period_press_play_to_start;
                    case PomodoroPhase.BreakEnded:
                        return Localizations.General.app_period_motivation_start_pomodoro;
                    case PomodoroPhase.WorkEnded:

                    case PomodoroPhase.Work:
                        return (!string.IsNullOrWhiteSpace(CurrentQuote.Text) ? CurrentQuote.Text : Localizations.General.app_period_pomodoro_caption_default);

                    case PomodoroPhase.Break:
                        if (ViewModel.Engine.Index == 4)
                            return String.Format(Localizations.General.app_period_long_break_caption, LongBreakTime);

                        return String.Format(Localizations.General.app_period_short_break_caption, BreakTime);
                    default:
                        return Localizations.General.app_period_motivation_start_pomodoro;
                }
            }
        }

        /// <summary>
        /// Motivation text source
        /// </summary>
        public string CurrentPeriodTextSource
        {
            get
            {
                if (ViewModel.Engine.Phase == PomodoroPhase.Work)
                    return (!string.IsNullOrWhiteSpace(CurrentQuote.Source) ? CurrentQuote.Source : string.Empty);

                return string.Empty;
            }
        }

        public string CurrentPeriodIcon
        {
            get
            {
                switch (ViewModel.Engine.Phase)
                {
                    case PomodoroPhase.Work:
                    case PomodoroPhase.WorkEnded:
                        return Const.ICON_PERIOD_POMODORO;
                    case PomodoroPhase.BreakEnded:
                    case PomodoroPhase.Break:
                        if (ViewModel.Engine.Index == 4)
                        {
                            return Const.ICON_PERIOD_REST_LONG;
                        }
                        else
                        {
                            return Const.ICON_PERIOD_REST;
                        }


                    default:
                        return Const.ICON_PERIOD_STOPPED;
                }
            }
        }

        private void Minimize_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                OnMouseLeftButtonDown(e);
                DragMove();
                e.Handled = true;
            }
            catch
            {
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ButtonExit_OnClick(object sender, RoutedEventArgs e)
        {
            CloseApp();
        }
    }
}
