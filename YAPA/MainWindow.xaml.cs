using Xceed.Wpf.Toolkit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Media;
using System.Windows.Shell;
using System.Reflection;
using System.IO;


namespace YAPA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainViewModel, INotifyPropertyChanged
    {
        DispatcherTimer dispacherTime = new DispatcherTimer();
        Stopwatch stopWatch = new Stopwatch();

        private ICommand _showSettings;
        private Storyboard TimerFlush;
        private Brush _textBrush;
        private double _clockOpacity;
        private int _workTime;
        private int _breakTime;
        private int _breakLongTime;
        private bool _soundEfects;
        private double _progressValue;
        private string _progressState;
        private int Ticks;
        private int Period;
        private bool IsBreak;
        private bool IsBreakLong;
        private bool IsWork;
        private string BreakLabel;
        private string WorkLabel;
        private SoundPlayer TickSound;
        private SoundPlayer RingSound;

        // For INCP
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;
            _showSettings = new ShowSettings(this);
            _textBrush = Brushes.Black;
            _clockOpacity = .6;
            _workTime = 25;
            _breakTime = 5;
            _breakLongTime = 15;
            _soundEfects = true;
            Ticks = 0;
            Period = 0;
            IsBreak = false;
            IsBreakLong = false;
            IsWork = true;                        
            BreakLabel = "break";
            WorkLabel = "work";

            dispacherTime.Tick += new EventHandler(DoTick);
            dispacherTime.Interval = new TimeSpan(0, 0, 0, 1);

            // position the clock at top / right, primary screen
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 15.0;

            // enable dragging
            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;

            // flash timer
            TimerFlush = (Storyboard)TryFindResource("FlashTimer");

            // play sounds
            TickSound = new System.Media.SoundPlayer(@"tick.wav");
            RingSound = new System.Media.SoundPlayer(@"ding.wav");

            Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CreateJumpList();

            //if you want to handle to command line args on the first instance you may want it to go here
            //or in the app.xaml.cs
            //ProcessCommandLineArgs(SingleInstance<App>.CommandLineArgs);
        }

        private void CreateJumpList()
        {
            JumpList jumpList = new JumpList();
            JumpList.SetJumpList(Application.Current, jumpList);

            JumpTask startTask = new JumpTask();
            startTask.Title = "Start";
            startTask.Description = "Start Pomodoro session";
            startTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            startTask.Arguments = "/start";
            startTask.IconResourceIndex = 7;
            jumpList.JumpItems.Add(startTask);

            JumpTask pauseTask = new JumpTask();
            pauseTask.Title = "Pause";
            pauseTask.Description = "Pause Pomodoro session";
            pauseTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            pauseTask.Arguments = "/pause";
            pauseTask.IconResourceIndex = 3;
            jumpList.JumpItems.Add(pauseTask);

            JumpTask stopTask = new JumpTask();
            stopTask.Title = "Restart";
            stopTask.Description = "Restart current Pomodori session";
            stopTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            stopTask.Arguments = "/restart";
            stopTask.IconResourceIndex = 4;
            jumpList.JumpItems.Add(stopTask);

            JumpTask resetTask = new JumpTask();
            resetTask.Title = "Start from the beginning";
            resetTask.Description = "Start new Pomodoro session";
            resetTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            resetTask.Arguments = "/reset";
            resetTask.IconResourceIndex = 2;
            jumpList.JumpItems.Add(resetTask);

            JumpTask settingsTask = new JumpTask();
            settingsTask.Title = "Settings";
            settingsTask.Description = "Show YAPA settings";
            settingsTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            settingsTask.Arguments = "/settings";
            settingsTask.IconResourceIndex = 5;
            jumpList.JumpItems.Add(settingsTask);

            JumpTask homepageTask = new JumpTask();
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
            get { return _textBrush; }
            set
            {
                _textBrush = value;
                RaisePropertyChanged("TextBrush");
            }
        }

        public bool SoundEfects
        {
            get { return _soundEfects; }
            set
            {
                _soundEfects = value;
                RaisePropertyChanged("UseSoundEfects");
            }
        }

        public double ClockOpacity
        {
            get { return _clockOpacity; }
            set
            {
                _clockOpacity = value;
                RaisePropertyChanged("ClockOpacity");
            }
        }

        public int WorkTime
        {
            get { return _workTime; }
            set
            {
                _workTime = value;
                RaisePropertyChanged("WorkTime");
            }
        }

        public int BreakTime
        {
            get { return _breakTime; }
            set
            {
                _breakTime = value;
                RaisePropertyChanged("BreakTime");
            }
        }

        public int BreakLongTime
        {
            get { return _breakLongTime; }
            set
            {
                _breakLongTime = value;
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

        public string ProgressState
        {
            get { return _progressState; }
            set
            {
                _progressState = value;
                RaisePropertyChanged("ProgressState");
            }
        }

        void DoTick(object sender, EventArgs e)
        {
            if (stopWatch.IsRunning)
            {
                TimeSpan ts = stopWatch.Elapsed;
                string currentTime = String.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
                CurrentTime.Text = currentTime;
                CurrentPeriod.Text = Period.ToString();
                Ticks++;
                if (IsWork)
                    StartTicking(_workTime, Ticks);
                else if (IsBreak)
                    StartTicking(_breakTime, Ticks);
                else if (IsBreakLong)
                    StartTicking(_breakLongTime, Ticks);
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (_soundEfects)
                TickSound.Play();
            TimerFlush.Stop(this);
            if (stopWatch.IsRunning)
            {
                Ticks = 0;
                stopWatch.Restart();
            }
            else
            {
                stopWatch.Start();
                dispacherTime.Start();
                if (IsWork)
                    Period ++;
            }            
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (_soundEfects)
            {
                TickSound.Stop();
                RingSound.Stop();
            }
            if (stopWatch.IsRunning)
            {
                Period--;
                stopWatch.Stop();
                ProgressState = "Paused";
            }
            else
                ResetTicking();
        }

        private void StartTicking(int TotalTime, int Increment)
        {
            int _totalTime = TotalTime * 60;
            ProgressState = "Normal";
            ProgressValue = (double)Increment / _totalTime;
            if (Increment >= _totalTime)
            {
                Ticks = 0;
                StopTicking();
            }
        }

        private void StopTicking()
        {
            if (_soundEfects)
                RingSound.Play();
            TimerFlush.Begin(this, true);            
            stopWatch.Reset();
            dispacherTime.Stop();
            ProgressState = "Error";
            if (IsWork)
            {
                CurrentTime.Text = BreakLabel;
                IsWork = false;
                if (Period == 4)
                {
                    IsBreak = false;
                    IsBreakLong = true;
                }
                else
                    IsBreak = true;
            }
            else
            {
                CurrentTime.Text = WorkLabel;
                IsBreak = false;
                IsBreakLong = false;
                IsWork = true;
                if (Period == 4)
                {
                    CurrentPeriod.Text = "";
                    Period = 0;
                }
            }
        }

        private void ResetTicking()
        {
            TimerFlush.Stop(this);            
            stopWatch.Reset();
            dispacherTime.Stop();
            CurrentTime.Text = "00:00";
            CurrentPeriod.Text = "";
            Period = 0;
            IsBreak = false;
            IsBreakLong = false;
            IsWork = true;
            ProgressValue = .0;
            ProgressState = "None";
            Ticks = 0;
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
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
            this.ShowSettings.Execute(this);
        }

        public bool ProcessCommandLineArgs(IList<string> args)
        {
            if (args == null || args.Count == 0)
                return true;

            if ((args.Count > 1))
            {
                //the first index always contains the location of the exe so we need to check the second index
                if ((args[1].ToLowerInvariant() == "/start"))
                {
                    if (!stopWatch.IsRunning)
                    {
                        if (_soundEfects)
                            TickSound.Play();
                        TimerFlush.Stop(this);
                        stopWatch.Start();
                        dispacherTime.Start();
                        if (IsWork)
                            Period++;
                    }  
                }
                else if ((args[1].ToLowerInvariant() == "/pause"))
                {
                    if (_soundEfects)
                    {
                        TickSound.Stop();
                        RingSound.Stop();
                    }
                    if (stopWatch.IsRunning)
                    {
                        Period--;
                        stopWatch.Stop();
                        ProgressState = "Paused";
                    }
                }
                else if ((args[1].ToLowerInvariant() == "/restart"))
                {
                    if (stopWatch.IsRunning)
                    {
                        if (_soundEfects)
                            TickSound.Play();
                        Ticks = 0;
                        stopWatch.Restart();
                    }
                }
                else if ((args[1].ToLowerInvariant() == "/reset"))
                {
                    ResetTicking();
                }
                else if ((args[1].ToLowerInvariant() == "/settings"))
                {
                    this.ShowSettings.Execute(this);
                }
                else if ((args[1].ToLowerInvariant() == "/homepage"))
                {
                    System.Diagnostics.Process.Start("http://lukaszbanasiak.github.io/YAPA/");
                }
            }

            return true;
        }
    }
}
