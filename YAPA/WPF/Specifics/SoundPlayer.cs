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
        private readonly WavPlayer _wavPlayer;
        private bool _repeat;
        private double _volume;

        //The file the last Load asked for, null when there is none or it could not be found
        private string _path;

        //Set once the media player reports that Windows Media Player is missing; .wav files then skip it
        private bool _windowsMediaPlayerMissing;

        public SoundPlayer(NLog.ILogger logger)
        {
            _logger = logger;
            _musicPlayer = new MediaPlayer();
            _musicPlayer.MediaEnded += MusicPlayer_MediaEnded;
            _musicPlayer.MediaFailed += MusicPlayer_MediaFailed;
            _wavPlayer = new WavPlayer();
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

        private void MusicPlayer_MediaFailed(object sender, ExceptionEventArgs e)
        {
            MediaPlayerFailed(e.ErrorException);
        }

        private void MediaPlayerFailed(Exception error)
        {
            _logger.Log(LogLevel.Warn, error, $"The media player could not play '{_path}'");

            if (error is InvalidWmpVersionException)
            {
                _windowsMediaPlayerMissing = true;
            }

            //Nothing is waiting to be heard, for example the sound was stopped before the failure arrived
            if (!IsPlaying)
            {
                return;
            }

            if (_path != null && WavPlayer.CanPlay(_path))
            {
                PlayWithWavPlayer();
            }
            else
            {
                IsPlaying = false;
            }
        }

        private void PlayWithWavPlayer()
        {
            try
            {
                _wavPlayer.Play(_path, _repeat, _volume);
                _logger.Log(LogLevel.Info, $"Playing '{_path}' without the media player");
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Warn, e, $"Could not play '{_path}'");
                IsPlaying = false;
            }
        }

        public void Load(string path)
        {
            if (IsPlaying)
            {
                _logger.Log(LogLevel.Info, $"Can't load new song '{path}' while old is playing '{_path}'");
                return;
            }

            if (!File.Exists(path))
            {
                _logger.Log(LogLevel.Warn, $"Audio file not found '{path}'");
                _path = null;
                return;
            }

            _path = path;

            if (_windowsMediaPlayerMissing && WavPlayer.CanPlay(path))
            {
                return;
            }

            _musicPlayer.Open(new Uri(path));
        }

        public void Play(bool repeat = false, double volume = 0.5)
        {
            if (_path == null)
            {
                _logger.Log(LogLevel.Info, "Song not specified");
                return;
            }

            if (!File.Exists(_path))
            {
                _logger.Log(LogLevel.Warn, $"Audio file not found '{_path}'");
                return;
            }

            if (IsPlaying)
            {
                _logger.Log(LogLevel.Info, $"Song already playing '{_path}'");
                return;
            }

            _repeat = repeat;
            _volume = volume;
            IsPlaying = true;

            if (_windowsMediaPlayerMissing && WavPlayer.CanPlay(_path))
            {
                PlayWithWavPlayer();
                return;
            }

            _musicPlayer.Volume = volume;
            _musicPlayer.Play();
        }

        public void Stop()
        {
            _musicPlayer.Stop();
            _wavPlayer.Stop();
            IsPlaying = false;
        }

        public bool IsPlaying { get; private set; }
    }
}
