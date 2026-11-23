using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Plugins.MinimizeToTray
{
    public class SystemTrayPlugin : IPluginMeta
    {
        public string Title => "System tray";

        public string Id => "SystemTray";

        public Type Plugin => typeof(SystemTray);

        public Type Settings => typeof(SystemTraySettings);

        public Type SettingEditWindow => typeof(MinimizeToTraySettingWindow);
    }

    /// <summary>
    /// The WPF NotifyIcon plugin on Avalonia's TrayIcon: same menu, the minutes drawn into the
    /// icon in the work/break colour, and hide-on-minimize. Balloon tips become system notifications.
    /// </summary>
    public class SystemTray : IPlugin
    {
        private readonly IApplication _app;
        private readonly IMainViewModel _viewModel;
        private readonly SystemTraySettings _settings;
        private readonly TrayIcon _sysTrayIcon;
        private readonly WindowIcon _defaultIcon;
        private string _lastIconText;

        public SystemTray(IApplication app, IMainViewModel viewModel, SystemTraySettings settings, ISettings globalSettings)
        {
            _app = app;
            _viewModel = viewModel;
            _settings = settings;

            globalSettings.PropertyChanged += _globalSettings_PropertyChanged;
            _app.StateChanged += _app_StateChanged;

            using (var stream = AssetLoader.Open(new Uri("avares://YAPA/Assets/pomoTray.ico")))
            {
                _defaultIcon = new WindowIcon(stream);
            }

            _sysTrayIcon = new TrayIcon
            {
                ToolTipText = "YAPA 2",
                Icon = _defaultIcon,
                IsVisible = SystemTrayVisible(),
                Menu = new NativeMenu(),
            };
            _sysTrayIcon.Clicked += (s, e) => SysTrayIcon_DoubleClick();
            _sysTrayIcon.Menu.NeedsUpdate += (s, e) => CreateNotifyIconContextMenu();
            CreateNotifyIconContextMenu();

            var icons = TrayIcon.GetIcons(Application.Current) ?? new TrayIcons();
            icons.Add(_sysTrayIcon);
            TrayIcon.SetIcons(Application.Current, icons);

            _viewModel.Engine.PropertyChanged += _engine_PropertyChanged;
            UpdateTooltip();
            _app.Closing += () => _sysTrayIcon.Dispose();
        }

        private bool SystemTrayVisible()
        {
            return _settings.ShowInTaskbar == false || (_settings.MinimizeToTray && _app.AppState == ApplicationState.Minimized);
        }

        private void _globalSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == $"{nameof(SystemTray)}.{nameof(_settings.ShowInTaskbar)}")
            {
                _app.ShowInTaskbar = _settings.ShowInTaskbar;
            }

            if (e.PropertyName.StartsWith(nameof(SystemTray)))
            {
                _sysTrayIcon.IsVisible = SystemTrayVisible();
            }
        }

        private void _engine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Engine.Elapsed) || e.PropertyName == nameof(_viewModel.Engine.Phase))
            {
                UpdateTooltip();
            }

            if (e.PropertyName == nameof(_viewModel.Engine.Elapsed))
            {
                UpdateIcon();
            }
            else if (e.PropertyName == nameof(_viewModel.Engine.Phase))
            {
                if (_sysTrayIcon.IsVisible == false || _settings.ShowBalloonTipWhenInSystemTray == false)
                {
                    return;
                }

                var messageToShow = string.Empty;
                switch (_viewModel.Engine.Phase)
                {
                    case PomodoroPhase.Work:
                        messageToShow = $"Starting {_viewModel.Engine.WorkTime / 60}min work interval.";
                        break;
                    case PomodoroPhase.WorkEnded:
                        messageToShow = "Work interval ended.";
                        break;
                    case PomodoroPhase.Break:
                        messageToShow = $"Starting {_viewModel.Engine.BreakTime / 60}min break interval.";
                        break;
                    case PomodoroPhase.BreakEnded:
                        messageToShow = "Break interval ended.";
                        break;
                }

                if (!string.IsNullOrEmpty(messageToShow))
                {
                    Notifications.Show("YAPA 2", messageToShow);
                }
            }
        }

        private void CreateNotifyIconContextMenu()
        {
            var commands = new List<Tuple<string, ICommand>>
            {
                Tuple.Create("Start", _viewModel.StartCommand),
                Tuple.Create("Pause", _viewModel.PauseCommand),
                Tuple.Create("Stop", _viewModel.StopCommand),
                Tuple.Create("Skip", _viewModel.SkipCommand),
                Tuple.Create("Reset session", _viewModel.ResetCommand),
                Tuple.Create("Settings", _viewModel.ShowSettingsCommand),
            };

            var menu = _sysTrayIcon.Menu;
            menu.Items.Clear();
            foreach (var (title, command) in commands)
            {
                var item = new NativeMenuItem(title) { IsEnabled = command.CanExecute(null) };
                item.Click += (s, e) =>
                {
                    if (command.CanExecute(null))
                    {
                        command.Execute(null);
                    }
                };
                menu.Items.Add(item);
            }

            var close = new NativeMenuItem("Exit");
            close.Click += (s, e) => _app.CloseApp();
            menu.Items.Add(close);
        }

        private void _app_StateChanged(ApplicationState state)
        {
            if (state == ApplicationState.Minimized && (_settings.MinimizeToTray || _settings.ShowInTaskbar == false))
            {
                _app.Hide();
                _sysTrayIcon.IsVisible = true;
            }

            if (state != ApplicationState.Minimized)
            {
                _app.ShowInTaskbar = _settings.ShowInTaskbar;
            }
        }

        private void SysTrayIcon_DoubleClick()
        {
            _app.Show();
            _app.AppState = ApplicationState.Normal;
        }

        //The icon only fits the minutes; the tooltip says the same in words, which is also what a screen reader reads
        private void UpdateTooltip()
        {
            var engine = _viewModel.Engine;
            var text = "YAPA 2: " + YAPA.Shared.Common.TimerSpeech.Status(engine.Phase, engine.Remaining, engine.WorkTime);

            //Windows allows 63 characters
            if (text.Length > 63)
            {
                text = text.Substring(0, 63);
            }

            if (_sysTrayIcon.ToolTipText != text)
            {
                _sysTrayIcon.ToolTipText = text;
            }
        }

        private void UpdateIcon()
        {
            var phase = _viewModel.Engine.Phase;
            var textColor = phase == PomodoroPhase.Break || phase == PomodoroPhase.BreakEnded
                ? _settings.BreakTrayIconColor
                : _settings.WorkTrayIconColor;

            var minutes = _viewModel.Engine.DisplayValue / 60;
            var seconds = _viewModel.Engine.DisplayValue % 60;
            var displayText = minutes == 0 && seconds > 0 ? "<1" : minutes.ToString();

            var key = displayText + textColor;
            if (key == _lastIconText)
            {
                return;
            }
            _lastIconText = key;
            _sysTrayIcon.Icon = new WindowIcon(TrayIconRenderer.Render(displayText, textColor, 32));
        }
    }

    public static class TrayIconRenderer
    {
        //The text was designed as an 11 pixel font on a 16 pixel icon; keep that proportion at every size
        private const double FontToIconRatio = 11.0 / 16.0;

        public static Bitmap Render(string text, Color color, int iconSize)
        {
            var bitmap = new RenderTargetBitmap(new PixelSize(iconSize, iconSize));
            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(color),
                FontWeight = FontWeight.Bold,
                FontFamily = new FontFamily("Microsoft Sans Serif, Arial, Helvetica, sans-serif"),
                FontSize = iconSize * FontToIconRatio,
            };
            textBlock.Measure(new Size(iconSize, iconSize));
            textBlock.Arrange(new Rect(0, 0, iconSize, iconSize));
            bitmap.Render(textBlock);
            return bitmap;
        }
    }

    /// <summary>Stand-in for NotifyIcon.ShowBalloonTip: the system's own notifications.</summary>
    public static class Notifications
    {
        public static void Show(string title, string message)
        {
            try
            {
                if (OperatingSystem.IsMacOS())
                {
                    var script = $"display notification \"{Escape(message)}\" with title \"{Escape(title)}\"";
                    var info = new ProcessStartInfo("osascript") { UseShellExecute = false, CreateNoWindow = true };
                    info.ArgumentList.Add("-e");
                    info.ArgumentList.Add(script);
                    Process.Start(info);
                }
                else if (OperatingSystem.IsLinux())
                {
                    var info = new ProcessStartInfo("notify-send") { UseShellExecute = false, CreateNoWindow = true };
                    info.ArgumentList.Add(title);
                    info.ArgumentList.Add(message);
                    Process.Start(info);
                }
            }
            catch
            {
                // notifications are a nicety
            }
        }

        private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    public class SystemTraySettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public Color WorkTrayIconColor
        {
            get => ColorHelpers.FromString(_settings.Get(nameof(WorkTrayIconColor), "DarkGreen"), Colors.DarkGreen);
            set => _settings.Update(nameof(WorkTrayIconColor), ColorHelpers.ToSetting(value));
        }

        public Color BreakTrayIconColor
        {
            get => ColorHelpers.FromString(_settings.Get(nameof(BreakTrayIconColor), "DarkRed"), Colors.DarkRed);
            set => _settings.Update(nameof(BreakTrayIconColor), ColorHelpers.ToSetting(value));
        }

        public bool ShowInTaskbar
        {
            get => _settings.Get(nameof(ShowInTaskbar), true);
            set => _settings.Update(nameof(ShowInTaskbar), value);
        }

        public bool MinimizeToTray
        {
            get => _settings.Get(nameof(MinimizeToTray), true);
            set => _settings.Update(nameof(MinimizeToTray), value);
        }

        public bool ShowBalloonTipWhenInSystemTray
        {
            get => _settings.Get(nameof(ShowBalloonTipWhenInSystemTray), true);
            set => _settings.Update(nameof(ShowBalloonTipWhenInSystemTray), value);
        }

        public bool ShowApplicationOnBalloonTipClick
        {
            get => _settings.Get(nameof(ShowApplicationOnBalloonTipClick), false);
            set => _settings.Update(nameof(ShowApplicationOnBalloonTipClick), value);
        }

        public int TimeToDisplayBalloontip
        {
            get => _settings.Get(nameof(TimeToDisplayBalloontip), 10);
            set => _settings.Update(nameof(TimeToDisplayBalloontip), value);
        }

        public SystemTraySettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(new SystemTrayPlugin().Id);
        }

        public void DeferChanges() => _settings.DeferChanges();
    }
}
