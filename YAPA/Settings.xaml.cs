using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;

namespace YAPA
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window, ISettingsViewModel, INotifyPropertyChanged
    {
        private IMainViewModel _host;
        private ICommand _saveSettings;
        private double _clockOpacity;
        private double _shadowOpacity;
        private bool _useWhiteText;
        private int _workTime;
        private int _breakTime;
        private int _breakLongTime;
        private bool _soundEfects;
        private bool _countBackwards;
        private ItemRepository _itemRepository;

        // INPC support
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Window constructor.
        /// </summary>
        public  Settings(IMainViewModel host, double currentOpacity, Brush currentTextColor, int workTime, int breakTime, int breakLongTime, bool soundEfects, double shadowOpacity, bool countBackwards)
        {
            InitializeComponent();
            this.DataContext = this;
            _host = host;
            _useWhiteText = true;
            _soundEfects = soundEfects;
            _clockOpacity = currentOpacity;
            _saveSettings = new SaveSettings(this);
            _useWhiteText = (currentTextColor.ToString() == Brushes.White.ToString());
            _breakTime = breakTime;
            _breakLongTime = breakLongTime;
            _workTime = workTime;
            _shadowOpacity = shadowOpacity;
            _countBackwards = countBackwards;
            MouseLeftButtonDown += Settings_MouseLeftButtonDown;
            _itemRepository = new ItemRepository();

            Loaded += Settings_Loaded;
        }

        private async void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
                Calendar cal = dfi.Calendar;

                var pomodoros =
                    _itemRepository.GetPomodoros()
                        .Select(
                            x => new { week = cal.GetWeekOfYear(x.DateTime, dfi.CalendarWeekRule, dfi.FirstDayOfWeek), x });
                int max = pomodoros.Max(x => x.x.Count);

                foreach (var pomodoro in pomodoros.GroupBy(x => x.week))
                {
                    var week = pomodoro.Select(x => x.x.ToPomodoroViewModel(GetLevelFromCount(x.x.Count, max)));
                    Dispatcher.Invoke(() =>
                    {
                        WeekStackPanel.Children.Add(new PomodoroWeek(week));
                    });
                }

                Dispatcher.Invoke(() =>
                {
                    int weekShift = DayOfWeek.Monday - System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                    MondayTextBlock.Margin = new Thickness(0, 12 * weekShift - 1, 0, 12);

                    DayPanel.Visibility = Visibility.Visible;
                    LoadingPanel.Visibility = Visibility.Collapsed;
                });
            });
        }

        private  PomodoroLevelEnum GetLevelFromCount(int count, int maxCount)
        {
            if (count == 0)
            {
                return PomodoroLevelEnum.Level0;
            }
            if (maxCount <= 4)
            {
                return PomodoroLevelEnum.Level4;
            }
            var level = (double)count / maxCount;
            if (level < 0.25)
            {
                return PomodoroLevelEnum.Level1;
            }
            else if (level < 0.50)
            {
                return PomodoroLevelEnum.Level2;
            }
            else if (level < 0.75)
            {
                return PomodoroLevelEnum.Level3;
            }

            return PomodoroLevelEnum.Level4;
        }

        /// <summary>
        /// Used to support dragging the window around.
        /// </summary>
        private void Settings_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        /// <summary>
        /// Closes this view.
        /// </summary>
        public void CloseSettings()
        {
            this.Close();
        }

        /// <summary>
        /// The desired opacity of the 
        /// </summary>
        public double ClockOpacity
        {
            get { return _clockOpacity; }
            set
            {
                _clockOpacity = value;
                _host.ClockOpacity = value;
                RaisePropertyChanged("ClockOpacity");
            }
        }

        /// <summary>
        /// The desired opacity of the 
        /// </summary>
        public double ShadowOpacity
        {
            get { return _shadowOpacity; }
            set
            {
                _shadowOpacity = value;
                _host.ShadowOpacity = value;
                RaisePropertyChanged("ShadowOpacity");
            }
        }

        /// <summary>
        /// True if we are to use white text to render;
        /// otherwise, false.
        /// </summary>
        public bool UseWhiteText
        {
            get { return _useWhiteText; }
            set
            {
                _useWhiteText = value;
                _host.TextBrush = (_useWhiteText ? Brushes.White : Brushes.Black);
                RaisePropertyChanged("UseWhiteText");
            }
        }

        public bool SoundEfects
        {
            get { return _soundEfects; }
            set
            {
                _soundEfects = value;
                _host.SoundEffects = value;
                RaisePropertyChanged("SoundEfects");
            }
        }

        public int WorkTime
        {
            get { return _workTime; }
            set
            {
                _workTime = value;
                _host.WorkTime = value;
                RaisePropertyChanged("WorkTime");
            }
        }

        public int BreakTime
        {
            get { return _breakTime; }
            set
            {
                _breakTime = value;
                _host.BreakTime = value;
                RaisePropertyChanged("BreakTime");
            }
        }

        public int BreakLongTime
        {
            get { return _breakLongTime; }
            set
            {
                _breakLongTime = value;
                _host.BreakLongTime = value;
                RaisePropertyChanged("BreakLongTime");
            }
        }

        public bool CountBackwards
        {
            get
            {
                return _countBackwards;
            }
            set
            {
                _countBackwards = value;
                _host.CountBackwards = value;
                this.RaisePropertyChanged("CountBackwards");
            }
        }

        /// <summary>
        /// Command invoked when user clicks 'Done'
        /// </summary>
        public ICommand SaveSettings
        {
            get { return _saveSettings; }
        }

        /// <summary>
        /// Used to raise change notifications to other consumers.
        /// </summary>
        private void RaisePropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        /// <summary>
        /// Handles a user click on the navigation link.
        /// </summary>
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(((Hyperlink)sender).NavigateUri.ToString());
        }
    }
}
