using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using YAPA.Shared;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;
using YAPA.WPF.Themes.YAPA;

namespace YAPA
{
    public partial class YapaTheme : AbstractWindow, INotifyPropertyChanged
    {

        private readonly double _sizeRatio = 60 / 130.0;

        private YapaThemeSettings Settings { get; }
        private readonly IPomodoroRepository _pomodoroRepository;
        private readonly PomodoroEngineSettings _engineSettings;

        public int PomodorosCompleted { get; set; }

        private readonly Storyboard TimerFlush;
        private readonly Storyboard AfterBreakTimerFlush;

        public YapaTheme(IMainViewModel viewModel, YapaThemeSettings settings, IPomodoroEngine engine, ISettings globalSettings, IPomodoroRepository pomodoroRepository, PomodoroEngineSettings engineSettings) : base(viewModel)
        {
            ViewModel = viewModel;
            Settings = settings;
            _pomodoroRepository = pomodoroRepository;
            _engineSettings = engineSettings;

            InitializeComponent();

            TimerFlush = (Storyboard)TryFindResource("FlashTimer");
            AfterBreakTimerFlush = (Storyboard)TryFindResource("AfterBreakFlashTimer");

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

            PropertyChanged += YapaTheme_PropertyChanged;
            UpdateDisplayedTime();
        }

        private void UpdateDisplayedTime()
        {
            var minutes = CurrentTimeValue / 60;
            var seconds = CurrentTimeValue % 60;
            CurrentTimeMinutes.Text = $"{minutes / 10:0}";
            CurrentTimeMinutes2.Text = $"{minutes % 10:0}";
            CurrentTimeSeconds.Text = $"{seconds / 10:0}";
            CurrentTimeSeconds2.Text = $"{seconds % 10:0}";
        }

        private void YapaTheme_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurrentTimeValue))
            {
                UpdateDisplayedTime();
            }
        }

        private void HideSeconds()
        {
            CurrentTime.ColumnDefinitions.Clear();
            CurrentTime.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            CurrentTime.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });

            SecondsVisible = Visibility.Collapsed;
            RaisePropertyChanged(nameof(SecondsVisible));
        }

        private void ShowSeconds()
        {
            CurrentTime.ColumnDefinitions.Clear();
            CurrentTime.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            CurrentTime.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            CurrentTime.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            CurrentTime.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            CurrentTime.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });

            SecondsVisible = Visibility.Visible;
            RaisePropertyChanged(nameof(SecondsVisible));
        }


        private Visibility _secondsVisibility = Visibility.Visible;
        public Visibility SecondsVisible
        {
            get => _secondsVisibility;
            set => _secondsVisibility = value;
        }

        public double ClockOpacity => Settings.ClockOpacity;
        public double ShadowOpacity => Settings.ShadowOpacity;

        private async void UpdateCompletedPomodoroCount()
        {
            await Task.Run(() =>
            {
                var today = DateTime.Today.Date;
                PomodorosCompleted = _pomodoroRepository.Pomodoros.Where(x => x.DateTime == today).Select(a => a.Count).DefaultIfEmpty(0).Sum();
                RaisePropertyChanged(nameof(PomodorosCompleted));
            });
        }

        public SolidColorBrush FlashingColor
        {
            get
            {
                if (ViewModel.Engine.Phase == PomodoroPhase.WorkEnded)
                {
                    return Brushes.Tomato;
                }
                else if (ViewModel.Engine.Phase == PomodoroPhase.BreakEnded)
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
                TimerFlush.Stop(this);
                AfterBreakTimerFlush.Stop(this);
            }
            else
            {
                CurrentTime.Background = Brushes.Transparent;
            }
        }

        private void StartAnimation()
        {
            Storyboard animationToStart = null;

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
                animationToStart.Begin(this, true);
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
            if (e.PropertyName.StartsWith($"{nameof(YapaTheme)}"))
            {
                RaisePropertyChanged(nameof(TextBrush));
                RaisePropertyChanged(nameof(TextShadowColor));
                RaisePropertyChanged(nameof(MouseOverBackgroundColor));
                RaisePropertyChanged(nameof(Settings.ClockOpacity));
                RaisePropertyChanged(nameof(Settings.ShadowOpacity));

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
                    if (Settings.HideSeconds)
                    {
                        HideSeconds();
                    }
                    else
                    {
                        ShowSeconds();
                    }
                }

            }
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
                RaisePropertyChanged(nameof(ProgressState));
                UpdateStatusText();
                StartAnimation();
            }
        }

        private string _statusText;

        public string Status
        {
            get { return _statusText; }
            set
            {
                if (Settings.ShowStatusText)
                {
                    _statusText = value;
                }
                else
                {
                    _statusText = String.Empty;
                }
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
                    Start.Visibility = Visibility.Visible;
                    Stop.Visibility = Visibility.Collapsed;
                    Pause.Visibility = Visibility.Collapsed;
                    Skip.Visibility = Visibility.Collapsed;
                    break;
                case PomodoroPhase.WorkEnded:
                    Start.Visibility = Visibility.Visible;
                    Stop.Visibility = Visibility.Collapsed;
                    Pause.Visibility = Visibility.Collapsed;
                    Skip.Visibility = Visibility.Visible;
                    break;
                case PomodoroPhase.BreakEnded:
                    Start.Visibility = Visibility.Visible;
                    Stop.Visibility = Visibility.Collapsed;
                    Pause.Visibility = Visibility.Collapsed;
                    Skip.Visibility = Visibility.Collapsed;
                    break;
                case PomodoroPhase.Work:
                    Start.Visibility = Visibility.Collapsed;
                    Stop.Visibility = Visibility.Visible;
                    Pause.Visibility = Visibility.Visible;
                    Skip.Visibility = Visibility.Collapsed;
                    break;
                case PomodoroPhase.Break:
                    Start.Visibility = Visibility.Collapsed;
                    Stop.Visibility = Visibility.Visible;
                    Skip.Visibility = Visibility.Collapsed;
                    break;
                case PomodoroPhase.Pause:
                    Start.Visibility = Visibility.Visible;
                    Stop.Visibility = Visibility.Visible;
                    Pause.Visibility = Visibility.Collapsed;
                    Skip.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        public Brush TextBrush
        {
            get
            {
                Color color;
                switch (Settings.ThemeColors)
                {
                    case ThemeColors.White:
                        color = Colors.White;
                        break;
                    case ThemeColors.Black:
                        color = Colors.Black;
                        break;
                    case ThemeColors.Custom:
                        color = Settings.TextColor;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return new SolidColorBrush(color);
            }
        }
        //Settings.UseWhiteText ? Brushes.LightGray : Brushes.Black;

        public Color TextShadowColor
        {
            get
            {
                Color color;
                switch (Settings.ThemeColors)
                {
                    case ThemeColors.White:
                        color = Colors.Black;
                        break;
                    case ThemeColors.Black:
                        color = Colors.White;
                        break;
                    case ThemeColors.Custom:
                        color = Settings.ShadowColor;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return color;
            }
        }

        public Brush MouseOverBackgroundColor => new SolidColorBrush(TextShadowColor);

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
                    case PomodoroPhase.Pause:
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
                    case PomodoroPhase.Pause:
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

        private void Minimize_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }


        //When mouse is no longer over app, wait 2s and if mouse don't come back over app hide minmax panel
        //There has to be a better way to do it!!
        CancellationTokenSource cancelMinMaxPanelHide = new CancellationTokenSource();

        private void MainWindow_OnMouseEnter(object sender, MouseEventArgs e)
        {
            MinExitPanel.Visibility = Visibility.Visible;
            ButtonPanel.Visibility = Visibility.Visible;

            cancelMinMaxPanelHide.Cancel();
            cancelMinMaxPanelHide = new CancellationTokenSource();
        }

        private async void MainWindow_OnMouseLeave(object sender, MouseEventArgs e)
        {

            await Task.Delay(TimeSpan.FromSeconds(2), cancelMinMaxPanelHide.Token).ContinueWith(
                 x =>
                {
                    if (x.IsCanceled)
                    {
                        return;
                    }

                    Dispatcher.Invoke(() =>
                   {
                       MinExitPanel.Visibility = Visibility.Hidden;
                       if (Settings.HideButtons)
                       {
                           ButtonPanel.Visibility = Visibility.Hidden;
                       }

                   });
                    cancelMinMaxPanelHide = new CancellationTokenSource();
                });
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TimerFlush.Stop(this);
            AfterBreakTimerFlush.Stop(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            CloseApp();
        }
    }
}
