using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Plugins.PomodoroEngine
{
    public partial class PomodoroProfileSettings : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<string> Profiles { get; set; } = new ObservableCollection<string>();
        public PomodoroEngineSettings Settings { get; }
        public IPomodoroEngine Engine { get; }

        private bool _refreshing;

        public PomodoroProfileSettings()
        {
            InitializeComponent();
        }

        public PomodoroProfileSettings(PomodoroEngineSettings settings, IPomodoroEngine engine)
        {
            settings.DeferChanges();

            InitializeComponent();
            var oneHour = Enumerable.Range(1, 60).Reverse().Select(x => x.ToString()).ToList();
            WorkTimeSelect.ItemsSource = oneHour;
            BreakTimeSelect.ItemsSource = oneHour;
            LongBreakTimeSelect.ItemsSource = oneHour;
            SessionRounds.ItemsSource = Enumerable.Range(2, 10).Select(x => x.ToString()).ToList();
            Settings = settings;
            Engine = engine;

            Engine.PropertyChanged += Engine_PropertyChanged;
            DetachedFromVisualTree += (s, e) => Engine.PropertyChanged -= Engine_PropertyChanged;
            AttachedToVisualTree += (s, e) =>
            {
                Engine.PropertyChanged -= Engine_PropertyChanged;
                Engine.PropertyChanged += Engine_PropertyChanged;
            };

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
            _refreshing = true;
            Profiles.Clear();
            foreach (var item in Settings.Profiles.Select(x => x.Key).ToList())
            {
                Profiles.Add(item);
            }
            ActiveProfileSelect.SelectedItem = Settings.ActiveProfile;
            _refreshing = false;
            NotifyPropertyChanged(nameof(Profiles));

            RemoveButton.IsEnabled = Profiles.Count > 1 && !Engine.IsRunning;
        }

        private void ActiveProfile_SelectectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_refreshing)
            {
                return;
            }
            var selected = ActiveProfileSelect.SelectedItem as string;
            if (string.IsNullOrEmpty(selected))
            {
                return;
            }
            Settings.ActiveProfile = selected;

            RefreshProfileProperties();
        }

        // Re-read every field from the newly selected profile (WPF: UpdateTarget on each binding)
        private void RefreshProfileProperties()
        {
            var context = DataContext;
            DataContext = null;
            DataContext = context;
            _refreshing = true;
            ActiveProfileSelect.SelectedItem = Settings.ActiveProfile;
            _refreshing = false;
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

        private async void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            var newProfile = new PomodoroProfile { BreakTime = 5 * 60, WorkTime = 25 * 60, LongBreakTime = 15 * 60, PomodorosBeforeLongBreak = PomodoroEngineSettings.DefaultPomodorosBeforeLongBreak };

            var createWindow = new CreatePomodoroProfile(Settings.Profiles.Select(x => x.Key).ToList());

            var parent = TopLevel.GetTopLevel(this) as Window;
            if (parent != null)
            {
                createWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                createWindow.Position = new global::Avalonia.PixelPoint(
                    parent.Position.X + (int)(parent.Bounds.Width / 2.5 * parent.RenderScaling),
                    parent.Position.Y + (int)(parent.Bounds.Height / 2.5 * parent.RenderScaling));
                await createWindow.ShowDialog(parent);
            }
            else
            {
                createWindow.Show();
                return;
            }

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

        private PropertyChangedEventHandler _propertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        private void NotifyPropertyChanged([CallerMemberName] string name = "")
        {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class SecondsToMinutesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var seconds = value as int? ?? 60;
            return (seconds / 60).ToString(CultureInfo.InvariantCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!int.TryParse(value?.ToString(), out var minutes))
            {
                minutes = 1;
            }
            return minutes * 60;
        }
    }
}
