using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;
using YAPA.Shared.Contracts;

namespace YAPA.Shared
{
    public abstract class AbstractWindow : Window, IApplication
    {
        public new event Action<ApplicationState> StateChanged;
        public new event Action Closing;
        public new event Action Loaded;

        public IMainViewModel ViewModel { get; set; }

        protected AbstractWindow()
        {
        }

        protected AbstractWindow(IMainViewModel viewModel)
        {
            Title = "YAPA 2";
            ViewModel = viewModel;
            base.StateChanged += AbstractWindow_StateChanged;
            base.Closing += AbstractWindow_Closing;
            base.Loaded += AbstractWindow_Loaded;
        }

        private void AbstractWindow_StateChanged(object sender, EventArgs e)
        {
            StateChanged?.Invoke(GetAppState());
        }

        private void AbstractWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded?.Invoke();
            CreateJumpList();

            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                ProcessCommandLineArg(args.Last());
            }

            AbstractWindow_StateChanged(this, EventArgs.Empty);
        }

        public void CloseApp()
        {
            Close();
        }

        private void AbstractWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ViewModel.Engine?.Phase == PomodoroPhase.Work)
            {
                if (MessageBox.Show("Are you sure you want to exit and cancel pomodoro ?", "Cancel pomodoro", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            Closing?.Invoke();
        }

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


        public bool ProcessCommandLineArg(string args)
        {
            if (string.IsNullOrEmpty(args))
                return false;

            var command = args.ToLowerInvariant();
            switch (command)
            {
                case "/start":
                    if (ViewModel.StartCommand.CanExecute(null))
                    {
                        ViewModel.StartCommand.Execute(null);
                    }
                    break;
                case "/reset":
                    if (ViewModel.ResetCommand.CanExecute(null))
                    {
                        ViewModel.ResetCommand.Execute(null);
                    }
                    break;
                case "/stop":
                    if (ViewModel.StopCommand.CanExecute(null))
                    {
                        ViewModel.StopCommand.Execute(null);
                    }
                    break;
                case "/pause":
                    if (ViewModel.PauseCommand.CanExecute(null))
                    {
                        ViewModel.StopCommand.Execute(null);
                    }
                    break;
                case "/settings":
                    if (ViewModel.ShowSettingsCommand.CanExecute(null))
                    {
                        ViewModel.ShowSettingsCommand.Execute(null);
                    }
                    break;
                case "/homepage":
                    Process.Start("http://lukaszbanasiak.github.io/YAPA/");
                    break;
            }

            return true;
        }

        private void CreateJumpList()
        {
            var jumpList = new JumpList();
            JumpList.SetJumpList(Application.Current, jumpList);

            var startTask = new JumpTask
            {
                Title = "Start",
                Description = "Start Pomodoro session",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = "/start",
                IconResourceIndex = 1
            };
            jumpList.JumpItems.Add(startTask);

            var resetTask = new JumpTask
            {
                Title = "Start from the beginning",
                Description = "Start new Pomodoro session",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = "/reset",
                IconResourceIndex = 4
            };
            jumpList.JumpItems.Add(resetTask);

            var pauseTask = new JumpTask
            {
                Title = "Pause",
                Description = "Pause Pomodoro session",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = "/pause",
                IconResourceIndex = 2
            };
            jumpList.JumpItems.Add(pauseTask);

            var stopTask = new JumpTask
            {
                Title = "Stop",
                Description = "Stop Pomodoro session",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = "/stop",
                IconResourceIndex = 2
            };
            jumpList.JumpItems.Add(stopTask);

            var settingsTask = new JumpTask
            {
                Title = "Settings",
                Description = "Show YAPA settings",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = "/settings",
                IconResourceIndex = 5
            };
            jumpList.JumpItems.Add(settingsTask);

            var homepageTask = new JumpTask
            {
                Title = "Visit home page",
                Description = "Go to YAPA home page",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = "/homepage",
                IconResourceIndex = 6
            };

            jumpList.Apply();
        }

    }
}