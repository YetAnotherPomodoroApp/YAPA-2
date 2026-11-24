using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace YAPA.Avalonia.Plugins.Dashboard
{
    /// <summary>One month of the contribution graph: its name over a row of week columns (PomodoroMonth.xaml).</summary>
    public class PomodoroMonth : UserControl
    {
        public PomodoroMonth(IReadOnlyList<PomodoroViewModel> month)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 5, 0), RowDefinitions = new RowDefinitions("Auto,*") };
            var monthName = new TextBlock
            {
                Margin = new Thickness(4, 0, 0, 0),
                Text = month.First().DateTime.ToString("MMMM", CultureInfo.CurrentCulture),
            };
            var weeksPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
            Grid.SetRow(weeksPanel, 1);

            var weeks = month.GroupBy(x => x.Week).ToList();
            foreach (var week in weeks)
            {
                weeksPanel.Children.Add(new PomodoroWeek(week.ToList()));
            }
            weeksPanel.Width = weeks.Count * 13;

            grid.Children.Add(monthName);
            grid.Children.Add(weeksPanel);
            Content = grid;
        }
    }

    public static class LevelBrushes
    {
        public static IBrush Level0 { get; } = SolidColorBrush.Parse("#eee");
        public static IBrush Level1 { get; } = SolidColorBrush.Parse("#d6e685");
        public static IBrush Level2 { get; } = SolidColorBrush.Parse("#8cc665");
        public static IBrush Level3 { get; } = SolidColorBrush.Parse("#44a340");
        public static IBrush Level4 { get; } = SolidColorBrush.Parse("#1e6823");

        public static IBrush For(PomodoroLevelEnum level)
        {
            switch (level)
            {
                case PomodoroLevelEnum.Level0: return Level0;
                case PomodoroLevelEnum.Level1: return Level1;
                case PomodoroLevelEnum.Level2: return Level2;
                case PomodoroLevelEnum.Level3: return Level3;
                case PomodoroLevelEnum.Level4: return Level4;
                default: return Brushes.Transparent;
            }
        }
    }

    /// <summary>One week column: a square per day, 11 px with a 1 px gap (PomodoroWeek.xaml).</summary>
    public class PomodoroWeek : UserControl
    {
        public PomodoroWeek(IReadOnlyList<PomodoroViewModel> week)
        {
            Width = 13;
            Height = 100;
            var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(1, 0, 0, 0) };
            Content = panel;

            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            var cal = CultureInfo.CurrentCulture.Calendar;
            var maxDate = week.Max(x => x.DateTime);
            var minDate = week.Min(x => x.DateTime);
            var daysToAdd = 0;

            if (cal.GetDayOfWeek(minDate) == firstDayOfWeek)
            {
                //do nothing, this is start of the week
            }
            else if (cal.GetDayOfWeek(maxDate.AddDays(1)) != firstDayOfWeek)
            {
                //this is not the start of the week and not the end of the week
                var daysTillStartOfWeek = 0;
                while (cal.GetDayOfWeek(maxDate.AddDays(1)) != firstDayOfWeek)
                {
                    maxDate = maxDate.AddDays(1);
                    daysTillStartOfWeek++;
                }
                daysToAdd = 7 - week.Count - daysTillStartOfWeek;
            }
            else
            {
                daysToAdd = 7 - week.Count;
            }

            for (var i = 0; i < daysToAdd; i++)
            {
                panel.Children.Add(new Rectangle { Width = 11, Height = 11, Fill = Brushes.Transparent, Margin = new Thickness(0, 0, 0, 1) });
            }

            foreach (var day in week)
            {
                var square = new Rectangle
                {
                    Width = 11,
                    Height = 11,
                    Margin = new Thickness(0, 0, 0, 1),
                    Fill = LevelBrushes.For(day.Level),
                };
                var tip = new TextBlock();
                tip.Inlines.Add(new Run(day.Count == 0 ? "No pomodoros" : $"{day.Count} pomodoros") { FontWeight = FontWeight.Bold });
                tip.Inlines.Add(new Run($" on {day.DateTime:yyyy-MM-dd}"));
                ToolTip.SetTip(square, tip);
                //What a screen reader says for the day
                var spokenCount = day.Count == 0 ? "No pomodoros" : day.Count == 1 ? "1 pomodoro" : $"{day.Count} pomodoros";
                global::Avalonia.Automation.AutomationProperties.SetName(square, $"{spokenCount} on {day.DateTime:D}");
                panel.Children.Add(square);
            }
        }
    }
}
