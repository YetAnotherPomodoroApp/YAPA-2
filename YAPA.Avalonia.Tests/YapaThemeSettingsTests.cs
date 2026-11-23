using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;
using YAPA.Avalonia.Infrastructure;
using YAPA.Avalonia.Themes.Yapa;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Tests
{
    /// <summary>
    /// Settings → Themes → YAPA 1.0. Like the WPF page, these apply at once (no Save needed)
    /// and are written to settings.json straight away.
    /// </summary>
    public class YapaThemeSettingsTests
    {
        private static YapaThemeSettingWindow ThemePage(Yapa yapa) =>
            yapa.OpenSettings("Themes").FindFirst<YapaThemeSettingWindow>();

        private static Slider Slider(YapaThemeSettingWindow page, int index) =>
            page.GetLogicalDescendants().OfType<Slider>().ElementAt(index);

        private static CheckBox CheckBox(Control page, string text) =>
            page.FindFirst<CheckBox>(x => (x.Content as string) == text);

        private static TextBlock Digit(Yapa yapa, string name) => yapa.Timer.Find<TextBlock>(name);

        private static ColorView OpenPicker(ColorPickerBox box)
        {
            Yapa.Click(box);
            var view = (ColorView)((Flyout)FlyoutBase.GetAttachedFlyout(box)).Content;
            Assert.True(view.IsVisible && TopLevel.GetTopLevel(view) != null, "the colour drop-down should open on click");
            return view;
        }

        [AvaloniaFact]
        public void Text_colour_picked_from_the_palette_recolours_the_digits_and_is_saved()
        {
            using var yapa = new Yapa();
            var box = ThemePage(yapa).Find<ColorPickerBox>("TextColor");
            var view = OpenPicker(box);
            Assert.Equal(1, view.SelectedIndex); // opens on the palette, like the Xceed picker

            // click a swatch that is not white
            var swatch = view.GetVisualDescendants().OfType<ListBoxItem>()
                .First(x => x.DataContext is Color c && c != Colors.White && c.A == 255);
            var picked = (Color)swatch.DataContext;
            Yapa.Click(swatch);

            Assert.Equal(picked, box.Color);
            Assert.Equal(picked, ((ISolidColorBrush)Digit(yapa, "CurrentTimeMinutes").Foreground).Color);
            Assert.Contains($"\"TextColor\":\"{ColorHelpers.ToSetting(picked)}\"", yapa.SettingsFile);
        }

        [AvaloniaTheory]
        [InlineData("ShadowColor")]
        [InlineData("BackgroundColor")]
        public void A_colour_that_starts_black_can_be_changed_in_the_spectrum(string name)
        {
            using var yapa = new Yapa();
            var box = ThemePage(yapa).Find<ColorPickerBox>(name);
            Assert.Equal(Colors.Black, box.Color);
            var view = OpenPicker(box);
            view.SelectedIndex = 0; // the spectrum tab
            Yapa.Pump(300); // the square is drawn in the background; a person takes longer than this

            // a click high in the square: saturation x brightness, so the colour is no longer black
            var spectrum = view.GetVisualDescendants().OfType<Control>().First(x => x.GetType().Name == "ColorSpectrum");
            var top = (Window)TopLevel.GetTopLevel(spectrum);
            var point = spectrum.TranslatePoint(new Point(spectrum.Bounds.Width * 0.8, spectrum.Bounds.Height * 0.2), top).Value;
            top.MouseDown(point, global::Avalonia.Input.MouseButton.Left);
            top.MouseUp(point, global::Avalonia.Input.MouseButton.Left);
            Yapa.Pump();

            Assert.NotEqual(Colors.Black, box.Color);
            Assert.Contains($"\"{name}\":\"{ColorHelpers.ToSetting(box.Color)}\"", yapa.SettingsFile);
            var settings = yapa.Resolve<YapaThemeSettings>();
            Assert.Equal(box.Color, name == "ShadowColor" ? settings.ShadowColor : settings.BackgroundColor);
        }

        [AvaloniaFact]
        public void Clock_opacity_changes_every_digit_and_the_small_text()
        {
            using var yapa = new Yapa();
            Slider(ThemePage(yapa), 0).Value = 0.3;
            Yapa.Pump(400);

            Assert.Equal(0.3, Digit(yapa, "CurrentTimeMinutes").Opacity, 3);
            Assert.Equal(0.3, yapa.Timer.Find<TextBlock>("StageTextBlock").Opacity, 3);
            Assert.Contains("\"ClockOpacity\":0.3", yapa.SettingsFile);
        }

        [AvaloniaFact]
        public void Shadow_opacity_and_colour_change_the_glow_round_the_digits()
        {
            using var yapa = new Yapa();
            Slider(ThemePage(yapa), 1).Value = 0.9;
            Yapa.Pump(400);

            var glow = (DropShadowEffect)Digit(yapa, "CurrentTimeSeconds").Effect;
            Assert.Equal(0.9, glow.Opacity, 3);
            Assert.Equal(0, glow.OffsetX);
            Assert.Equal(10, glow.BlurRadius);
        }

        [AvaloniaFact]
        public void Application_size_resizes_the_timer_keeping_its_proportions()
        {
            using var yapa = new Yapa();
            Assert.Equal(200, yapa.Timer.Width);
            Slider(ThemePage(yapa), 3).Value = 390; // clock, shadow, background opacity, size
            Yapa.Pump(400);

            Assert.Equal(390, yapa.Timer.Width);
            Assert.Equal(390 * 60 / 130.0, yapa.Timer.Height, 3);
        }

        [AvaloniaFact]
        public void Hide_seconds_leaves_two_minute_digits()
        {
            using var yapa = new Yapa();
            Yapa.Click(CheckBox(ThemePage(yapa), "Hide seconds"));

            var visible = new[] { "CurrentTimeMinutes3", "CurrentTimeMinutes2", "CurrentTimeMinutes", "CurrentTimeSeparator", "CurrentTimeSeconds2", "CurrentTimeSeconds" }
                .Where(x => Digit(yapa, x).IsVisible)
                .ToList();
            Assert.Equal(new[] { "CurrentTimeMinutes2", "CurrentTimeMinutes" }, visible);
            Assert.Contains("\"HideSeconds\":true", yapa.SettingsFile);
        }

        [AvaloniaFact]
        public void Hide_seconds_shows_less_than_one_minute_as_lt1()
        {
            using var yapa = new Yapa("{\"PomodoroEngine\":{\"CountBackwards\":true},\"YapaTheme\":{\"HideSeconds\":true}}");
            var engine = yapa.Resolve<IPomodoroEngine>();
            engine.Start();
            yapa.Clock.Advance(TimeSpan.FromSeconds(25 * 60 - 30));

            Assert.Equal("<", Digit(yapa, "CurrentTimeMinutes2").Text);
            Assert.Equal("1", Digit(yapa, "CurrentTimeMinutes").Text);
        }

        [AvaloniaFact]
        public void Show_pomodoro_status_off_empties_the_status_text()
        {
            using var yapa = new Yapa();
            var status = yapa.Timer.Find<TextBlock>("StageTextBlock");
            Assert.Equal("YAPA 2.0", status.Text);

            Yapa.Click(CheckBox(ThemePage(yapa), "Show pomodoro status"));
            Assert.Equal(string.Empty, status.Text ?? string.Empty);

            yapa.Resolve<IPomodoroEngine>().Start();
            Yapa.Pump();
            Assert.Equal(string.Empty, status.Text ?? string.Empty);
        }

        [AvaloniaFact]
        public void Background_opacity_shows_a_plate_in_the_background_colour()
        {
            using var yapa = new Yapa();
            var plate = yapa.Timer.GetLogicalDescendants().OfType<Border>().First(x => x.CornerRadius.TopLeft == 3);
            Assert.False(plate.IsVisible);

            ThemePage(yapa).Find<Slider>("BackgroundOpacity").Value = 0.5;
            Yapa.Pump(400);

            Assert.True(plate.IsVisible);
            var brush = (ISolidColorBrush)plate.Background;
            Assert.Equal(Colors.Black, brush.Color);
            Assert.Equal(0.5, brush.Opacity, 3);
        }

        [AvaloniaFact]
        public void Shadow_on_small_text_puts_a_shadow_on_status_buttons_and_counter()
        {
            using var yapa = new Yapa();
            var status = yapa.Timer.Find<TextBlock>("StageTextBlock");
            Assert.Null(status.Effect);

            var box = CheckBox(ThemePage(yapa), "Shadow on status text and buttons");
            Yapa.Click(box);

            Assert.True(box.IsChecked, "the click should tick the box");
            Assert.Contains("\"ShadowOnSmallText\":true", yapa.SettingsFile);
            Assert.IsType<DropShadowEffect>(status.Effect);
            Assert.IsType<DropShadowEffect>(yapa.Timer.Find<TextBlock>("CurrentPeriod").Effect);
        }

        [AvaloniaFact]
        public void Work_end_flashes_unless_flashing_is_disabled()
        {
            using var yapa = new Yapa();
            var panel = yapa.Timer.Find<StackPanel>("CurrentTime");
            var engine = yapa.Resolve<IPomodoroEngine>();
            engine.Start();
            yapa.Clock.Advance(TimeSpan.FromMinutes(25));
            Assert.Equal(PomodoroPhase.WorkEnded, engine.Phase);
            var first = Colour(panel);
            Yapa.Pump(250);
            Assert.NotEqual(first, Colour(panel)); // animating towards tomato and back

            engine.Stop();
            Yapa.Pump();
            Yapa.Click(CheckBox(ThemePage(yapa), "Disable flashing animation"));
            engine.Start();
            yapa.Clock.Advance(TimeSpan.FromMinutes(25));
            Assert.Equal(Colors.Tomato, Colour(panel));
            Yapa.Pump(250);
            Assert.Equal(Colors.Tomato, Colour(panel)); // steady, no animation
        }

        private static Color Colour(Panel panel) => ((ISolidColorBrush)panel.Background).Color;

        [AvaloniaFact]
        public void Hide_buttons_hides_the_start_buttons_two_seconds_after_the_pointer_leaves()
        {
            using var yapa = new Yapa();
            Yapa.Click(CheckBox(ThemePage(yapa), "Hide buttons"));
            yapa.SettingsWindow.Find<Button>("CloseButton").Command.Execute(null);
            Yapa.Pump();

            var buttons = yapa.Timer.Find<StackPanel>("ButtonPanel");
            Yapa.PointerLeaves(yapa.Timer);
            Yapa.Pump(1500);
            Assert.Equal(1, buttons.Opacity); // still there before two seconds
            Yapa.Pump(800);
            Assert.Equal(0, buttons.Opacity);
            Assert.False(buttons.IsHitTestVisible);

            yapa.Timer.MouseMove(new Point(50, 30));
            Yapa.Pump();
            Assert.Equal(1, buttons.Opacity);
        }
    }
}
