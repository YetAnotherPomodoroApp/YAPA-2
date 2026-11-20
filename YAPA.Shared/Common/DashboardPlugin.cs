using System;
using System.Linq;
using System.Threading.Tasks;
using YAPA.Shared.Contracts;

namespace YAPA.Shared.Common
{
    public class Dashboard : IPlugin
    {
        private readonly IPomodoroRepository _itemRepository;
        private readonly PomodoroEngineSettings _engineSettings;
        private readonly IPomodoroEngine _engine;
        private readonly IDate _date;

        public Dashboard(IPomodoroEngine engine, IPomodoroRepository itemRepository, PomodoroEngineSettings engineSettings, IDate date)
        {
            _itemRepository = itemRepository;
            _engineSettings = engineSettings;
            _engine = engine;
            _date = date;

            _engine.OnPomodoroCompleted += _engine_OnPomodoroCompleted;
        }

        public PomodoroDashboardModel GetPomodoros(int numberOfMonths, string profileFilter)
        {
            return GetPomodoros(numberOfMonths, profileFilter, _date.DateTimeUtc(), TimeZoneInfo.Local);
        }

        /// <summary>
        /// One entry for every calendar day in <paramref name="zone"/>, from the first day of the month
        /// <paramref name="numberOfMonths"/> months back up to and including today.
        /// </summary>
        public PomodoroDashboardModel GetPomodoros(int numberOfMonths, string profileFilter, DateTime utcNow, TimeZoneInfo zone)
        {
            var today = LocalDay.ToLocal(utcNow, zone).Date;

            var firstMonth = today.AddMonths(numberOfMonths * -1);
            var firstDay = new DateTime(firstMonth.Year, firstMonth.Month, 1);

            var totalDays = (int)(today - firstDay).TotalDays + 1;
            var emptyDays = Enumerable.Range(0, totalDays).Select(x => new { Day = firstDay.AddDays(x), Count = 0, WorkTime = 0 });

            var capturedPomodoros = _itemRepository.After(LocalDay.StartUtc(firstDay, zone));

            var distinctProfiles = capturedPomodoros.Select(_ => _.ProfileName).Distinct().ToList();

            if (string.IsNullOrEmpty(profileFilter) == false)
            {
                capturedPomodoros = capturedPomodoros.Where(_ => _.ProfileName == profileFilter).ToList();
            }

            var joinedPomodoros = capturedPomodoros.ToList()
                .Select(c => new { Day = LocalDay.ToLocal(c.DateTime, zone).Date, c.Count, WorkTime = c.DurationMin })
                .Concat(emptyDays)
                .GroupBy(c => c.Day,
                    (day, items) =>
                    new PomodoroGithubDashboardModel
                    {
                        DateTime = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Local),
                        Count = items.Sum(x => x.Count),
                        DurationMin = items.Sum(x => x.WorkTime)
                    });

            var result = new PomodoroDashboardModel
            {
                DashboardItems = joinedPomodoros.OrderBy(x => x.DateTime.Date),
                Profiles = distinctProfiles
            };

            return result;
        }

        private void _engine_OnPomodoroCompleted()
        {
            _itemRepository.Add(new PomodoroEntity
            {
                Count = 1,
                DateTime = _date.DateTimeUtc(),
                DurationMin = _engine.WorkTime / 60,
                ProfileName = _engineSettings.ActiveProfile
            });
        }
    }

    public class DashboardSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public DashboardSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(nameof(Dashboard));
        }

        public int NumberOfMonths
        {
            get => _settings.Get(nameof(NumberOfMonths), 6);
            set => _settings.Update(nameof(NumberOfMonths), value);
        }

        public string ProfileFilter
        {
            get => _settings.Get<string>(nameof(ProfileFilter), null);
            set => _settings.Update(nameof(ProfileFilter), value);
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}
