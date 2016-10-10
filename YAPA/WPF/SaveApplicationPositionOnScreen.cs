using System.Windows;
using System.Windows.Forms;
using YAPA.Contracts;
using GDIScreen = System.Windows.Forms.Screen;

namespace YAPA.WPF
{
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
        }
    }

    public class SaveApplicationPositionOnScreenSettings : IPluginSettings
    {
        private readonly ISettings _settings;

        public bool IsFirstRun
        {
            get { return _settings.Get("IsFirstRun", true); }
            set { _settings.Update("IsFirstRun", value, true); }
        }

        public int CurrentScreenHeight
        {
            get { return _settings.Get("CurrentScreenHeight", -1); }
            set { _settings.Update("CurrentScreenHeight", value, true); }
        }

        public int CurrentScreenWidth
        {
            get { return _settings.Get("CurrentScreenWidth", -1); }
            set { _settings.Update("CurrentScreenWidth", value, true); }
        }

        public SaveApplicationPositionOnScreenSettings(ISettings settings)
        {
            _settings = settings;
        }
    }
}
