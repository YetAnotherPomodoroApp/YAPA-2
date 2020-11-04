using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using YAPA.Shared.Contracts;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

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

            _sysTrayIcon.MouseClick += SysTrayIcon_MouseClick;

            _viewModel.Engine.PropertyChanged += _engine_PropertyChanged;

            _sysTrayIcon.ContextMenu = new ContextMenu(CreateNotifyIconContextMenu());
            _sysTrayIcon.ContextMenu.Popup += ContextMenu_Popup;
            _sysTrayIcon.BalloonTipClicked += _sysTrayIcon_BalloonTipClicked;

            _app.Closing += () => _sysTrayIcon.Dispose();
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
            return _settings.ShowInTaskbar == false || (_settings.MinimizeToTray && _app.AppState == ApplicationState.Minimized);
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

        private void SysTrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            _app.Show();
            _app.AppState = ApplicationState.Normal;
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

            if (_systemTrayIcon != IntPtr.Zero)
            {
                DestroyIcon(_systemTrayIcon);
            }

            var minutes = _viewModel.Engine.DisplayValue / 60;
            var seconds = _viewModel.Engine.DisplayValue % 60;
            var displayText = minutes == 0 && seconds > 0 ? "<1" : minutes.ToString();

            Brush brush = new SolidBrush(Color.FromArgb(textColor.A, textColor.R, textColor.G, textColor.B));

            var bitmap = new Bitmap(16, 16);
            var graphics = Graphics.FromImage(bitmap);
            graphics.DrawString(displayText, new Font("Microsoft Sans Serif", 8.25f, FontStyle.Bold), brush, 0, 0);

            _systemTrayIcon = bitmap.GetHicon();

            var icon = Icon.FromHandle(_systemTrayIcon);
            _sysTrayIcon.Icon = icon;
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
                return (System.Windows.Media.Color)(System.Windows.Media.ColorConverter.ConvertFromString(color) ?? Color.DarkGreen);
            }
            set => _settings.Update(nameof(WorkTrayIconColor), value);
        }

        public System.Windows.Media.Color BreakTrayIconColor
        {
            get
            {

                var color = _settings.Get(nameof(BreakTrayIconColor), "DarkRed");
                return (System.Windows.Media.Color)(System.Windows.Media.ColorConverter.ConvertFromString(color) ?? Color.DarkRed);
            }
            set => _settings.Update(nameof(BreakTrayIconColor), value);
        }

        public bool ShowInTaskbar
        {
            get => _settings.Get(nameof(ShowInTaskbar), true);
            set => _settings.Update(nameof(ShowInTaskbar), value);
        }

        public bool MinimizeToTray
        {
            get => _settings.Get(nameof(MinimizeToTray), false);
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
