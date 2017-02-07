namespace YAPA.Contracts
{
    public interface IMusicPlayer
    {
        void Load(string path);
        void Play(bool repeat = false, double volume = 0.5);
        void Stop();
        bool IsPlaying { get; }
    }
}
