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
    public static class CommandLineArguments
    {
        public const string Start = "/start";
        public const string Stop = "/stop";
        public const string Reset = "/reset";
        public const string Pause = "/pause";
        public const string Skip = "/skip";
        public const string Settings = "/settings";
        public const string HomePage = "/homepage";
    }

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
            CreateJumpList();

            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                ProcessCommandLineArg(args.Last());
            }

            AbstractWindow_StateChanged(this, EventArgs.Empty);
            Loaded?.Invoke();
        }

        public void CloseApp()
        {
            Close();
        }

        private void AbstractWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ViewModel.Engine.IsRunning)
            {
                var messageBoxResult = MessageBox.Show( $"Do you really want to close application ?{Environment.NewLine}{Environment.NewLine}Next time you start application, you can continue from this checkpoint.", "Timer is running", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.No)
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
            get => GetAppState();
            set => SetAppState(value);
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
                case CommandLineArguments.Start:
                    if (ViewModel.StartCommand.CanExecute(null))
                    {
                        ViewModel.StartCommand.Execute(null);
                    }
                    break;
                case CommandLineArguments.Reset:
                    if (ViewModel.ResetCommand.CanExecute(null))
                    {
                        ViewModel.ResetCommand.Execute(null);
                    }
                    break;
                case CommandLineArguments.Stop:
                    if (ViewModel.StopCommand.CanExecute(null))
                    {
                        ViewModel.StopCommand.Execute(null);
                    }
                    break;
                case CommandLineArguments.Pause:
                    if (ViewModel.PauseCommand.CanExecute(null))
                    {
                        ViewModel.PauseCommand.Execute(null);
                    }
                    break;
                case CommandLineArguments.Settings:
                    if (ViewModel.ShowSettingsCommand.CanExecute(null))
                    {
                        ViewModel.ShowSettingsCommand.Execute(null);
                    }
                    break;
                case CommandLineArguments.Skip:
                    if (ViewModel.SkipCommand.CanExecute(null))
                    {
                        ViewModel.SkipCommand.Execute(null);
                    }
                    break;
                case CommandLineArguments.HomePage:
                    Process.Start("https://github.com/YetAnotherPomodoroApp/YAPA-2/");
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
                Arguments = CommandLineArguments.Start,
                IconResourceIndex = 1
            };
            jumpList.JumpItems.Add(startTask);

            var resetTask = new JumpTask
            {
                Title = "Start from the beginning",
                Description = "Start new Pomodoro session",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = CommandLineArguments.Reset,
                IconResourceIndex = 4
            };
            jumpList.JumpItems.Add(resetTask);

            var pauseTask = new JumpTask
            {
                Title = "Pause",
                Description = "Pause Pomodoro session",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = CommandLineArguments.Pause,
                IconResourceIndex = 2
            };
            jumpList.JumpItems.Add(pauseTask);

            var stopTask = new JumpTask
            {
                Title = "Stop",
                Description = "Stop Pomodoro session",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = CommandLineArguments.Stop,
                IconResourceIndex = 2
            };
            jumpList.JumpItems.Add(stopTask);

            var skipTask = new JumpTask
            {
                Title = "Skip",
                Description = "Skip break and start working",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = CommandLineArguments.Skip,
                IconResourceIndex = 2
            };
            jumpList.JumpItems.Add(skipTask);

            var settingsTask = new JumpTask
            {
                Title = "Settings",
                Description = "Show YAPA settings",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = CommandLineArguments.Settings,
                IconResourceIndex = 5
            };
            jumpList.JumpItems.Add(settingsTask);

            var homepageTask = new JumpTask
            {
                Title = "Visit home page",
                Description = "Go to YAPA home page",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = CommandLineArguments.HomePage,
                IconResourceIndex = 6
            };
            jumpList.JumpItems.Add(homepageTask);

            jumpList.Apply();
        }

    }
}