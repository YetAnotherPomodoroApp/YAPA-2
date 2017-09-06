using System;
using System.ComponentModel;
using YAPA.Shared.Common;

namespace YAPA.Shared.Contracts
{
    public interface IPomodoroEngine : INotifyPropertyChanged
    {
        int Index { get; }
        int Counter { get; }
        PomodoroPhase Phase { get; }
        int Elapsed { get; }
        int Remaining { get; }
        int DisplayValue { get; }
        int CurrentIntervalLength { get; }
        int WorkTime { get; }
        int BreakTime { get; }

        event Func<bool> OnStarting;
        event Action OnStarted;
        event Action OnStopped;
        event Action OnPaused;
        event Action OnPomodoroCompleted;

        void Start();
        void Stop();
        void Pause();
        void Reset();

        bool IsRunning { get; }

        PomodoroEngineSnapshot GetSnapshot();
        void LoadSnapshot(PomodoroEngineSnapshot snapshot);
    }

    public enum PomodoroPhase
    {
        NotStarted,
        Work,
        WorkEnded,
        Break,
        BreakEnded,
        Pause
    }
    
    public enum CounterEnum
    {
        PomodoroIndex,
        CompletedToday,
        CompletedThisSession
    }
}
