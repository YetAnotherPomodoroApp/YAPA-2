using System;
using System.Collections.Generic;

namespace YAPA.Shared.Contracts
{
    public interface IPomodoroRepository
    {
        void Delete(int id);
        void Add(PomodoroEntity pomo);
        int CompletedToday();
        IEnumerable<PomodoroEntity> After(DateTime date);
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
