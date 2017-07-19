using System.Windows.Input;
using YAPA.Shared.Contracts;
using YAPA.WPF;

namespace YAPA
{
    public partial class MainWindow
    {
        private readonly ISettings _globalSettings;
        private readonly DefaultThemeSettings _settings;

        public MainWindow(IMainViewModel viewModel, ISettings globalSettings, DefaultThemeSettings settings) : base(viewModel)
        {
            _globalSettings = globalSettings;
            _settings = settings;
            DataContext = ViewModel;

            _globalSettings.PropertyChanged += _settings_PropertyChanged;

            // enable dragging
            MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;

            InitializeComponent();
        }

        private void _settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == $"{nameof(DefaultTheme)}.{nameof(_settings.Width)}")
            {
                Width = _settings.Width;
            }
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
