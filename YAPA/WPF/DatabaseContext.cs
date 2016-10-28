using System;
using System.Data.Entity;
using YAPA.Shared.Contracts;

namespace YAPA
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(string connString) : base(connString)
        {

        }
        public DbSet<PomodoroEntity> Pomodoros { get; set; }
    }

    public static class PomodoroExtensions
    {

        public static PomodoroViewModel ToPomodoroViewModel(this PomodoroEntity pomo, PomodoroLevelEnum level = PomodoroLevelEnum.Level0)
        {
            return new PomodoroViewModel()
            {
                Count = pomo.Count,
                DateTime = pomo.DateTime,
                Level = level
            };
        }
    }

    public class PomodoroViewModel
    {
        public DateTime DateTime { get; set; }

        public int Count { get; set; }

        public PomodoroLevelEnum Level { get; set; }
    }
}
