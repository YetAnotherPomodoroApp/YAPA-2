using System;
using System.IO;
using System.Windows.Media;
using YAPA.Shared.Contracts;
using LogLevel = NLog.LogLevel;

namespace YAPA.WPF.Specifics
{
    public class SoundPlayer : IMusicPlayer
    {
        private readonly NLog.ILogger _logger;
        private readonly MediaPlayer _musicPlayer;
        private bool _repeat;

        public SoundPlayer(NLog.ILogger logger)
        {
            _logger = logger;
            _musicPlayer = new MediaPlayer();
            _musicPlayer.MediaEnded += MusicPlayer_MediaEnded;
            _repeat = false;
        }

        private void MusicPlayer_MediaEnded(object sender, System.EventArgs e)
        {
            if (_musicPlayer.Source == null)
            {
                _logger.Log(LogLevel.Info, $"Audio file not specified");
                return;
            }

            if (!File.Exists(_musicPlayer.Source.LocalPath))
            {
                _logger.Log(LogLevel.Info, $"Audio file not found '{_musicPlayer.Source.LocalPath}'");
                return;
            }

            if (!_repeat) return;
            _musicPlayer.Position = TimeSpan.Zero;
            _musicPlayer.Play();
        }

        public void Load(string path)
        {
            if (IsPlaying)
            {
                _logger.Log(LogLevel.Info, $"Can't load new song '{path}' while old is playing '{_musicPlayer.Source.LocalPath}'");
                return;
            }

            if (!File.Exists(path))
            {
                _logger.Log(LogLevel.Info, $"Can't load new song '{path}' while old is playing '{_musicPlayer.Source.LocalPath}'");
                return;
            }

            _musicPlayer.Open(new Uri(path));
        }

        public void Play(bool repeat = false, double volume = 0.5)
        {
            if (_musicPlayer.Source == null)
            {
                _logger.Log(LogLevel.Info, "Song not specified");
                return;
            }

            if (!File.Exists(_musicPlayer.Source.LocalPath))
            {
                _logger.Log(LogLevel.Info, $"Audio file not found '{_musicPlayer.Source.LocalPath}'");
                return;
            }

            if (IsPlaying)
            {
                _logger.Log(LogLevel.Info, $"Song already playing '{_musicPlayer.Source.LocalPath}'");
                return;
            }

            _repeat = repeat;

            _musicPlayer.Volume = volume;
            _musicPlayer.Play();
            IsPlaying = true;
        }

        public void Stop()
        {
            _musicPlayer.Stop();
            IsPlaying = false;
        }

        public bool IsPlaying { get; private set; }
    }
}
