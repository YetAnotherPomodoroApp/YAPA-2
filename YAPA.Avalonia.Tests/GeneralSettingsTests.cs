using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Xunit;
using YAPA.Avalonia.Infrastructure;
using YAPA.Avalonia.Plugins.PomodoroEngine;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Tests
{
    /// <summary>
    /// Settings → General. These changes wait for Save, as in WPF: until then the timer runs
    /// with the old values, and Close throws them away.
    /// </summary>
    public class GeneralSettingsTests
    {
        private static Control General(Yapa yapa) => yapa.OpenSettings("General");

        private static string Digits(Yapa yapa) =>
            string.Concat(new[] { "CurrentTimeMinutes2", "CurrentTimeMinutes", "CurrentTimeSeparator", "CurrentTimeSeconds2", "CurrentTimeSeconds" }
                .Select(x => yapa.Timer.Find<TextBlock>(x))
                .Where(x => x.IsVisible)
                .Select(x => x.Text));

        private const string TwoProfiles = "{\"PomodoroEngine\":{\"ActiveProfile\":\"Pomodoro\",\"Profiles\":{" +
            "\"Pomodoro\":{\"WorkTime\":1500,\"BreakTime\":300,\"LongBreakTime\":900,\"PomodorosBeforeLongBreak\":4}," +
            "\"Writing\":{\"WorkTime\":3000,\"BreakTime\":600,\"LongBreakTime\":1200,\"PomodorosBeforeLongBreak\":3,\"AutoStartBreak\":true}}}}";

        [AvaloniaFact]
        public void Work_time_typed_in_minutes_is_used_after_save()
        {
            using var yapa = new Yapa();
            var page = General(yapa);
            Yapa.Type(page.Find<EditableComboBox>("WorkTimeSelect"), "30");
            Assert.True(yapa.SaveEnabled);
            Assert.Equal(25 * 60, yapa.Resolve<IPomodoroEngine>().WorkTime); // not before Save

            yapa.Save();

            Assert.False(yapa.SaveEnabled);
            Assert.Equal(30 * 60, yapa.Resolve<IPomodoroEngine>().WorkTime);
            Assert.Contains("\"WorkTime\":1800", yapa.SettingsFile);
        }

        [AvaloniaFact]
        public void Break_session_rounds_and_long_break_are_saved_in_the_profile()
        {
            using var yapa = new Yapa();
            var page = General(yapa);
            Yapa.Type(page.Find<EditableComboBox>("BreakTimeSelect"), "7");
            Yapa.Type(page.Find<EditableComboBox>("SessionRounds"), "3");
            Yapa.Type(page.Find<EditableComboBox>("LongBreakTimeSelect"), "20");
            yapa.Save();

            var settings = yapa.Resolve<PomodoroEngineSettings>();
            Assert.Equal(7 * 60, settings.BreakTime);
            Assert.Equal(3, settings.PomodorosBeforeLongBreak);
            Assert.Equal(20 * 60, settings.LongBreakTime);
        }

        [AvaloniaFact]
        public void Picking_a_value_from_the_list_works_as_well_as_typing()
        {
            using var yapa = new Yapa();
            var work = General(yapa).Find<EditableComboBox>("WorkTimeSelect");
            work.SelectedItem = "45";
            Yapa.Pump();
            yapa.Save();
            Assert.Equal(45 * 60, yapa.Resolve<IPomodoroEngine>().WorkTime);
        }

        [AvaloniaFact]
        public void Close_without_save_keeps_the_old_values()
        {
            using var yapa = new Yapa();
            var page = General(yapa);
            Yapa.Click(page.CheckBox("Count backwards"));
            Assert.True(yapa.SaveEnabled);

            // Close asks before throwing changes away; the answer here is yes
            yapa.Settings.Load();
            yapa.SettingsWindow.Find<Button>("CloseButton").Command.Execute(null);
            Yapa.Pump();

            Assert.False(yapa.SettingsWindow.IsVisible);
            Assert.False(yapa.Resolve<PomodoroEngineSettings>().CountBackwards);
            Assert.DoesNotContain("CountBackwards", yapa.SettingsFile);
        }

        [AvaloniaFact]
        public void Count_backwards_shows_the_time_left()
        {
            using var yapa = new Yapa();
            Assert.Equal("00:00", Digits(yapa));
            Yapa.Click(General(yapa).CheckBox("Count backwards"));
            yapa.Save();
            Assert.Equal("25:00", Digits(yapa));

            yapa.Resolve<IPomodoroEngine>().Start();
            yapa.Clock.Advance(TimeSpan.FromSeconds(61));
            Assert.Equal("23:59", Digits(yapa));
        }

        [AvaloniaFact]
        public void Auto_start_break_starts_the_break_when_work_ends()
        {
            using var yapa = new Yapa();
            Yapa.Click(General(yapa).Find<CheckBox>("AutoStartBreak"));
            yapa.Save();

            var engine = yapa.Resolve<IPomodoroEngine>();
            engine.Start();
            yapa.Clock.Advance(TimeSpan.FromMinutes(25));
            Yapa.Pump(1700); // the engine waits 1.5 s before starting the break
            Assert.Equal(PomodoroPhase.Break, engine.Phase);
            Assert.Equal("Break", yapa.Timer.Find<TextBlock>("StageTextBlock").Text);
        }

        [AvaloniaFact]
        public void Auto_start_work_starts_work_when_the_break_ends()
        {
            using var yapa = new Yapa();
            var page = General(yapa);
            Yapa.Click(page.Find<CheckBox>("AutoStartBreak"));
            Yapa.Click(page.Find<CheckBox>("AutoStartWork"));
            yapa.Save();

            var engine = yapa.Resolve<IPomodoroEngine>();
            engine.Start();
            yapa.Clock.Advance(TimeSpan.FromMinutes(25));
            Yapa.Pump(1700);
            yapa.Clock.Advance(TimeSpan.FromMinutes(5));
            Yapa.Pump(1700);
            Assert.Equal(PomodoroPhase.Work, engine.Phase);
            Assert.Equal(2, engine.Index);
        }

        [AvaloniaFact]
        public void Without_auto_start_the_timer_waits_with_skip_and_start()
        {
            using var yapa = new Yapa();
            var engine = yapa.Resolve<IPomodoroEngine>();
            engine.Start();
            yapa.Clock.Advance(TimeSpan.FromMinutes(25));

            Assert.Equal(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.Equal("Work Ended", yapa.Timer.Find<TextBlock>("StageTextBlock").Text);
            Assert.True(yapa.Timer.Find<Button>("Start").IsVisible);
            Assert.True(yapa.Timer.Find<Button>("Skip").IsVisible);
            Assert.False(yapa.Timer.Find<Button>("Stop").IsVisible);
        }

        [AvaloniaTheory]
        [InlineData("Pomodoro index", "2")]
        [InlineData("Completed this session", "1")]
        [InlineData("Completed today", "1")]
        public void Display_count_of_chooses_the_counter_under_the_buttons(string choice, string expected)
        {
            using var yapa = new Yapa();
            var list = General(yapa).Find<ComboBox>("CounterList");
            list.SelectedItem = list.Items.OfType<CounterListItem>().First(x => x.Title == choice);
            Yapa.Pump();
            if (choice != "Pomodoro index") // the default: choosing it is no change
            {
                yapa.Save();
            }

            // one finished pomodoro, then the next one started
            var engine = yapa.Resolve<IPomodoroEngine>();
            engine.Start();
            yapa.Clock.Advance(TimeSpan.FromMinutes(25));
            engine.Start();
            yapa.Clock.Advance(TimeSpan.FromMinutes(5));
            engine.Start();
            Yapa.Pump();

            Assert.Equal(expected, yapa.Timer.Find<TextBlock>("CurrentPeriod").Text);
        }

        [AvaloniaFact]
        public void Font_choice_changes_the_digits_font()
        {
            using var yapa = new Yapa();
            var fonts = General(yapa).Find<ComboBox>("FontSelector");
            var items = fonts.Items.OfType<KeyValuePair<string, string>>().ToList();
            Assert.Contains(items, x => x.Key == "Segoe UI Light.ttf");
            var other = items.First(x => x.Key != "Segoe UI Light.ttf");

            fonts.SelectedItem = other;
            Yapa.Pump();
            yapa.Save();

            var digit = yapa.Timer.Find<TextBlock>("CurrentTimeMinutes");
            Assert.Equal(new FontFamily(other.Value), digit.FontFamily);
            Assert.Contains($"\"FontFamily\":\"{other.Key}\"", yapa.SettingsFile);
        }

        [AvaloniaFact]
        public void Switching_profile_shows_that_profiles_times()
        {
            using var yapa = new Yapa(TwoProfiles);
            var page = General(yapa);
            var profiles = page.Find<ComboBox>("ActiveProfileSelect");
            Assert.Equal("Pomodoro", profiles.SelectedItem);
            Assert.Equal("25", page.Find<EditableComboBox>("WorkTimeSelect").Text);

            profiles.SelectedItem = "Writing";
            Yapa.Pump();

            Assert.Equal("50", page.Find<EditableComboBox>("WorkTimeSelect").Text);
            Assert.Equal("3", page.Find<EditableComboBox>("SessionRounds").Text);
            Assert.True(page.Find<CheckBox>("AutoStartBreak").IsChecked);
            yapa.Save();
            Assert.Equal(50 * 60, yapa.Resolve<IPomodoroEngine>().WorkTime);
        }

        [AvaloniaFact]
        public void Remove_deletes_the_active_profile_and_is_disabled_for_the_last_one()
        {
            using var yapa = new Yapa(TwoProfiles);
            var page = General(yapa);
            var remove = page.Find<Button>("RemoveButton");
            Assert.True(remove.IsEffectivelyEnabled);

            Yapa.Click(remove);

            var profiles = page.Find<ComboBox>("ActiveProfileSelect");
            Assert.Equal(new[] { "Writing" }, profiles.Items.OfType<string>().ToArray());
            Assert.False(remove.IsEffectivelyEnabled);
            yapa.Save();
            Assert.DoesNotContain("\"Pomodoro\":{", yapa.SettingsFile);
        }

        [AvaloniaFact]
        public void Profile_name_dialog_refuses_a_name_in_use_whatever_the_case()
        {
            var dialog = new CreatePomodoroProfile(new List<string> { "Pomodoro" });
            dialog.Show();
            Yapa.Pump();
            Yapa.Type(dialog.Find<TextBox>("ProfileName"), "pomodoro");
            Yapa.Click(dialog.Find<Button>("Save"));
            Assert.True(dialog.IsVisible); // stays open; a message says the name is in use
        }

        [AvaloniaFact]
        public void Profile_name_dialog_returns_a_new_name()
        {
            var dialog = new CreatePomodoroProfile(new List<string> { "Pomodoro" });
            dialog.Show();
            Yapa.Pump();
            Yapa.Type(dialog.Find<TextBox>("ProfileName"), "Reading");
            Yapa.Click(dialog.Find<Button>("Save"));
            Assert.False(dialog.IsVisible);
            Assert.Equal("Reading", dialog.SelectedName);
        }

        [AvaloniaFact]
        public void Profile_controls_are_locked_while_the_timer_runs()
        {
            using var yapa = new Yapa(TwoProfiles);
            yapa.Resolve<IPomodoroEngine>().Start();
            var page = General(yapa);
            Assert.False(page.Find<ComboBox>("ActiveProfileSelect").IsEffectivelyEnabled);
            Assert.False(page.Find<Button>("AddButton").IsEffectivelyEnabled);
            Assert.False(page.Find<Button>("RemoveButton").IsEffectivelyEnabled);
        }
    }
}
