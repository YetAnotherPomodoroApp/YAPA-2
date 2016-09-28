using System;
using System.IO;
using System.Runtime.InteropServices;
using YAPA.Contracts;

namespace YAPA.WPF
{
    public class MinimizeToTray
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern bool DestroyIcon(IntPtr handle);

        private readonly System.Windows.Forms.NotifyIcon _sysTrayIcon;
        private IntPtr _systemTrayIcon;

        private readonly IApplication _app;
        private readonly MinimizeToTraySettings _settings;

        public MinimizeToTray(IApplication app, MinimizeToTraySettings settings)
        {
            _app = app;
            _settings = settings;

            _app.StateChanged += _app_StateChanged;

            _sysTrayIcon = new System.Windows.Forms.NotifyIcon
            {
                Text = @"YAPA",
                Icon = new System.Drawing.Icon(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\pomoTray.ico"), 40, 40),
                Visible = false
            };
            _sysTrayIcon.DoubleClick += SysTrayIcon_DoubleClick;

            //sysTrayIcon.ContextMenu = new System.Windows.Forms.ContextMenu(CreateNotifyIconContextMenu());
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
        public void ShowText(string text)
        {
            System.Drawing.Color textColor;

            //TODO
            if (true)//_isWork)
            {
                textColor = _settings.WorkTrayIconColor;
            }
            else
            {
                textColor = _settings.BreakTrayIconColor;
            }

            if (_systemTrayIcon != IntPtr.Zero)
            {
                DestroyIcon(_systemTrayIcon);
            }

            System.Drawing.Brush brush = new System.Drawing.SolidBrush(textColor);

            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(16, 16);
            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap);
            graphics.DrawString(text, new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Bold), brush, 0, 0);

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
            get { return _settings.Get("MinimizeToTray", false); }
            set { _settings.Update("MinimizeToTray", value); }
        }

        public MinimizeToTraySettings(ISettings settings)
        {
            _settings = settings;
        }
    }
}
