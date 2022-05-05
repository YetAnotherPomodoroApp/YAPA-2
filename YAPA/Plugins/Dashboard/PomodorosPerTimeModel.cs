using YAPA.Shared.Contracts;

namespace YAPA.Plugins.Dashboard
{
    public class PomodorosPerTimeModel
    {
        public PomodoroGithubDashboardModel Pomodoro { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Week { get; set; }
    }
}
