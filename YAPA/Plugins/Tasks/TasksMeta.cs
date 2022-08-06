using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
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

    public class TasksSettings : IPluginSettings, INotifyPropertyChanged
    {
        private readonly IPomodoroEngine _pomodoroEngine;

        public LocalTasksProvider TaskProvider { get; set; }

        public TaskGroup Selected { get; set; } = null;

        public event PropertyChangedEventHandler PropertyChanged;

        public TasksSettings(IPomodoroEngine pomodoroEngine)
        {
            TaskProvider = new LocalTasksProvider();

            TaskProvider.LoadTasks();
            _pomodoroEngine = pomodoroEngine;

            _pomodoroEngine.OnPomodoroCompleted += _pomodoroEngine_OnPomodoroCompleted;
        }

        private void _pomodoroEngine_OnPomodoroCompleted()
        {
            if (Selected != null)
            {

                var workTime = _pomodoroEngine.WorkTime;
                Selected.AddCompletion(workTime);
                Save();
            }
        }

        public void Select(TaskGroup group)
        {
            Selected = group;
            OnPropertyChanged(nameof(Selected));
        }

        public void Save()
        {
            TaskProvider.SaveTasks();
        }

        public void DeferChanges()
        {
        }

        internal void Remove()
        {
            TaskProvider.Remove(Selected);
            Selected = null;
            OnPropertyChanged(nameof(Selected));
        }

        internal void AddNew(string title)
        {
            var random = new Random(DateTime.Now.Millisecond);
            TaskProvider.AddNew(Selected, title + " " + random.Next());
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TasksPlugin : IPlugin
    {
        Window _appWindow;
        Window _taskPreview;

        public TasksPlugin(IApplication app, TasksSettings tasksSettings)
        {
            _taskPreview = new TasksPreview(tasksSettings);
            _taskPreview.Visibility = Visibility.Visible;
            _taskPreview.WindowStyle = WindowStyle.None;
            _taskPreview.ResizeMode = ResizeMode.NoResize;
            _appWindow = (Window)app;
            _appWindow.LocationChanged += Window_LocationChanged;
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            Console.WriteLine(_appWindow.Left + " " + _appWindow.Top);
            _taskPreview.Left = _appWindow.Left;
            _taskPreview.Top = _appWindow.Top + _appWindow.Height;
        }
    }

    public class TaskGroup : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public bool Completed { get; set; }
        public bool CanBeCompleted => SubGroups == null || SubGroups.Count == 0;
        public string Path { get; set; }
        public ObservableCollection<CompletedTaskPomodoro> CompletedPomodoros { get; set; }
        public int WorkTime
        {
            get
            {
                return CompletedPomodoros.Sum(_ => _.WorkTime);
            }
        }

        public ObservableCollection<TaskGroup> SubGroups { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public TaskGroup()
        {
            CompletedPomodoros = new ObservableCollection<CompletedTaskPomodoro>();
        }

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

        internal void AddCompletion(int workTime)
        {
            CompletedPomodoros.Add(new CompletedTaskPomodoro
            {
                WorkTime = workTime
            });
            OnPropertyChanged(nameof(SubGroups));
            OnPropertyChanged(nameof(WorkTime));
        }
    }

    public class CompletedTaskPomodoro
    {
        public int WorkTime { get; set; }
    }
}
