using System;
using System.Windows;
using System.Windows.Forms;
using YAPA.Contracts;
using GDIScreen = System.Windows.Forms.Screen;

namespace YAPA.WPF
{
    public class SaveApplicationPositionOnScreenPlugin : IPluginMeta
    {
        public string Title => "Save application position on screen";

        public Type Plugin => typeof(SaveApplicationPositionOnScreen);

        public Type Settings => typeof(SaveApplicationPositionOnScreenSettings);

        public Type SettingEditWindow => null;
    }

    public class SaveApplicationPositionOnScreen : IPlugin
    {
        private readonly IApplication _app;
        private readonly SaveApplicationPositionOnScreenSettings _settings;

        public SaveApplicationPositionOnScreen(IApplication app, SaveApplicationPositionOnScreenSettings settings)
        {
            _app = app;
            _settings = settings;
            _app.Closing += App_Closing;
            App_Loaded();
        }

        private void App_Closing()
        {
            _settings.IsFirstRun = false;

            var currentScreen = GDIScreen.FromHandle(_app.WindowHandle);

            _settings.CurrentScreenHeight = currentScreen.WorkingArea.Height;
            _settings.CurrentScreenWidth = currentScreen.WorkingArea.Width;

            _settings.WindowTop = (int)_app.Top;
            _settings.WindowLeft = (int)_app.Left;
        }

        private void App_Loaded()
        {
            var currentScreen = Screen.FromHandle(_app.WindowHandle);

            var screenChanged = (currentScreen.WorkingArea.Height != _settings.CurrentScreenHeight || currentScreen.WorkingArea.Width != _settings.CurrentScreenWidth);

            // default position only for first run or when screen size changes
            // position the clock at top / right, primary screen
            if (_settings.IsFirstRun || screenChanged)
            {
                _app.Left = SystemParameters.PrimaryScreenWidth - _app.Width - 15.0;
                _app.Top = 0;
            }
            else if (_settings.WindowLeft != -1 && _settings.WindowTop != -1)
            {
                _app.Left = _settings.WindowLeft;
                _app.Top = _settings.WindowTop;
            }
        }
    }

    public class SaveApplicationPositionOnScreenSettings : IPluginSettings
    {
        private readonly ISettingsForPlugin _settings;

        public bool IsFirstRun
        {
            get { return _settings.Get(nameof(IsFirstRun), true); }
            set { _settings.Update(nameof(IsFirstRun), value); }
        }

        public int CurrentScreenHeight
        {
            get { return _settings.Get(nameof(CurrentScreenHeight), -1); }
            set { _settings.Update(nameof(CurrentScreenHeight), value); }
        }

        public int CurrentScreenWidth
        {
            get { return _settings.Get(nameof(CurrentScreenWidth), -1); }
            set { _settings.Update(nameof(CurrentScreenWidth), value); }
        }

        public int WindowLeft
        {
            get { return _settings.Get(nameof(WindowLeft), -1); }
            set { _settings.Update(nameof(WindowLeft), value); }
        }

        public int WindowTop
        {
            get { return _settings.Get(nameof(WindowTop), -1); }
            set { _settings.Update(nameof(WindowTop), value); }
        }

        public SaveApplicationPositionOnScreenSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForPlugin(nameof(SaveApplicationPositionOnScreen));
        }

        public void DefereChanges()
        {
            _settings.DeferChanges();
        }
    }
}
