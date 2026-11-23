using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Themes.Motivational
{
    /// <summary>The fonts the WPF theme compiles into Motivational.dll.</summary>
    public static class MotivationalFonts
    {
        private const string Folder = "avares://YAPA/Assets/Fonts/Motivational";

        public static FontFamily RobotoCondensedLight { get; } = new FontFamily($"{Folder}#Roboto Condensed Light");

        public static FontFamily RobotoCondensed { get; } = new FontFamily($"{Folder}#Roboto Condensed");

        public static FontFamily FontAwesome { get; } = new FontFamily($"{Folder}#FontAwesome");
    }

    public partial class MotivationalTheme : AbstractWindow, INotifyPropertyChanged
    {
        private readonly MotivationalThemeSettings _settings;
        private readonly PomodoroEngineSettings _baseSettings;
        private readonly IPomodoroRepository _pomodoroRepository;

        // PeriodCompletedIndicatorStoryboard: 0.5 s auto-reversed, forever, the dot shown from 0.2 s;
        // so each second it is hidden for 0.2 s, shown for 0.6 s, hidden for 0.2 s (at 10 frames a second)
        private readonly DispatcherTimer _periodCompletedTimer;
        private DateTime _periodCompletedStarted;

        private readonly DoubleTransition _contentFade = new DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromSeconds(0.15) };

        public MotivationalTheme()
        {
            // for the XAML previewer
            InitializeComponent();
        }

        public MotivationalTheme(IMainViewModel viewModel, MotivationalThemeSettings settings, PomodoroEngineSettings baseSettings, IPomodoroRepository pomodoroRepository, ISettings globalSettings) : base(viewModel)
        {
            ViewModel = viewModel;
            _settings = settings;
            _baseSettings = baseSettings;
            _pomodoroRepository = pomodoroRepository;

            InitializeComponent();
            MakeTransparentOverlay();
            // AllowsTransparency with a Transparent background: the margins round the card stay see-through
            Background = Brushes.Transparent;

            globalSettings.PropertyChanged += _globalSettings_PropertyChanged;

            DataContext = this;

            _periodCompletedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _periodCompletedTimer.Tick += (s, e) => RenderPeriodCompletedIndicator();

            WindowContentPanel.Transitions = new Transitions { _contentFade };

            TimeOutsideWindow.SpokenText = TimeInsideWindow.SpokenText =
                () => YAPA.Shared.Common.TimerSpeech.Display(ViewModel.Engine.Phase, CurrentTimeValue, _baseSettings.CountBackwards);

            // Initialize Pomodoro session
            ResetPomodoroPeriod();

            ViewModel.Engine.PropertyChanged += Engine_PropertyChanged;
            ViewModel.Engine.OnPomodoroCompleted += Engine_OnPomodoroCompleted;
            ViewModel.Engine.OnStarted += EngineOnOnStarted;

            UpdateCompletedPomodoroCount();
            UpdateTime();

            PointerPressed += MainWindow_MouseLeftButtonDown;
            PointerReleased += MainWindow_PointerReleased;
            PointerEntered += (s, e) => FadeContent(1, 0.15);
            PointerExited += (s, e) => FadeContent(0, 0.3);
        }

        // Window.MouseEnter / MouseLeave storyboards on WindowContentPanel's opacity
        private void FadeContent(double to, double seconds)
        {
            _contentFade.Duration = TimeSpan.FromSeconds(seconds);
            WindowContentPanel.Opacity = to;
        }

        private void EngineOnOnStarted()
        {
            if (CurrentPomodoroPeriod > 0)
            {
                HidePeriodCompletedIndicator();
            }
            CurrentQuote = null; // Reset quote
        }

        private void _globalSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.StartsWith("Motivational"))
            {
                RaisePropertyChanged(nameof(UseLightTheme));
                RaisePropertyChanged(nameof(TimerForegroundColor));
                RaisePropertyChanged(nameof(TimerShadowColor));
                RaisePropertyChanged(nameof(TimerEffect));
                RaisePropertyChanged(nameof(WindowBackgroundColor));
                RaisePropertyChanged(nameof(WindowBackground2Color));
                RaisePropertyChanged(nameof(WindowShadowColor));
                RaisePropertyChanged(nameof(WindowShadowOpacity));
                RaisePropertyChanged(nameof(WindowEffect));
                RaisePropertyChanged(nameof(WindowForegroundColor));
            }
        }

        private void UpdateTime()
        {
            var min = CurrentTimeValue / 60;
            var sec = CurrentTimeValue % 60;
            CurrentTimeMinutesText = $"{min:00}";
            CurrentTimeSecondsText = $"{sec:00}";
        }

        private async void UpdateCompletedPomodoroCount()
        {
            var completed = await Task.Run(() => _pomodoroRepository.CompletedToday());
            CurrentPomodoroPeriod = completed;
            RaisePropertyChanged(nameof(CurrentPomodoroPeriod));
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
                UpdateTime();
            }
            else if (e.PropertyName == nameof(ViewModel.Engine.Phase))
            {
                NotifyPeriodChanged();
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

        private void ResetPomodoroPeriod()
        {
            CurrentTimeMinutesText = "00";
            CurrentTimeSecondsText = "00";
        }

        private void ShowPeriodCompletedIndicator()
        {
            _periodCompletedStarted = DateTime.UtcNow;
            _periodCompletedTimer.Start();
            RenderPeriodCompletedIndicator();
        }

        private void HidePeriodCompletedIndicator()
        {
            _periodCompletedTimer.Stop();
            PeriodCompletedIndicatorEl.Opacity = 0;
            PeriodCompletedIndicatorInsideWindowEl.Opacity = 0;
        }

        private void RenderPeriodCompletedIndicator()
        {
            var t = (DateTime.UtcNow - _periodCompletedStarted).TotalSeconds % 1.0;
            var visible = t >= 0.2 && t < 0.8;
            PeriodCompletedIndicatorEl.Opacity = visible ? 1 : 0;
            PeriodCompletedIndicatorInsideWindowEl.Opacity = visible ? 1 : 0;
        }

        private void NotifyPeriodChanged()
        {
            RaisePropertyChanged(nameof(CurrentPomodoroPeriod));
            RaisePropertyChanged(nameof(CurrentPeriodText));
            RaisePropertyChanged(nameof(CurrentPeriodTextSource));
            RaisePropertyChanged(nameof(CurrentPeriodIcon));
            RaisePropertyChanged(nameof(CanStop));
            RaisePropertyChanged(nameof(CanStart));
            RaisePropertyChanged(nameof(CanPause));
        }

        public bool CanStart => ViewModel.StartCommand.CanExecute(null);

        public bool CanStop => ViewModel.StopCommand.CanExecute(null);

        public bool CanPause => ViewModel.PauseCommand.CanExecute(null);

        /// <summary>Are we using light theme currently?</summary>
        public bool UseLightTheme => _settings.UseWhiteText;

        public IBrush AccentColor => Const.ToBrush("#FF0080");

        public double TimerForegroundOpacity => 0.3;

        public double TimerShadowOpacity => 0.6;

        public IBrush TimerForegroundColor => Const.ToBrush(UseLightTheme ? Const.COLOR_LIGHT_TIMER_FOREGROUND : Const.COLOR_DARK_TIMER_FOREGROUND);

        public Color TimerShadowColor => Const.ToColor(UseLightTheme ? Const.COLOR_LIGHT_TIMER_SHADOW : Const.COLOR_DARK_TIMER_SHADOW);

        /// <summary>StyleTextTime's DropShadowEffect: ShadowDepth 0, BlurRadius 10.</summary>
        public IEffect TimerEffect => new DropShadowEffect
        {
            OffsetX = 0,
            OffsetY = 0,
            BlurRadius = 10,
            Color = TimerShadowColor,
            Opacity = TimerShadowOpacity,
        };

        public IBrush WindowBackgroundColor => Const.ToBrush(UseLightTheme ? Const.COLOR_LIGHT_WINDOW_BACKGROUND : Const.COLOR_DARK_WINDOW_BACKGROUND);

        public IBrush WindowBackground2Color => Const.ToBrush(UseLightTheme ? Const.COLOR_LIGHT_WINDOW_BACKGROUND2 : Const.COLOR_DARK_WINDOW_BACKGROUND2);

        public IBrush WindowForegroundColor => Const.ToBrush(UseLightTheme ? Const.COLOR_LIGHT_WINDOW_FOREGROUND : Const.COLOR_DARK_WINDOW_FOREGROUND);

        public Color WindowShadowColor => Const.ToColor(UseLightTheme ? Const.COLOR_LIGHT_WINDOW_SHADOW : Const.COLOR_DARK_WINDOW_SHADOW);

        public double WindowShadowOpacity => UseLightTheme ? Const.COLOR_LIGHT_WINDOW_SHADOW_OPACITY : Const.COLOR_DARK_WINDOW_SHADOW_OPACITY;

        /// <summary>The card's DropShadowEffect: ShadowDepth 10 straight down (Direction 270), BlurRadius 30.</summary>
        public IEffect WindowEffect => new DropShadowEffect
        {
            OffsetX = 0,
            OffsetY = 10,
            BlurRadius = 30,
            Color = WindowShadowColor,
            Opacity = WindowShadowOpacity,
        };

        // The WPF theme reads these settings in seconds and showed them as minutes (300 for 5);
        // the captions here say minutes
        public int BreakTime => _baseSettings.BreakTime / 60;

        public int LongBreakTime => _baseSettings.LongBreakTime / 60;

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

        private Quote _currentQuote;

        public Quote CurrentQuote
        {
            get => _currentQuote ??= Quotes.GetRandomQuote();
            set => _currentQuote = value;
        }

        /// <summary>Current period motivation text</summary>
        public string CurrentPeriodText
        {
            get
            {
                switch (ViewModel.Engine.Phase)
                {
                    case PomodoroPhase.NotStarted:
                        return Strings.app_period_press_play_to_start;
                    case PomodoroPhase.BreakEnded:
                        return Strings.app_period_motivation_start_pomodoro;
                    case PomodoroPhase.WorkEnded:
                    case PomodoroPhase.Work:
                        return !string.IsNullOrWhiteSpace(CurrentQuote.Text) ? CurrentQuote.Text : Strings.app_period_pomodoro_caption_default;
                    case PomodoroPhase.Break:
                        if (ViewModel.Engine.Index == 4)
                        {
                            return string.Format(Strings.app_period_long_break_caption, LongBreakTime);
                        }
                        return string.Format(Strings.app_period_short_break_caption, BreakTime);
                    default:
                        return Strings.app_period_motivation_start_pomodoro;
                }
            }
        }

        /// <summary>Motivation text source</summary>
        public string CurrentPeriodTextSource
        {
            get
            {
                if (ViewModel.Engine.Phase == PomodoroPhase.Work)
                {
                    return !string.IsNullOrWhiteSpace(CurrentQuote.Source) ? CurrentQuote.Source : string.Empty;
                }
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
                        return ViewModel.Engine.Index == 4 ? Const.ICON_PERIOD_REST_LONG : Const.ICON_PERIOD_REST;
                    default:
                        return Const.ICON_PERIOD_STOPPED;
                }
            }
        }

        private void Minimize_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MainWindow_MouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            BeginDrag(e);
        }

        // Right click opens the settings, as in the YAPA 1.0 theme
        private void MainWindow_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Right && ViewModel.ShowSettingsCommand.CanExecute(null))
            {
                ViewModel.ShowSettingsCommand.Execute(null);
            }
        }

        private void ButtonExit_OnClick(object sender, RoutedEventArgs e)
        {
            CloseApp();
        }

        // Avalonia windows already have a PropertyChanged event (for Avalonia properties), so the
        // bindings to this window's plain properties go through an explicit INotifyPropertyChanged
        private PropertyChangedEventHandler _propertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
