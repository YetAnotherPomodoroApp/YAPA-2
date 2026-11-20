using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Plugins.SaveApplicationPosition
{
    [BuiltInPlugin(Hide = true)]
    public class SaveApplicationPositionOnScreenPlugin : IPluginMeta
    {
        public string Title => "Save application position on screen";

        public string Id => "SaveApplicationPositionOnScreen";

        public Type Plugin => typeof(SaveApplicationPositionOnScreen);

        public Type Settings => typeof(SaveApplicationPositionOnScreenSettings);

        public Type SettingEditWindow => null;
    }

    /// <summary>
    /// Puts the timer back where it was. The WPF plugin stores a Windows WINDOWPLACEMENT under
    /// "Position"; this one stores "x,y" in pixels under its own key so the two never clash.
    /// </summary>
    public class SaveApplicationPositionOnScreen : IPlugin
    {
        private readonly Window _window;
        private readonly SaveApplicationPositionOnScreenSettings _settings;

        public SaveApplicationPositionOnScreen(IApplication app, SaveApplicationPositionOnScreenSettings settings)
        {
            _window = (Window)app;
            _settings = settings;
            app.Closing += App_Closing;

            _window.WindowStartupLocation = WindowStartupLocation.Manual;
            // Placed once the window exists: on macOS a position set before that leaves the
            // window without a size
            _window.Opened += Restore;
        }

        private void Restore(object sender, EventArgs e)
        {
            _window.Opened -= Restore;
            var saved = Parse(_settings.ScreenPosition);
            if (saved.HasValue)
            {
                _window.Position = saved.Value;
                KeepOnScreen(sender, e);
            }
            else
            {
                // First start: top right of the main screen, out of the way
                PlaceFirstTime(sender, e);
            }
        }

        private void PlaceFirstTime(object sender, EventArgs e)
        {
            var screen = _window.Screens.Primary;
            if (screen == null)
            {
                return;
            }
            var area = screen.WorkingArea;
            var width = (int)(_window.Width * screen.Scaling);
            _window.Position = new PixelPoint(area.Right - width - (int)(40 * screen.Scaling), area.Y + (int)(40 * screen.Scaling));
        }

        // A screen that was unplugged would leave the timer out of reach
        private void KeepOnScreen(object sender, EventArgs e)
        {
            var position = _window.Position;
            if (_window.Screens.All.Any(s => s.Bounds.Contains(position)))
            {
                return;
            }
            var area = _window.Screens.Primary?.WorkingArea;
            if (area.HasValue)
            {
                _window.Position = new PixelPoint(area.Value.X + 40, area.Value.Y + 40);
            }
        }

        private void App_Closing()
        {
            var p = _window.Position;
            // Not deferred, so this is written to localSettings.json straight away
            _settings.ScreenPosition = string.Create(CultureInfo.InvariantCulture, $"{p.X},{p.Y}");
        }

        private static PixelPoint? Parse(string value)
        {
            var parts = value?.Split(',');
            if (parts?.Length == 2
                && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)
                && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
            {
                return new PixelPoint(x, y);
            }
            return null;
        }
    }

    public class SaveApplicationPositionOnScreenSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public string ScreenPosition
        {
            get => _settings.Get<string>(nameof(ScreenPosition), null, true);
            set => _settings.Update(nameof(ScreenPosition), value, true);
        }

        public SaveApplicationPositionOnScreenSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(new SaveApplicationPositionOnScreenPlugin().Id);
        }

        public void DeferChanges() => _settings.DeferChanges();
    }
}
