using System;

namespace YAPA.Contracts
{
    public enum ApplicationState
    {
        Normal,
        Minimized,
        Maximized,
    }

    public interface IApplication
    {
        bool ShowInTaskbar { get; set; }
        void Show();
        void Hide();

        event Action<ApplicationState> StateChanged;
        event Action Closing;
        event Action Loaded;

        IntPtr WindowHandle { get; }
        double Left { get; set; }
        double Top { get; set; }
        double Width { get; set; }
        double Height { get; set; }
        ApplicationState AppState { get; set; }
    }
}
