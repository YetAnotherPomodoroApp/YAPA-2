using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using YAPA.Shared.Contracts;

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

            _engine.PropertyChanged += _engine_PropertyChanged;

            _window.Loaded += (sender, args) => SaveInitialStyle();

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
        ClickThrough
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
