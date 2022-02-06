using NLog;
using System;
using YAPA.Shared.Contracts;
using YAPA.WPF;

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
        private readonly ILogger _logger;

        public SaveApplicationPositionOnScreen(IApplication app,
            SaveApplicationPositionOnScreenSettings settings,
            ILogger logger)
        {
            _app = app;
            _settings = settings;
            _logger = logger;
            _app.Closing += App_Closing;
            _app.Loaded += App_Loaded;
        }

        private void App_Loaded()
        {
            try
            {
                WindowPlacement.SetPlacement(_app.WindowHandle, _settings.Position);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to restore application position ");
            }
        }

        private void App_Closing()
        {
            try
            {
                var currentPosition = WindowPlacement.GetPlacement(_app.WindowHandle);
                _settings.Position = currentPosition;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to save application position ");
            }
        }
    }

    public class SaveApplicationPositionOnScreenSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public string Position
        {
            get => _settings.Get<string>(nameof(Position), null);
            set => _settings.Update(nameof(Position), value);
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
