using System;
using System.Linq;
using YAPA.Shared.Contracts;
using YAPA.WPF;
using GDIScreen = System.Windows.Forms.Screen;

namespace YAPA.Plugins.SaveApplicationPossitionOnScreen
{
    [BuiltInPlugin(Hide = true)]
    public class SaveApplicationPositionOnScreenPlugin : IPluginMeta
    {
        public string Title => "Save application position on screen";
        public string Id => "SaveApplicationPositionOnScreen";

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
            var currentScreen = GDIScreen.FromHandle(_app.WindowHandle);

            _settings.ActiveMonitor = currentScreen.DeviceName;

            var top = _app.Top - currentScreen.Bounds.Top;
            var left = _app.Left - currentScreen.Bounds.Left;

            _settings.WindowTopPerc = top / currentScreen.WorkingArea.Height;
            _settings.WindowLeftPerc = left / currentScreen.WorkingArea.Width;
        }

        private void App_Loaded()
        {
            var activeScreen = GetActiveScreen();
            var workingArea = activeScreen.WorkingArea;
            var screenBounds = activeScreen.Bounds;

            double calculatedLeft;
            double calculatedTop;

            if (!double.IsNaN(_settings.WindowLeftPerc) && !double.IsNaN(_settings.WindowTopPerc))
            {
                calculatedLeft = workingArea.Width * _settings.WindowLeftPerc;
                calculatedTop = workingArea.Height * _settings.WindowTopPerc;
            }
            else
            {
                calculatedLeft = (workingArea.Width - _app.Width - 15.0) / workingArea.Width;
                calculatedTop = 0;
            }

            _app.Left = calculatedLeft;
            _app.Top = calculatedTop;
        }

        private GDIScreen GetActiveScreen()
        {
            return GDIScreen.FromHandle(_app.WindowHandle);
        }
    }

    public class SaveApplicationPositionOnScreenSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public double WindowLeftPerc
        {
            get => _settings.Get(nameof(WindowLeftPerc), double.NaN);
            set => _settings.Update(nameof(WindowLeftPerc), value);
        }

        public double WindowTopPerc
        {
            get => _settings.Get(nameof(WindowTopPerc), double.NaN);
            set => _settings.Update(nameof(WindowTopPerc), value);
        }

        public string ActiveMonitor
        {
            get => _settings.Get<string>(nameof(ActiveMonitor), null);
            set => _settings.Update(nameof(ActiveMonitor), value);
        }

        public SaveApplicationPositionOnScreenSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(new SaveApplicationPositionOnScreenPlugin().Id);
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}
