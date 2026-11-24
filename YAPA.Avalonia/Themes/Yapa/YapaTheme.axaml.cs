using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Themes.Yapa
{
    public partial class YapaTheme : AbstractWindow, INotifyPropertyChanged
    {
        private readonly double _sizeRatio = 60 / 130.0;
        private YapaThemeSettings Settings { get; }
        private readonly IPomodoroRepository _pomodoroRepository;
        private readonly PomodoroEngineSettings _engineSettings;
        private readonly IFontService _fontService;

        public int PomodorosCompleted { get; set; }

        private readonly FlashAnimation TimerFlush;
        private readonly FlashAnimation AfterBreakTimerFlush;

        public YapaTheme()
        {
            // for the XAML previewer
            InitializeComponent();
        }

        public YapaTheme(IMainViewModel viewModel, YapaThemeSettings settings, IPomodoroEngine engine, ISettings globalSettings, IPomodoroRepository pomodoroRepository, PomodoroEngineSettings engineSettings, IFontService fontService) : base(viewModel)
        {
            ViewModel = viewModel;
            Settings = settings;
            _pomodoroRepository = pomodoroRepository;
            _engineSettings = engineSettings;
            _fontService = fontService;

            InitializeComponent();
            MakeTransparentOverlay();
            // Clicks on a fully clear window fall through on some systems; WPF used #01000000 too
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));

            TimerFlush = new FlashAnimation(CurrentTime, Colors.Tomato);
            AfterBreakTimerFlush = new FlashAnimation(CurrentTime, Colors.MediumSeaGreen);

            PomodorosCompleted = 0;

            ViewModel.Engine.PropertyChanged += Engine_PropertyChanged;
            ViewModel.Engine.OnPomodoroCompleted += Engine_OnPomodoroCompleted;
            ViewModel.Engine.OnStarted += StopAnimation;
            ViewModel.Engine.OnStopped += StopAnimation;
            globalSettings.PropertyChanged += _globalSettings_PropertyChanged;

            DataContext = this;

            UpdateAppSize();
            PhaseChanged();
            UpdateStatusText();
            UpdateCompletedPomodoroCount();

            UpdateDisplayedTime();
            UpdateSecondVisibility();
            UpdateFontFamily(_engineSettings.FontFamily);

            PointerPressed += MainWindow_MouseLeftButtonDown;
            PointerReleased += MainWindow_PointerReleased;
            PointerEntered += MainWindow_OnMouseEnter;
            PointerExited += MainWindow_OnMouseLeave;
            DoubleTapped += Window_MouseDoubleClick;

            CurrentTime.SpokenText = () => TimerSpeech.Display(ViewModel.Engine.Phase, CurrentTimeValue, _engineSettings.CountBackwards);

            //The buttons hide when the mouse leaves; bring them back for someone working with the keyboard
            AddHandler(KeyDownEvent, (s, e) => ShowButtonsForKeyboard(), global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
            Deactivated += (s, e) => HideButtonsShownForKeyboard();
        }

        private bool _buttonsShownForKeyboard;

        private void ShowButtonsForKeyboard()
        {
            if (MinExitPanel.IsHitTestVisible && ButtonPanel.IsHitTestVisible)
            {
                return;
            }

            _buttonsShownForKeyboard = true;
            MainWindow_OnMouseEnter(this, null);
        }

        private void HideButtonsShownForKeyboard()
        {
            if (!_buttonsShownForKeyboard || IsPointerOver)
            {
                return;
            }

            _buttonsShownForKeyboard = false;
            MainWindow_OnMouseLeave(this, null);
        }

        private void UpdateDisplayedTime()
        {
            var minutes = CurrentTimeValue / 60;
            var seconds = CurrentTimeValue % 60;
            var thirdDigit = Math.Truncate(minutes / 100.0);

            CurrentTimeMinutes3.Text = thirdDigit.ToString();
            CurrentTimeMinutes3.IsVisible = thirdDigit > 0;
            CurrentTimeMinutes2.Text = Math.Truncate(minutes / 10.0 % 10).ToString();
            CurrentTimeMinutes.Text = Math.Truncate(minutes % 10.0).ToString();

            CurrentTimeSeconds2.Text = Math.Truncate(seconds / 10.0).ToString();
            CurrentTimeSeconds.Text = Math.Truncate(seconds % 10.0).ToString();

            if (!SecondsVisible && minutes == 0 && seconds > 0)
            {
                CurrentTimeMinutes2.Text = "<";
                CurrentTimeMinutes.Text = "1";
            }
        }

        private void YapaTheme_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurrentTimeValue))
            {
                UpdateDisplayedTime();
            }
        }

        private void UpdateSecondsVisibility(bool visible)
        {
            CurrentTimeSeconds.IsVisible = visible;
            CurrentTimeSeparator.IsVisible = visible;
            // In WPF the tens digit is bound to SecondsVisible in XAML, so it goes too
            CurrentTimeSeconds2.IsVisible = visible;
            SecondsVisible = visible;
            RaisePropertyChanged(nameof(SecondsVisible));
        }

        private void UpdateFontFamily(string fontName)
        {
            var font = new FontFamily(_fontService.GetFontPath(fontName));
            CurrentTimeSeconds.FontFamily = font;
            CurrentTimeSeconds2.FontFamily = font;
            CurrentTimeSeparator.FontFamily = font;
            CurrentTimeMinutes.FontFamily = font;
            CurrentTimeMinutes2.FontFamily = font;
            CurrentTimeMinutes3.FontFamily = font;
        }

        public bool SecondsVisible { get; set; } = true;

        public double ClockOpacity => Settings.ClockOpacity;

        public double ShadowOpacity => Settings.ShadowOpacity;

        private async void UpdateCompletedPomodoroCount()
        {
            await Task.Run(() =>
            {
                PomodorosCompleted = _pomodoroRepository.CompletedToday();
            });
            RaisePropertyChanged(nameof(PomodorosCompleted));
        }

        public IBrush FlashingColor
        {
            get
            {
                if (ViewModel.Engine.Phase == PomodoroPhase.WorkEnded)
                {
                    return Brushes.Tomato;
                }
                if (ViewModel.Engine.Phase == PomodoroPhase.BreakEnded)
                {
                    return Brushes.MediumSeaGreen;
                }
                return Brushes.Transparent;
            }
        }

        private void StopAnimation()
        {
            if (Settings.DisableFlashingAnimation == false)
            {
                TimerFlush.Stop();
                AfterBreakTimerFlush.Stop();
            }
            else
            {
                CurrentTime.Background = Brushes.Transparent;
            }
        }

        private void StartAnimation()
        {
            FlashAnimation animationToStart = null;
            if (ViewModel.Engine.Phase == PomodoroPhase.WorkEnded)
            {
                animationToStart = TimerFlush;
            }
            else if (ViewModel.Engine.Phase == PomodoroPhase.BreakEnded)
            {
                animationToStart = AfterBreakTimerFlush;
            }

            if (animationToStart == null)
            {
                return;
            }

            if (Settings.DisableFlashingAnimation == false)
            {
                animationToStart.Begin();
            }
            else
            {
                CurrentTime.Background = FlashingColor;
            }
        }

        private void Engine_OnPomodoroCompleted()
        {
            PomodorosCompleted++;
            RaisePropertyChanged(nameof(PomodorosCompleted));
        }

        private void _globalSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.StartsWith(nameof(YapaTheme)))
            {
                RaisePropertyChanged(nameof(TextBrush));
                RaisePropertyChanged(nameof(TextShadowColor));
                RaisePropertyChanged(nameof(MouseOverBackgroundColor));
                RaisePropertyChanged(nameof(ClockOpacity));
                RaisePropertyChanged(nameof(ShadowOpacity));
                RaisePropertyChanged(nameof(DigitEffect));
                RaisePropertyChanged(nameof(BackgroundBrush));
                RaisePropertyChanged(nameof(BackgroundVisible));
                RaisePropertyChanged(nameof(SmallTextEffect));

                if (e.PropertyName.StartsWith($"{nameof(YapaTheme)}.{nameof(YapaThemeSettings.Width)}"))
                {
                    UpdateAppSize();
                }
                if (e.PropertyName.StartsWith($"{nameof(YapaTheme)}.{nameof(YapaThemeSettings.ShowStatusText)}"))
                {
                    UpdateStatusText();
                }
                if (e.PropertyName.StartsWith($"{nameof(YapaTheme)}.{nameof(YapaThemeSettings.HideSeconds)}"))
                {
                    UpdateSecondVisibility();
                }
                if (e.PropertyName.StartsWith($"{nameof(YapaTheme)}.{nameof(YapaThemeSettings.HideButtons)}") && !Settings.HideButtons)
                {
                    SetHidden(ButtonPanel, false);
                }
            }

            if (e.PropertyName == $"{nameof(PomodoroEngine)}.{nameof(PomodoroEngineSettings.FontFamily)}")
            {
                UpdateFontFamily(_engineSettings.FontFamily);
            }
        }

        private void UpdateSecondVisibility()
        {
            UpdateSecondsVisibility(!Settings.HideSeconds);
            UpdateDisplayedTime();
        }

        private void UpdateAppSize()
        {
            Width = Settings.Width;
            Height = Settings.Width * _sizeRatio;
        }

        private void Engine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.Engine.Elapsed) || e.PropertyName == nameof(ViewModel.Engine.DisplayValue))
            {
                RaisePropertyChanged(nameof(CurrentTimeValue));
                RaisePropertyChanged(nameof(ProgressValue));
            }
            else if (e.PropertyName == nameof(ViewModel.Engine.Phase))
            {
                PhaseChanged();
                UpdateStatusText();
                StartAnimation();
            }
        }

        private string _statusText;

        public string Status
        {
            get => _statusText;
            set
            {
                _statusText = Settings.ShowStatusText ? value : string.Empty;
                RaisePropertyChanged(nameof(Status));
            }
        }

        private void UpdateStatusText()
        {
            switch (ViewModel.Engine.Phase)
            {
                case PomodoroPhase.NotStarted:
                    Status = "YAPA 2.0";
                    break;
                case PomodoroPhase.WorkEnded:
                    Status = "Work Ended";
                    break;
                case PomodoroPhase.BreakEnded:
                    Status = "Break Ended";
                    break;
                case PomodoroPhase.Work:
                    Status = "Work";
                    break;
                case PomodoroPhase.Break:
                    Status = "Break";
                    break;
                case PomodoroPhase.Pause:
                    Status = "Work Paused";
                    break;
            }
        }

        private void PhaseChanged()
        {
            switch (ViewModel.Engine.Phase)
            {
                case PomodoroPhase.NotStarted:
                    Start.IsVisible = true;
                    Stop.IsVisible = false;
                    Pause.IsVisible = false;
                    Skip.IsVisible = false;
                    break;
                case PomodoroPhase.WorkEnded:
                    Start.IsVisible = true;
                    Stop.IsVisible = false;
                    Pause.IsVisible = false;
                    Skip.IsVisible = true;
                    break;
                case PomodoroPhase.BreakEnded:
                    Start.IsVisible = true;
                    Stop.IsVisible = false;
                    Pause.IsVisible = false;
                    Skip.IsVisible = false;
                    break;
                case PomodoroPhase.Work:
                    Start.IsVisible = false;
                    Stop.IsVisible = true;
                    Pause.IsVisible = true;
                    Skip.IsVisible = false;
                    break;
                case PomodoroPhase.Break:
                    Start.IsVisible = false;
                    Stop.IsVisible = true;
                    Skip.IsVisible = false;
                    break;
                case PomodoroPhase.Pause:
                    Start.IsVisible = true;
                    Stop.IsVisible = true;
                    Pause.IsVisible = false;
                    Skip.IsVisible = false;
                    break;
            }
        }

        public IBrush TextBrush => new SolidColorBrush(Settings.TextColor);

        public Color TextShadowColor => Settings.ShadowColor;

        //The button under the mouse gets a background, half see-through rather than a hard block
        public IBrush MouseOverBackgroundColor => new SolidColorBrush(TextShadowColor, 0.6);

        /// <summary>The glow round each digit: WPF DropShadowEffect with ShadowDepth 0 and BlurRadius 10.</summary>
        public IEffect DigitEffect => new DropShadowEffect
        {
            OffsetX = 0,
            OffsetY = 0,
            BlurRadius = 10,
            Color = Settings.ShadowColor,
            Opacity = Settings.ShadowOpacity,
        };

        /// <summary>
        /// Optional plate behind the whole timer, to keep it readable on any wallpaper or window. Off by default.
        /// </summary>
        public IBrush BackgroundBrush => new SolidColorBrush(Settings.BackgroundColor, Settings.BackgroundOpacity);

        public bool BackgroundVisible => Settings.BackgroundOpacity > 0;

        /// <summary>
        /// The digits' shadow, for the status text, the buttons and the counter too, so they read on any
        /// background the way the digits do (a smaller blur for the smaller text).
        /// </summary>
        public IEffect SmallTextEffect => new DropShadowEffect { OffsetX = 0, OffsetY = 0, Color = Settings.ShadowColor, Opacity = Settings.ShadowOpacity, BlurRadius = 3 };

        public double ProgressValue
        {
            get
            {
                var elapsed = ViewModel.Engine.Elapsed;
                switch (ViewModel.Engine.Phase)
                {
                    case PomodoroPhase.WorkEnded:
                    case PomodoroPhase.Work:
                    case PomodoroPhase.Pause:
                        return (double)elapsed / ViewModel.Engine.WorkTime;
                    case PomodoroPhase.Break:
                    case PomodoroPhase.BreakEnded:
                        return (double)elapsed / ViewModel.Engine.BreakTime;
                }
                return 0;
            }
        }

        public int CurrentTimeValue => ViewModel.Engine.DisplayValue;

        public int CurrentIntervalLength => ViewModel.Engine.CurrentIntervalLength;

        private void MainWindow_MouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            BeginDrag(e);
        }

        // Right click opens the settings, as the WPF theme's MouseBinding did
        private void MainWindow_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Right && ViewModel.ShowSettingsCommand.CanExecute(null))
            {
                ViewModel.ShowSettingsCommand.Execute(null);
            }
        }

        private void Minimize_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // WPF "Hidden": invisible but still taking its space, so the timer does not move or rescale
        private static void SetHidden(Control control, bool hidden)
        {
            control.Opacity = hidden ? 0 : 1;
            control.IsHitTestVisible = !hidden;
        }

        //When mouse is no longer over app, wait 2s and if mouse don't come back over app hide minmax panel
        private CancellationTokenSource cancelMinMaxPanelHide = new CancellationTokenSource();

        private void MainWindow_OnMouseEnter(object sender, PointerEventArgs e)
        {
            SetHidden(MinExitPanel, false);
            SetHidden(ButtonPanel, false);
            cancelMinMaxPanelHide.Cancel();
            cancelMinMaxPanelHide = new CancellationTokenSource();
        }

        private async void MainWindow_OnMouseLeave(object sender, PointerEventArgs e)
        {
            var token = cancelMinMaxPanelHide.Token;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            Dispatcher.UIThread.Post(() =>
            {
                //Shown for the keyboard and still in use: they go when the window is left
                if (_buttonsShownForKeyboard && IsActive)
                {
                    return;
                }
                SetHidden(MinExitPanel, true);
                if (Settings.HideButtons)
                {
                    SetHidden(ButtonPanel, true);
                }
            });
            cancelMinMaxPanelHide = new CancellationTokenSource();
        }

        private void Window_MouseDoubleClick(object sender, TappedEventArgs e)
        {
            TimerFlush.Stop();
            AfterBreakTimerFlush.Stop();
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
            var args = new PropertyChangedEventArgs(propertyName);
            YapaTheme_PropertyChanged(this, args);
            _propertyChanged?.Invoke(this, args);
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            CloseApp();
        }
    }
}
