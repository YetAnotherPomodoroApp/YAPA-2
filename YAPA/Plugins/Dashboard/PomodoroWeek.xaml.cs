using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using YAPA.WPF;

namespace YAPA.Plugins.Dashboard
{
    public static class LevelBrushes
    {
        public static Brush Level0 { get; }
        public static Brush Level1 { get; }
        public static Brush Level2 { get; }
        public static Brush Level3 { get; }
        public static Brush Level4 { get; }

        static LevelBrushes()
        {
            var converter = new BrushConverter();
            Level0 = (Brush)converter.ConvertFromString("#eee");
            Level1 = (Brush)converter.ConvertFromString("#d6e685");
            Level2 = (Brush)converter.ConvertFromString("#8cc665");
            Level3 = (Brush)converter.ConvertFromString("#44a340");
            Level4 = (Brush)converter.ConvertFromString("#1e6823");
        }
    }

    public partial class PomodoroWeek
    {
        private static Brush GreenFill(PomodoroLevelEnum level)
        {
            Brush fill = Brushes.Transparent;
            switch (level)
            {
                case PomodoroLevelEnum.Level0:
                    fill = LevelBrushes.Level0;
                    break;
                case PomodoroLevelEnum.Level1:
                    fill = LevelBrushes.Level1;
                    break;
                case PomodoroLevelEnum.Level2:
                    fill = LevelBrushes.Level2;
                    break;
                case PomodoroLevelEnum.Level3:
                    fill = LevelBrushes.Level3;
                    break;
                case PomodoroLevelEnum.Level4:
                    fill = LevelBrushes.Level4;
                    break;
            }
            return fill;
        }

        public PomodoroWeek(IEnumerable<PomodoroViewModel> week = null)
        {
            InitializeComponent();

            if (week == null)
            {
                return;
            }

            var pomodoroViewModels = week as PomodoroViewModel[] ?? week.ToArray();
            if (pomodoroViewModels.Length < 7)
            {
                var firstDay = pomodoroViewModels.Min(x => x.DateTime);
                if (firstDay.DayOfWeek != System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek)
                {
                    for (var i = 0; i < 7 - pomodoroViewModels.Length; i++)
                    {
                        var green = new Rectangle
                        {
                            Width = 11,
                            Height = 11,
                            Fill = Brushes.Transparent,
                            Margin = new Thickness(0, 0, 0, 1)
                        };
                        PomodorPanel.Children.Add(green);
                    }
                }
            }

            foreach (var pomodoroViewModel in pomodoroViewModels)
            {
                var green = new Rectangle
                {
                    Width = 11,
                    Height = 11,
                    Margin = new Thickness(0, 0, 0, 1),
                    Fill = GreenFill(pomodoroViewModel.Level)
                };

                var toolTip = new ToolTip();
                var pomodoros = new TextBlock();
                pomodoros.Inlines.Add(new Bold(new Run(pomodoroViewModel.Count == 0 ? "No pomodoros" : $"{pomodoroViewModel.Count} pomodoros")));
                pomodoros.Inlines.Add(new Run($" on {pomodoroViewModel.DateTime:yyyy-MM-dd}"));
                toolTip.Content = pomodoros;

                green.ToolTip = toolTip;

                PomodorPanel.Children.Add(green);
            }

            PomodorPanel.Margin = new Thickness(1, 0, 0, 0);
        }

        ~PomodoroWeek()
        {
            Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    PomodorPanel.Children.Clear();
                }
                catch
                {
                }
            });
        }

    }
}
