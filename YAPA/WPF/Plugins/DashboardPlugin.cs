using System;
using System.Collections.Generic;
using YAPA.Contracts;

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
        private readonly IPomodoroEngine _engine;
        private ItemRepository _itemRepository;

        public Dashboard(IPomodoroEngine engine)
        {
            _engine = engine;
            _itemRepository = new ItemRepository();

            _engine.OnPomodoroCompleted += _engine_OnPomodoroCompleted;
        }

        public IEnumerable<PomodoroEntity> GetPomodoros()
        {
            return _itemRepository.GetPomodoros();
        }

        private void _engine_OnPomodoroCompleted()
        {
            _itemRepository.CompletePomodoro();
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
