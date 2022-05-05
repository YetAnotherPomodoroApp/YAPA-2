using System;
using System.Collections.Generic;
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

        public Dashboard(IPomodoroEngine engine, IPomodoroRepository itemRepository, PomodoroEngineSettings engineSettings)
        {
            _itemRepository = itemRepository;
            _engineSettings = engineSettings;
            _engine = engine;

            _engine.OnPomodoroCompleted += _engine_OnPomodoroCompleted;
        }

        public IEnumerable<PomodoroEntity> GetPomodoros(int numberOfMonths)
        {
            var today = DateTime.Now.Date;

            var date = today.Date.AddMonths(numberOfMonths * -1);
            var fromDate = new DateTime(date.Year, date.Month, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            var totalDays = (int)Math.Truncate((today - fromDate).TotalDays);
            var emptyPomodoros = Enumerable.Range(0, totalDays).Select(x => new PomodoroEntity { Count = 0, DateTime = fromDate.AddDays(x) }).ToList();
            var capturedPomodoros = _itemRepository.Pomodoros.Where(x => x.DateTime >= fromDate).ToList();

            var joinedPomodoros = capturedPomodoros.Union(emptyPomodoros)
                .GroupBy(c =>
                {
                    var local = c.DateTime.TryToLocalTime();
                    return new Tuple<int, int, int>(local.Year, local.Month, local.Day);
                },
                    c => new { c.Count, WorkTime = c.DurationMin },
                    (time, ints) =>
                    new PomodoroEntity
                    {
                        DateTime = new DateTime(time.Item1, time.Item2, time.Item3, 0, 0, 0, DateTimeKind.Local),
                        Count = ints.Sum(x => x.Count),
                        DurationMin = ints.Sum(x => x.WorkTime)
                    });

            return joinedPomodoros.OrderBy(x => x.DateTime.Date);
        }

        public int CompletedToday()
        {
            var startDate = DateTime.Now.Date;
            var endDate = startDate.AddDays(1).AddSeconds(-1);
            return _itemRepository.Pomodoros
                .Where(pomodoro => startDate <= pomodoro.DateTime && pomodoro.DateTime <= endDate)
                .Select(_ => _.Count)
                .DefaultIfEmpty(0)
                .Sum();
        }

        private void _engine_OnPomodoroCompleted()
        {
            Task.Run(() =>
            {
                _itemRepository.Add(new PomodoroEntity
                {
                    Count = 1,
                    DateTime = DateTime.UtcNow,
                    DurationMin = _engine.WorkTime / 60,
                    ProfileName = _engineSettings.ActiveProfile
                });
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

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}
