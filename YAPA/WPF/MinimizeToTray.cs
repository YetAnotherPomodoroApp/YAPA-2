using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using YAPA.Contracts;
using YAPA.Shared;

namespace YAPA.WPF
{

    public class MinimizeToTrayPlugin : IPluginMeta
    {
        public string Title => "Minimize to tray";

        public Type Plugin => typeof(MinimizeToTray);

        public Type Settings => typeof(MinimizeToTraySettings);

        public Type SettingEditWindow => null;
    }

    public class MinimizeToTray : IPlugin
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern bool DestroyIcon(IntPtr handle);

        private readonly System.Windows.Forms.NotifyIcon _sysTrayIcon;
        private IntPtr _systemTrayIcon;

        public ICommand StopCommand { get; set; }
        public ICommand StartCommand { get; set; }
        public ICommand ResetCommand { get; set; }


        private readonly IApplication _app;
        private readonly IPomodoroEngine _engine;
        private readonly MinimizeToTraySettings _settings;

        public MinimizeToTray(IApplication app, IPomodoroEngine engine, MinimizeToTraySettings settings)
        {
            _app = app;
            _engine = engine;
            _settings = settings;

            _app.StateChanged += _app_StateChanged;

            StopCommand = new StopCommand(_engine);
            StartCommand = new StartCommand(_engine);
            ResetCommand = new ResetCommand(_engine);

            _sysTrayIcon = new System.Windows.Forms.NotifyIcon
            {
                Text = @"YAPA 2",
                Icon = new System.Drawing.Icon(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\pomoTray.ico"), 40, 40),
                Visible = false
            };
            _sysTrayIcon.DoubleClick += SysTrayIcon_DoubleClick;

            _engine.PropertyChanged += _engine_PropertyChanged;

            _sysTrayIcon.ContextMenu = new System.Windows.Forms.ContextMenu(CreateNotifyIconContextMenu());
        }

        private void _engine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_engine.Elapsed))
            {
                UpdateIcon();
            }
        }

        private System.Windows.Forms.MenuItem[] CreateNotifyIconContextMenu()
        {
            var startTask = new System.Windows.Forms.MenuItem { Text = @"Start" };
            startTask.Click += (o, s) =>
            {
                if (StartCommand.CanExecute(null))
                {
                    StartCommand.Execute(null);
                }
            };

            var stopTask = new System.Windows.Forms.MenuItem { Text = @"Stop" };
            stopTask.Click += (o, s) =>
            {
                if (ResetCommand.CanExecute(null))
                {
                    ResetCommand.Execute(null);
                }
            };

            var resetTask = new System.Windows.Forms.MenuItem { Text = @"Reset session" };
            resetTask.Click += (o, s) =>
            {
                if (ResetCommand.CanExecute(null))
                {
                    ResetCommand.Execute(null);
                }
            };

            var settings = new System.Windows.Forms.MenuItem { Text = @"Settings" };
            settings.Click += (o, s) =>
            {
                throw new NotImplementedException();
            };

            var close = new System.Windows.Forms.MenuItem { Text = @"Exit" };
            close.Click += (o, s) =>
            {
                _app.CloseApp();
            };

            return new[]
            {
                startTask,stopTask, resetTask, settings,close
            };
        }

        private void _app_StateChanged(ApplicationState state)
        {

            if (state == ApplicationState.Minimized && _settings.MinimizeToTray && _settings.ShowInTaskbar)
            {
                _app.Hide();
                _sysTrayIcon.Visible = true;
            }

            if (!_settings.ShowInTaskbar)
            {
                if (state == ApplicationState.Minimized)
                {
                    _sysTrayIcon.Visible = true;
                }
                else
                {
                    _app.ShowInTaskbar = _settings.ShowInTaskbar;
                }
            }
        }

        private void SysTrayIcon_DoubleClick(object sender, EventArgs e)
        {
            _app.Show();
            _app.AppState = ApplicationState.Normal;
            _sysTrayIcon.Visible = false;
        }

        //http://blogs.msdn.com/b/abhinaba/archive/2005/09/12/animation-and-text-in-system-tray-using-c.aspx
        private void UpdateIcon()
        {
            System.Drawing.Color textColor;

            if (_engine.Phase == PomodoroPhase.Break || _engine.Phase == PomodoroPhase.BreakEnded)
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

            var displayText = _engine.Elapsed / 60;

            System.Drawing.Brush brush = new System.Drawing.SolidBrush(textColor);

            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(16, 16);
            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap);
            graphics.DrawString(displayText.ToString(), new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Bold), brush, 0, 0);

            _systemTrayIcon = bitmap.GetHicon();

            System.Drawing.Icon icon = System.Drawing.Icon.FromHandle(_systemTrayIcon);
            _sysTrayIcon.Icon = icon;
        }
    }

    public class MinimizeToTraySettings : IPluginSettings
    {
        private readonly ISettings _settings;

        public System.Drawing.Color WorkTrayIconColor
        {
            get { return _settings.Get(nameof(WorkTrayIconColor), System.Drawing.Color.DarkGreen); }
            set { _settings.Update(nameof(WorkTrayIconColor), value); }
        }

        public System.Drawing.Color BreakTrayIconColor
        {
            get { return _settings.Get(nameof(BreakTrayIconColor), System.Drawing.Color.DarkRed); }
            set { _settings.Update(nameof(BreakTrayIconColor), value); }
        }

        public bool ShowInTaskbar
        {
            get { return _settings.Get(nameof(ShowInTaskbar), true); }
            set { _settings.Update(nameof(ShowInTaskbar), value); }
        }

        public bool MinimizeToTray
        {
            get { return _settings.Get(nameof(MinimizeToTray), true); }
            set { _settings.Update(nameof(MinimizeToTray), value); }
        }

        public MinimizeToTraySettings(ISettings settings)
        {
            _settings = settings;
        }
    }
}
