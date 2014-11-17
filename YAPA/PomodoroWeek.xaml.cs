using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace YAPA
{
    /// <summary>
    /// Interaction logic for PomdoroWeek.xaml
    /// </summary>
    public partial class PomodoroWeek : UserControl
    {
        public PomodoroWeek(IEnumerable<PomodoroViewModel> week = null)
        {
            InitializeComponent();

            var converter = new System.Windows.Media.BrushConverter();
            var Level0 = (Brush)converter.ConvertFromString("#eee");
            var Level1 = (Brush)converter.ConvertFromString("#d6e685");
            var Level2 = (Brush)converter.ConvertFromString("#8cc665");
            var Level3 = (Brush)converter.ConvertFromString("#44a340");
            var Level4 = (Brush)converter.ConvertFromString("#1e6823");

            if (week == null)
            {
                return;
            }

            if (week.Count() < 7)
            {
                var firstDay = week.Min(x => x.DateTime);
                if (firstDay.DayOfWeek != DayOfWeek.Monday)
                {
                    for (int i = 0; i < 7 - week.Count(); i++)
                    {
                        var green = new Rectangle();
                        green.Width = 13;
                        green.Height = 11;
                        green.Fill = Brushes.White;
                        green.Margin = new Thickness(0, 0, 0, 1);
                        PomodorPanel.Children.Add(green);
                    }
                }
            }

            foreach (var pomodoroViewModel in week)
            {
                var green = new Rectangle();
                green.Width = 13;
                green.Height = 11;

                switch (pomodoroViewModel.Level)
                {
                    case PomodoroLevelEnum.Level0:
                        green.Fill = Level0;
                        break;
                    case PomodoroLevelEnum.Level1:
                        green.Fill = Level1;
                        break;
                    case PomodoroLevelEnum.Level2:
                        green.Fill = Level2;
                        break;
                    case PomodoroLevelEnum.Level3:
                        green.Fill = Level3;
                        break;
                    case PomodoroLevelEnum.Level4:
                        green.Fill = Level4;
                        break;
                }

                green.Margin = new Thickness(0, 0, 0, 1);

                ToolTip toolTip = new ToolTip();
                TextBlock pomodoros = new TextBlock();
                pomodoros.Inlines.Add(
                    new Bold(new Run(pomodoroViewModel.Count == 0 ? "No pomodoros" : string.Format("{0} pomodoros", pomodoroViewModel.Count))));
                pomodoros.Inlines.Add(new Run(string.Format(" on {0}", pomodoroViewModel.DateTime.ToString("yyyy-MM-dd"))));
                toolTip.Content = pomodoros;

                green.ToolTip = toolTip;


                PomodorPanel.Children.Add(green);
            }

            PomodorPanel.Margin = new Thickness(1, 0, 0, 0);
        }
    }
}
