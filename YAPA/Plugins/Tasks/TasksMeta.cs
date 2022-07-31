using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

    public class TasksSettings : IPluginSettings
    {
        public void DeferChanges()
        {
        }
    }

    public class TasksPlugin : IPlugin
    {
        Window _appWindow;
        Window _taskPreview;
        public TasksPlugin(IApplication app)
        {
            _taskPreview = new TasksPreview();
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
