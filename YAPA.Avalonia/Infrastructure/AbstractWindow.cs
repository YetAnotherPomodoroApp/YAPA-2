using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Infrastructure
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
        public const string Exit = "/exit";
    }

    /// <summary>
    /// Base for theme windows, the Avalonia counterpart of YAPA.WPF.Shared's AbstractWindow:
    /// a borderless, see-through, always-on-top window that is the application (IApplication).
    /// </summary>
    public abstract class AbstractWindow : Window, IApplication
    {
        private Action<ApplicationState> _stateChanged;
        private Action _closing;
        private Action _loaded;
        private bool _closeConfirmed;

        event Action<ApplicationState> IApplication.StateChanged { add => _stateChanged += value; remove => _stateChanged -= value; }
        event Action IApplication.Closing { add => _closing += value; remove => _closing -= value; }
        event Action IApplication.Loaded { add => _loaded += value; remove => _loaded -= value; }

        public IMainViewModel ViewModel { get; set; }

        protected AbstractWindow()
        {
        }

        protected AbstractWindow(IMainViewModel viewModel)
        {
            Title = "YAPA 2";
            ViewModel = viewModel;
            using (var icon = global::Avalonia.Platform.AssetLoader.Open(new Uri("avares://YAPA/Assets/pomo.ico")))
            {
                Icon = new WindowIcon(icon);
            }
            Opened += AbstractWindow_Opened;
            Closing += AbstractWindow_Closing;
            PropertyChanged += (s, e) =>
            {
                if (e.Property == WindowStateProperty)
                {
                    _stateChanged?.Invoke(GetAppState());
                }
            };
            SetUpScreenReaderSupport();
        }

        #region Screen reader and keyboard support, shared by all themes (as YAPA.WPF.Shared's AbstractWindow)

        private PomodoroPhase _announcedPhase;
        private bool _announcementQueued;
        private TextBlock _liveRegion;

        private void SetUpScreenReaderSupport()
        {
            if (ViewModel?.Engine == null)
            {
                return;
            }
            AutomationProperties.SetHelpText(this, "Pomodoro timer. Tab moves between the buttons. The Menu key or Shift+F10 opens a menu with every command. "
                + (OperatingSystem.IsMacOS() ? "Command" : "Control") + "+T reads the timer.");

            _announcedPhase = ViewModel.Engine.Phase;
            ViewModel.Engine.PropertyChanged += Engine_PropertyChangedForSpeech;

            var sayStatus = new ActionCommand(SayStatus);
            KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.T, KeyModifiers.Control), Command = sayStatus });
            KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.T, KeyModifiers.Meta), Command = sayStatus });

            ContextRequested += AbstractWindow_ContextRequested;
            // Shift+F10 and the Menu key open it on every platform (a handled key is not turned into a
            // context request again, so the menu opens once)
            AddHandler(KeyUpEvent, (s, e) =>
            {
                if ((e.Key == Key.F10 && e.KeyModifiers.HasFlag(KeyModifiers.Shift)) || e.Key == Key.Apps)
                {
                    e.Handled = true;
                    OpenKeyboardMenu();
                }
            }, global::Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
        }

        private void Engine_PropertyChangedForSpeech(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IPomodoroEngine.Phase) || _announcementQueued)
            {
                return;
            }

            // Stopping passes through "ended" on its way to "not started": wait until the phase has settled, then speak once
            _announcementQueued = true;
            Dispatcher.UIThread.Post(AnnouncePhase, DispatcherPriority.Background);
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

        private void SayStatus()
        {
            Announce(TimerSpeech.Status(ViewModel.Engine.Phase, ViewModel.Engine.Remaining, ViewModel.Engine.WorkTime));
        }

        /// <summary>The last thing <see cref="Announce"/> had a screen reader speak (for tests).</summary>
        public string LastAnnouncement => _liveRegion?.Text;

        /// <summary>
        /// Has a screen reader speak the text now: a live region nobody sees, whose text changes.
        /// </summary>
        protected void Announce(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            try
            {
                if (_liveRegion == null)
                {
                    if (!(Content is Panel root))
                    {
                        return;
                    }
                    _liveRegion = new TextBlock { Width = 1, Height = 1, Opacity = 0, IsHitTestVisible = false, Focusable = false };
                    AutomationProperties.SetLiveSetting(_liveRegion, AutomationLiveSetting.Assertive);
                    root.Children.Add(_liveRegion);
                }
                // The same words twice in a row are still spoken twice (Ctrl+T pressed again)
                if (_liveRegion.Text == text)
                {
                    text += " ";
                }
                _liveRegion.Text = text;
                AutomationProperties.SetName(_liveRegion, text);
            }
            catch
            {
                // Announcements are an extra; never let them get in the way of the timer
            }
        }

        private void AbstractWindow_ContextRequested(object sender, ContextRequestedEventArgs e)
        {
            // The menu is for the keyboard (Menu key, Shift+F10). A right click keeps doing what the theme decides.
            if (e.TryGetPosition(this, out _))
            {
                return;
            }
            e.Handled = true;
            OpenKeyboardMenu();
        }

        private void OpenKeyboardMenu()
        {
            var menu = new ContextMenu();
            void Add(string header, System.Windows.Input.ICommand command, string shortcut = null)
            {
                var item = new MenuItem { Header = header, IsEnabled = command?.CanExecute(null) == true };
                if (shortcut != null)
                {
                    item.InputGesture = KeyGesture.Parse(shortcut);
                }
                item.Click += (s, a) => Execute(command);
                menu.Items.Add(item);
            }
            Add("_Start", ViewModel.StartCommand);
            Add("_Pause", ViewModel.PauseCommand);
            Add("S_top", ViewModel.StopCommand);
            Add("S_kip", ViewModel.SkipCommand);
            Add("_Reset session", ViewModel.ResetCommand);
            menu.Items.Add(new Separator());
            Add("Read the _timer", new ActionCommand(SayStatus), OperatingSystem.IsMacOS() ? "Meta+T" : "Ctrl+T");
            Add("S_ettings", ViewModel.ShowSettingsCommand);
            menu.Items.Add(new Separator());
            Add("_Minimize", new ActionCommand(() => WindowState = WindowState.Minimized));
            Add("E_xit", new ActionCommand(CloseApp));
            KeyboardMenu = menu;
            try
            {
                menu.Open(this);
            }
            catch (InvalidOperationException)
            {
                // No popup window to put it in (headless test runs); the keyboard menu is an extra
            }
        }

        /// <summary>The menu the Menu key or Shift+F10 opened last (for tests).</summary>
        public ContextMenu KeyboardMenu { get; private set; }

        private sealed class ActionCommand : System.Windows.Input.ICommand
        {
            private readonly Action _action;

            public ActionCommand(Action action)
            {
                _action = action;
            }

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter) => _action();

            public event EventHandler CanExecuteChanged
            {
                add { }
                remove { }
            }
        }

        #endregion

        /// <summary>Settings shared by all theme windows: no frame, see-through, on top.</summary>
        protected void MakeTransparentOverlay()
        {
            WindowDecorations = WindowDecorations.None;
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaTitleBarHeightHint = -1;
            Background = global::Avalonia.Media.Brushes.Transparent;
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
            CanResize = false;
            Topmost = true;
            ShowInTaskbar = true;
            SizeToContent = SizeToContent.Manual;
        }

        private void AbstractWindow_Opened(object sender, EventArgs e)
        {
            // macOS ignores the transparency asked for before the native window exists;
            // clear the hint first so the same value counts as a change
            TransparencyLevelHint = Array.Empty<WindowTransparencyLevel>();
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };

            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                ProcessCommandLineArg(args.Last());
            }
            _stateChanged?.Invoke(GetAppState());
            _loaded?.Invoke();
        }

        public void CloseApp()
        {
            Close();
        }

        private async void AbstractWindow_Closing(object sender, WindowClosingEventArgs e)
        {
            if (!_closeConfirmed && ViewModel?.Engine?.IsRunning == true)
            {
                e.Cancel = true;
                var result = await MessageBox.Show(this,
                    $"Do you really want to close application ?{Environment.NewLine}{Environment.NewLine}Next time you start application, you can continue from this checkpoint.",
                    "Timer is running", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    _closeConfirmed = true;
                    Close();
                }
                return;
            }
            _closing?.Invoke();
        }

        /// <summary>Drag the window from anywhere, like DragMove() in the WPF themes.</summary>
        protected void BeginDrag(PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                try
                {
                    BeginMoveDrag(e);
                }
                catch
                {
                    // the pointer was released already
                }
            }
        }

        public IntPtr WindowHandle => TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

        double IApplication.Left
        {
            get => Position.X;
            set => Position = new PixelPoint((int)value, Position.Y);
        }

        double IApplication.Top
        {
            get => Position.Y;
            set => Position = new PixelPoint(Position.X, (int)value);
        }

        public ApplicationState AppState
        {
            get => GetAppState();
            set => WindowState = value switch
            {
                ApplicationState.Minimized => WindowState.Minimized,
                ApplicationState.Maximized => WindowState.Maximized,
                _ => WindowState.Normal,
            };
        }

        private ApplicationState GetAppState()
        {
            return WindowState switch
            {
                WindowState.Minimized => ApplicationState.Minimized,
                WindowState.Maximized => ApplicationState.Maximized,
                WindowState.FullScreen => ApplicationState.Maximized,
                _ => ApplicationState.Normal,
            };
        }

        public bool ProcessCommandLineArg(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return false;
            }
            switch (args.ToLowerInvariant())
            {
                case CommandLineArguments.Start:
                    Execute(ViewModel.StartCommand);
                    break;
                case CommandLineArguments.Reset:
                    Execute(ViewModel.ResetCommand);
                    break;
                case CommandLineArguments.Stop:
                    Execute(ViewModel.StopCommand);
                    break;
                case CommandLineArguments.Pause:
                    Execute(ViewModel.PauseCommand);
                    break;
                case CommandLineArguments.Settings:
                    Execute(ViewModel.ShowSettingsCommand);
                    break;
                case CommandLineArguments.Skip:
                    Execute(ViewModel.SkipCommand);
                    break;
                case CommandLineArguments.HomePage:
                    OpenUrl("https://github.com/YetAnotherPomodoroApp/YAPA-2/");
                    break;
                case CommandLineArguments.Exit:
                    // closes as the x button does (asks first if the timer is running)
                    CloseApp();
                    break;
            }
            return true;
        }

        private static void Execute(System.Windows.Input.ICommand command)
        {
            if (command != null && command.CanExecute(null))
            {
                command.Execute(null);
            }
        }

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // no browser or mail client
            }
        }
    }
}
