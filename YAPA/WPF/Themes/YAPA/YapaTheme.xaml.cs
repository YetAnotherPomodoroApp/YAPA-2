using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using YAPA.Contracts;
using YAPA.Shared;
using YAPA.WPF.Plugins;
using YAPA.WPF.Themes.YAPA;

namespace YAPA
{
    public partial class YapaTheme : AbstractWindow, INotifyPropertyChanged
    {
        private IMainViewModel ViewModel { get; set; }
        private YapaThemeSettings Settings { get; set; }
        private readonly IPomodoroEngine _engine;
        private readonly ISettings _globalSettings;
        private readonly Dashboard _dashboard;

        private int PomodorosCompleted { get; set; }

        private Storyboard TimerFlush;

        public YapaTheme(IMainViewModel viewModel, YapaThemeSettings settings, IPomodoroEngine engine, ISettings globalSettings, Dashboard dashboard) : base(viewModel)
        {
            ViewModel = viewModel;
            Settings = settings;
            _engine = engine;
            _globalSettings = globalSettings;
            _dashboard = dashboard;
            InitializeComponent();
            TimerFlush = (Storyboard)TryFindResource("FlashTimer");
            PomodorosCompleted = 0;

            ViewModel.Engine.PropertyChanged += Engine_PropertyChanged;
            ViewModel.Engine.OnPomodoroCompleted += Engine_OnPomodoroCompleted;
            ViewModel.Engine.OnStarted += Engine_OnStarted;
            ViewModel.Engine.OnStopped += Engine_OnStopped;
            _globalSettings.PropertyChanged += _globalSettings_PropertyChanged;

            DataContext = this;

            UpdateCompletedPomodoroCount();
        }


        private async void UpdateCompletedPomodoroCount()
        {
            await Task.Run(() =>
            {
                PomodorosCompleted = _dashboard.CompletedToday();
                RaisePropertyChanged(nameof(PomodorosCompleted));
                //Dispatcher.Invoke(() =>
                //{
                //    RaisePropertyChanged(nameof(PomodorosCompleted));
                //});

            });
        }

        private void Engine_OnStopped()
        {
            TimerFlush.Stop(this);
        }

        private void Engine_OnStarted()
        {
            TimerFlush.Stop(this);
        }

        private void Engine_OnPomodoroCompleted()
        {
            TimerFlush.Begin(this, true);
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
            }
        }

        private void Engine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.Engine.Elapsed))
            {
                RaisePropertyChanged(nameof(CurrentTimeValue));
                RaisePropertyChanged(nameof(ProgressValue));
            }
            else if (e.PropertyName == nameof(ViewModel.Engine.Phase))
            {
                RaisePropertyChanged(nameof(ProgressState));
            }
        }

        public Brush TextBrush => Settings.UseWhiteText ? Brushes.White : Brushes.Black;

        public Color TextShadowColor
        {
            get
            {
                var shadowColor = Colors.White;

                if (TextBrush.ToString() == Brushes.White.ToString())
                {
                    shadowColor = Colors.Black;
                }
                else
                {
                    shadowColor = Colors.White;
                }

                return shadowColor;
            }
        }

        public Brush MouseOverBackgroundColor
        {
            get
            {
                var mouseOverBackgroundColor = Brushes.White;

                if (TextBrush.ToString() == Brushes.White.ToString())
                {
                    mouseOverBackgroundColor = Brushes.Black;
                }
                else
                {
                    mouseOverBackgroundColor = Brushes.White;
                }

                return mouseOverBackgroundColor;
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
                        progress = (double)elapsed / (ViewModel.Engine.WorkTime * 60);
                        break;
                    case PomodoroPhase.Break:
                    case PomodoroPhase.BreakEnded:
                        progress = (double)elapsed / (ViewModel.Engine.BreakTime * 60);
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
                    case PomodoroPhase.Break:
                        progressState = "Normal";
                        break;
                    case PomodoroPhase.WorkEnded:
                    case PomodoroPhase.BreakEnded:
                        progressState = "Paused";
                        break;
                }
                return progressState;
            }
        }

        public int CurrentTimeValue
        {
            get
            {
                if (Settings.CountBackwards)
                {
                    var total = 0;
                    switch (ViewModel.Engine.Phase)
                    {
                        case PomodoroPhase.WorkEnded:
                        case PomodoroPhase.Work:
                            total = ViewModel.Engine.WorkTime;
                            break;
                        case PomodoroPhase.Break:
                        case PomodoroPhase.BreakEnded:
                            total = ViewModel.Engine.BreakTime;
                            break;
                    }
                    return total * 60 - ViewModel.Engine.Elapsed;
                }
                else
                {
                    return ViewModel.Engine.Elapsed;
                }
            }
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

        private void Minimize_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MainWindow_OnMouseEnter(object sender, MouseEventArgs e)
        {
            MinExitPanel.Visibility = Visibility.Visible;
        }

        private void MainWindow_OnMouseLeave(object sender, MouseEventArgs e)
        {
            MinExitPanel.Visibility = Visibility.Hidden;
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TimerFlush.Stop(this);
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
