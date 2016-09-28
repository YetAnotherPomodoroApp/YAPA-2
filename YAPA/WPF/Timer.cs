using System;
using System.Windows.Threading;
using YAPA.Contracts;

namespace YAPA.WPF
{
    public class Timer : ITimer
    {
        private readonly DispatcherTimer _timer;

        public Timer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timer.Tick += _timer_Tick;
        }


        private void _timer_Tick(object sender, EventArgs e)
        {
            Tick?.Invoke();
        }

        public event Action Tick;

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}
