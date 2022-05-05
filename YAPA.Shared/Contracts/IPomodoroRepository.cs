using System;
using System.Collections.Generic;
using System.Linq;

namespace YAPA.Shared.Contracts
{
    public interface IPomodoroRepository
    {
        IQueryable<PomodoroEntity> Pomodoros { get; }

        void Delete(int id);
        void Add(PomodoroEntity pomo);
    }

    public class PomodoroEntity
    {
        public int Id { get; set; }
        public string ProfileName { get; set; }

        public DateTime DateTime { get; set; }
        public int Count { get; set; }
        public int DurationMin { get; set; }
    }

    public class PomodoroGithubDashboardModel
    {
        public DateTime DateTime { get; set; }
        public int Count { get; set; }
        public int DurationMin { get; set; }
    }

    public class PomodoroDashboardModel
    {
        public IEnumerable<PomodoroGithubDashboardModel> DashboardItems { get; set; }
        public IEnumerable<string> Profiles { get; set; }
    }
}
