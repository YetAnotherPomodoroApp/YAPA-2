using System.Windows.Input;
using YAPA.Contracts;
using YAPA.Shared;

namespace YAPA
{
    public partial class MainWindow
    {
        public IPomodoroEngine Engine { get; set; }

        public ICommand StopCommand { get; set; }
        public ICommand StartCommand { get; set; }
        public ICommand ResetCommand { get; set; }

        public MainWindow(IPomodoroEngine engine) : base(engine)
        {
            Engine = engine;

            StopCommand = new StopCommand(Engine);
            StartCommand = new StartCommand(Engine);
            ResetCommand = new ResetCommand(Engine);

            DataContext = this;

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
