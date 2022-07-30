using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace YAPA.Plugins.Tasks
{
    /// <summary>
    /// Interaction logic for TasksWindow.xaml
    /// </summary>
    public partial class TasksWindow
    {

        public List<TaskGroup> TreeItems { get; set; }
        public TasksWindow()
        {

            TreeItems = new List<TaskGroup>
            {
                new TaskGroup
                {
                    Title = "testas",
                    SubGroups =new List<TaskGroup>
                    {
                        new TaskGroup{ 
                            Title = "kazkas",
                            SubGroups = new List<TaskGroup>
                            {
                                new TaskGroup
                                {
                                    Title = "trecias lygis"
                                }
                            }
                        },
                    }
                }
            };
            this.DataContext = this;
            InitializeComponent();
        }
    }
}
