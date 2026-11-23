using System;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Tests
{
    /// <summary>Time that moves only when a test says so; the engine's one-second timer ticks with it.</summary>
    public sealed class FakeClock : IDate, ITimer
    {
        // 10:00 today: the repository's "completed today" compares with the real date
        public DateTime Now { get; private set; } = DateTime.Today.AddHours(10).ToUniversalTime();
        public bool Running { get; private set; }

        public DateTime DateTimeUtc() => Now;

        public event Action Tick;

        public void Start() => Running = true;

        public void Stop() => Running = false;

        public void Advance(TimeSpan time)
        {
            Now += time;
            if (Running)
            {
                Tick?.Invoke();
            }
            Yapa.Pump();
        }
    }
}
