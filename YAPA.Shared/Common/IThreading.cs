using System;

namespace YAPA.Shared.Common
{
    public interface IThreading
    {
        void RunOnUiThread(Action action);
    }
}
