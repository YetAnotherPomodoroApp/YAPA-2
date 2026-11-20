using Avalonia.Controls;
using Avalonia.Media;

namespace YAPA.Avalonia.Infrastructure
{
    public static class ScreenHelpers
    {
        /// <summary>
        /// Window sizes are in device-independent units; positions are in screen units. Screen
        /// units are pixels on Windows and Linux but points on macOS, so use the screen's own
        /// scaling (1 on a Retina Mac), not the window's render scaling.
        /// </summary>
        public static double ScreenScaling(this Window window)
        {
            return window.Screens.ScreenFromWindow(window)?.Scaling ?? window.Screens.Primary?.Scaling ?? 1;
        }
    }

    public static class ColorHelpers
    {
        /// <summary>Reads a colour the way WPF wrote it: a name ("White") or #AARRGGBB.</summary>
        public static Color FromString(string color, Color defaultColor)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return defaultColor;
            }
            return Color.TryParse(color, out var parsed) ? parsed : defaultColor;
        }

        /// <summary>Writes #AARRGGBB in upper case, as WPF's Color.ToString() does.</summary>
        public static string ToSetting(Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
