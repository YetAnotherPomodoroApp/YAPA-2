using System;
using System.Data.Entity;
using YAPA.Contracts;

namespace YAPA
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(string connString) : base(connString)
        {

        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PomodoroEntity>().HasKey(x => x.Id);
            base.OnModelCreating(modelBuilder);
        }


        public DbSet<PomodoroEntity> Pomodoros { get; set; }
    }

    public static class PomodoroExtensions
    {

        public static PomodoroViewModel ToPomodoroViewModel(this PomodoroEntity pomo, int week, PomodoroLevelEnum level = PomodoroLevelEnum.Level0)
        {
            return new PomodoroViewModel()
            {
                Count = pomo.Count,
                DateTime = pomo.DateTime,
                Week = week,
                Level = level
            };
        }
    }

    public class PomodoroViewModel
    {
        public DateTime DateTime { get; set; }

        public int Count { get; set; }

        public PomodoroLevelEnum Level { get; set; }
        public int Week { get; set; }
    }
}
