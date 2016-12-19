using System;
using YAPA.Contracts;
using YAPA.Shared;
using YAPA.WPF;
using YAPA.WPF.Plugins;

namespace YAPA.Plugins
{
    [BuiltInPlugin(Order = 1)]
    public class DashboardPlugin : IPluginMeta
    {
        public string Title => "Dashboard";

        public Type Plugin => typeof(Shared.Dashboard);

        public Type Settings => typeof(DashboardSettings);

        public Type SettingEditWindow => typeof(GithubDashboard);
    }
}
