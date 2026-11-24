using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;
using YAPA.WPF.Shared.Shared;

namespace YAPA.Plugins.MinimizeToTray
{
    public class SystemTrayPlugin : IPluginMeta
    {
        public string Title => "System tray";
        public string Id => "SystemTray";

        public Type Plugin => typeof(SystemTray);

        public Type Settings => typeof(SystemTraySettings);

        public Type SettingEditWindow => typeof(MinimizeToTraySettingWindow);
    }

    public class SystemTray : IPlugin
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern bool DestroyIcon(IntPtr handle);

        private readonly NotifyIcon _sysTrayIcon;
        private IntPtr _systemTrayIcon;

        private readonly IApplication _app;
        private readonly IMainViewModel _viewModel;
        private readonly SystemTraySettings _settings;

        public SystemTray(IApplication app, IMainViewModel viewModel, SystemTraySettings settings, ISettings globalSettings)
        {
            _app = app;
            _viewModel = viewModel;
            _settings = settings;

            globalSettings.PropertyChanged += _globalSettings_PropertyChanged;

            _app.StateChanged += _app_StateChanged;

            _sysTrayIcon = new NotifyIcon
            {
                Text = @"YAPA 2",
                Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\pomoTray.ico"), 40, 40),
                Visible = SystemTrayVisible()
            };

            _sysTrayIcon.DoubleClick += SysTrayIcon_DoubleClick;

            _viewModel.Engine.PropertyChanged += _engine_PropertyChanged;

            _sysTrayIcon.ContextMenu = new ContextMenu(CreateNotifyIconContextMenu());
            _sysTrayIcon.ContextMenu.Popup += ContextMenu_Popup;
            _sysTrayIcon.BalloonTipClicked += _sysTrayIcon_BalloonTipClicked;

            _app.Closing += () => _sysTrayIcon.Dispose();

            UpdateTooltip();
        }

        private void _sysTrayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (_settings.ShowApplicationOnBalloonTipClick)
            {
                _app.Show();
                _app.AppState = ApplicationState.Normal;
            }
        }

        private bool SystemTrayVisible()
        {
            return _settings.AlwaysShowTrayIcon || _settings.ShowInTaskbar == false || (_settings.MinimizeToTray && _app.AppState == ApplicationState.Minimized);
        }

        private void ContextMenu_Popup(object sender, EventArgs e)
        {
            _sysTrayIcon.ContextMenu.MenuItems.Clear();
            _sysTrayIcon.ContextMenu.MenuItems.AddRange(CreateNotifyIconContextMenu());
        }

        private void _globalSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == $"{nameof(SystemTray)}.{nameof(_settings.ShowInTaskbar)}")
            {
                _app.ShowInTaskbar = _settings.ShowInTaskbar;
            }

            if (e.PropertyName.StartsWith($"{nameof(SystemTray)}"))
            {
                _sysTrayIcon.Visible = SystemTrayVisible();
            }
        }

        private void _engine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Engine.Elapsed))
            {
                UpdateIcon();
            }
            else if (e.PropertyName == nameof(_viewModel.Engine.Phase))
            {
                UpdateTooltip();

                if (_sysTrayIcon.Visible == false || _settings.ShowBalloonTipWhenInSystemTray == false)
                {
                    return;
                }

                var phase = _viewModel.Engine.Phase;

                var workEndedMessage = "Work interval ended.";
                var breakEndedMessage = "Break interval ended.";

                var workStartingMessage = $"Starting {_viewModel.Engine.WorkTime / 60}min work interval.";
                var breakStartingMessage = $"Starting {_viewModel.Engine.BreakTime / 60}min break interval.";

                var messageToShow = string.Empty;

                switch (phase)
                {
                    case PomodoroPhase.Work:
                        messageToShow = workStartingMessage;
                        break;
                    case PomodoroPhase.WorkEnded:
                        messageToShow = workEndedMessage;
                        break;
                    case PomodoroPhase.Break:
                        messageToShow = breakStartingMessage;
                        break;
                    case PomodoroPhase.BreakEnded:
                        messageToShow = breakEndedMessage;
                        break;
                }


                if (!string.IsNullOrEmpty(messageToShow))
                {
                    _sysTrayIcon.ShowBalloonTip(_settings.TimeToDisplayBalloontip, "YAPA 2", messageToShow, ToolTipIcon.None);
                }
            }
        }

        private MenuItem[] CreateNotifyIconContextMenu()
        {
            var commands = new List<Tuple<string, ICommand>>
            {
                new Tuple<string, ICommand>("Start",_viewModel.StartCommand),
                new Tuple<string, ICommand>("Pause",_viewModel.PauseCommand),
                new Tuple<string, ICommand>("Stop",_viewModel.StopCommand),
                new Tuple<string, ICommand>("Skip",_viewModel.SkipCommand),
                new Tuple<string, ICommand>("Reset session",_viewModel.ResetCommand),
                new Tuple<string, ICommand>("Settings",_viewModel.ShowSettingsCommand),
            };

            var menuItems = commands.Select(x =>
            {
                var command = x.Item2;

                var item = new MenuItem
                {
                    Text = x.Item1,
                    Enabled = command.CanExecute(null)
                };
                item.Click += (sender, args) =>
                {
                    if (command.CanExecute(null))
                    {
                        command.Execute(null);
                    }
                };
                return item;
            });

            var close = new MenuItem
            {
                Text = @"Exit"
            };
            close.Click += (o, s) =>
            {
                _app.CloseApp();
            };

            return menuItems.Union(new List<MenuItem> { close }).ToArray();
        }

        private void _app_StateChanged(ApplicationState state)
        {
            if (state == ApplicationState.Minimized && (_settings.MinimizeToTray || _settings.ShowInTaskbar == false))
            {
                _app.Hide();
                _sysTrayIcon.Visible = true;
            }

            if (state != ApplicationState.Minimized)
            {
                _app.ShowInTaskbar = _settings.ShowInTaskbar;
            }
        }

        private void SysTrayIcon_DoubleClick(object sender, EventArgs e)
        {
            _app.Show();
            _app.AppState = ApplicationState.Normal;
        }

        //The icon only fits the minutes; the tooltip says the same in words, which is also what a screen reader reads.
        //Whole minutes, so a screen reader on the icon is not read a new tooltip every second.
        //No "YAPA 2" in front: Windows 11 already puts the application's name before the tooltip.
        private void UpdateTooltip()
        {
            var engine = _viewModel.Engine;
            var text = TimerSpeech.Minutes(engine.Phase, engine.Remaining);

            //Windows allows 63 characters
            if (text.Length > 63)
            {
                text = text.Substring(0, 63);
            }

            if (_sysTrayIcon.Text != text)
            {
                _sysTrayIcon.Text = text;
            }
        }

        //http://blogs.msdn.com/b/abhinaba/archive/2005/09/12/animation-and-text-in-system-tray-using-c.aspx
        private void UpdateIcon()
        {
            System.Windows.Media.Color textColor;

            if (_viewModel.Engine.Phase == PomodoroPhase.Break || _viewModel.Engine.Phase == PomodoroPhase.BreakEnded)
            {
                textColor = _settings.BreakTrayIconColor;
            }
            else
            {
                textColor = _settings.WorkTrayIconColor;
            }

            var minutes = _viewModel.Engine.DisplayValue / 60;
            var seconds = _viewModel.Engine.DisplayValue % 60;
            var displayText = minutes == 0 && seconds > 0 ? "<1" : minutes.ToString();

            UpdateTooltip();

            var previousIcon = _systemTrayIcon;

            //Windows shows tray icons at the small icon size, which grows with display scaling
            var iconSize = SystemInformation.SmallIconSize.Width;
            using (var bitmap = TrayIconRenderer.Render(displayText, System.Drawing.Color.FromArgb(textColor.A, textColor.R, textColor.G, textColor.B), iconSize))
            {
                _systemTrayIcon = bitmap.GetHicon();
            }

            var icon = Icon.FromHandle(_systemTrayIcon);
            _sysTrayIcon.Icon = icon;

            if (previousIcon != IntPtr.Zero)
            {
                DestroyIcon(previousIcon);
            }
        }
    }

    public static class TrayIconRenderer
    {
        //The text was designed as an 11 pixel font on a 16 pixel icon; keep that proportion at every size
        private const float FontToIconRatio = 11f / 16f;

        public static Bitmap Render(string text, System.Drawing.Color color, int iconSize)
        {
            var bitmap = new Bitmap(iconSize, iconSize);

            using (var graphics = Graphics.FromImage(bitmap))
            using (var brush = new SolidBrush(color))
            using (var font = new Font("Microsoft Sans Serif", iconSize * FontToIconRatio, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                graphics.DrawString(text, font, brush, 0, 0);
            }

            return bitmap;
        }
    }

    public class SystemTraySettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public System.Windows.Media.Color WorkTrayIconColor
        {
            get
            {
                var color = _settings.Get(nameof(WorkTrayIconColor), "DarkGreen");
                return YapaHelpers.ColorFromString(color, Colors.DarkGreen);
            }
            set => _settings.Update(nameof(WorkTrayIconColor), value);
        }

        public System.Windows.Media.Color BreakTrayIconColor
        {
            get
            {
                var color = _settings.Get(nameof(BreakTrayIconColor), "DarkRed");
                return YapaHelpers.ColorFromString(color, Colors.DarkRed);
            }
            set => _settings.Update(nameof(BreakTrayIconColor), value);
        }

        public bool ShowInTaskbar
        {
            get => _settings.Get(nameof(ShowInTaskbar), true);
            set => _settings.Update(nameof(ShowInTaskbar), value);
        }

        //The icon shows the minutes left and its tooltip says the time in words, also to a screen reader
        public bool AlwaysShowTrayIcon
        {
            get => _settings.Get(nameof(AlwaysShowTrayIcon), false);
            set => _settings.Update(nameof(AlwaysShowTrayIcon), value);
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

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}
