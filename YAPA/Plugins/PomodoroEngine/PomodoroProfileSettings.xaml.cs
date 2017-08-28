using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Plugins.PomodoroEngine
{
    public partial class PomodoroProfileSettings : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<string> Profiles { get; set; } = new ObservableCollection<string>();
        public PomodoroEngineSettings Settings { get; }
        public IPomodoroEngine Engine { get; }

        private ISettings _globalSetting { get; }

        public PomodoroProfileSettings(PomodoroEngineSettings settings, IPomodoroEngine engine, ISettings globalSetting)
        {
            settings.DeferChanges();

            InitializeComponent();

            var oneHour = Enumerable.Range(1, 60).ToList();
            WorkTimeSelect.ItemsSource = oneHour;
            BreakTimeSelect.ItemsSource = oneHour;
            LongBreakTimeSelect.ItemsSource = oneHour;
            Settings = settings;
            Engine = engine;

            _globalSetting = globalSetting;
            DataContext = this;
            ActiveProfileSelect.ItemsSource = Profiles;
            ActiveProfileSelect.SelectionChanged += ActiveProfile_SelectectionChanged;
            RefreshProfilesList();
        }

        private void RefreshProfilesList()
        {
            Profiles.Clear();
            foreach (var item in Settings.Profiles.Select(x => x.Key).ToList())
            {
                Profiles.Add(item);
            }
            NotifyPropertyChanged(nameof(Profiles));

            RemoveButton.IsEnabled = Profiles.Count > 1;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName]string name = "")
        {
            PropertyChanged?.Invoke(name, new PropertyChangedEventArgs(name));
        }

        private void ActiveProfile_SelectectionChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            var selected = (string)((ComboBox)sender).SelectedItem;
            if (string.IsNullOrEmpty(selected))
            {
                return;
            }
            Settings.ActiveProfile = (string)((ComboBox)sender).SelectedItem;

            RefreshProfileProperties();
        }

        private void RefreshProfileProperties()
        {
            ActiveProfileSelect.GetBindingExpression(ComboBox.SelectedItemProperty)?.UpdateTarget();
            WorkTimeSelect.GetBindingExpression(ComboBox.TextProperty)?.UpdateTarget();
            BreakTimeSelect.GetBindingExpression(ComboBox.TextProperty)?.UpdateTarget();
            LongBreakTimeSelect.GetBindingExpression(ComboBox.TextProperty)?.UpdateTarget();
            AutoStartBreak.GetBindingExpression(CheckBox.IsCheckedProperty)?.UpdateTarget();
        }

        private void RemoveProfile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var profiles = Settings.Profiles;
            profiles.Remove(Settings.ActiveProfile);
            Settings.ActiveProfile = profiles.First().Key;

            Settings.Profiles = profiles;

            RefreshProfilesList();

            RefreshProfileProperties();
        }

        private void AddProfile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var newProfile = new PomodoroProfile { BreakTime = 5 * 60, WorkTime = 25 * 60, LongBreakTime = 15 * 60 };

            var createWindow = new CreatePomodoroProfile(Settings.Profiles.Select(x => x.Key).ToList());

            var parent = Window.GetWindow(this);
            createWindow.Left = parent.Left + parent.Width / 2.5;
            createWindow.Top = parent.Top + parent.Height / 2.5;
            createWindow.Topmost = true;
            createWindow.ShowDialog();
            if (string.IsNullOrEmpty(createWindow.SelectedName))
            {
                return;
            }

            var profileName = createWindow.SelectedName;
            var profiles = Settings.Profiles;
            profiles.Add(profileName, newProfile);

            Settings.Profiles = profiles;
            Settings.ActiveProfile = profileName;

            RefreshProfilesList();

            RefreshProfileProperties();
        }
    }
}
