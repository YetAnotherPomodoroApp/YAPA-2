using System;
using System.IO;
using System.Windows.Media;
using YAPA.Contracts;

namespace YAPA.WPF
{
    public class MusicPlayer : IMusicPlayer
    {
        private readonly MediaPlayer _musicPlayer;
        private bool _repeat;

        public MusicPlayer()
        {
            _musicPlayer = new MediaPlayer();
            _musicPlayer.MediaEnded += _musicPlayer_MediaEnded;
            _repeat = false;
        }

        private void _musicPlayer_MediaEnded(object sender, System.EventArgs e)
        {
            if (_repeat && _musicPlayer.Source != null && File.Exists(_musicPlayer.Source.AbsolutePath))
            {
                _musicPlayer.Play();
            }
        }

        public void Load(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            _musicPlayer.Open(new Uri(path));

        }

        public void Play(bool repeat = false)
        {
            if (_musicPlayer.Source == null || File.Exists(_musicPlayer.Source.AbsolutePath))
            {
                return;
            }

            _repeat = repeat;

            _musicPlayer.Play();
        }

        public void Stop()
        {
            _musicPlayer.Stop();
        }
    }
}
