using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace YAPA.Avalonia.Infrastructure
{
    public enum MessageBoxButton
    {
        OK,
        YesNo
    }

    public enum MessageBoxResult
    {
        None,
        OK,
        Yes,
        No
    }

    /// <summary>
    /// Stand-in for System.Windows.MessageBox: a small modal window with a message and buttons.
    /// </summary>
    public static class MessageBox
    {
        public static async Task<MessageBoxResult> Show(Window owner, string text, string caption, MessageBoxButton buttons = MessageBoxButton.OK)
        {
            var result = MessageBoxResult.None;
            var dialog = new Window
            {
                Title = caption,
                SizeToContent = SizeToContent.WidthAndHeight,
                CanResize = false,
                ShowInTaskbar = false,
                Topmost = true,
                MinWidth = 260,
                MaxWidth = 460,
                WindowStartupLocation = owner != null && owner.IsVisible ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            };

            var panel = new StackPanel { Margin = new Thickness(20, 18, 20, 12), Spacing = 16 };
            panel.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap });

            var buttonRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 4 };
            void Add(string label, MessageBoxResult value, bool isDefault)
            {
                var button = new Button { Content = label, IsDefault = isDefault, MinWidth = 64 };
                button.Classes.Add("Flat");
                button.Click += (s, e) =>
                {
                    result = value;
                    dialog.Close();
                };
                buttonRow.Children.Add(button);
            }

            if (buttons == MessageBoxButton.YesNo)
            {
                Add("Yes", MessageBoxResult.Yes, true);
                Add("No", MessageBoxResult.No, false);
            }
            else
            {
                Add("OK", MessageBoxResult.OK, true);
            }
            panel.Children.Add(buttonRow);
            dialog.Content = panel;

            var parent = owner != null && owner.IsVisible ? owner : MainWindow();
            if (parent != null && parent.IsVisible)
            {
                await dialog.ShowDialog(parent);
            }
            else
            {
                var closed = new TaskCompletionSource<bool>();
                dialog.Closed += (s, e) => closed.TrySetResult(true);
                dialog.Show();
                await closed.Task;
            }

            if (result == MessageBoxResult.None)
            {
                result = buttons == MessageBoxButton.YesNo ? MessageBoxResult.No : MessageBoxResult.OK;
            }
            return result;
        }

        private static Window MainWindow()
        {
            return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        }
    }
}
