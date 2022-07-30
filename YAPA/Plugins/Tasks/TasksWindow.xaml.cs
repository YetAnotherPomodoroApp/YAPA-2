using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace YAPA.Plugins.Tasks
{
    public partial class TasksWindow
    {
        private LocalTasksProvider _taskProvider;

        private TaskGroup _selected = null;

        public TasksWindow()
        {
            _taskProvider = new LocalTasksProvider();

            _taskProvider.LoadTasks();

            DataContext = _taskProvider;
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selected = (TaskGroup)e.NewValue;
            _taskProvider.SaveTasks();
        }


        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _taskProvider.SaveTasks();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            _taskProvider.Remove(_selected);
        }

        private void AddNew_Click(object sender, RoutedEventArgs e)
        {
            _taskProvider.AddNew(_selected, "testas");
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
