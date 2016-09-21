using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Threading;
using GDIScreen = System.Windows.Forms.Screen;
using WindowState = System.Windows.WindowState;

namespace YAPA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainViewModel, INotifyPropertyChanged
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal extern static bool DestroyIcon(IntPtr handle);

        private DispatcherTimer _dispacherTime;
        private Stopwatch _stopWatch;
        private ItemRepository _itemRepository;

        private ICommand _showSettings;
        private Storyboard TimerFlush;

        private double _progressValue;
        private string _progressState;
        private int _ticks;
        private int _period;
        private bool _isBreak;
        private bool _isBreakLong;
        private bool _isWork;
        private string _breakLabel;
        private string _workLabel;
        private SoundPlayer _tickSound;
        private SoundPlayer _ringSound;
        private System.Windows.Forms.NotifyIcon sysTrayIcon;
        private IntPtr _systemTrayIcon;

        private System.Drawing.Color WorkTrayIconColor;
        private System.Drawing.Color BreakTrayIconColor;

        private SoundPlayer _musicPlayer;
        // For INCP
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();

            _dispacherTime = new DispatcherTimer();
            _stopWatch = new Stopwatch();
            _itemRepository = new ItemRepository();

            DataContext = this;
            _showSettings = new ShowSettings(this);
            _ticks = 0;
            _period = 0;
            _isBreak = false;
            _isBreakLong = false;
            _isWork = true;
            _breakLabel = "break";
            _workLabel = "work";

            _dispacherTime.Tick += new EventHandler(DoTick);
            _dispacherTime.Interval = new TimeSpan(0, 0, 0, 1);

            // enable dragging
            MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;

            // save window position on close
            Closing += MainWindow_Closing;

            // flash timer
            TimerFlush = (Storyboard)TryFindResource("FlashTimer");

            // play sounds
            _tickSound = new SoundPlayer(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\tick.wav"));
            _ringSound = new SoundPlayer(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\ding.wav"));

            _musicPlayer = new SoundPlayer();

            Loaded += MainWindow_Loaded;
            StateChanged += MainWindow_StateChanged;

            this.ShowInTaskbar = Properties.Settings.Default.ShowInTaskbar;

            WorkTrayIconColor = (System.Drawing.Color)System.Drawing.ColorTranslator.FromHtml(Properties.Settings.Default.WorkTrayIconColor);
            BreakTrayIconColor = (System.Drawing.Color)System.Drawing.ColorTranslator.FromHtml(Properties.Settings.Default.BreakTrayIconColor);
        }

        private void PlayMusic()
        {
            if (_isWork)
            {
                if (File.Exists(WorkMusic))
                {
                    _musicPlayer.SoundLocation = WorkMusic;
                    _musicPlayer.PlayLooping();
                }
            }
            else
            {
                if (File.Exists(BreakMusic))
                {
                    _musicPlayer.SoundLocation = BreakMusic;
                    _musicPlayer.Play();
                }
            }

        }

        private void PauseMusic()
        {
            _musicPlayer.Stop();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized && MinimizeToTray == true && Properties.Settings.Default.ShowInTaskbar)
            {
                Hide();
                sysTrayIcon.Visible = true;
            }

            if (!Properties.Settings.Default.ShowInTaskbar)
            {
                if (this.WindowState == WindowState.Minimized)
                {
                    sysTrayIcon.Visible = true;
                }
                else
                {
                    this.ShowInTaskbar = Properties.Settings.Default.ShowInTaskbar;
                }
            }

        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (Properties.Settings.Default.IsFirstRun)
            {
                Properties.Settings.Default.IsFirstRun = false;
            }

            GDIScreen currentScreen = GDIScreen.FromHandle(new WindowInteropHelper(this).Handle);

            Properties.Settings.Default.CurrentScreenHeight = currentScreen.WorkingArea.Height;
            Properties.Settings.Default.CurrentScreenWidth = currentScreen.WorkingArea.Width;

            Properties.Settings.Default.Save();

            PauseMusic();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CreateJumpList();
            ResetTicking();

            //if you want to handle to command line args on the first instance you may want it to go here
            //or in the app.xaml.cs
            //ProcessCommandLineArgs(SingleInstance<App>.CommandLineArgs);

            var currentScreen = GDIScreen.FromHandle(new WindowInteropHelper(this).Handle);

            var screenChanged = (currentScreen.WorkingArea.Height != Properties.Settings.Default.CurrentScreenHeight ||
                                currentScreen.WorkingArea.Width != Properties.Settings.Default.CurrentScreenWidth);

            // default position only for first run or when screen size changes
            // position the clock at top / right, primary screen
            if (Properties.Settings.Default.IsFirstRun || screenChanged)
            {
                Left = SystemParameters.PrimaryScreenWidth - Width - 15.0;
                Top = 0;
            }

            sysTrayIcon = new System.Windows.Forms.NotifyIcon();
            sysTrayIcon.Text = "YAPA";
            sysTrayIcon.Icon = new System.Drawing.Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\pomoTray.ico"), 40, 40);
            sysTrayIcon.Visible = false;
            sysTrayIcon.DoubleClick += SysTrayIcon_DoubleClick;

            sysTrayIcon.ContextMenu = new System.Windows.Forms.ContextMenu(CreateNotifyIconContextMenu());
        }

        private System.Windows.Forms.MenuItem[] CreateNotifyIconContextMenu()
        {
            var startTask = new System.Windows.Forms.MenuItem { Text = "Start" };
            startTask.Click += (o, s) => { ProcessCommandLineArgs(string.Empty, "/start"); };

            var pauseTask = new System.Windows.Forms.MenuItem { Text = "Pause" };
            pauseTask.Click += (o, s) => { ProcessCommandLineArgs(string.Empty, "/pause"); };

            var stopTask = new System.Windows.Forms.MenuItem { Text = "Restart" };
            stopTask.Click += (o, s) => { ProcessCommandLineArgs(string.Empty, "/restart"); };

            var resetTask = new System.Windows.Forms.MenuItem { Text = "Reset session" };
            resetTask.Click += (o, s) => { ProcessCommandLineArgs(string.Empty, "/reset"); };

            return new System.Windows.Forms.MenuItem[] {
                startTask, pauseTask,stopTask, resetTask
            };
        }

        private void SysTrayIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            this.WindowState = WindowState.Normal;
            sysTrayIcon.Visible = false;
        }

        //http://blogs.msdn.com/b/abhinaba/archive/2005/09/12/animation-and-text-in-system-tray-using-c.aspx
        public void ShowText(string text)
        {
            System.Drawing.Color textColor;

            if (_isWork)
            {
                textColor = WorkTrayIconColor;
            }
            else
            {
                textColor = BreakTrayIconColor;
            }

            if (_systemTrayIcon != IntPtr.Zero)
            {
                DestroyIcon(_systemTrayIcon);
            }

            System.Drawing.Brush brush = new System.Drawing.SolidBrush(textColor);

            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(16, 16);
            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap);
            graphics.DrawString(text, new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Bold), brush, 0, 0);

            _systemTrayIcon = bitmap.GetHicon();


            System.Drawing.Icon icon = System.Drawing.Icon.FromHandle(_systemTrayIcon);
            sysTrayIcon.Icon = icon;
        }

        private void CreateJumpList()
        {
            var jumpList = new JumpList();
            JumpList.SetJumpList(Application.Current, jumpList);

            var startTask = new JumpTask();
            startTask.Title = "Start";
            startTask.Description = "Start Pomodoro session";
            startTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            startTask.Arguments = "/start";
            startTask.IconResourceIndex = 7;
            jumpList.JumpItems.Add(startTask);

            var pauseTask = new JumpTask();
            pauseTask.Title = "Pause";
            pauseTask.Description = "Pause Pomodoro session";
            pauseTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            pauseTask.Arguments = "/pause";
            pauseTask.IconResourceIndex = 3;
            jumpList.JumpItems.Add(pauseTask);

            var stopTask = new JumpTask();
            stopTask.Title = "Restart";
            stopTask.Description = "Restart current Pomodori session";
            stopTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            stopTask.Arguments = "/restart";
            stopTask.IconResourceIndex = 4;
            jumpList.JumpItems.Add(stopTask);

            var resetTask = new JumpTask();
            resetTask.Title = "Start from the beginning";
            resetTask.Description = "Start new Pomodoro session";
            resetTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            resetTask.Arguments = "/reset";
            resetTask.IconResourceIndex = 2;
            jumpList.JumpItems.Add(resetTask);

            var settingsTask = new JumpTask();
            settingsTask.Title = "Settings";
            settingsTask.Description = "Show YAPA settings";
            settingsTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            settingsTask.Arguments = "/settings";
            settingsTask.IconResourceIndex = 5;
            jumpList.JumpItems.Add(settingsTask);

            var homepageTask = new JumpTask();
            homepageTask.Title = "Visit home page";
            homepageTask.Description = "Go to YAPA home page";
            homepageTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            homepageTask.Arguments = "/homepage";
            homepageTask.IconResourceIndex = 6;
            jumpList.JumpItems.Add(homepageTask);

            jumpList.Apply();
        }

        public ICommand ShowSettings
        {
            get { return _showSettings; }
        }

        public Brush TextBrush
        {
            get { return new BrushConverter().ConvertFromString(Properties.Settings.Default.TextBrush) as SolidColorBrush; }
            set
            {
                Properties.Settings.Default.TextBrush = value.ToString();
                RaisePropertyChanged("TextBrush");
                RaisePropertyChanged("TextShadowColor");
                RaisePropertyChanged("MouseOverBackgroundColor");
            }
        }

        public Color TextShadowColor
        {
            get
            {
                var shadowColor = Colors.White;

                if (TextBrush.ToString() == Brushes.White.ToString())
                {
                    shadowColor = Colors.Black;
                }
                else
                {
                    shadowColor = Colors.White;
                }

                return shadowColor;
            }
            set
            {

            }
        }

        public Brush MouseOverBackgroundColor
        {
            get
            {
                var mouseOverBackgroundColor = Brushes.White;

                if (TextBrush.ToString() == Brushes.White.ToString())
                {
                    mouseOverBackgroundColor = Brushes.Black;
                }
                else
                {
                    mouseOverBackgroundColor = Brushes.White;
                }

                return mouseOverBackgroundColor;
            }
            set
            {
            }
        }


        public bool SoundEffects
        {
            get { return Properties.Settings.Default.SoundNotification; }
            set
            {
                Properties.Settings.Default.SoundNotification = value;
                RaisePropertyChanged("UseSoundEfects");
            }
        }

        public double ClockOpacity
        {
            get { return Properties.Settings.Default.OpacityLevel; }
            set
            {
                Properties.Settings.Default.OpacityLevel = value;
                RaisePropertyChanged("ClockOpacity");
            }
        }

        public double ShadowOpacity
        {
            get { return Properties.Settings.Default.ShadowOpacityLevel; }
            set
            {
                Properties.Settings.Default.ShadowOpacityLevel = value;
                RaisePropertyChanged("ShadowOpacity");
            }
        }


        public bool MinimizeToTray
        {
            get { return Properties.Settings.Default.MinimizeToTray; }
            set
            {
                Properties.Settings.Default.MinimizeToTray = value;
                RaisePropertyChanged("MinimizeToTray");
            }
        }

        public string WorkMusic
        {
            get { return Properties.Settings.Default.WorkMusic; }
            set
            {
                Properties.Settings.Default.WorkMusic = value;
                RaisePropertyChanged("WorkMusic");
            }
        }

        public string BreakMusic
        {
            get { return Properties.Settings.Default.BreakMusic; }
            set
            {
                Properties.Settings.Default.BreakMusic = value;
                RaisePropertyChanged("BreakMusic");
            }
        }


        public int WorkTime
        {
            get { return Properties.Settings.Default.PeriodWork; }
            set
            {
                Properties.Settings.Default.PeriodWork = value;
                RaisePropertyChanged("WorkTime");
            }
        }

        public int BreakTime
        {
            get { return Properties.Settings.Default.PeriodShortBreak; }
            set
            {
                Properties.Settings.Default.PeriodShortBreak = value;
                RaisePropertyChanged("BreakTime");
            }
        }

        public int BreakLongTime
        {
            get { return Properties.Settings.Default.PeriodLongBreak; }
            set
            {
                Properties.Settings.Default.PeriodLongBreak = value;
                RaisePropertyChanged("BreakLongTime");
            }
        }

        public double ProgressValue
        {
            get { return _progressValue; }
            set
            {
                _progressValue = value;
                RaisePropertyChanged("ProgressValue");
            }
        }

        public bool CountBackwards
        {
            get
            {
                return Properties.Settings.Default.CountBackwards;
            }
            set
            {
                Properties.Settings.Default.CountBackwards = value;
                RaisePropertyChanged("CountBackwards");
            }
        }

        public string ProgressState
        {
            get { return _progressState; }
            set
            {
                _progressState = value;
                RaisePropertyChanged("ProgressState");
            }
        }

        public string CurrentTimeValue
        {
            set
            {
                CurrentTime.Text = value;
                Title = String.Format("YAPA - {0}", value);
            }
        }

        void DoTick(object sender, EventArgs e)
        {
            if (_stopWatch.IsRunning)
            {
                var ts = _stopWatch.Elapsed;
                _ticks = (int)ts.TotalSeconds;

                if (_isWork)
                {
                    if (CountBackwards)
                    {
                        ts = TimeSpan.FromMinutes(WorkTime) - ts;
                    }
                    StartTicking(WorkTime, _ticks);
                }
                else if (_isBreak)
                {
                    if (CountBackwards)
                    {
                        ts = TimeSpan.FromMinutes(BreakTime) - ts;
                    }
                    StartTicking(BreakTime, _ticks);
                }
                else if (_isBreakLong)
                {
                    if (CountBackwards)
                    {
                        ts = TimeSpan.FromMinutes(BreakLongTime) - ts;
                    }
                    StartTicking(BreakLongTime, _ticks);
                }

                string currentTime = String.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);

                ShowText(String.Format("{0:00}", ts.Minutes));

                CurrentTimeValue = currentTime;
                CurrentPeriod.Text = _period.ToString();
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (SoundEffects)
                _tickSound.Play();
            PlayMusic();

            TimerFlush.Stop(this);
            if (_stopWatch.IsRunning)
            {
                _ticks = 0;
                _stopWatch.Restart();
            }
            else
            {
                _stopWatch.Start();
                _dispacherTime.Start();
                if (_isWork)
                    _period++;
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (SoundEffects)
            {
                _tickSound.Stop();
                _ringSound.Stop();
            }

            if (_stopWatch.IsRunning)
            {
                _period--;
                _stopWatch.Stop();
                ProgressState = "Paused";
                PauseMusic();
            }
            else
            {
                ResetTicking();
                _isWork = false;
                PlayMusic();
                _isWork = true;
            }
        }

        private void StartTicking(int TotalTime, int Increment)
        {
            int _totalTime = TotalTime * 60;
            ProgressState = "Normal";
            ProgressValue = (double)Increment / _totalTime;
            if (Increment >= _totalTime)
            {
                _ticks = 0;
                if (_isWork)
                {
                    _itemRepository.CompletePomodoro();
                }
                StopTicking();
                PauseMusic();
            }
        }

        private void StopTicking()
        {
            if (SoundEffects)
            {
                _ringSound.Play();
            }
            PauseMusic();
            TimerFlush.Begin(this, true);
            _stopWatch.Reset();
            _dispacherTime.Stop();
            ProgressState = "Error";
            if (_isWork)
            {
                CurrentTimeValue = _breakLabel;
                _isWork = false;
                if (_period == 4)
                {
                    _isBreak = false;
                    _isBreakLong = true;
                }
                else
                    _isBreak = true;
            }
            else
            {
                CurrentTimeValue = _workLabel;
                _isBreak = false;
                _isBreakLong = false;
                _isWork = true;
                if (_period == 4)
                {
                    CurrentPeriod.Text = "";
                    _period = 0;
                }
            }
        }

        private void ResetTicking()
        {
            TimerFlush.Stop(this);
            PauseMusic();
            _stopWatch.Reset();
            _dispacherTime.Stop();
            CurrentTimeValue = "00:00";
            _period = 0;
            _isBreak = false;
            _isBreakLong = false;
            _isWork = true;
            ProgressValue = .0;
            ProgressState = "None";
            _ticks = 0;

            var lastDay = _itemRepository.GetPomodoros().Last();
            if (lastDay.DateTime == DateTime.Now.Date)
            {
                _period = lastDay.Count % 4;
            }

            CurrentPeriod.Text = _period != 0 ? _period.ToString() : "";
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                base.OnMouseLeftButtonDown(e);
                DragMove();
                e.Handled = true;
            }
            catch
            {
            }
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

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ShowSettings.Execute(this);
        }

        public bool ProcessCommandLineArgs(params string[] args)
        {
            if (args == null || args.Length == 0)
                return true;

            if ((args.Length > 1))
            {
                //the first index always contains the location of the exe so we need to check the second index
                if ((args[1].ToLowerInvariant() == "/start"))
                {
                    if (!_stopWatch.IsRunning)
                    {
                        if (SoundEffects)
                            _tickSound.Play();
                        TimerFlush.Stop(this);
                        _stopWatch.Start();
                        _dispacherTime.Start();
                        if (_isWork)
                            _period++;
                    }
                }
                else if ((args[1].ToLowerInvariant() == "/pause"))
                {
                    if (SoundEffects)
                    {
                        _tickSound.Stop();
                        _ringSound.Stop();
                    }
                    if (_stopWatch.IsRunning)
                    {
                        _period--;
                        _stopWatch.Stop();
                        ProgressState = "Paused";
                    }
                }
                else if ((args[1].ToLowerInvariant() == "/restart"))
                {
                    if (_stopWatch.IsRunning)
                    {
                        if (SoundEffects)
                            _tickSound.Play();
                        _ticks = 0;
                        _stopWatch.Restart();
                    }
                }
                else if ((args[1].ToLowerInvariant() == "/reset"))
                {
                    ResetTicking();
                }
                else if ((args[1].ToLowerInvariant() == "/settings"))
                {
                    ShowSettings.Execute(this);
                }
                else if ((args[1].ToLowerInvariant() == "/homepage"))
                {
                    Process.Start("http://lukaszbanasiak.github.io/YAPA/");
                }
            }

            return true;
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            if (_stopWatch.IsRunning)
            {
                if (MessageBox.Show("Are you sure you want to exit and cancel pomodoro ?", "Cancel pomodoro", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    return;
                }
            }
            Close();
        }

        private void Minimize_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MainWindow_OnMouseEnter(object sender, MouseEventArgs e)
        {
            MinExitPanel.Visibility = Visibility.Visible;
        }

        private void MainWindow_OnMouseLeave(object sender, MouseEventArgs e)
        {
            MinExitPanel.Visibility = Visibility.Hidden;
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TimerFlush.Stop(this);
        }
    }
}
