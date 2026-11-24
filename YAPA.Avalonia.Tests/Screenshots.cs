using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Xunit;

namespace YAPA.Avalonia.Tests
{
    /// <summary>
    /// Saves a picture of every settings page, to look at layout changes:
    /// YAPA_SCREENSHOTS=/some/folder dotnet test --filter Screenshots
    /// Does nothing without YAPA_SCREENSHOTS.
    /// </summary>
    public class Screenshots
    {
        [AvaloniaFact]
        public void Every_settings_page()
        {
            var folder = Environment.GetEnvironmentVariable("YAPA_SCREENSHOTS");
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }
            Directory.CreateDirectory(folder);
            //Plugins on, so their pages are in the menu too
            using var yapa = new Yapa("{\"PluginManager\":{\"EnabledPlugins\":[\"SystemTray\",\"Unclickable\"]}}");
            yapa.OpenSettings("General");
            var tree = yapa.SettingsWindow.Find<TreeView>("SettingsTree");
            foreach (var parent in tree.GetLogicalDescendants().OfType<TreeViewItem>())
            {
                parent.IsExpanded = true;
            }
            Yapa.Pump();
            var titles = tree.GetLogicalDescendants().OfType<TreeViewItem>()
                .Select(x => (x.Header as TextBlock)?.Text).Where(x => x != null).ToList();
            var n = 0;
            foreach (var title in titles)
            {
                yapa.OpenSettings(title);
                Yapa.Pump(300);
                var frame = yapa.SettingsWindow.CaptureRenderedFrame();
                frame?.Save(Path.Combine(folder, $"{++n:00}-{title.Replace(' ', '-')}.png"));
            }
        }
    }
}
