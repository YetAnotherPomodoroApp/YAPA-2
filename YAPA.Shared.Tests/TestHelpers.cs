using System;
using YAPA.Contracts;
using YAPA.Shared.Contracts;

namespace YAPA.Shared.Tests
{
    public class TimerMock : ITimer
    {
        public event Action Tick;

        public void PerformTick()
        {
            Tick?.Invoke();
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }

    public class DateMock : IDate
    {
        public DateTime DateToReturn;

        public DateTime DateTimeUtc()
        {
            return DateToReturn;
        }
    }
}
