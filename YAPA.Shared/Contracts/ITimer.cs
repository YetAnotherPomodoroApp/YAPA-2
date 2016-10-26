using System;

namespace YAPA.Contracts
{
    public interface ITimer
    {
        event Action Tick;
        void Start();
        void Stop();
    }
}
