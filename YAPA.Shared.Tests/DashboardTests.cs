using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Shared.Tests
{
    [TestClass]
    public class DashboardTests
    {
        private static readonly TimeZoneInfo Minus8 = TimeZoneInfo.CreateCustomTimeZone("UTC-8", TimeSpan.FromHours(-8), "UTC-8", "UTC-8");
        private static readonly TimeZoneInfo Plus2 = TimeZoneInfo.CreateCustomTimeZone("UTC+2", TimeSpan.FromHours(2), "UTC+2", "UTC+2");
        private static readonly TimeZoneInfo Plus13 = TimeZoneInfo.CreateCustomTimeZone("UTC+13", TimeSpan.FromHours(13), "UTC+13", "UTC+13");

        private static TimeZoneInfo Pacific()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
            }
        }

        private class RepositoryFake : IPomodoroRepository
        {
            public readonly List<PomodoroEntity> Items = new List<PomodoroEntity>();
            public DateTime? AskedAfter;

            public void Delete(int id)
            {
            }

            public void Add(PomodoroEntity pomo)
            {
                Items.Add(pomo);
            }

            public int CompletedToday()
            {
                return 0;
            }

            public IEnumerable<PomodoroEntity> After(DateTime date)
            {
                AskedAfter = date;
                return Items.Where(x => x.DateTime >= date).ToList();
            }

            //The database returns timestamps without a kind
            public void AddUtc(int year, int month, int day, int hour, int minute, int count = 1, string profile = "Pomodoro")
            {
                Items.Add(new PomodoroEntity
                {
                    Count = count,
                    DurationMin = 25 * count,
                    ProfileName = profile,
                    DateTime = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified)
                });
            }
        }

        private static Dashboard CreateDashboard(RepositoryFake repository, DateMock date = null)
        {
            return new Dashboard(Substitute.For<IPomodoroEngine>(), repository,
                new PomodoroEngineSettings(Substitute.For<ISettings>()), date ?? new DateMock());
        }

        private static DateTime Utc(int year, int month, int day, int hour, int minute = 0)
        {
            return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        }

        private static List<PomodoroGithubDashboardModel> Days(Dashboard dashboard, int months, DateTime utcNow, TimeZoneInfo zone, string profile = null)
        {
            return dashboard.GetPomodoros(months, profile, utcNow, zone).DashboardItems.ToList();
        }

        private static void AssertEveryDayOnce(List<PomodoroGithubDashboardModel> days, DateTime first, DateTime last)
        {
            var expected = Enumerable.Range(0, (int)(last - first).TotalDays + 1).Select(x => first.AddDays(x)).ToList();
            CollectionAssert.AreEqual(expected, days.Select(x => x.DateTime).ToList());
        }

        [TestMethod]
        public void NoPomodoros_EveryDayFromFirstOfMonthThroughToday()
        {
            var dashboard = CreateDashboard(new RepositoryFake());

            //10:00 on the 15th of May, west of UTC
            var days = Days(dashboard, 1, Utc(2024, 5, 15, 18), Minus8);

            AssertEveryDayOnce(days, new DateTime(2024, 4, 1), new DateTime(2024, 5, 15));
            Assert.IsTrue(days.All(x => x.Count == 0 && x.DurationMin == 0));
        }

        [TestMethod]
        public void FirstOfMonth_NewMonthIsListed_WestOfUtc()
        {
            var dashboard = CreateDashboard(new RepositoryFake());

            //09:00 on the 1st of May in UTC-8
            var days = Days(dashboard, 1, Utc(2024, 5, 1, 17), Minus8);

            AssertEveryDayOnce(days, new DateTime(2024, 4, 1), new DateTime(2024, 5, 1));
        }

        [TestMethod]
        public void FirstOfMonth_NewMonthIsListed_EastOfUtc()
        {
            var dashboard = CreateDashboard(new RepositoryFake());

            //01:00 on the 1st of May in UTC+2, still the 30th of April in UTC
            var days = Days(dashboard, 1, Utc(2024, 4, 30, 23), Plus2);

            AssertEveryDayOnce(days, new DateTime(2024, 4, 1), new DateTime(2024, 5, 1));
        }

        [TestMethod]
        public void LateEveningPomodoro_WestOfUtc_CountsOnTheLocalDay()
        {
            var repository = new RepositoryFake();
            //23:30 on the 14th in UTC-8 is 07:30 on the 15th in UTC
            repository.AddUtc(2024, 5, 15, 7, 30);
            var dashboard = CreateDashboard(repository);

            var days = Days(dashboard, 1, Utc(2024, 5, 15, 18), Minus8);

            AssertEveryDayOnce(days, new DateTime(2024, 4, 1), new DateTime(2024, 5, 15));
            Assert.AreEqual(1, days.Single(x => x.DateTime == new DateTime(2024, 5, 14)).Count);
            Assert.AreEqual(0, days.Single(x => x.DateTime == new DateTime(2024, 5, 15)).Count);
        }

        [TestMethod]
        public void EarlyMorningPomodoro_EastOfUtc_CountsOnTheLocalDay()
        {
            var repository = new RepositoryFake();
            //00:30 on the 15th in UTC+2 is 22:30 on the 14th in UTC
            repository.AddUtc(2024, 5, 14, 22, 30);
            var dashboard = CreateDashboard(repository);

            var days = Days(dashboard, 1, Utc(2024, 5, 15, 10), Plus2);

            Assert.AreEqual(0, days.Single(x => x.DateTime == new DateTime(2024, 5, 14)).Count);
            Assert.AreEqual(1, days.Single(x => x.DateTime == new DateTime(2024, 5, 15)).Count);
        }

        [TestMethod]
        public void PomodoroJustAfterLocalMidnightOnFirstDay_IsIncluded()
        {
            var repository = new RepositoryFake();
            //00:30 on the 1st of April in UTC+2 is still the 31st of March in UTC
            repository.AddUtc(2024, 3, 31, 22, 30);
            //23:30 on the 31st of March in UTC+2 belongs to the month before the range
            repository.AddUtc(2024, 3, 31, 21, 30);
            var dashboard = CreateDashboard(repository);

            var days = Days(dashboard, 1, Utc(2024, 5, 15, 10), Plus2);

            AssertEveryDayOnce(days, new DateTime(2024, 4, 1), new DateTime(2024, 5, 15));
            Assert.AreEqual(1, days.Single(x => x.DateTime == new DateTime(2024, 4, 1)).Count);
            Assert.AreEqual(Utc(2024, 3, 31, 22), repository.AskedAfter);
        }

        [TestMethod]
        public void SeveralPomodorosOnOneDay_AreSummed()
        {
            var repository = new RepositoryFake();
            repository.AddUtc(2024, 5, 14, 17, 0);
            repository.AddUtc(2024, 5, 14, 18, 0);
            repository.AddUtc(2024, 5, 15, 6, 0); //22:00 on the 14th in UTC-8
            var dashboard = CreateDashboard(repository);

            var day = Days(dashboard, 1, Utc(2024, 5, 15, 18), Minus8).Single(x => x.DateTime == new DateTime(2024, 5, 14));

            Assert.AreEqual(3, day.Count);
            Assert.AreEqual(75, day.DurationMin);
        }

        [TestMethod]
        public void ProfileFilter_KeepsAllProfilesInTheListButCountsOnlyTheSelected()
        {
            var repository = new RepositoryFake();
            repository.AddUtc(2024, 5, 14, 17, 0, profile: "Work");
            repository.AddUtc(2024, 5, 14, 18, 0, profile: "Study");
            var dashboard = CreateDashboard(repository);

            var model = dashboard.GetPomodoros(1, "Study", Utc(2024, 5, 15, 18), Minus8);

            CollectionAssert.AreEquivalent(new[] { "Work", "Study" }, model.Profiles.ToList());
            Assert.AreEqual(1, model.DashboardItems.Sum(x => x.Count));
        }

        [TestMethod]
        public void DaylightSavingChanges_DoNotSkipOrRepeatDays()
        {
            var dashboard = CreateDashboard(new RepositoryFake());

            //Clocks go forward on 2024-03-10 and back on 2024-11-03 in the Pacific zone
            var spring = Days(dashboard, 1, Utc(2024, 3, 31, 20), Pacific());
            var autumn = Days(dashboard, 1, Utc(2024, 11, 30, 20), Pacific());

            AssertEveryDayOnce(spring, new DateTime(2024, 2, 1), new DateTime(2024, 3, 31));
            AssertEveryDayOnce(autumn, new DateTime(2024, 10, 1), new DateTime(2024, 11, 30));
        }

        [TestMethod]
        public void DaysAreOrderedAndMarkedAsLocalDates()
        {
            var repository = new RepositoryFake();
            repository.AddUtc(2024, 5, 14, 17, 0);
            var dashboard = CreateDashboard(repository);

            var days = Days(dashboard, 6, Utc(2024, 5, 15, 18), Minus8);

            Assert.AreEqual(new DateTime(2023, 11, 1), days.First().DateTime);
            Assert.AreEqual(new DateTime(2024, 5, 15), days.Last().DateTime);
            Assert.IsTrue(days.All(x => x.DateTime.Kind == DateTimeKind.Local && x.DateTime.TimeOfDay == TimeSpan.Zero));
        }

        [TestMethod]
        public void GetPomodoros_UsesTheInjectedClock()
        {
            var date = new DateMock { DateToReturn = DateTime.UtcNow };
            var dashboard = CreateDashboard(new RepositoryFake(), date);

            var days = dashboard.GetPomodoros(1, null).DashboardItems.ToList();

            Assert.AreEqual(DateTime.Now.Date, days.Last().DateTime);
        }

        [TestMethod]
        public void CompletedPomodoro_IsStoredWithTheClockTime()
        {
            var repository = new RepositoryFake();
            var engine = Substitute.For<IPomodoroEngine>();
            engine.WorkTime.Returns(25 * 60);
            var date = new DateMock { DateToReturn = Utc(2024, 5, 15, 18, 30) };
            var dashboard = new Dashboard(engine, repository, new PomodoroEngineSettings(Substitute.For<ISettings>()), date);

            engine.OnPomodoroCompleted += Raise.Event<Action>();

            Assert.AreEqual(1, repository.Items.Count);
            Assert.AreEqual(Utc(2024, 5, 15, 18, 30), repository.Items[0].DateTime);
            Assert.AreEqual(25, repository.Items[0].DurationMin);
            Assert.AreEqual(1, repository.Items[0].Count);
            Assert.IsNotNull(dashboard);
        }

        [TestMethod]
        public void LocalDay_ToLocal_TreatsStoredTimestampsAsUtc()
        {
            var stored = new DateTime(2024, 5, 15, 7, 30, 0, DateTimeKind.Unspecified);

            Assert.AreEqual(new DateTime(2024, 5, 14, 23, 30, 0), LocalDay.ToLocal(stored, Minus8));
            Assert.AreEqual(new DateTime(2024, 5, 15, 9, 30, 0), LocalDay.ToLocal(stored, Plus2));
            Assert.AreEqual(new DateTime(2024, 5, 15, 20, 30, 0), LocalDay.ToLocal(stored, Plus13));
            Assert.AreEqual(new DateTime(2024, 5, 14, 23, 30, 0), LocalDay.ToLocal(DateTime.SpecifyKind(stored, DateTimeKind.Utc), Minus8));
        }

        [TestMethod]
        public void LocalDay_StartUtc_IsLocalMidnightInUtc()
        {
            Assert.AreEqual(Utc(2024, 5, 15, 8), LocalDay.StartUtc(new DateTime(2024, 5, 15, 13, 45, 0), Minus8));
            Assert.AreEqual(Utc(2024, 5, 14, 22), LocalDay.StartUtc(new DateTime(2024, 5, 15), Plus2));
            Assert.AreEqual(Utc(2024, 5, 14, 11), LocalDay.StartUtc(new DateTime(2024, 5, 15), Plus13));
            Assert.AreEqual(DateTimeKind.Utc, LocalDay.StartUtc(new DateTime(2024, 5, 15), Plus2).Kind);
        }

        [TestMethod]
        public void LocalDay_StartUtc_FollowsDaylightSaving()
        {
            var pacific = Pacific();

            //The day the clocks go forward is 23 hours long, the day they go back 25
            Assert.AreEqual(Utc(2024, 3, 10, 8), LocalDay.StartUtc(new DateTime(2024, 3, 10), pacific));
            Assert.AreEqual(Utc(2024, 3, 11, 7), LocalDay.StartUtc(new DateTime(2024, 3, 11), pacific));
            Assert.AreEqual(Utc(2024, 11, 3, 7), LocalDay.StartUtc(new DateTime(2024, 11, 3), pacific));
            Assert.AreEqual(Utc(2024, 11, 4, 8), LocalDay.StartUtc(new DateTime(2024, 11, 4), pacific));
        }

        [TestMethod]
        public void LocalDay_TodayRange_ContainsExactlyTheLocalDay()
        {
            //What the "completed today" counter uses: 21:00 on the 14th in UTC-8
            var now = Utc(2024, 5, 15, 5);
            var today = LocalDay.ToLocal(now, Minus8).Date;
            var start = LocalDay.StartUtc(today, Minus8);
            var end = LocalDay.StartUtc(today.AddDays(1), Minus8);

            Assert.AreEqual(new DateTime(2024, 5, 14), today);
            Assert.AreEqual(Utc(2024, 5, 14, 8), start);
            Assert.AreEqual(Utc(2024, 5, 15, 8), end);
        }
    }
}
