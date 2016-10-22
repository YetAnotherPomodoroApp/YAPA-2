using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace YAPA
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(string connString) : base(connString)
        {

        }
        public DbSet<PomodoroEntity> Pomodoros { get; set; }
    }

    public class PomodoroEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTime DateTime { get; set; }

        public int Count { get; set; }

        public PomodoroViewModel ToPomodoroViewModel(PomodoroLevelEnum level = PomodoroLevelEnum.Level0)
        {
            return new PomodoroViewModel()
            {
                Count = Count,
                DateTime = DateTime,
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
