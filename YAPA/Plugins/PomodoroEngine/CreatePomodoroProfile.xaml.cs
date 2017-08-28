using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace YAPA.Plugins.PomodoroEngine
{
    public partial class CreatePomodoroProfile : Window
    {
        public CreatePomodoroProfile(List<string> invalidNames)
        {
            InitializeComponent();
            InvalidNames = invalidNames;
            Closing += CreatePomodoroProfile_Closing;
        }

        private void CreatePomodoroProfile_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_saved)
            {
                SelectedName = null;
            }
        }

        private List<string> InvalidNames { get; }
        public string SelectedName { get; private set; }
        private bool _saved;

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedName = null;
            _saved = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SelectedName = ProfileName.Text;
            var isInvalidName = InvalidNames.Any(x => x.Equals(SelectedName, System.StringComparison.InvariantCultureIgnoreCase));

            if (isInvalidName)
            {
                MessageBox.Show("Profile name already in use.", "Unable to create profile", MessageBoxButton.OK);
            }
            else
            {
                _saved = true;
                Close();
            }
        }
    }
}
