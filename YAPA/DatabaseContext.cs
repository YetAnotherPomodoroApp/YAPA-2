using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAPA
{
    public class DatabaseContext : DbContext
    {

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
