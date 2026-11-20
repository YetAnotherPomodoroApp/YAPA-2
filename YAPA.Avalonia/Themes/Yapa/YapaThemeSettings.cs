using System;
using Avalonia.Media;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Themes.Yapa
{
    public class YapaThemeMeta : IThemeMeta
    {
        public string Title => "YAPA 1.0";

        public Type Theme => typeof(YapaTheme);

        public Type Settings => typeof(YapaThemeSettings);

        public Type SettingEditWindow => typeof(YapaThemeSettingWindow);
    }

    /// <summary>Same keys and defaults as the WPF YapaThemeSettings, stored under "YapaTheme".</summary>
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
            get => ColorHelpers.FromString(_settings.Get(nameof(TextColor), "White"), Colors.White);
            set => _settings.Update(nameof(TextColor), ColorHelpers.ToSetting(value));
        }

        public Color ShadowColor
        {
            get => ColorHelpers.FromString(_settings.Get(nameof(ShadowColor), "Black"), Colors.Black);
            set => _settings.Update(nameof(ShadowColor), ColorHelpers.ToSetting(value));
        }

        public Color BackgroundColor
        {
            get => ColorHelpers.FromString(_settings.Get(nameof(BackgroundColor), "Black"), Colors.Black);
            set => _settings.Update(nameof(BackgroundColor), ColorHelpers.ToSetting(value));
        }

        //0 means no background at all, which is how the timer has always looked
        public double BackgroundOpacity
        {
            get => _settings.Get(nameof(BackgroundOpacity), 0.0);
            set => _settings.Update(nameof(BackgroundOpacity), value);
        }

        public bool ShadowOnSmallText
        {
            get => _settings.Get(nameof(ShadowOnSmallText), false);
            set => _settings.Update(nameof(ShadowOnSmallText), value);
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
            _settings = settings.GetSettingsForComponent("YapaTheme");
        }

        public void DeferChanges() => _settings.DeferChanges();
    }
}
