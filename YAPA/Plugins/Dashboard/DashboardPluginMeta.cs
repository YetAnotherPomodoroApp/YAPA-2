using System;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;
using YAPA.WPF;

namespace YAPA.Plugins.Dashboard
{
    [BuiltInPlugin(Order = 1)]
    public class DashboardPlugin : IPluginMeta
    {
        public string Title => "Dashboard";
        public string Id => "Dashboard";

        public Type Plugin => typeof(Shared.Common.Dashboard);

        public Type Settings => typeof(DashboardSettings);

        public Type SettingEditWindow => typeof(GithubDashboard);
    }
}
