using System;

namespace YAPA.Shared.Contracts
{
    public interface ITimer
    {
        event Action Tick;
        void Start();
        void Stop();
    }
}
