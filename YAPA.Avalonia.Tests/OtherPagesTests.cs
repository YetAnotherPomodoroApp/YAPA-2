using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Xunit;
using YAPA.Avalonia.Infrastructure;
using YAPA.Avalonia.Plugins.Unclickable;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Tests
{
    /// <summary>Settings → Sound: sounds are recorded by a fake player instead of played.</summary>
    public class SoundSettingsTests
    {
        [AvaloniaFact]
        public void Starting_work_plays_the_tick_at_the_default_volume()
        {
            using var yapa = new Yapa();
            yapa.Resolve<IPomodoroEngine>().Start();
            Yapa.Pump();
            var (file, repeat, volume) = Assert.Single(yapa.Sound.Played);
            Assert.EndsWith("tick.wav", file);
            Assert.True(File.Exists(file));
            Assert.False(repeat);
            Assert.Equal(0.5, volume);
        }

        [AvaloniaFact]
        public void Work_end_plays_the_ding()
        {
            using var yapa = new Yapa();
            yapa.Resolve<IPomodoroEngine>().Start();
            yapa.Clock.Advance(TimeSpan.FromMinutes(25));
            Assert.EndsWith("ding.wav", yapa.Sound.Played.Last().file);
        }

        [AvaloniaFact]
        public void Volume_is_used_after_save()
        {
            using var yapa = new Yapa();
            yapa.OpenSettings("Sound").Find<Slider>("Slider").Value = 0.8;
            Yapa.Pump(400);
            yapa.Save();
            yapa.Resolve<IPomodoroEngine>().Start();
            Yapa.Pump();
            Assert.Equal(0.8, yapa.Sound.Played.Last().volume, 3);
        }

        [AvaloniaTheory]
        [InlineData("Disable sound notifications")]
        [InlineData("Play period start\\end sounds")]
        public void Sounds_can_be_switched_off(string checkBox)
        {
            using var yapa = new Yapa();
            Yapa.Click(yapa.OpenSettings("Sound").CheckBox(checkBox));
            yapa.Save();
            yapa.Resolve<IPomodoroEngine>().Start();
            yapa.Clock.Advance(TimeSpan.FromMinutes(25));
            Assert.Empty(yapa.Sound.Played);
        }

        [AvaloniaFact]
        public void Work_song_plays_and_repeats_during_work()
        {
            using var yapa = new Yapa();
            var song = Path.Combine(AppContext.BaseDirectory, "Resources", "ding.wav");
            var page = yapa.OpenSettings("Sound");
            Yapa.Type(page.Find<TextBox>("WorkSong"), song);
            Yapa.Click(page.CheckBox("Repeat work song"));
            yapa.Save();
            Assert.Contains("WorkSong", File.ReadAllText(Path.Combine(TestApp.Home, "localSettings.json")));

            yapa.Resolve<IPomodoroEngine>().Start();
            Yapa.Pump();
            Assert.Contains(yapa.Sound.Played, x => x.file == song && x.repeat);
        }
    }

    /// <summary>Settings → Plugins, System tray, Unclickable.</summary>
    public class PluginSettingsTests
    {
        [AvaloniaFact]
        public void Enabling_a_plugin_saves_it_and_asks_for_a_restart()
        {
            using var yapa = new Yapa();
            var page = yapa.OpenSettings("Plugins");
            var notice = yapa.SettingsWindow.Find<Border>("RestartAppNotification");
            Assert.False(notice.IsVisible);

            var checkBox = global::Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(page).OfType<CheckBox>()
                .First(x => (x.Content as string) == "System tray");
            Assert.False(checkBox.IsChecked);
            Yapa.Click(checkBox);
            Assert.True(checkBox.IsChecked, "the click should tick the box");
            yapa.Save();

            Assert.True(notice.IsVisible);
            Assert.Equal("Restart application to apply changes", yapa.SettingsWindow.Find<TextBlock>("NotificationMessage").Text);
            Assert.Contains("\"EnabledPlugins\":[\"SystemTray\"]", yapa.SettingsFile);
        }

        [AvaloniaFact]
        public void Enabled_plugins_get_their_page_under_plugins()
        {
            using var yapa = new Yapa("{\"PluginManager\":{\"EnabledPlugins\":[\"SystemTray\",\"Unclickable\"]}}");
            var tray = yapa.OpenSettings("System tray");
            Assert.NotNull(tray.CheckBox("Minimize to tray"));
            var unclickable = yapa.OpenSettings("Unclickable");
            var behaviour = unclickable.FindFirst<ComboBox>();
            Assert.Equal(UnclickablityType.ClickThrough, behaviour.SelectedValue);
        }

        [AvaloniaFact]
        public void Tray_settings_are_saved_and_show_in_taskbar_applies()
        {
            using var yapa = new Yapa("{\"PluginManager\":{\"EnabledPlugins\":[\"SystemTray\"]}}");
            var page = yapa.OpenSettings("System tray");
            Assert.Equal("10", page.Find<EditableComboBox>("BalloonTipSelect").Text);

            Yapa.Click(page.CheckBox("Show in taskbar"));
            Yapa.Type(page.Find<EditableComboBox>("BalloonTipSelect"), "5");
            yapa.Save();

            Assert.Contains("\"ShowInTaskbar\":false", yapa.SettingsFile);
            Assert.Contains("\"TimeToDisplayBalloontip\":5", yapa.SettingsFile);
            Assert.False(yapa.Timer.ShowInTaskbar);
        }

        [AvaloniaFact]
        public void Tray_icon_colours_are_saved()
        {
            using var yapa = new Yapa("{\"PluginManager\":{\"EnabledPlugins\":[\"SystemTray\"]}}");
            var box = yapa.OpenSettings("System tray").Find<ColorPickerBox>("WorkTrayColor");
            Assert.Equal(Colors.DarkGreen, box.Color);
            box.Color = Colors.Orange;
            Yapa.Pump();
            yapa.Save();
            Assert.Contains("\"WorkTrayIconColor\":\"#FFFFA500\"", yapa.SettingsFile);
        }
    }

    /// <summary>Settings → Dashboard: the history of finished pomodoros.</summary>
    public class DashboardTests
    {
        private static void AddHistory(Yapa yapa)
        {
            var repository = yapa.Resolve<IPomodoroRepository>();
            var today = DateTime.UtcNow.Date.AddHours(12);
            for (var day = 0; day < 40; day++)
            {
                for (var i = 0; i < day % 4; i++)
                {
                    repository.Add(new PomodoroEntity { Count = 1, DateTime = today.AddDays(-day), DurationMin = 25, ProfileName = day % 2 == 0 ? "Pomodoro" : "Writing" });
                }
            }
        }

        [AvaloniaFact]
        public void Shows_the_month_grid_summary_and_chart()
        {
            using var yapa = new Yapa();
            AddHistory(yapa);
            var page = yapa.OpenSettings("Dashboard");

            Assert.True(page.Find<StackPanel>("DayPanel").IsVisible);
            Assert.Equal(7, page.Find<StackPanel>("WeekStackPanel").Children.Count); // six months back and this one
            Assert.Equal("Total time: 1.01:00:00, Pomodoros: 60", page.Find<TextBlock>("Summary").Text);
            Assert.True(page.Find<StackPanel>("CartesianContainer").IsVisible);
        }

        [AvaloniaFact]
        public void Number_of_months_changes_the_grid()
        {
            using var yapa = new Yapa();
            AddHistory(yapa);
            var page = yapa.OpenSettings("Dashboard");
            page.Find<ComboBox>("NumberOfMonths").SelectedItem = 2;
            Yapa.Pump();
            Assert.Equal(3, page.Find<StackPanel>("WeekStackPanel").Children.Count);
            Assert.Contains("\"NumberOfMonths\":2", yapa.SettingsFile);
        }

        [AvaloniaFact]
        public void Profile_filter_counts_only_that_profile()
        {
            using var yapa = new Yapa();
            AddHistory(yapa);
            var page = yapa.OpenSettings("Dashboard");
            var profiles = page.Find<ComboBox>("FilteredProfiles");
            Assert.Contains("Writing", profiles.Items.OfType<string>());

            profiles.SelectedItem = "Writing";
            Yapa.Pump();
            Assert.Equal("Total time: 16:40:00, Pomodoros: 40", page.Find<TextBlock>("Summary").Text);

            profiles.SelectedItem = "(all)";
            Yapa.Pump();
            Assert.Equal("Total time: 1.01:00:00, Pomodoros: 60", page.Find<TextBlock>("Summary").Text);
        }

        [AvaloniaFact]
        public void A_finished_pomodoro_is_recorded()
        {
            using var yapa = new Yapa();
            yapa.Resolve<IPomodoroEngine>().Start();
            yapa.Clock.Advance(TimeSpan.FromMinutes(25));
            var page = yapa.OpenSettings("Dashboard");
            Assert.Equal("Total time: 00:25:00, Pomodoro: 1", page.Find<TextBlock>("Summary").Text);
        }
    }

    /// <summary>Settings → Themes: picking a theme, and the Motivational theme's page.</summary>
    public class ThemeChoiceTests
    {
        [AvaloniaFact]
        public void Choosing_another_theme_saves_it_and_asks_for_a_restart()
        {
            using var yapa = new Yapa();
            var list = yapa.OpenSettings("Themes").Find<ComboBox>("ThemeList");
            Assert.Equal(new[] { "Motivational", "YAPA 1.0" }, list.Items.OfType<string>().OrderBy(x => x).ToArray());

            list.SelectedItem = "Motivational";
            Yapa.Pump();
            yapa.Save();

            Assert.True(yapa.SettingsWindow.Find<Border>("RestartAppNotification").IsVisible);
            Assert.Contains("\"SelectedTheme\":\"Motivational\"", yapa.SettingsFile);
        }

        [AvaloniaFact]
        public void Motivational_theme_starts_and_its_page_saves_white_text()
        {
            using var yapa = new Yapa("{\"ThemeManager\":{\"SelectedTheme\":\"Motivational\"}}");
            Assert.Equal("MotivationalTheme", yapa.Timer.GetType().Name);
            var page = yapa.OpenSettings("Themes");
            Yapa.Click(page.CheckBox("Use white text"));
            Assert.Contains("\"UseWhiteText\":true", yapa.SettingsFile);
        }
    }

    /// <summary>The timer's own buttons.</summary>
    public class TimerButtonTests
    {
        private static Button Button(Yapa yapa, string name) => yapa.Timer.Find<Button>(name);

        private static string Status(Yapa yapa) => yapa.Timer.Find<TextBlock>("StageTextBlock").Text;

        [AvaloniaFact]
        public void Start_pause_and_stop()
        {
            using var yapa = new Yapa();
            var engine = yapa.Resolve<IPomodoroEngine>();

            Yapa.Click(Button(yapa, "Start"));
            Assert.Equal(PomodoroPhase.Work, engine.Phase);
            Assert.Equal("Work", Status(yapa));
            Assert.True(Button(yapa, "Pause").IsVisible);
            Assert.False(Button(yapa, "Start").IsVisible);

            yapa.Clock.Advance(TimeSpan.FromSeconds(90));
            Yapa.Click(Button(yapa, "Pause"));
            Assert.Equal("Work Paused", Status(yapa));
            Assert.True(Button(yapa, "Start").IsVisible);
            Assert.True(Button(yapa, "Stop").IsVisible);
            Assert.Equal("1", yapa.Timer.Find<TextBlock>("CurrentTimeMinutes").Text);

            Yapa.Click(Button(yapa, "Stop"));
            Assert.Equal(PomodoroPhase.NotStarted, engine.Phase);
            Assert.Equal("YAPA 2.0", Status(yapa));
        }

        [AvaloniaFact]
        public void Skip_after_work_goes_straight_to_the_next_pomodoro()
        {
            using var yapa = new Yapa();
            var engine = yapa.Resolve<IPomodoroEngine>();
            engine.Start();
            yapa.Clock.Advance(TimeSpan.FromMinutes(25));
            Yapa.Click(Button(yapa, "Skip"));
            Assert.Equal(PomodoroPhase.Work, engine.Phase);
            Assert.Equal(2, engine.Index);
        }

        [AvaloniaFact]
        public void Right_click_opens_settings()
        {
            using var yapa = new Yapa();
            var centre = new Point(60, 50);
            global::Avalonia.Headless.HeadlessWindowExtensions.MouseDown(yapa.Timer, centre, global::Avalonia.Input.MouseButton.Right);
            global::Avalonia.Headless.HeadlessWindowExtensions.MouseUp(yapa.Timer, centre, global::Avalonia.Input.MouseButton.Right);
            Yapa.Pump();
            Assert.True(yapa.SettingsWindow.IsVisible);
        }

        [AvaloniaFact]
        public void Double_click_stops_the_flashing()
        {
            using var yapa = new Yapa();
            yapa.Resolve<IPomodoroEngine>().Start();
            yapa.Clock.Advance(TimeSpan.FromMinutes(25));
            var panel = yapa.Timer.Find<StackPanel>("CurrentTime");
            Yapa.Pump(100);
            yapa.Timer.RaiseEvent(new global::Avalonia.Input.TappedEventArgs(global::Avalonia.Input.InputElement.DoubleTappedEvent, null));
            Yapa.Pump();
            var stopped = ((ISolidColorBrush)panel.Background).Color;
            Yapa.Pump(250);
            Assert.Equal(stopped, ((ISolidColorBrush)panel.Background).Color);
            Assert.Equal(0, stopped.A); // back to transparent
        }
    }
}
