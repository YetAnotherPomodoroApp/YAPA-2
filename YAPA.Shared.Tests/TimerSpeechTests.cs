using Microsoft.VisualStudio.TestTools.UnitTesting;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Shared.Tests
{
    [TestClass]
    public class TimerSpeechTests
    {
        [TestMethod]
        public void Duration_IsSpokenInMinutesAndSeconds()
        {
            Assert.AreEqual("25 minutes", TimerSpeech.Duration(25 * 60));
            Assert.AreEqual("12 minutes 34 seconds", TimerSpeech.Duration(12 * 60 + 34));
            Assert.AreEqual("1 minute", TimerSpeech.Duration(60));
            Assert.AreEqual("1 minute 1 second", TimerSpeech.Duration(61));
            Assert.AreEqual("45 seconds", TimerSpeech.Duration(45));
            Assert.AreEqual("1 second", TimerSpeech.Duration(1));
            Assert.AreEqual("0 seconds", TimerSpeech.Duration(0));
            Assert.AreEqual("90 minutes", TimerSpeech.Duration(90 * 60));
            Assert.AreEqual("0 seconds", TimerSpeech.Duration(-5));
        }

        [TestMethod]
        public void PhaseChanged_SaysWhatStartedOrEnded()
        {
            Assert.AreEqual("Work started. 25 minutes.", TimerSpeech.PhaseChanged(PomodoroPhase.NotStarted, PomodoroPhase.Work, 1500));
            Assert.AreEqual("Work started. 25 minutes.", TimerSpeech.PhaseChanged(PomodoroPhase.BreakEnded, PomodoroPhase.Work, 1500));
            Assert.AreEqual("Work finished. Time for a break.", TimerSpeech.PhaseChanged(PomodoroPhase.Work, PomodoroPhase.WorkEnded, 0));
            Assert.AreEqual("Break started. 5 minutes.", TimerSpeech.PhaseChanged(PomodoroPhase.WorkEnded, PomodoroPhase.Break, 300));
            Assert.AreEqual("Break finished.", TimerSpeech.PhaseChanged(PomodoroPhase.Break, PomodoroPhase.BreakEnded, 0));
            Assert.AreEqual("Timer stopped.", TimerSpeech.PhaseChanged(PomodoroPhase.Work, PomodoroPhase.NotStarted, 1500));
        }

        [TestMethod]
        public void PhaseChanged_TellsPausingAndResumingApart()
        {
            Assert.AreEqual("Paused. 12 minutes 34 seconds left.", TimerSpeech.PhaseChanged(PomodoroPhase.Work, PomodoroPhase.Pause, 754));
            Assert.AreEqual("Work resumed. 12 minutes 34 seconds left.", TimerSpeech.PhaseChanged(PomodoroPhase.Pause, PomodoroPhase.Work, 754));
        }

        [TestMethod]
        public void Status_AnswersWhereTheTimerIs()
        {
            Assert.AreEqual("Timer is not running. The work period is 25 minutes.", TimerSpeech.Status(PomodoroPhase.NotStarted, 1500, 1500));
            Assert.AreEqual("Work. 12 minutes 34 seconds left.", TimerSpeech.Status(PomodoroPhase.Work, 754, 1500));
            Assert.AreEqual("Break. 4 minutes left.", TimerSpeech.Status(PomodoroPhase.Break, 240, 1500));
            Assert.AreEqual("Paused. 30 seconds left.", TimerSpeech.Status(PomodoroPhase.Pause, 30, 1500));
            Assert.AreEqual("Work finished. The break has not started.", TimerSpeech.Status(PomodoroPhase.WorkEnded, 0, 1500));
            Assert.AreEqual("Break finished. Work has not started.", TimerSpeech.Status(PomodoroPhase.BreakEnded, 0, 1500));
        }

        [TestMethod]
        public void Minutes_ChangesOnlyOnceAMinute()
        {
            Assert.AreEqual("Timer is not running", TimerSpeech.Minutes(PomodoroPhase.NotStarted, 1500));
            Assert.AreEqual("Work, 25 minutes left", TimerSpeech.Minutes(PomodoroPhase.Work, 1500));
            Assert.AreEqual("Work, 25 minutes left", TimerSpeech.Minutes(PomodoroPhase.Work, 1441));
            Assert.AreEqual("Break, 1 minute left", TimerSpeech.Minutes(PomodoroPhase.Break, 1));
            Assert.AreEqual("Work finished", TimerSpeech.Minutes(PomodoroPhase.WorkEnded, 0));
        }

        [TestMethod]
        public void WindowTitle_PutsTheMinutesLeftFirst()
        {
            Assert.AreEqual("YAPA 2", TimerSpeech.WindowTitle(PomodoroPhase.NotStarted, 1500));
            Assert.AreEqual("Work, 25 minutes left - YAPA 2", TimerSpeech.WindowTitle(PomodoroPhase.Work, 1500));
            Assert.AreEqual("Work, 25 minutes left - YAPA 2", TimerSpeech.WindowTitle(PomodoroPhase.Work, 1441));
            Assert.AreEqual("Work, 24 minutes left - YAPA 2", TimerSpeech.WindowTitle(PomodoroPhase.Work, 1440));
            Assert.AreEqual("Break, 1 minute left - YAPA 2", TimerSpeech.WindowTitle(PomodoroPhase.Break, 40));
            Assert.AreEqual("Paused, 12 minutes left - YAPA 2", TimerSpeech.WindowTitle(PomodoroPhase.Pause, 700));
            Assert.AreEqual("Work, less than a minute left - YAPA 2", TimerSpeech.WindowTitle(PomodoroPhase.Work, 0));
            Assert.AreEqual("Work finished - YAPA 2", TimerSpeech.WindowTitle(PomodoroPhase.WorkEnded, 0));
            Assert.AreEqual("Break finished - YAPA 2", TimerSpeech.WindowTitle(PomodoroPhase.BreakEnded, 0));
        }

        [TestMethod]
        public void Display_ReadsTheDigitsWithTheirMeaning()
        {
            Assert.AreEqual("Timer, 0 seconds", TimerSpeech.Display(PomodoroPhase.NotStarted, 0, false));
            Assert.AreEqual("Timer, 25 minutes", TimerSpeech.Display(PomodoroPhase.NotStarted, 1500, true));
            Assert.AreEqual("Work, 12 minutes 34 seconds done", TimerSpeech.Display(PomodoroPhase.Work, 754, false));
            Assert.AreEqual("Work, 12 minutes 26 seconds left", TimerSpeech.Display(PomodoroPhase.Work, 746, true));
            Assert.AreEqual("Break, 1 minute left", TimerSpeech.Display(PomodoroPhase.Break, 60, true));
            Assert.AreEqual("Paused, 5 minutes done", TimerSpeech.Display(PomodoroPhase.Pause, 300, false));
            Assert.AreEqual("Work, 25 minutes done", TimerSpeech.Display(PomodoroPhase.WorkEnded, 1500, false));
            Assert.AreEqual("Break, 5 minutes done", TimerSpeech.Display(PomodoroPhase.BreakEnded, 300, false));
        }
    }
}
