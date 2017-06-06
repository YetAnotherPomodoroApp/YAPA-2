using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

    public static class Timeout
    {
        public static async Task WaitPhaseChange(PomodoroEngine engine, PomodoroPhase phase)
        {
            var timeout = TimeSpan.FromSeconds(5);
            var task = Task.Run(() =>
                {
                    while (true)
                    {
                        if (engine.Phase == phase)
                            break;

                        Thread.Sleep(50);
                    }
                });

            if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
            {
                Assert.Fail($"Timout: Phase didn't change to {phase}, actual phase:{engine.Phase}");
            }
        }
    }

}
