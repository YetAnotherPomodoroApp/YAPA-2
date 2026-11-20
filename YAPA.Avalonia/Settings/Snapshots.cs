#if DEBUG
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Controls.Primitives;

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
                if (Environment.GetEnvironmentVariable("YAPA_DUMP") == name)
                {
                    foreach (var kind in new[] { typeof(ComboBox), typeof(TextBox), typeof(Button), typeof(CheckBox) })
                    {
                        var control = window.GetVisualDescendants().FirstOrDefault(x => x.GetType() == kind && !(x.GetVisualAncestors().OfType<TreeView>().Any())) as Control;
                        if (control != null)
                        {
                            Dump(control, 0, Path.Combine(folder, "dump.txt"));
                        }
                    }
                }
            }
        }

        private static void Dump(Visual visual, int depth, string file)
        {
            var c = visual as Control;
            var layoutable = visual as global::Avalonia.Layout.Layoutable;
            var padding = (visual as TemplatedControl)?.Padding.ToString() ?? (visual as Decorator)?.Padding.ToString() ?? (visual as global::Avalonia.Controls.Presenters.ContentPresenter)?.Padding.ToString() ?? "";
            File.AppendAllText(file, $"{new string(' ', depth * 2)}{visual.GetType().Name}#{c?.Name} classes={string.Join(",", c?.Classes ?? new Classes())} bounds={visual.Bounds} margin={layoutable?.Margin} pad={padding} minH={layoutable?.MinHeight} h={layoutable?.Height} font={(visual as TemplatedControl)?.FontSize}{(visual as TextBlock)?.FontSize}\n");
            foreach (var child in visual.GetVisualChildren())
            {
                Dump(child, depth + 1, file);
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
        }

        public static void Save(Control control, string file)
        {
            var scaling = TopLevel.GetTopLevel(control)?.RenderScaling ?? 1;
            var size = new PixelSize((int)(control.Bounds.Width * scaling), (int)(control.Bounds.Height * scaling));
            using (var bitmap = new RenderTargetBitmap(size, new Vector(96 * scaling, 96 * scaling)))
            {
                bitmap.Render(control);
                bitmap.Save(file);
            }
        }
    }
}
#endif
