using System.Windows.Input;
using YAPA.Contracts;

namespace YAPA
{
    public partial class MainWindow
    {
        public MainWindow(IMainViewModel viewModel) : base(viewModel)
        {
            DataContext = ViewModel;

            // enable dragging
            MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;

            InitializeComponent();
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                OnMouseLeftButtonDown(e);
                DragMove();
                e.Handled = true;
            }
            catch
            {
                // ignored
            }
        }
    }
}
