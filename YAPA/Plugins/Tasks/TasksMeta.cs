using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    public class TaskGroup : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public bool Completed { get; set; }
        public bool CanBeCompleted => SubGroups == null || SubGroups.Count == 0;
        public ObservableCollection<TaskGroup> SubGroups { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddSubGroup(TaskGroup group)
        {
            if (SubGroups == null)
            {
                SubGroups = new ObservableCollection<TaskGroup>();
            }

            SubGroups.Add(group);
            TriggerPropertyChanged();
        }

        public void TriggerPropertyChanged()
        {
            OnPropertyChanged(nameof(SubGroups));
            OnPropertyChanged(nameof(CanBeCompleted));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
