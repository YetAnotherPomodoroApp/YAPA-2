using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using YAPA.Shared.Contracts;
using YAPA.WPF;

namespace YAPA.Plugins.Dashboard
{
    public class ItemRepository : IPomodoroRepository
    {
        private readonly DatabaseContext _context;

        public ItemRepository()
        {
            _context = new DatabaseContext(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YAPA2","Yapa.db"));
            _context.Database.Migrate();
        }

        public IQueryable<PomodoroEntity> Pomodoros => _context.Pomodoros;

        public void Delete(int id)
        {
            var existing = Pomodoros.FirstOrDefault(x => x.Id == id);

            if (existing != null)
            {
                _context.Pomodoros.Remove(existing);
                _context.SaveChanges();
            }
        }

        public void Add(PomodoroEntity pomodoroEntity)
        {
            _context.Pomodoros.Add(pomodoroEntity);
            _context.SaveChanges();
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
