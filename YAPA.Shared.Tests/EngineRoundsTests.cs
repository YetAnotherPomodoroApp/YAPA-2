using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Shared.Tests
{
    [TestClass]
    public class EngineRoundsTests
    {
        private const int WorkTime = 25 * 60;
        private const int ShortBreak = 5 * 60;
        private const int LongBreak = 15 * 60;

        private class Fixture
        {
            public readonly TimerMock Timer = new TimerMock();
            public readonly DateMock Date = new DateMock { DateToReturn = new DateTime(2024, 1, 1, 12, 0, 0) };
            public readonly Dictionary<string, PomodoroProfile> Profiles = new Dictionary<string, PomodoroProfile>();
            public readonly PomodoroEngineSettings Settings;
            public readonly PomodoroEngine Engine;

            public Fixture(int rounds)
            {
                Settings = new PomodoroEngineSettings(Substitute.For<ISettings>());
                Profiles["main"] = NewProfile(rounds);
                Settings.Profiles.Returns(Profiles);
                Settings.ActiveProfile.Returns("main");
                Settings.CountBackwards.Returns(false);

                Engine = new PomodoroEngine(Settings, Timer, Date, Substitute.For<IThreading>(), Substitute.For<ISettings>(), Substitute.For<IPomodoroRepository>());
            }

            public static PomodoroProfile NewProfile(int rounds)
            {
                return new PomodoroProfile
                {
                    WorkTime = WorkTime,
                    BreakTime = ShortBreak,
                    LongBreakTime = LongBreak,
                    PomodorosBeforeLongBreak = rounds
                };
            }

            public void Advance(int seconds)
            {
                Date.DateToReturn = Date.DateToReturn.AddSeconds(seconds);
                Timer.PerformTick();
            }

            public void FinishWork()
            {
                Engine.Start();
                Assert.AreEqual(PomodoroPhase.Work, Engine.Phase);
                Advance(Engine.WorkTime);
                Assert.AreEqual(PomodoroPhase.WorkEnded, Engine.Phase);
            }

            /// <summary>
            /// Runs the next pomodoro and the break after it. Returns "index:break length".
            /// </summary>
            public string Round()
            {
                FinishWork();
                var index = Engine.Index;

                Engine.Start();
                Assert.AreEqual(PomodoroPhase.Break, Engine.Phase);
                var breakLength = Engine.BreakTime;
                Advance(breakLength);
                Assert.AreEqual(PomodoroPhase.BreakEnded, Engine.Phase);

                return $"{index}:{(breakLength == LongBreak ? "long" : breakLength == ShortBreak ? "short" : breakLength.ToString())}";
            }

            public string Rounds(int count)
            {
                var rounds = new List<string>();
                for (var i = 0; i < count; i++)
                {
                    rounds.Add(Round());
                }
                return string.Join(" ", rounds);
            }
        }

        [TestMethod]
        public void FourRounds_LongBreakAfterTheFourth_ThenStartsOver()
        {
            var f = new Fixture(4);

            Assert.AreEqual("1:short 2:short 3:short 4:long 1:short 2:short 3:short 4:long 1:short", f.Rounds(9));
        }

        [TestMethod]
        public void TwoRounds_LongBreakAfterTheSecond()
        {
            var f = new Fixture(2);

            Assert.AreEqual("1:short 2:long 1:short 2:long", f.Rounds(4));
        }

        [TestMethod]
        public void SixRounds_LongBreakAfterTheSixth()
        {
            var f = new Fixture(6);

            Assert.AreEqual("1:short 2:short 3:short 4:short 5:short 6:long 1:short", f.Rounds(7));
        }

        [TestMethod]
        public void RoundsRaisedWhileTheAppRuns_TakeEffectWithoutRestart()
        {
            var f = new Fixture(2);
            Assert.AreEqual("1:short", f.Rounds(1));

            f.Profiles["main"].PomodorosBeforeLongBreak = 4;

            Assert.AreEqual("2:short 3:short 4:long 1:short", f.Rounds(4));
        }

        [TestMethod]
        public void RoundsLoweredWhileTheAppRuns_TakeEffectWithoutRestart()
        {
            var f = new Fixture(4);
            Assert.AreEqual("1:short", f.Rounds(1));

            f.Profiles["main"].PomodorosBeforeLongBreak = 2;

            Assert.AreEqual("2:long 1:short 2:long", f.Rounds(3));
        }

        [TestMethod]
        public void RoundsLoweredBelowTheCurrentPomodoro_NextOneStartsOver()
        {
            var f = new Fixture(6);
            Assert.AreEqual("1:short 2:short 3:short", f.Rounds(3));
            f.FinishWork();
            Assert.AreEqual(4, f.Engine.Index);

            f.Profiles["main"].PomodorosBeforeLongBreak = 2;
            f.Engine.Start();
            f.Advance(f.Engine.BreakTime);

            Assert.AreEqual("1:short 2:long 1:short", f.Rounds(3));
        }

        [TestMethod]
        public void SwitchingProfile_UsesTheRoundsOfThatProfile()
        {
            var f = new Fixture(4);
            f.Profiles["short"] = Fixture.NewProfile(2);
            Assert.AreEqual("1:short", f.Rounds(1));

            f.Settings.ActiveProfile.Returns("short");
            Assert.AreEqual("2:long 1:short 2:long", f.Rounds(3));

            //Back on the four-round profile the count carries on from the second pomodoro
            f.Settings.ActiveProfile.Returns("main");
            Assert.AreEqual("3:short 4:long 1:short 2:short 3:short", f.Rounds(5));
        }

        [TestMethod]
        public void ProfileSavedWithoutRounds_GetsFourRounds()
        {
            //Profiles added by older versions were saved with 0 rounds and never got a long break
            foreach (var rounds in new[] { 0, -1 })
            {
                var f = new Fixture(rounds);

                Assert.AreEqual(4, f.Settings.PomodorosBeforeLongBreak, $"rounds = {rounds}");
                Assert.AreEqual("1:short 2:short 3:short 4:long 1:short", f.Rounds(5), $"rounds = {rounds}");
                Assert.AreEqual(rounds, f.Profiles["main"].PomodorosBeforeLongBreak, "the stored value is not rewritten");
            }
        }

        [TestMethod]
        public void ProfileSavedWithoutRounds_KeepsARoundsValueTheUserSets()
        {
            var f = new Fixture(0);
            Assert.AreEqual("1:short", f.Rounds(1));

            f.Settings.PomodorosBeforeLongBreak = 2;

            Assert.AreEqual(2, f.Profiles["main"].PomodorosBeforeLongBreak);
            Assert.AreEqual("2:long 1:short 2:long", f.Rounds(3));
        }

        [TestMethod]
        public void OneRound_BehavesAsBefore()
        {
            //Not offered in the list but can be typed; unchanged: a long break after the first of two pomodoros
            var f = new Fixture(1);

            Assert.AreEqual(1, f.Settings.PomodorosBeforeLongBreak);
            Assert.AreEqual("1:long 2:short 1:long 2:short", f.Rounds(4));
        }

        [TestMethod]
        public void StopDuringTheLastBreak_NextPomodoroIsTheFirst()
        {
            var f = new Fixture(2);
            Assert.AreEqual("1:short", f.Rounds(1));
            f.FinishWork();
            f.Engine.Start();
            Assert.AreEqual(PomodoroPhase.Break, f.Engine.Phase);
            Assert.AreEqual(2, f.Engine.Index);

            f.Engine.Stop();

            Assert.AreEqual(PomodoroPhase.NotStarted, f.Engine.Phase);
            Assert.AreEqual(1, f.Engine.Index);
        }

        [TestMethod]
        public void StopDuringABreak_MovesToTheNextPomodoro()
        {
            var f = new Fixture(4);
            f.FinishWork();
            f.Engine.Start();
            Assert.AreEqual(PomodoroPhase.Break, f.Engine.Phase);

            f.Engine.Stop();

            Assert.AreEqual(PomodoroPhase.NotStarted, f.Engine.Phase);
            Assert.AreEqual(2, f.Engine.Index);
        }

        [TestMethod]
        public void StopDuringWork_StaysOnTheSamePomodoro()
        {
            var f = new Fixture(4);
            Assert.AreEqual("1:short 2:short", f.Rounds(2));
            f.Engine.Start();
            f.Advance(60);
            Assert.AreEqual(3, f.Engine.Index);

            f.Engine.Stop();

            Assert.AreEqual(PomodoroPhase.NotStarted, f.Engine.Phase);
            Assert.AreEqual(3, f.Engine.Index);
        }

        [TestMethod]
        public void Reset_GoesBackToTheFirstPomodoro()
        {
            var f = new Fixture(4);
            Assert.AreEqual("1:short 2:short", f.Rounds(2));
            f.Engine.Start();
            Assert.AreEqual(3, f.Engine.Index);

            f.Engine.Reset();

            Assert.AreEqual(PomodoroPhase.NotStarted, f.Engine.Phase);
            Assert.AreEqual(1, f.Engine.Index);
            Assert.AreEqual("1:short 2:short 3:short 4:long", f.Rounds(4));
        }
    }
}
