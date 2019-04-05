using System;
using YAPA.Shared.Contracts;

namespace Motivational
{
    public class YapaThemeMeta : IThemeMeta
    {
        public string Title => "Motivational";

        public Type Theme => typeof(MainWindow);

        public Type Settings => typeof(MotivationalThemeSettings);

        public Type SettingEditWindow => typeof(MotivationalThemeSettingsWindow);
    }

    public class MotivationalThemeSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public double ClockOpacity
        {
            get { return _settings.Get(nameof(ClockOpacity), 0.6); }
            set { _settings.Update(nameof(ClockOpacity), value); }
        }

        public double ShadowOpacity
        {
            get { return _settings.Get(nameof(ShadowOpacity), 0.6); }
            set { _settings.Update(nameof(ShadowOpacity), value); }
        }

        public bool UseWhiteText
        {
            get { return _settings.Get(nameof(UseWhiteText), false); }
            set { _settings.Update(nameof(UseWhiteText), value); }
        }

        public string TextBrush
        {
            get { return _settings.Get(nameof(TextBrush), "White"); }
            set { _settings.Update(nameof(TextBrush), value); }
        }

        public bool DisableFlashingAnimation
        {
            get { return _settings.Get(nameof(DisableFlashingAnimation), false); }
            set { _settings.Update(nameof(DisableFlashingAnimation), value); }
        }


        public MotivationalThemeSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent("MotivationalTheme");
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }

}
