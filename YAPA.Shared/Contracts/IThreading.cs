using System;

namespace YAPA.Shared.Contracts
{
    public interface IThreading
    {
        void RunOnUiThread(Action action);
    }
}
