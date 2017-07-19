using System;
using System.Windows;
using YAPA.Contracts;
using GDIScreen = System.Windows.Forms.Screen;

namespace YAPA.WPF
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

            _settings.WindowTopPerc = _app.Top / currentScreen.WorkingArea.Height;
            _settings.WindowLeftPerc = _app.Left / currentScreen.WorkingArea.Width;
        }

        private void App_Loaded()
        {
            var currentScreen = GDIScreen.FromHandle(_app.WindowHandle);

            if (!double.IsNaN(_settings.WindowLeftPerc) && !double.IsNaN(_settings.WindowTopPerc))
            {
                _app.Top = currentScreen.WorkingArea.Height * _settings.WindowTopPerc;
                _app.Left = currentScreen.WorkingArea.Width * _settings.WindowLeftPerc;
            }
            else
            {
                _app.Left = (SystemParameters.PrimaryScreenWidth - _app.Width - 15.0) / currentScreen.WorkingArea.Width;
                _app.Top = 0;
            }
        }
    }

    public class SaveApplicationPositionOnScreenSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public double WindowLeftPerc
        {
            get { return _settings.Get(nameof(WindowLeftPerc), double.NaN); }
            set { _settings.Update(nameof(WindowLeftPerc), value); }
        }

        public double WindowTopPerc
        {
            get { return _settings.Get(nameof(WindowTopPerc), double.NaN); }
            set { _settings.Update(nameof(WindowTopPerc), value); }
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
