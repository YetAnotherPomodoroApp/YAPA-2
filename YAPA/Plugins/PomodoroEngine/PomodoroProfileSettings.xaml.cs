using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Plugins.PomodoroEngine
{
    public partial class PomodoroProfileSettings : INotifyPropertyChanged
    {
        public ObservableCollection<string> Profiles { get; set; } = new ObservableCollection<string>();
        public PomodoroEngineSettings Settings { get; }
        public IPomodoroEngine Engine { get; }

        private ISettings GlobalSetting { get; }

        public PomodoroProfileSettings(PomodoroEngineSettings settings, IPomodoroEngine engine, ISettings globalSetting)
        {
            settings.DeferChanges();

            InitializeComponent();

            var oneHour = Enumerable.Range(1, 60).Reverse().ToList();
            WorkTimeSelect.ItemsSource = oneHour;
            BreakTimeSelect.ItemsSource = oneHour;
            LongBreakTimeSelect.ItemsSource = oneHour;
            Settings = settings;
            Engine = engine;

            Engine.PropertyChanged += Engine_PropertyChanged;

            GlobalSetting = globalSetting;
            DataContext = this;
            ActiveProfileSelect.ItemsSource = Profiles;
            ActiveProfileSelect.SelectionChanged += ActiveProfile_SelectectionChanged;
            RefreshProfilesList();
        }

        private void Engine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Engine.Phase))
            {
                RemoveButton.IsEnabled = Profiles.Count > 1 && !Engine.IsRunning;
            }
        }

        private void RefreshProfilesList()
        {
            Profiles.Clear();
            foreach (var item in Settings.Profiles.Select(x => x.Key).ToList())
            {
                Profiles.Add(item);
            }
            NotifyPropertyChanged(nameof(Profiles));

            RemoveButton.IsEnabled = Profiles.Count > 1 && !Engine.IsRunning;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName]string name = "")
        {
            PropertyChanged?.Invoke(name, new PropertyChangedEventArgs(name));
        }

        private void ActiveProfile_SelectectionChanged(object sender, RoutedEventArgs e)
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
            ActiveProfileSelect.GetBindingExpression(Selector.SelectedItemProperty)?.UpdateTarget();
            WorkTimeSelect.GetBindingExpression(ComboBox.TextProperty)?.UpdateTarget();
            BreakTimeSelect.GetBindingExpression(ComboBox.TextProperty)?.UpdateTarget();
            LongBreakTimeSelect.GetBindingExpression(ComboBox.TextProperty)?.UpdateTarget();
            AutoStartBreak.GetBindingExpression(ToggleButton.IsCheckedProperty)?.UpdateTarget();
        }

        private void RemoveProfile_Click(object sender, RoutedEventArgs e)
        {
            var profiles = Settings.Profiles;
            profiles.Remove(Settings.ActiveProfile);
            Settings.ActiveProfile = profiles.First().Key;

            Settings.Profiles = profiles;

            RefreshProfilesList();

            RefreshProfileProperties();
        }

        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            var newProfile = new PomodoroProfile { BreakTime = 5 * 60, WorkTime = 25 * 60, LongBreakTime = 15 * 60 };

            var createWindow = new CreatePomodoroProfile(Settings.Profiles.Select(x => x.Key).ToList());

            var parent = Window.GetWindow(this);
            if (parent != null)
            {
                createWindow.Left = parent.Left + parent.Width / 2.5;
                createWindow.Top = parent.Top + parent.Height / 2.5;
            }
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
