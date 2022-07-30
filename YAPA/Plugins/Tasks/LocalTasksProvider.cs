using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using YAPA.WPF.Specifics;

namespace YAPA.Plugins.Tasks
{
    public class LocalTasksProvider : INotifyPropertyChanged
    {
        public ObservableCollection<TaskGroup> Tasks { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<TaskGroup> GetTasks()
        {
            return Tasks;
        }

        public void LoadTasks()
        {
            var taskLocation = Path.Combine(WpfEnviroment.BaseDir, "localTasks.json");
            if (File.Exists(taskLocation))
            {
                var content = File.ReadAllText(taskLocation);
                var tasks = JsonConvert.DeserializeObject<ObservableCollection<TaskGroup>>(content);
                Tasks = tasks;
            }
            else
            {
                Tasks = new ObservableCollection<TaskGroup>();
            }
        }

        public void SaveTasks()
        {
            var taskLocation = Path.Combine(WpfEnviroment.BaseDir, "localTasks.json");
            var content = JsonConvert.SerializeObject(Tasks);
            File.WriteAllText(taskLocation, content);
        }

        internal void Remove(TaskGroup selected)
        {
            var parent = FindParent(Tasks.First(), selected);
            if (parent != null)
            {
                parent.SubGroups.Remove(selected);
                OnPropertyChanged(nameof(Tasks));
            }
        }

        private TaskGroup FindParent(TaskGroup group, TaskGroup child)
        {
            if (group?.SubGroups?.Contains(child) == true)
            {
                return group;
            }
            else
            {
                return group?.SubGroups?.Select(_ => FindParent(_, child)).FirstOrDefault(_ => _ != null);
            }
        }

        internal void AddNew(TaskGroup selected, string title)
        {
            selected.AddSubGroup(new TaskGroup { Title = title });
            OnPropertyChanged(nameof(Tasks));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
