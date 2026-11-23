using YAPA.Shared.Contracts;

namespace YAPA.Shared.Common
{
    /// <summary>
    /// The sentences a screen reader speaks for the timer.
    /// </summary>
    public static class TimerSpeech
    {
        /// <summary>
        /// A length of time in words: "25 minutes", "1 minute 5 seconds", "30 seconds".
        /// </summary>
        public static string Duration(int totalSeconds)
        {
            if (totalSeconds < 0)
            {
                totalSeconds = 0;
            }

            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;

            var minuteText = minutes == 1 ? "1 minute" : $"{minutes} minutes";
            var secondText = seconds == 1 ? "1 second" : $"{seconds} seconds";

            if (minutes == 0)
            {
                return secondText;
            }

            return seconds == 0 ? minuteText : $"{minuteText} {secondText}";
        }

        /// <summary>
        /// What to say when the timer has moved from one phase to another.
        /// </summary>
        public static string PhaseChanged(PomodoroPhase previous, PomodoroPhase current, int remainingSeconds)
        {
            switch (current)
            {
                case PomodoroPhase.Work:
                    return previous == PomodoroPhase.Pause
                        ? $"Work resumed. {Duration(remainingSeconds)} left."
                        : $"Work started. {Duration(remainingSeconds)}.";
                case PomodoroPhase.WorkEnded:
                    return "Work finished. Time for a break.";
                case PomodoroPhase.Break:
                    return $"Break started. {Duration(remainingSeconds)}.";
                case PomodoroPhase.BreakEnded:
                    return "Break finished.";
                case PomodoroPhase.Pause:
                    return $"Paused. {Duration(remainingSeconds)} left.";
                default:
                    return "Timer stopped.";
            }
        }

        /// <summary>
        /// The answer to "where is the timer now?".
        /// </summary>
        public static string Status(PomodoroPhase phase, int remainingSeconds, int workSeconds)
        {
            switch (phase)
            {
                case PomodoroPhase.Work:
                    return $"Work. {Duration(remainingSeconds)} left.";
                case PomodoroPhase.Break:
                    return $"Break. {Duration(remainingSeconds)} left.";
                case PomodoroPhase.Pause:
                    return $"Paused. {Duration(remainingSeconds)} left.";
                case PomodoroPhase.WorkEnded:
                    return "Work finished. The break has not started.";
                case PomodoroPhase.BreakEnded:
                    return "Break finished. Work has not started.";
                default:
                    return $"Timer is not running. The work period is {Duration(workSeconds)}.";
            }
        }

        /// <summary>
        /// The digits on screen read as one piece of text.
        /// </summary>
        public static string Display(PomodoroPhase phase, int displayedSeconds, bool countsBackwards)
        {
            var time = Duration(displayedSeconds);

            switch (phase)
            {
                case PomodoroPhase.Work:
                case PomodoroPhase.WorkEnded:
                    return countsBackwards ? $"Work, {time} left" : $"Work, {time} done";
                case PomodoroPhase.Break:
                case PomodoroPhase.BreakEnded:
                    return countsBackwards ? $"Break, {time} left" : $"Break, {time} done";
                case PomodoroPhase.Pause:
                    return countsBackwards ? $"Paused, {time} left" : $"Paused, {time} done";
                default:
                    return $"Timer, {time}";
            }
        }
    }
}
