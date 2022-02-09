using YAPA.Shared.Contracts;

namespace YAPA.Plugins.Dashboard
{
    public class PomodorosPerTimeModel
    {
        public PomodoroEntity Pomodoro { get; set; }
        public int Month { get; set; }
        public int Week { get; set; }
    }
}
