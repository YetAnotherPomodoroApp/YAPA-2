using System;
using Avalonia.Media;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Themes.Motivational
{
    public class MotivationalThemeMeta : IThemeMeta
    {
        public string Title => "Motivational";

        public Type Theme => typeof(MotivationalTheme);

        public Type Settings => typeof(MotivationalThemeSettings);

        public Type SettingEditWindow => typeof(MotivationalThemeSettingsWindow);
    }

    /// <summary>Same keys and defaults as the WPF MotivationalThemeSettings, stored under "MotivationalTheme".</summary>
    public class MotivationalThemeSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

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

        public bool UseWhiteText
        {
            get => _settings.Get(nameof(UseWhiteText), false);
            set => _settings.Update(nameof(UseWhiteText), value);
        }

        public string TextBrush
        {
            get => _settings.Get(nameof(TextBrush), "White");
            set => _settings.Update(nameof(TextBrush), value);
        }

        public bool DisableFlashingAnimation
        {
            get => _settings.Get(nameof(DisableFlashingAnimation), false);
            set => _settings.Update(nameof(DisableFlashingAnimation), value);
        }

        public MotivationalThemeSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent("MotivationalTheme");
        }

        public void DeferChanges() => _settings.DeferChanges();
    }

    /// <summary>YAPA.WPF.MotivationalTheme/Const.cs</summary>
    public static class Const
    {
        // Font awesome icons
        public const string ICON_WINDOW_MINIMIZE = "";
        public const string ICON_WINDOW_CLOSE = "";
        public const string ICON_SETTINGS = "";
        public const string ICON_PLAY = "";
        public const string ICON_PAUSE = "";
        public const string ICON_STOP = "";
        public const string ICON_PERIOD_POMODORO = "";
        public const string ICON_PERIOD_REST = "";
        public const string ICON_PERIOD_REST_LONG = "";
        public const string ICON_PERIOD_STOPPED = "";

        // Colors
        public const string COLOR_LIGHT_WINDOW_BACKGROUND = "#F2F2F2";
        public const string COLOR_LIGHT_WINDOW_BACKGROUND2 = "#E5E5E5";
        public const string COLOR_LIGHT_WINDOW_FOREGROUND = "#222222";
        public const string COLOR_LIGHT_WINDOW_SHADOW = "#000000";
        public const double COLOR_LIGHT_WINDOW_SHADOW_OPACITY = 0.3;
        public const string COLOR_LIGHT_TIMER_FOREGROUND = "#FFFFFF";
        public const string COLOR_LIGHT_TIMER_SHADOW = "#000000";

        public const string COLOR_DARK_WINDOW_BACKGROUND = "#222222";
        public const string COLOR_DARK_WINDOW_BACKGROUND2 = "#111111";
        public const string COLOR_DARK_WINDOW_FOREGROUND = "#DDDDDD";
        public const string COLOR_DARK_WINDOW_SHADOW = "#30000000";
        public const double COLOR_DARK_WINDOW_SHADOW_OPACITY = 0.3;
        public const string COLOR_DARK_TIMER_FOREGROUND = "#000000";
        public const string COLOR_DARK_TIMER_SHADOW = "#999999";

        public static IBrush ToBrush(string hex) => new SolidColorBrush(Color.Parse(hex));

        public static Color ToColor(string hex) => Color.Parse(hex);
    }

    /// <summary>The English strings from YAPA.WPF.MotivationalTheme/Localizations/General.resx that the theme shows.</summary>
    public static class Strings
    {
        public const string app_button_pause_title = "Pause current period";
        public const string app_button_settings = "Show settings";
        public const string app_button_start_title = "Start period";
        public const string app_button_stop_title = "Reset all sessions and start from the beginning";
        public const string app_button_window_close = "Close window";
        public const string app_button_window_minimize = "Minimize window";
        public const string app_period_long_break_caption = "Hurray, you've got whole {0} minutes to rest! Go for a walk!";
        public const string app_period_motivation_start_pomodoro = "Press play to start your new challenge";
        public const string app_period_pomodoro_caption_default = "Shouldn't you be focused already? Stop staring on this window and let's get some things done.";
        public const string app_period_press_play_to_start = "Press play to start new period";
        public const string app_period_short_break_caption = "You've got {0} minutes to rest. Let's do some stretching.";
    }
}
