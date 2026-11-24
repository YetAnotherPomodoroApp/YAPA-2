#if DEBUG
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace YAPA.Avalonia.Settings
{
    /// <summary>
    /// Debug builds only: with YAPA_SNAPSHOTS=folder, the settings window saves a picture of
    /// every page there, to compare with the WPF version without clicking through.
    /// </summary>
    internal static class Snapshots
    {
        private static bool _done;

        public static async void RenderPages(Window window, TreeView tree)
        {
            var folder = Environment.GetEnvironmentVariable("YAPA_SNAPSHOTS");
            if (string.IsNullOrEmpty(folder) || _done)
            {
                return;
            }
            _done = true;
            Directory.CreateDirectory(folder);

            var items = tree.Items.OfType<TreeViewItem>()
                .SelectMany(x => new[] { x }.Concat(x.Items.OfType<TreeViewItem>()))
                .ToList();
            var index = 0;
            foreach (var item in items)
            {
                if (item.Parent is TreeViewItem parent)
                {
                    parent.IsExpanded = true;
                }
                tree.SelectedItem = item;
                await Task.Delay(700);
                var name = ((item.Header as TextBlock)?.Text ?? "page").Replace(' ', '-');
                Save(window, Path.Combine(folder, $"{index++:00}-{name}.png"));
            }
        }

        /// <summary>A picture of the timer window a moment after it opens.</summary>
        public static async void RenderMainWindow(Window window)
        {
            var folder = Environment.GetEnvironmentVariable("YAPA_SNAPSHOTS");
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }
            Directory.CreateDirectory(folder);
            await Task.Delay(1500);
            Save(window, Path.Combine(folder, "main.png"));
            // The same timer over a white and a grey background, to check that it stays readable
            var background = window.Background;
            foreach (var (name, color) in new[] { ("white", "#FFFFFF"), ("grey", "#808080"), ("dark", "#1E1E2E") })
            {
                window.Background = global::Avalonia.Media.SolidColorBrush.Parse(color);
                await Task.Delay(200);
                Save(window, Path.Combine(folder, $"main-{name}.png"));
            }
            window.Background = background;
        }

        public static void Save(Control control, string file)
        {
            var scaling = TopLevel.GetTopLevel(control)?.RenderScaling ?? 1;
            var size = new PixelSize((int)(control.Bounds.Width * scaling), (int)(control.Bounds.Height * scaling));
            if (size.Width < 1 || size.Height < 1)
            {
                return;
            }
            using (var bitmap = new RenderTargetBitmap(size, new Vector(96 * scaling, 96 * scaling)))
            {
                bitmap.Render(control);
                bitmap.Save(file);
            }
        }
    }
}
#endif
