using System.Windows.Media;

namespace Motivational
{
    public class Utils
    {
        /// <summary>
        /// Convert HEX to SolidColorBrush
        /// </summary>
        /// <param name="hexValue">Color HEX code</param>
        /// <returns></returns>
        public static SolidColorBrush HexToBrush(string hexValue)
        {
            if (string.IsNullOrWhiteSpace(hexValue))
                return null;

            return (new BrushConverter().ConvertFrom(hexValue)) as SolidColorBrush;
        }

        /// <summary>
        /// Converts HEX to Color
        /// </summary>
        /// <param name="hexValue">Color HEX code</param>
        /// <returns></returns>
        public static Color HexToColor(string hexValue)
        {
            if (string.IsNullOrWhiteSpace(hexValue))
                return Colors.White;

            return (Color)(new ColorConverter().ConvertFrom(hexValue));
        }


        /// <summary>
        /// Brush to HEX code
        /// </summary>
        /// <param name="brush"></param>
        /// <returns></returns>
        public static string BrushToHex(SolidColorBrush brush)
        {
            if (null == brush)
                return null;

            return brush.Color.ToString();
        }

        /// <summary>
        /// Color to HEX code
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string ColorToHex(Color color)
        {
            if (null == color)
                return null;

            return color.ToString();
        }
    }
}
