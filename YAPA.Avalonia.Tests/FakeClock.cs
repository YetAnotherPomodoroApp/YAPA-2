using System;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Tests
{
    /// <summary>Time that moves only when a test says so; the engine's one-second timer ticks with it.</summary>
    public sealed class FakeClock : IDate, ITimer
    {
        public DateTime Now { get; private set; } = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
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
