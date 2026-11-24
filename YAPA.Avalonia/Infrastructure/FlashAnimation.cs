using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace YAPA.Avalonia.Infrastructure
{
    /// <summary>
    /// The WPF themes' flash storyboard: a ColorAnimation of a panel background to a colour over
    /// 0.2 s, auto-reversed, repeated forever, at 15 frames a second. Stopping it puts the
    /// original background back, as Storyboard.Stop does.
    /// </summary>
    public class FlashAnimation
    {
        private static readonly TimeSpan HalfPeriod = TimeSpan.FromSeconds(0.2);

        private readonly Panel _target;
        private readonly Color _to;
        private readonly DispatcherTimer _timer;
        private DateTime _started;
        private IBrush _original;
        private Color _from;

        public bool IsRunning => _timer.IsEnabled;

        public FlashAnimation(Panel target, Color to)
        {
            _target = target;
            _to = to;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000.0 / 15) };
            _timer.Tick += (s, e) => Render();
        }

        public void Begin()
        {
            if (IsRunning)
            {
                Stop();
            }
            _original = _target.Background;
            // WPF's "Transparent" is white with no alpha, so the animation fades from clear white
            _from = (_original as ISolidColorBrush)?.Color ?? Color.FromArgb(0, 255, 255, 255);
            _started = DateTime.UtcNow;
            _timer.Start();
            Render();
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }
            _timer.Stop();
            _target.Background = _original;
        }

        private void Render()
        {
            var elapsed = (DateTime.UtcNow - _started).TotalSeconds;
            var period = HalfPeriod.TotalSeconds * 2;
            var phase = elapsed % period / HalfPeriod.TotalSeconds; // 0..2
            var t = phase <= 1 ? phase : 2 - phase;
            _target.Background = new SolidColorBrush(Lerp(_from, _to, t));
        }

        private static Color Lerp(Color a, Color b, double t)
        {
            byte L(byte x, byte y) => (byte)Math.Round(x + (y - x) * t);
            return Color.FromArgb(L(a.A, b.A), L(a.R, b.R), L(a.G, b.G), L(a.B, b.B));
        }
    }
}
