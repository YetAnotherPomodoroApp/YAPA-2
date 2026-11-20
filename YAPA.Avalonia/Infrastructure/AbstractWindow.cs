using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
        }

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
