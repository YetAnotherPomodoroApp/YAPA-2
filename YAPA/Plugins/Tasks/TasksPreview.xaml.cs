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
    /// Interaction logic for TasksPreview.xaml
    /// </summary>
    public partial class TasksPreview : Window
    {
        public TasksSettings Tasks { get; set; }

        public TasksPreview(TasksSettings tasksSettings)
        {
            Topmost = true;
            Tasks = tasksSettings;
            DataContext = this;
            InitializeComponent();
        }

        public double ClockOpacity => 1;

        public Brush TextBrush
        {
            get
            {
                return new SolidColorBrush(Colors.Red);
            }
        }

    }
}
