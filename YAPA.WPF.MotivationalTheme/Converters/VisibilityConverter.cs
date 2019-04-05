using System;
using System.Collections;
using System.Windows;

namespace Motivational.Converters
{
    public class VisibilityConverter : System.Windows.Data.IValueConverter
    {
        public enum Mode
        {
            Default,
            Inverted,
            HideOnly,
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility collapsedVisibility = Visibility.Collapsed;
            Visibility returnVisibility = Visibility.Visible;
            Mode mode = Mode.Default;

            // Resolve input parameter
            if (parameter == null || !Enum.TryParse<Mode>((string)parameter, true, out mode))
                mode = Mode.Default;

            if (mode == Mode.HideOnly)
                collapsedVisibility = Visibility.Hidden;

            if (value == null)
            {
                returnVisibility = collapsedVisibility;
            }
            else if (value is bool)
            {
                bool bVal = (bool)value;
                if (!bVal)
                    returnVisibility = collapsedVisibility;
            }
            else if (value is string)
            {
                string itemVal = value as String;

                if (String.IsNullOrWhiteSpace(itemVal))
                    returnVisibility = collapsedVisibility;
            }
            else if (value is IList)
            {
                IList objectList = value as IList;
                if (objectList == null || objectList.Count == 0)
                    returnVisibility = collapsedVisibility;
            }

            if (mode == Mode.Inverted)
                return returnVisibility == Visibility.Visible ? collapsedVisibility : Visibility.Visible;
            else
                return returnVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
