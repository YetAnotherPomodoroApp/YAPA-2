using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using YAPA.Avalonia.Infrastructure;

namespace YAPA.Avalonia.Plugins.PomodoroEngine
{
    public partial class CreatePomodoroProfile : Window
    {
        private readonly List<string> _invalidNames = new List<string>();
        private bool _saved;

        public string SelectedName { get; private set; }

        public CreatePomodoroProfile()
        {
            InitializeComponent();
        }

        public CreatePomodoroProfile(List<string> invalidNames)
        {
            InitializeComponent();
            _invalidNames = invalidNames;
            Closing += (s, e) =>
            {
                if (!_saved)
                {
                    SelectedName = null;
                }
            };
            Opened += (s, e) => ProfileName.Focus();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedName = null;
            _saved = false;
            Close();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            SelectedName = ProfileName.Text;
            var isInvalidName = _invalidNames.Any(x => x.Equals(SelectedName, StringComparison.InvariantCultureIgnoreCase));

            if (isInvalidName)
            {
                await MessageBox.Show(this, "Profile name already in use.", "Unable to create profile");
            }
            else
            {
                _saved = true;
                Close();
            }
        }
    }
}
