using System.Collections.Generic;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Tests
{
    /// <summary>Records what would have been played instead of playing it.</summary>
    public sealed class FakeSound : IMusicPlayer
    {
        public List<(string file, bool repeat, double volume)> Played { get; } = new List<(string, bool, double)>();
        private string _loaded;

        public bool IsPlaying => false;

        public void Load(string path) => _loaded = path;

        public void Play(bool repeat = false, double volume = 0.5) => Played.Add((_loaded, repeat, volume));

        public void Stop()
        {
        }
    }
}
