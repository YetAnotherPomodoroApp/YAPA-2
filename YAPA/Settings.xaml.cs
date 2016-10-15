using System.Windows;
using System.Windows.Input;
using YAPA.Contracts;

namespace YAPA
{

    public partial class Settings : Window
    {
        private readonly ISettings _settings;
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public Settings(ISettings settings)
        {
            _settings = settings;

            SaveCommand = new SaveSettingsCommand(this, settings);
            CancelCommand = new CancelSettingsCommand(this, settings);

            DataContext = this;
            InitializeComponent();
        }
    }
}
