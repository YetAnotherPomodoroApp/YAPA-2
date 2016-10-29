using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace YAPA.Contracts
{
    public interface IPomodoroRepository
    {
        IQueryable<PomodoroEntity> Pomodoros { get; }

        void Delete(int id);
        void Add(PomodoroEntity pomo);
    }

    public class PomodoroEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTime DateTime { get; set; }

        public int Count { get; set; }

    }
}
