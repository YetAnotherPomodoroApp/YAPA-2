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
        public Type SettingEditWindow { get; }
    }

    public class UnclickablePlugin : IPlugin
    {
        private readonly IPomodoroEngine _engine;
        private readonly Window _window;

        private bool _setOnce;

        int extendedStyle;
        private bool is64Bit;
        private IntPtr hwnd;

        public UnclickablePlugin(IApplication app, IPomodoroEngine engine)
        {
            _engine = engine;
            _window = (Window)app;

            _engine.PropertyChanged += _engine_PropertyChanged;

            _window.Loaded += (sender, args) => SaveInitialStyle();
            _window.MouseEnter += _window_MouseEnter;
        }

        private void _window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Console.WriteLine("Enter");

        }

        private void _engine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
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
            is64Bit = (Marshal.SizeOf(typeof(IntPtr))) == 8;
            hwnd = new WindowInteropHelper(_window).Handle;

            if (is64Bit)
                extendedStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            else
                extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        }

        private void Unclickable()
        {
            if (is64Bit)
                SetWindowLongPtr(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            else
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        private void Clickable()
        {
            if (is64Bit)
                SetWindowLongPtr(hwnd, GWL_EXSTYLE, extendedStyle);
            else
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle);
        }
    }

    public class UnclickableSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public bool MoveHorizontally
        {
            get => _settings.Get(nameof(MoveHorizontally), true);
            set => _settings.Update(nameof(MoveHorizontally), value);
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
