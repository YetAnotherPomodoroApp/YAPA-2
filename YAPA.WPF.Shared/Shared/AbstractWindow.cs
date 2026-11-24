using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Windows.Threading;
using YAPA.Shared.Common;
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

            SetUpScreenReaderSupport();
        }

        #region Screen reader and keyboard support, shared by all themes

        private PomodoroPhase _announcedPhase;
        private bool _announcementQueued;

        private void SetUpScreenReaderSupport()
        {
            //Read every time the window gets focus, so kept short; F1 has the rest
            AutomationProperties.SetHelpText(this, "Pomodoro timer. Control+T reads the timer, F1 for keyboard help.");

            _announcedPhase = ViewModel.Engine.Phase;
            ViewModel.Engine.PropertyChanged += Engine_PropertyChangedForSpeech;
            ViewModel.Engine.PropertyChanged += (sender, args) => UpdateTitle();
            UpdateTitle();

            InputBindings.Add(new KeyBinding(new ActionCommand(SayStatus), Key.T, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(new ActionCommand(SayKeyboardHelp), Key.F1, ModifierKeys.None));

            ContextMenu = new ContextMenu();
            ContextMenuOpening += AbstractWindow_ContextMenuOpening;
        }

        private void Engine_PropertyChangedForSpeech(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IPomodoroEngine.Phase) || _announcementQueued)
            {
                return;
            }

            //Stopping passes through "ended" on its way to "not started": wait until the phase has settled, then speak once
            _announcementQueued = true;
            Dispatcher.BeginInvoke(new Action(AnnouncePhase), DispatcherPriority.ContextIdle);
        }

        private void AnnouncePhase()
        {
            _announcementQueued = false;

            var phase = ViewModel.Engine.Phase;
            if (phase == _announcedPhase)
            {
                return;
            }

            var text = TimerSpeech.PhaseChanged(_announcedPhase, phase, ViewModel.Engine.Remaining);
            _announcedPhase = phase;

            Announce(text);
        }

        //The time left is in the title too, in whole minutes, so it changes once a minute
        private void UpdateTitle()
        {
            var title = TimerSpeech.WindowTitle(ViewModel.Engine.Phase, ViewModel.Engine.Remaining);
            if (Title != title)
            {
                Title = title;
            }
        }

        private string StatusText()
        {
            return TimerSpeech.Status(ViewModel.Engine.Phase, ViewModel.Engine.Remaining, ViewModel.Engine.WorkTime);
        }

        private void SayKeyboardHelp()
        {
            Announce(TimerSpeech.KeyboardHelp);
        }

        private void SayStatus()
        {
            Announce(StatusText());
        }

        private TextBlock _liveRegion;
        private TimerStatusBar _statusBar;

        //Screen readers have a key for "read the status bar" (NVDA: Insert+End, JAWS: Insert+Page Down).
        //The themes have no status bar, so add one nobody sees that says where the timer is
        private void AddStatusBar()
        {
            if (_statusBar != null || !(Content is Panel root))
            {
                return;
            }

            _statusBar = new TimerStatusBar(StatusText);
            root.Children.Add(_statusBar);
        }

        /// <summary>
        /// Has a screen reader speak the text now.
        /// </summary>
        protected void Announce(string text)
        {
            if (!Accessible.Notify(this, text))
            {
                Accessible.SpeakThroughLiveRegion(_liveRegion, text);
            }
        }

        private void AddLiveRegionWhereNotificationsAreMissing()
        {
            //Only needed on .NET Framework 4.8 without the 4.8.1 update; elsewhere nothing is added to the window
            if (Accessible.NotificationsSupported || _liveRegion != null || !(Content is Panel root))
            {
                return;
            }

            _liveRegion = Accessible.CreateLiveRegion();
            root.Children.Add(_liveRegion);
        }

        private void AbstractWindow_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            //The menu is for the keyboard (Menu key, Shift+F10). A right click keeps doing what the theme decides.
            var openedWithMouse = e.CursorLeft >= 0 || e.CursorTop >= 0;
            if (openedWithMouse)
            {
                e.Handled = true;
                return;
            }

            ContextMenu.Items.Clear();
            AddMenuItem("_Start", ViewModel.StartCommand);
            AddMenuItem("_Pause", ViewModel.PauseCommand);
            AddMenuItem("S_top", ViewModel.StopCommand);
            AddMenuItem("S_kip", ViewModel.SkipCommand);
            AddMenuItem("_Reset session", ViewModel.ResetCommand);
            ContextMenu.Items.Add(new Separator());
            AddMenuItem("Read the _timer", new ActionCommand(SayStatus), "Ctrl+T");
            AddMenuItem("Keyboard _help", new ActionCommand(SayKeyboardHelp), "F1");
            AddMenuItem("S_ettings", ViewModel.ShowSettingsCommand);
            ContextMenu.Items.Add(new Separator());
            AddMenuItem("_Minimize", new ActionCommand(() => WindowState = WindowState.Minimized));
            AddMenuItem("E_xit", new ActionCommand(Close));
        }

        private void AddMenuItem(string header, ICommand command, string shortcut = null)
        {
            var item = new MenuItem
            {
                Header = header,
                IsEnabled = command.CanExecute(null),
                InputGestureText = shortcut ?? string.Empty
            };
            item.Click += (sender, args) =>
            {
                if (command.CanExecute(null))
                {
                    command.Execute(null);
                }
            };
            ContextMenu.Items.Add(item);
        }

        private class ActionCommand : ICommand
        {
            private readonly Action _action;

            public ActionCommand(Action action)
            {
                _action = action;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                _action();
            }

            public event EventHandler CanExecuteChanged
            {
                add { }
                remove { }
            }
        }

        #endregion

        private void AbstractWindow_StateChanged(object sender, EventArgs e)
        {
            StateChanged?.Invoke(GetAppState());
        }

        private void AbstractWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AddLiveRegionWhereNotificationsAreMissing();
            AddStatusBar();
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