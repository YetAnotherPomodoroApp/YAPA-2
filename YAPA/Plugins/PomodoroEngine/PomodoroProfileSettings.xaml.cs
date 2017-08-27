using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Plugins.PomodoroEngine
{
    public partial class PomodoroProfileSettings : UserControl, INotifyPropertyChanged
    {
        public List<string> Profiles { get; set; } = new List<string>();
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
            ActiveProfile.ItemsSource = Profiles;
            ActiveProfile.SelectionChanged += ActiveProfile_SelectectionChanged;
            RefreshProfilesList();
        }

        private void RefreshProfilesList()
        {
            Profiles.Clear();
            Profiles.AddRange(Settings.Profiles.Select(x => x.Key).ToList());
            NotifyPropertyChanged(nameof(Profiles));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName]string name = "")
        {
            PropertyChanged?.Invoke(name, new PropertyChangedEventArgs(name));
        }

        private void ActiveProfile_SelectectionChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            WorkTimeSelect.GetBindingExpression(ComboBox.TextProperty)?.UpdateTarget();
            BreakTimeSelect.GetBindingExpression(ComboBox.TextProperty)?.UpdateTarget();
            LongBreakTimeSelect.GetBindingExpression(ComboBox.TextProperty)?.UpdateTarget();
            AutoStartBreak.GetBindingExpression(CheckBox.IsCheckedProperty)?.UpdateTarget();
        }
    }
}
