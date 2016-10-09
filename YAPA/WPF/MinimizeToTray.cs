using System;
using System.IO;
using System.Runtime.InteropServices;
using YAPA.Contracts;

namespace YAPA.WPF
{
    public class MinimizeToTray : IPlugin
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern bool DestroyIcon(IntPtr handle);

        private readonly System.Windows.Forms.NotifyIcon _sysTrayIcon;
        private IntPtr _systemTrayIcon;

        private readonly IApplication _app;
        private readonly IPomodoroEngine _engine;
        private readonly MinimizeToTraySettings _settings;

        public MinimizeToTray(IApplication app, IPomodoroEngine engine, MinimizeToTraySettings settings)
        {
            _app = app;
            _engine = engine;
            _settings = settings;

            _app.StateChanged += _app_StateChanged;

            _sysTrayIcon = new System.Windows.Forms.NotifyIcon
            {
                Text = @"YAPA 2",
                Icon = new System.Drawing.Icon(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\pomoTray.ico"), 40, 40),
                Visible = false
            };
            _sysTrayIcon.DoubleClick += SysTrayIcon_DoubleClick;

            _engine.PropertyChanged += _engine_PropertyChanged;

            //sysTrayIcon.ContextMenu = new System.Windows.Forms.ContextMenu(CreateNotifyIconContextMenu());
        }

        private void _engine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_engine.Elapsed))
            {
                UpdateIcon();
            }
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
            get { return _settings.Get("WorkTrayIconColor", System.Drawing.Color.DarkGreen); }
            set { _settings.Update("WorkTrayIconColor", value); }
        }

        public System.Drawing.Color BreakTrayIconColor
        {
            get { return _settings.Get("BreakTrayIconColor", System.Drawing.Color.DarkRed); }
            set { _settings.Update("BreakTrayIconColor", value); }
        }

        public bool ShowInTaskbar
        {
            get { return _settings.Get("ShowInTaskbar", true); }
            set { _settings.Update("ShowInTaskbar", value); }
        }

        public bool MinimizeToTray
        {
            get { return _settings.Get("MinimizeToTray", true); }
            set { _settings.Update("MinimizeToTray", value); }
        }

        public MinimizeToTraySettings(ISettings settings)
        {
            _settings = settings;
        }
    }
}
