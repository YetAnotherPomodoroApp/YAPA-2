using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using YAPA.Avalonia.Infrastructure;
using Xunit;

namespace YAPA.Avalonia.Tests
{
    /// <summary>Screen reader and keyboard support, as in the WPF version (README: Accessibility).</summary>
    public class AccessibilityTests
    {
        private static AbstractWindow Timer(Yapa yapa) => (AbstractWindow)yapa.Timer;

        [AvaloniaFact]
        public void Timer_buttons_have_names_and_a_tab_order()
        {
            using var yapa = new Yapa();
            var expected = new[] { ("Skip", "Skip break", 1), ("Start", "Start", 2), ("Pause", "Pause", 3), ("Stop", "Stop", 4), ("Minimize", "Minimize", 5), ("Exit", "Close", 6) };
            foreach (var (name, spoken, tab) in expected)
            {
                var button = yapa.Timer.Find<Button>(name);
                Assert.Equal(spoken, AutomationProperties.GetName(button));
                Assert.Equal(tab, button.TabIndex);
            }
        }

        [AvaloniaFact]
        public void The_digits_are_read_as_one_text()
        {
            using var yapa = new Yapa();
            var digits = yapa.Timer.Find<SpokenStackPanel>("CurrentTime");
            var peer = ControlAutomationPeer.CreatePeerForElement(digits);
            //What the screen shows before the start: 00:00, counting up
            Assert.Equal("Timer, 0 seconds", peer.GetName());
            Assert.Empty(peer.GetChildren());
        }

        [AvaloniaFact]
        public void Starting_pausing_and_stopping_are_spoken()
        {
            using var yapa = new Yapa();
            Yapa.Click(yapa.Timer.Find<Button>("Start"));
            Yapa.Pump();
            Assert.Equal("Work started. 25 minutes.", Timer(yapa).LastAnnouncement?.Trim());

            yapa.Clock.Advance(TimeSpan.FromSeconds(60));
            Yapa.Click(yapa.Timer.Find<Button>("Pause"));
            Yapa.Pump();
            Assert.Equal("Paused. 24 minutes left.", Timer(yapa).LastAnnouncement?.Trim());

            Yapa.Click(yapa.Timer.Find<Button>("Stop"));
            Yapa.Pump();
            Assert.Equal("Timer stopped.", Timer(yapa).LastAnnouncement?.Trim());
        }

        [AvaloniaFact]
        public void Control_T_reads_where_the_timer_is()
        {
            using var yapa = new Yapa();
            yapa.Timer.KeyPressQwerty(PhysicalKey.T, RawInputModifiers.Control);
            Yapa.Pump();
            Assert.False(string.IsNullOrWhiteSpace(Timer(yapa).LastAnnouncement));
            Assert.Contains("25 minutes", Timer(yapa).LastAnnouncement);
        }

        [AvaloniaFact]
        public void Shift_F10_opens_a_menu_with_every_command()
        {
            using var yapa = new Yapa();
            yapa.Timer.Find<Button>("Start").Focus();
            yapa.Timer.KeyPressQwerty(PhysicalKey.F10, RawInputModifiers.Shift);
            yapa.Timer.KeyReleaseQwerty(PhysicalKey.F10, RawInputModifiers.Shift);
            Yapa.Pump();
            var menu = Timer(yapa).KeyboardMenu;
            Assert.NotNull(menu);
            var headers = menu.Items.OfType<MenuItem>().Select(x => x.Header as string).ToList();
            Assert.Equal(new[] { "_Start", "_Pause", "S_top", "S_kip", "_Reset session", "Read the _timer", "S_ettings", "_Minimize", "E_xit" }, headers);
        }

        [AvaloniaFact]
        public void Tray_icon_tooltip_tells_the_remaining_time()
        {
            using var yapa = new Yapa("{\"PluginManager\":{\"EnabledPlugins\":[\"SystemTray\"]}}");
            var icon = TrayIcon.GetIcons(Application.Current).Last();
            Assert.StartsWith("YAPA 2: ", icon.ToolTipText);

            Yapa.Click(yapa.Timer.Find<Button>("Start"));
            yapa.Clock.Advance(TimeSpan.FromSeconds(90));
            Yapa.Pump();
            Assert.Contains("23 minutes 30 seconds", icon.ToolTipText);
        }

        [AvaloniaTheory]
        [InlineData("General")]
        [InlineData("Themes")]
        [InlineData("Sound")]
        [InlineData("System tray")]
        [InlineData("Unclickable")]
        [InlineData("Dashboard")]
        public void Every_control_on_a_settings_page_has_a_spoken_name(string pageTitle)
        {
            using var yapa = new Yapa("{\"PluginManager\":{\"EnabledPlugins\":[\"SystemTray\",\"Unclickable\"]}}");
            var page = yapa.OpenSettings(pageTitle);
            var controls = page.GetLogicalDescendants().OfType<Control>()
                .Where(x => x is ComboBox || x is Slider || x is TextBox || x is ColorPickerBox || x is Button)
                .Where(x => x.IsEffectivelyVisible && !(x.GetLogicalParent() is ComboBox || x.GetLogicalParent() is ColorPickerBox))
                .ToList();
            Assert.NotEmpty(controls);
            var unnamed = controls.Where(x => string.IsNullOrWhiteSpace(AutomationProperties.GetName(x)) && !(x is Button b && b.Content is string))
                .Select(x => $"{x.GetType().Name} {x.Name}").ToList();
            Assert.True(unnamed.Count == 0, "no spoken name: " + string.Join(", ", unnamed));
        }
    }
}
