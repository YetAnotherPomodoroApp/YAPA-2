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
        /// What F1 says: the keys of the timer window, and where a screen reader finds the time.
        /// </summary>
        public const string KeyboardHelp =
            "Keyboard help. Control+T reads the time left. Tab moves between the timer buttons. " +
            "The Menu key or Shift+F10 opens a menu with every command. " +
            "The window title tells the minutes left, and so does the status bar: in NVDA, Insert+T and Insert+End. " +
            "Settings, System tray, Always show tray icon keeps the time in the tray. F1 repeats this help.";

        /// <summary>
        /// Where the timer is, in whole minutes, so that it changes once a minute: for the tray icon's tooltip and the window title.
        /// A screen reader reads those again whenever they change while they have its attention.
        /// </summary>
        public static string Minutes(PomodoroPhase phase, int remainingSeconds)
        {
            switch (phase)
            {
                case PomodoroPhase.Work:
                    return $"Work, {MinutesLeft(remainingSeconds)}";
                case PomodoroPhase.Break:
                    return $"Break, {MinutesLeft(remainingSeconds)}";
                case PomodoroPhase.Pause:
                    return $"Paused, {MinutesLeft(remainingSeconds)}";
                case PomodoroPhase.WorkEnded:
                    return "Work finished";
                case PomodoroPhase.BreakEnded:
                    return "Break finished";
                default:
                    return "Timer is not running";
            }
        }

        /// <summary>
        /// The window title. A screen reader reads it on request (NVDA: Insert+T) and when switching windows,
        /// and it shows in the taskbar. The time comes first, so it fits.
        /// </summary>
        public static string WindowTitle(PomodoroPhase phase, int remainingSeconds)
        {
            const string app = "YAPA 2";
            return phase == PomodoroPhase.NotStarted ? app : $"{Minutes(phase, remainingSeconds)} - {app}";
        }

        //Rounded up, the way a countdown is read: 24:30 left is "25 minutes left", 0:40 is "1 minute left"
        private static string MinutesLeft(int remainingSeconds)
        {
            var minutes = (System.Math.Max(remainingSeconds, 0) + 59) / 60;

            if (minutes == 0)
            {
                return "less than a minute left";
            }

            return minutes == 1 ? "1 minute left" : $"{minutes} minutes left";
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
