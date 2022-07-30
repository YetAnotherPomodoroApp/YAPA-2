using System;
using System.Collections.Generic;
using YAPA.Shared.Contracts;

namespace YAPA.Plugins.Tasks
{
    public class TasksMeta : IPluginMeta
    {
        public string Title => "Tasks";
        public string Id => "Tasks";
        public Type Plugin => typeof(TasksPlugin);
        public Type Settings => typeof(TasksSettings);
        public Type SettingEditWindow => typeof(TasksWindow);
    }

    public class TasksSettings : IPluginSettings
    {
        public void DeferChanges()
        {
        }
    }

    public class TasksPlugin : IPlugin
    {

    }

    public class TaskGroup
    {
        public string Title { get; set; }
        public bool Completed { get; set; }
        public bool CanBeCompleted => SubGroups == null || SubGroups.Count == 0;
        public List<TaskGroup> SubGroups { get; set; }
    }

}
