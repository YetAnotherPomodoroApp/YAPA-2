using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;
using YAPA.Contracts;
using YAPA.Shared;
using WindowState = System.Windows.WindowState;

namespace YAPA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IApplication
    {
        private ItemRepository _itemRepository;

        public IPomodoroEngine Engine { get; set; }

        public ICommand StopCommand { get; set; }
        public ICommand StartCommand { get; set; }
        public ICommand ResetCommand { get; set; }

        // For INCP
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow(IPomodoroEngine engine)
        {
            Engine = engine;

            StopCommand = new StopCommand(Engine);
            StartCommand = new StartCommand(Engine);
            ResetCommand = new ResetCommand(Engine);

            _itemRepository = new ItemRepository();


            DataContext = this;

            // enable dragging
            MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;

            // save window position on close
            base.Closing += MainWindow_Closing;

            base.Loaded += MainWindow_Loaded;
            base.StateChanged += MainWindow_StateChanged;

            base.ShowInTaskbar = Properties.Settings.Default.ShowInTaskbar;

            InitializeComponent();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            StateChanged?.Invoke(GetAppState());
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Closing?.Invoke();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CreateJumpList();

            //if you want to handle to command line args on the first instance you may want it to go here
            //or in the app.xaml.cs
            //ProcessCommandLineArgs(SingleInstance<App>.CommandLineArgs);

            Loaded?.Invoke();
        }

        private System.Windows.Forms.MenuItem[] CreateNotifyIconContextMenu()
        {
            var startTask = new System.Windows.Forms.MenuItem { Text = "Start" };
            startTask.Click += (o, s) => { ProcessCommandLineArgs(string.Empty, "/start"); };

            var pauseTask = new System.Windows.Forms.MenuItem { Text = "Pause" };
            pauseTask.Click += (o, s) => { ProcessCommandLineArgs(string.Empty, "/pause"); };

            var stopTask = new System.Windows.Forms.MenuItem { Text = "Restart" };
            stopTask.Click += (o, s) => { ProcessCommandLineArgs(string.Empty, "/restart"); };

            var resetTask = new System.Windows.Forms.MenuItem { Text = "Reset session" };
            resetTask.Click += (o, s) => { ProcessCommandLineArgs(string.Empty, "/reset"); };

            return new System.Windows.Forms.MenuItem[]
            {
                startTask, pauseTask, stopTask, resetTask
            };
        }


        private void CreateJumpList()
        {
            var jumpList = new JumpList();
            JumpList.SetJumpList(Application.Current, jumpList);

            var startTask = new JumpTask();
            startTask.Title = "Start";
            startTask.Description = "Start Pomodoro session";
            startTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            startTask.Arguments = "/start";
            startTask.IconResourceIndex = 7;
            jumpList.JumpItems.Add(startTask);

            var pauseTask = new JumpTask();
            pauseTask.Title = "Pause";
            pauseTask.Description = "Pause Pomodoro session";
            pauseTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            pauseTask.Arguments = "/pause";
            pauseTask.IconResourceIndex = 3;
            jumpList.JumpItems.Add(pauseTask);

            var stopTask = new JumpTask();
            stopTask.Title = "Restart";
            stopTask.Description = "Restart current Pomodori session";
            stopTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            stopTask.Arguments = "/restart";
            stopTask.IconResourceIndex = 4;
            jumpList.JumpItems.Add(stopTask);

            var resetTask = new JumpTask();
            resetTask.Title = "Start from the beginning";
            resetTask.Description = "Start new Pomodoro session";
            resetTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            resetTask.Arguments = "/reset";
            resetTask.IconResourceIndex = 2;
            jumpList.JumpItems.Add(resetTask);

            var settingsTask = new JumpTask();
            settingsTask.Title = "Settings";
            settingsTask.Description = "Show YAPA settings";
            settingsTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            settingsTask.Arguments = "/settings";
            settingsTask.IconResourceIndex = 5;
            jumpList.JumpItems.Add(settingsTask);

            var homepageTask = new JumpTask();
            homepageTask.Title = "Visit home page";
            homepageTask.Description = "Go to YAPA home page";
            homepageTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            homepageTask.Arguments = "/homepage";
            homepageTask.IconResourceIndex = 6;
            jumpList.JumpItems.Add(homepageTask);

            jumpList.Apply();
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                base.OnMouseLeftButtonDown(e);
                DragMove();
                e.Handled = true;
            }
            catch
            {
                // ignored
            }
        }

        private void RaisePropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        public bool ProcessCommandLineArgs(params string[] args)
        {
            if (args == null || args.Length == 0)
                return true;

            if ((args.Length > 1))
            {
                //the first index always contains the location of the exe so we need to check the second index
                if ((args[1].ToLowerInvariant() == "/start"))
                {

                }
                else if ((args[1].ToLowerInvariant() == "/pause"))
                {

                }
                else if ((args[1].ToLowerInvariant() == "/restart"))
                {

                }
                else if ((args[1].ToLowerInvariant() == "/reset"))
                {
                }
                else if ((args[1].ToLowerInvariant() == "/settings"))
                {

                }
                else if ((args[1].ToLowerInvariant() == "/homepage"))
                {
                    Process.Start("http://lukaszbanasiak.github.io/YAPA/");
                }
            }

            return true;
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            //if (_stopWatch.IsRunning)
            {
                if (MessageBox.Show("Are you sure you want to exit and cancel pomodoro ?", "Cancel pomodoro", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    return;
                }
            }
            Close();
        }

        private void Minimize_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MainWindow_OnMouseEnter(object sender, MouseEventArgs e)
        {
            //MinExitPanel.Visibility = Visibility.Visible;
        }

        private void MainWindow_OnMouseLeave(object sender, MouseEventArgs e)
        {
            //MinExitPanel.Visibility = Visibility.Hidden;
        }

        public void ShowInTaskbar(bool show)
        {
            base.ShowInTaskbar = show;
        }

        public event Action<ApplicationState> StateChanged;
        public event Action Closing;
        public event Action Loaded;

        public IntPtr WindowHandle => new WindowInteropHelper(this).Handle;

        public ApplicationState AppState
        {
            get { return GetAppState(); }
            set { SetAppState(value); }
        }

        private ApplicationState GetAppState()
        {
            ApplicationState state;

            switch (WindowState)
            {
                case WindowState.Minimized:
                    state = ApplicationState.Minimized;
                    break;
                case WindowState.Maximized:
                    state = ApplicationState.Maximized;
                    break;
                case WindowState.Normal:
                    state = ApplicationState.Normal;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return state;
        }

        private void SetAppState(ApplicationState state)
        {

            switch (state)
            {
                case ApplicationState.Minimized:
                    WindowState = WindowState.Minimized;
                    break;
                case ApplicationState.Maximized:
                    WindowState = WindowState.Maximized;
                    break;
                case ApplicationState.Normal:
                    WindowState = WindowState.Normal;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}
