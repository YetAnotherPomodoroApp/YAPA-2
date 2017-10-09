using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using YAPA.Shared.Contracts;
using GDIScreen = System.Windows.Forms.Screen;

namespace YAPA.Plugins.Unclickable
{
    public class UnclickableMeta : IPluginMeta
    {
        public string Title => "Unclickable";
        public string Id => "Unclickable";
        public Type Plugin => typeof(UnclickablePlugin);
        public Type Settings => typeof(UnclickableSettings);
        public Type SettingEditWindow => typeof(UnclickableSettingsWindow);
    }

    public class UnclickablePlugin : IPlugin
    {
        private readonly IPomodoroEngine _engine;
        private readonly UnclickableSettings _settings;
        private readonly ISettings _globalSettings;
        private readonly Window _window;

        private readonly IApplication _app;

        private bool _setOnce;

        private int _extendedStyle;
        private bool _is64Bit;
        private IntPtr _windowHandle;

        public UnclickablePlugin(IApplication app, IPomodoroEngine engine, UnclickableSettings settings, ISettings globalSettings)
        {
            _engine = engine;
            _settings = settings;
            _globalSettings = globalSettings;
            _window = (Window)app;
            _app = app;

            _engine.PropertyChanged += _engine_PropertyChanged;

            _window.Loaded += (sender, args) => SaveInitialStyle();

            _window.MouseEnter += _window_MouseEnter;
            _globalSettings.PropertyChanged += _globalSettings_PropertyChanged; ;
        }

        private void _globalSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.EndsWith(nameof(_settings.UnclickablityType)))
            {
                if (_settings.UnclickablityType != UnclickablityType.ClickThrough)
                {
                    Clickable();
                }
            }
        }

        private void _window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_settings.UnclickablityType != UnclickablityType.MoveHorizontally &&
                _settings.UnclickablityType != UnclickablityType.MoveVertically)
            {
                return;
            }

            if (_engine.Phase != PomodoroPhase.Break && _engine.Phase != PomodoroPhase.Work)
            {
                return;
            }

            var screen = GDIScreen.FromHandle(_app.WindowHandle);

            if (_settings.UnclickablityType == UnclickablityType.MoveHorizontally)
            {
                var appCenter = _window.Left - screen.Bounds.X;
                var relativePossition = 1 - appCenter / (screen.Bounds.Width - _window.Width / 2);

                var newPossition = relativePossition * (screen.Bounds.Width - _window.Width / 2);

                _window.Left = newPossition + screen.Bounds.X;
            }
            else if (_settings.UnclickablityType == UnclickablityType.MoveVertically)
            {
                var appCenter = _window.Top - screen.Bounds.Y;
                var relativePossition = 1 - appCenter / (screen.Bounds.Height - _window.Height / 2);

                var newPossition = relativePossition * (screen.Bounds.Height - _window.Height / 2);

                _window.Top = newPossition + screen.Bounds.Y;
            }

        }

        private void _engine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_settings.UnclickablityType != UnclickablityType.ClickThrough)
            {
                return;
            }
            if (_engine.Phase == PomodoroPhase.Break || _engine.Phase == PomodoroPhase.Work)
            {
                if (!_setOnce)
                {
                    Unclickable();
                    _setOnce = true;
                }
            }
            else
            {
                if (_setOnce)
                {
                    Clickable();
                    _setOnce = false;
                }
            }
        }

        public const int WS_EX_TRANSPARENT = 0x00000020;

        public const int GWL_EXSTYLE = (-20);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        public static extern int GetWindowLongPtr(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLongPtr(IntPtr hwnd, int index, int newStyle);

        void SaveInitialStyle()
        {
            _is64Bit = (Marshal.SizeOf(typeof(IntPtr))) == 8;
            _windowHandle = new WindowInteropHelper(_window).Handle;

            if (_is64Bit)
                _extendedStyle = GetWindowLongPtr(_windowHandle, GWL_EXSTYLE);
            else
                _extendedStyle = GetWindowLong(_windowHandle, GWL_EXSTYLE);
        }

        private void Unclickable()
        {
            if (_is64Bit)
                SetWindowLongPtr(_windowHandle, GWL_EXSTYLE, _extendedStyle | WS_EX_TRANSPARENT);
            else
                SetWindowLong(_windowHandle, GWL_EXSTYLE, _extendedStyle | WS_EX_TRANSPARENT);
        }

        private void Clickable()
        {
            if (_is64Bit)
                SetWindowLongPtr(_windowHandle, GWL_EXSTYLE, _extendedStyle);
            else
                SetWindowLong(_windowHandle, GWL_EXSTYLE, _extendedStyle);
        }
    }

    public enum UnclickablityType
    {
        ClickThrough,
        MoveHorizontally,
        MoveVertically
    }

    public class UnclickableSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public UnclickablityType UnclickablityType
        {
            get => _settings.Get(nameof(UnclickablityType), UnclickablityType.ClickThrough);
            set => _settings.Update(nameof(UnclickablityType), value);
        }

        public UnclickableSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(new UnclickableMeta().Id);
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}
