using System.Windows.Media;

namespace YAPA.WPF.Shared.Shared
{
    public static class YapaHelpers
    {
        public static Color ColorFromString(string color, Color defaultColor)
        {
            try
            {
                return (Color)(ColorConverter.ConvertFromString(color) ?? defaultColor);
            }
            catch
            {
                return defaultColor;
            }
        }
    }
}
