using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using YAPA.Shared.Contracts;

namespace YAPA
{
    public partial class MainWindow2
    {
        public MainWindow2(IMainViewModel viewModel) : base(viewModel)
        {
            DataContext = ViewModel;

            // enable dragging
            MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;

            InitializeComponent();

            Loaded += MainWindow2_Loaded;
            viewModel.Engine.OnStarted += Engine_OnStarted;

        }

        private void Engine_OnStarted()
        {
            startDate = DateTime.Now;
        }

        private void MainWindow2_Loaded()
        {
            this.dispatchTimer = new DispatcherTimer();
            this.dispatchTimer.Interval = new TimeSpan(0, 0, 1);
            this.dispatchTimer.Tick += dispatchTimer_Tick;
            this.dispatchTimer.Start();
        }

        const float PI = 3.141592654F;
        DateTime startDate;
        DateTime clockTime;
        DispatcherTimer dispatchTimer;

        private void DrawClock()
        {
            clockTime = DateTime.Now;
            var canvas = Math.Min(InkCanvas.ActualHeight, InkCanvas.ActualWidth);

            float clockHeight = (int)canvas;
            float clockRadius = (int)canvas / 2;

            float centerX = (float)canvas / 2;
            float centerY = (float)canvas / 2;

            float secThinkness = 1;
            float minThinkness = 5;
            float hourThinkness = 10;

            float hourLength = clockHeight / 3 / 1.65F;
            float minLength = clockHeight / 2.8F;
            float secLength = clockHeight / 3 / 1.15F;

            float hourThickness = clockHeight / 100;
            float minThickness = clockHeight / 150;
            float secThickness = clockHeight / 200;

            float center = clockHeight / 50;

            InkCanvas.Children.Clear();

            int minute = clockTime.Minute;
            int sec = clockTime.Second;

            float hour = clockTime.Hour % 12 + (float)clockTime.Minute / 60;

            float hourRadian = hour * 360 / 12 * PI / 180;
            float minRadian = minute * 360 / 60 * PI / 180;
            float secRadian = sec * 360 / 60 * PI / 180;

            float hourEndPointX = hourLength * (float)System.Math.Sin(hourRadian);
            float hourEndPointY = hourLength * (float)System.Math.Cos(hourRadian);


            if (ViewModel.Engine.IsRunning)
            {
                var currMin = startDate.Minute;
                var duration = ViewModel.Engine.WorkTime / 60;
                var periodColor = Colors.Green;

                if (ViewModel.Engine.Phase == PomodoroPhase.Break)
                {
                    periodColor = Colors.Red;
                    duration = ViewModel.Engine.BreakTime / 60;
                }

                for (int i = currMin; i < currMin + duration; i++)
                {
                    DrawLine(
                        centerX + (float)(clockRadius / 1.50F * System.Math.Sin(i * 6 * PI / 180)),
                        centerY - (float)(clockRadius / 1.50F * System.Math.Cos(i * 6 * PI / 180)),
                        centerX + (float)(clockRadius / 1.65F * System.Math.Sin(i * 6 * PI / 180)),
                        centerY - (float)(clockRadius / 1.65F * System.Math.Cos(i * 6 * PI / 180)), periodColor, 20);
                }
            }

            //Hour
            DrawLine(centerX, centerY, centerX + hourEndPointX, centerY - hourEndPointY, Colors.Black, hourThinkness);

            //minute
            for (int i = 0; i < 60; i++)
            {
                if (i % 5 == 0)
                {
                    DrawLine(
                    centerX + (float)(clockRadius / 1.50F * System.Math.Sin(i * 6 * PI / 180)),
                    centerY - (float)(clockRadius / 1.50F * System.Math.Cos(i * 6 * PI / 180)),
                    centerX + (float)(clockRadius / 1.65F * System.Math.Sin(i * 6 * PI / 180)),
                    centerY - (float)(clockRadius / 1.65F * System.Math.Cos(i * 6 * PI / 180)), Colors.Black, hourThinkness);

                }
                else
                {
                    DrawLine(
                    centerX + (float)(clockRadius / 1.50F * System.Math.Sin(i * 6 * PI / 180)),
                    centerY - (float)(clockRadius / 1.50F * System.Math.Cos(i * 6 * PI / 180)),
                    centerX + (float)(clockRadius / 1.55F * System.Math.Sin(i * 6 * PI / 180)),
                    centerY - (float)(clockRadius / 1.55F * System.Math.Cos(i * 6 * PI / 180)), Colors.Red, hourThinkness);

                }
            }

            float minEndPointX = minLength * (float)System.Math.Sin(minRadian);
            float minEndPointY = minLength * (float)System.Math.Cos(minRadian);

            DrawLine(centerX, centerY, centerX + minEndPointX, centerY - minEndPointY, Colors.Blue, minThinkness);

            //Second
            float secEndPointX = secLength * (float)System.Math.Sin(secRadian);
            float secEndPointY = secLength * (float)System.Math.Cos(secRadian);

            DrawLine(centerX, centerY, centerX + secEndPointX, centerY - secEndPointY, Colors.Green, secThinkness);

        }

        void dispatchTimer_Tick(object sender, object e)
        {
            try
            {
                DrawClock();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void DrawLine(double x1, double y1, double x2, double y2, Color color, float thinkness)
        {

            Line line = new Line()
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                StrokeThickness = thinkness,
                Stroke = new SolidColorBrush(color)
            };

            InkCanvas.Children.Add(line);
        }


        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                OnMouseLeftButtonDown(e);
                DragMove();
                e.Handled = true;
            }
            catch
            {
                // ignored
            }
        }
    }
}
