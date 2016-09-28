namespace YAPA.Contracts
{
    public interface IMusicPlayer
    {
        void Load(string path);
        void Play(bool repeat = false);
        void Stop();
    }
}
