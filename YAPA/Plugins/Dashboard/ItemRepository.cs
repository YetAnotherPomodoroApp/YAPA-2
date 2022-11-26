using System;
using System.Collections.Generic;
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
            _context = new DatabaseContext(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YAPA2", "Yapa.db"));
            _context.Database.Migrate();
        }

        public void Delete(int id)
        {
            lock (_context)
            {
                var existing = _context.Pomodoros.FirstOrDefault(x => x.Id == id);

                if (existing != null)
                {
                    _context.Pomodoros.Remove(existing);
                    _context.SaveChanges();
                }
            }
        }

        public int CompletedToday()
        {
            lock (_context)
            {
                var startDate = DateTime.Now.Date;
                var endDate = startDate.AddDays(1).AddSeconds(-1);
                return _context.Pomodoros
                    .Where(pomodoro => startDate <= pomodoro.DateTime && pomodoro.DateTime <= endDate)
                    .Select(_ => _.Count)
                    .DefaultIfEmpty(0)
                    .Sum();
            }
        }

        public void Add(PomodoroEntity pomodoroEntity)
        {
            lock (_context)
            {
                _context.Pomodoros.Add(pomodoroEntity);
                _context.SaveChanges();
            }
        }

        public IEnumerable<PomodoroEntity> After(DateTime date)
        {
            lock (_context)
            {
                return _context.Pomodoros.Where(x => x.DateTime >= date).ToList();
            }
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
