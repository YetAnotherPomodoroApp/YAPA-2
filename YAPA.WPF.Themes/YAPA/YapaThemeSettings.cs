using System;
using YAPA.Contracts;

namespace YAPA.WPF.Themes.YAPA
{

    public class YapaThemeMeta : IThemeMeta
    {
        public string Title => "YAPA 1.0";

        public Type Theme => typeof(YapaTheme);

        public Type Settings => typeof(YapaThemeSettings);

        public Type SettingEditWindow => typeof(YapaThemeSettingWindow);
    }

    public class YapaThemeSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public int Width
        {
            get { return _settings.Get(nameof(Width), 200); }
            set { _settings.Update(nameof(Width), value); }
        }

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

        public bool ShowStatusText
        {
            get { return _settings.Get(nameof(ShowStatusText), true); }
            set { _settings.Update(nameof(ShowStatusText), value); }
        }

        public YapaThemeSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(nameof(YapaTheme));
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}
