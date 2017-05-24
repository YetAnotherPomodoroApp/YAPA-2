using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using YAPA.Shared;

namespace YAPA.WPF
{
    public partial class PomodoroEngineSettingWindow : UserControl
    {

        public PomodoroEngineSettingWindow(PomodoroEngineSettings settings)
        {
            settings.DeferChanges();
            InitializeComponent();

            //HACK!
            Enumerable.Range(1, 60).Select(x =>
             {
                 WorkTimeSelect.Items.Add(x);
                 BreakTimeSelect.Items.Add(x);
                 LongBreakTimeSelect.Items.Add(x);

                 return true;
             }).ToList();

            DataContext = settings;
        }

    }

    public class SecondsToMinutesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var seconds = (int)value;
            return seconds / 60;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var minutes = int.Parse(value.ToString());
            return minutes * 60;
        }
    }
}
