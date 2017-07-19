using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Shared.Tests
{
    [TestClass]
    public class SimpleWorkflow
    {
        [TestMethod]
        public void SimpleWorkflowThroughAllPomodoros()
        {
            var timerSub = new TimerMock();
            var engineSettings = Substitute.For<PomodoroEngineSettings>(Substitute.For<ISettings>());
            var dateTime = new DateMock();

            var baseDate = new DateTime(2017, 6, 1, 12, 0, 0);
            var workTime = 25 * 60;
            var breakTime = 5 * 60;
            var longBreakTime = 10 * 60;

            engineSettings.AutoStartBreak.Returns(false);
            engineSettings.CountBackwards.Returns(false);
            engineSettings.BreakTime.Returns(breakTime);
            engineSettings.WorkTime.Returns(workTime);
            engineSettings.LongBreakTime.Returns(longBreakTime);

            dateTime.DateToReturn = baseDate;

            var engine = new PomodoroEngine(engineSettings, timerSub, dateTime);

            //----First pomodoro
            Assert.AreEqual(PomodoroPhase.NotStarted, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            //----Second pomodoro
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            //----Third pomodoro
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            //----Fourth pomodoro (long break)
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(longBreakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(longBreakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(9);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(longBreakTime, engine.DisplayValue);
            Assert.AreEqual(longBreakTime, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);


            //----Fifth pomodoro (return to first
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);
        }

        [TestMethod]
        public void SimpleWorkflowThroughAllPomodoros_CountBackwards()
        {
            var timerSub = new TimerMock();
            var engineSettings = Substitute.For<PomodoroEngineSettings>(Substitute.For<ISettings>());
            var dateTime = new DateMock();

            var baseDate = new DateTime(2017, 6, 1, 12, 0, 0);
            var workTime = 25 * 60;
            var breakTime = 5 * 60;
            var longBreakTime = 10 * 60;

            engineSettings.AutoStartBreak.Returns(false);
            engineSettings.CountBackwards.Returns(true);
            engineSettings.BreakTime.Returns(breakTime);
            engineSettings.WorkTime.Returns(workTime);
            engineSettings.LongBreakTime.Returns(longBreakTime);

            dateTime.DateToReturn = baseDate;

            var engine = new PomodoroEngine(engineSettings, timerSub, dateTime);

            //----First pomodoro
            Assert.AreEqual(PomodoroPhase.NotStarted, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(workTime - 60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(breakTime - 60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            //----Second pomodoro
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(workTime - 60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(breakTime - 60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            //----Third pomodoro
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(workTime - 60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(breakTime - 60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            //----Fourth pomodoro (long break)
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(workTime - 60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(longBreakTime, engine.Remaining);
            Assert.AreEqual(longBreakTime, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(longBreakTime - 60, engine.Remaining);
            Assert.AreEqual(longBreakTime - 60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(9);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(longBreakTime, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);


            //----Fifth pomodoro (return to first
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(workTime - 60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(breakTime - 60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);
        }

        [TestMethod]
        public void SimpleWorkflowThroughAllPomodoros_AutoStartBreak()
        {
            var timerSub = new TimerMock();
            var engineSettings = Substitute.For<PomodoroEngineSettings>(Substitute.For<ISettings>());
            var dateTime = new DateMock();

            var baseDate = new DateTime(2017, 6, 1, 12, 0, 0);
            var workTime = 25 * 60;
            var breakTime = 5 * 60;
            var longBreakTime = 10 * 60;

            engineSettings.AutoStartBreak.Returns(true);
            engineSettings.CountBackwards.Returns(false);
            engineSettings.BreakTime.Returns(breakTime);
            engineSettings.WorkTime.Returns(workTime);
            engineSettings.LongBreakTime.Returns(longBreakTime);

            dateTime.DateToReturn = baseDate;

            var engine = new PomodoroEngine(engineSettings, timerSub, dateTime);

            //----First pomodoro
            Assert.AreEqual(PomodoroPhase.NotStarted, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            timerSub.PerformTick();
            Timeout.WaitPhaseChange(engine, PomodoroPhase.Break).Wait();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            //----Second pomodoro
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            timerSub.PerformTick();
            Timeout.WaitPhaseChange(engine, PomodoroPhase.Break).Wait();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            //----Third pomodoro
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            timerSub.PerformTick();
            Timeout.WaitPhaseChange(engine, PomodoroPhase.Break).Wait();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            //----Fourth pomodoro (long break)
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            timerSub.PerformTick();
            Timeout.WaitPhaseChange(engine, PomodoroPhase.Break).Wait();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(longBreakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(longBreakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(9);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(longBreakTime, engine.DisplayValue);
            Assert.AreEqual(longBreakTime, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);


            //----Fifth pomodoro (return to first
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            timerSub.PerformTick();
            Timeout.WaitPhaseChange(engine, PomodoroPhase.Break).Wait();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);
        }

        [TestMethod]
        public void SimpleWorkflowThroughAllPomodoros_AutoStartBreak_ManuallyStarting()
        {
            var timerSub = new TimerMock();
            var engineSettings = Substitute.For<PomodoroEngineSettings>(Substitute.For<ISettings>());
            var dateTime = new DateMock();

            var baseDate = new DateTime(2017, 6, 1, 12, 0, 0);
            var workTime = 25 * 60;
            var breakTime = 5 * 60;
            var longBreakTime = 10 * 60;

            engineSettings.AutoStartBreak.Returns(true);
            engineSettings.CountBackwards.Returns(false);
            engineSettings.BreakTime.Returns(breakTime);
            engineSettings.WorkTime.Returns(workTime);
            engineSettings.LongBreakTime.Returns(longBreakTime);

            dateTime.DateToReturn = baseDate;

            var engine = new PomodoroEngine(engineSettings, timerSub, dateTime);

            //----First pomodoro
            Assert.AreEqual(PomodoroPhase.NotStarted, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            timerSub.PerformTick();
            engine.Start();
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            //----Second pomodoro
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            timerSub.PerformTick();
            engine.Start();
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

        }

        [TestMethod]
        public void SimpleWorkflowThroughAllPomodoros_PauseAndStop()
        {
            var timerSub = new TimerMock();
            var engineSettings = Substitute.For<PomodoroEngineSettings>(Substitute.For<ISettings>());
            var dateTime = new DateMock();

            var baseDate = new DateTime(2017, 6, 1, 12, 0, 0);
            var workTime = 25 * 60;
            var breakTime = 5 * 60;
            var longBreakTime = 10 * 60;

            engineSettings.AutoStartBreak.Returns(false);
            engineSettings.CountBackwards.Returns(false);
            engineSettings.BreakTime.Returns(breakTime);
            engineSettings.WorkTime.Returns(workTime);
            engineSettings.LongBreakTime.Returns(longBreakTime);

            dateTime.DateToReturn = baseDate;

            var engine = new PomodoroEngine(engineSettings, timerSub, dateTime);

            //----First pomodoro
            Assert.AreEqual(PomodoroPhase.NotStarted, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            engine.Pause();

            Assert.AreEqual(PomodoroPhase.Pause, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            engine.Start();

            baseDate = baseDate.AddMinutes(2);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - (3 * 60), engine.Remaining);
            Assert.AreEqual(3 * 60, engine.DisplayValue);
            Assert.AreEqual(3 * 60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(23);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            //----Second pomodoro
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            engine.Pause();

            Assert.AreEqual(PomodoroPhase.Pause, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            engine.Start();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(2);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            engine.Pause();

            Assert.AreEqual(PomodoroPhase.Pause, engine.Phase);
            Assert.AreEqual(workTime - (3 * 60), engine.Remaining);
            Assert.AreEqual((3 * 60), engine.DisplayValue);
            Assert.AreEqual((3 * 60), engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            engine.Stop();

            Assert.AreEqual(PomodoroPhase.NotStarted, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            engine.Start();

            baseDate = baseDate.AddMinutes(3);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - (3 * 60), engine.Remaining);
            Assert.AreEqual((3 * 60), engine.DisplayValue);
            Assert.AreEqual((3 * 60), engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(22);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(2, engine.Index);

            //----Third pomodoro
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            engine.Pause();

            Assert.AreEqual(PomodoroPhase.Pause, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            engine.Start();

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(3, engine.Index);

            engine.Stop();

            Assert.AreEqual(PomodoroPhase.NotStarted, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            //----Fourth pomodoro (long break)
            Assert.AreEqual(PomodoroPhase.NotStarted, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            engine.Stop();

            Assert.AreEqual(PomodoroPhase.NotStarted, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);
           
            engine.Start();

            baseDate = baseDate.AddMinutes(25);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(longBreakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(longBreakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);

            baseDate = baseDate.AddMinutes(9);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(longBreakTime, engine.DisplayValue);
            Assert.AreEqual(longBreakTime, engine.Elapsed);
            Assert.AreEqual(4, engine.Index);


            //----Fifth pomodoro (return to first
            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Work, engine.Phase);
            Assert.AreEqual(workTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(24);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.WorkEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(workTime, engine.DisplayValue);
            Assert.AreEqual(workTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            engine.Start();
            timerSub.PerformTick();
            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime, engine.Remaining);
            Assert.AreEqual(0, engine.DisplayValue);
            Assert.AreEqual(0, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(1);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.Break, engine.Phase);
            Assert.AreEqual(breakTime - 60, engine.Remaining);
            Assert.AreEqual(60, engine.DisplayValue);
            Assert.AreEqual(60, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);

            baseDate = baseDate.AddMinutes(4);
            dateTime.DateToReturn = baseDate;
            timerSub.PerformTick();

            Assert.AreEqual(PomodoroPhase.BreakEnded, engine.Phase);
            Assert.AreEqual(0, engine.Remaining);
            Assert.AreEqual(breakTime, engine.DisplayValue);
            Assert.AreEqual(breakTime, engine.Elapsed);
            Assert.AreEqual(1, engine.Index);
        }

    }

}
