using System;
using System.Collections.Generic;
using System.Linq;
using YAPA.Contracts;
using YAPA.Shared.Contracts;

namespace YAPA.WPF.Plugins
{

    public class DashboardPlugin : IPluginMeta
    {
        public string Title => "Dashboard";

        public Type Plugin => typeof(Dashboard);

        public Type Settings => typeof(DashboardSettings);

        public Type SettingEditWindow => null;
    }


    public class Dashboard : IPlugin
    {
        private readonly IPomodoroRepository _itemRepository;

        public Dashboard(IPomodoroEngine engine, IPomodoroRepository itemRepository)
        {
            _itemRepository = itemRepository;

            engine.OnPomodoroCompleted += _engine_OnPomodoroCompleted;
        }

        public IEnumerable<PomodoroEntity> GetPomodoros()
        {
            var days = 190;
            var today = DateTime.Now.Date.Date;
            var fromDate = today.AddDays(-days);
            var emptyPomodoros = Enumerable.Range(0, days + 1).Select(x => new PomodoroEntity() { Count = 0, DateTime = fromDate.AddDays(x) }).ToList();
            var capturedPomodoros = _itemRepository.Pomodoros.Where(x => x.DateTime >= fromDate).ToList();

            var joinedPomodoros = capturedPomodoros.Union(emptyPomodoros)
                .GroupBy(c => c.DateTime.Date, c => c.Count,
                    (time, ints) => new PomodoroEntity() { DateTime = time, Count = ints.Sum(x => x) });

            return joinedPomodoros.OrderBy(x => x.DateTime.Date);
        }


        public int CompletedToday()
        {
            var today = DateTime.Now.Date;
            return _itemRepository.Pomodoros.Where(x => x.DateTime == today).Select(a => a.Count).DefaultIfEmpty(0).Sum();
        }

        private void _engine_OnPomodoroCompleted()
        {
            _itemRepository.Add(new PomodoroEntity { Count = 1, DateTime = DateTime.UtcNow.Date });
        }
    }

    public class DashboardSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public DashboardSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(nameof(Dashboard));
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}
