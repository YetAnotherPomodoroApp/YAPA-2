using System;
using System.Windows.Media;
using YAPA.Shared.Contracts;
using YAPA.WPF.Shared.Shared;

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
            get => _settings.Get(nameof(Width), 200);
            set => _settings.Update(nameof(Width), value);
        }

        public double ClockOpacity
        {
            get => _settings.Get(nameof(ClockOpacity), 0.6);
            set => _settings.Update(nameof(ClockOpacity), value);
        }

        public double ShadowOpacity
        {
            get => _settings.Get(nameof(ShadowOpacity), 0.6);
            set => _settings.Update(nameof(ShadowOpacity), value);
        }

        public Color TextColor
        {
            get
            {
                var color = _settings.Get(nameof(TextColor), "White");
                return YapaHelpers.ColorFromString(color, Colors.White);
            }
            set => _settings.Update(nameof(TextColor), value.ToString());
        }

        public Color ShadowColor
        {
            get
            {
                var color = _settings.Get(nameof(ShadowColor), "Black");
                return YapaHelpers.ColorFromString(color, Colors.Black);
            }
            set => _settings.Update(nameof(ShadowColor), value.ToString());
        }

        public bool DisableFlashingAnimation
        {
            get => _settings.Get(nameof(DisableFlashingAnimation), false);
            set => _settings.Update(nameof(DisableFlashingAnimation), value);
        }

        public bool ShowStatusText
        {
            get => _settings.Get(nameof(ShowStatusText), true);
            set => _settings.Update(nameof(ShowStatusText), value);
        }

        public bool HideSeconds
        {
            get => _settings.Get(nameof(HideSeconds), false);
            set => _settings.Update(nameof(HideSeconds), value);
        }

        public bool HideButtons
        {
            get => _settings.Get(nameof(HideButtons), false);
            set => _settings.Update(nameof(HideButtons), value);
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
