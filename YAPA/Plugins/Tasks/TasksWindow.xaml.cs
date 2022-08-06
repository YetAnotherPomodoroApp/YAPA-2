using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace YAPA.Plugins.Tasks
{
    public partial class TasksWindow
    {
        private readonly TasksSettings _tasksSettings;

        public TasksWindow(TasksSettings tasksSettings)
        {
            _tasksSettings = tasksSettings;
            DataContext = _tasksSettings.TaskProvider;
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _tasksSettings.Select((TaskGroup)e.NewValue);
            _tasksSettings.Save();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _tasksSettings.Save();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            _tasksSettings.Remove();
        }

        private void AddNew_Click(object sender, RoutedEventArgs e)
        {
            _tasksSettings.AddNew( "testas");
        }

        private void TriggerChanges(ObservableCollection<TaskGroup> group)
        {
            foreach (TaskGroup groupItem in group)
            {
                if (groupItem != null)
                {
                    if (groupItem.SubGroups != null)
                    {
                        TriggerChanges(groupItem.SubGroups);
                    }
                }
                groupItem.TriggerPropertyChanged();
                Console.WriteLine(groupItem.Title);
            }
        }
    }
}
