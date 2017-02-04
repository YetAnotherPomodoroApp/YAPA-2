using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using YAPA.Contracts;

namespace YAPA
{
    public class ItemRepository : IPomodoroRepository
    {
        private DatabaseContext context;

        public ItemRepository()
        {
            context = new DatabaseContext(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YAPA2","Yapa.db"));
            context.Database.Migrate();
        }

        public IQueryable<PomodoroEntity> Pomodoros => context.Pomodoros;

        public void Delete(int id)
        {
            var existing = Pomodoros.FirstOrDefault(x => x.Id == id);

            if (existing != null)
            {
            }

        }

        public void Add(PomodoroEntity pomodoroEntity)
        {
            context.Pomodoros.Add(pomodoroEntity);
            context.SaveChanges();
        }
    }

    public enum PomodoroLevelEnum
    {
        Level0 = 0,
        Level1 = 1,
        Level2 = 2,
        Level3 = 3,
        Level4 = 4,
    }
}
