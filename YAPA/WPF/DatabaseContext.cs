using System;
using Microsoft.EntityFrameworkCore;
using YAPA.Plugins.Dashboard;
using YAPA.Shared.Contracts;
using DbContext = Microsoft.EntityFrameworkCore.DbContext;
using YAPA.Shared.Common;

namespace YAPA.WPF
{
    public class DatabaseContext : DbContext
    {
        private readonly string _dbPath;

        public DatabaseContext()
        {
            
        }

        public DatabaseContext(string dbPath)
        {
            _dbPath = dbPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Filename={_dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
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
                DateTime = pomo.DateTime.TryToLocalTime(),
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
