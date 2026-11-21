using System;
using System.IO;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Plugins.SoundSettings.MusicPlayer
{
    public partial class MusicPlayerPluginSettingWindow
    {
        private readonly IMusicPlayer _musicPlayer;
        private readonly PomodoroEngineSettings _pomodoroEngineSettings;

        public MusicPlayerPluginSettingWindow(MusicPlayerPluginSettings settings, IMusicPlayer musicPlayer, PomodoroEngineSettings pomodoroEngineSettings)
        {
            settings.DeferChanges();

            InitializeComponent();

            DataContext = settings;

            _musicPlayer = musicPlayer;
            _pomodoroEngineSettings = pomodoroEngineSettings;

            BrowseBreakSong.Command = new BrowseSong(BreakSong);
            BrowseWorkSong.Command = new BrowseSong(WorkSong);
            BrowseSessionBreakSong.Command = new BrowseSong(SessionBreakSong);

            PlaySessionBreakSong.Command = new PlaySong(SessionBreakSong, PlaySessionBreakSong, "session break song", _musicPlayer, _pomodoroEngineSettings);
            PlayBreakSong.Command = new PlaySong(BreakSong, PlayBreakSong, "break song", _musicPlayer, _pomodoroEngineSettings);
            PlayWorkSong.Command = new PlaySong(WorkSong, PlayWorkSong, "work song", _musicPlayer, _pomodoroEngineSettings);
        }

        public class PlaySong : ICommand
        {
            private readonly TextBox _song;
            private readonly Button _playButton;
            private readonly IMusicPlayer _musicPlayer;
            private readonly PomodoroEngineSettings _pomodoroEngineSettings;
            private bool _isPlaying = false;

            private readonly string _songName;

            public PlaySong(TextBox song, Button playButton, string songName, IMusicPlayer musicPlayer, PomodoroEngineSettings pomodoroEngineSettings)
            {
                _song = song;
                _playButton = playButton;
                _songName = songName;
                _musicPlayer = musicPlayer;
                _pomodoroEngineSettings = pomodoroEngineSettings;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                if (!_isPlaying)
                {
                    if (File.Exists(_song.Text))
                    {
                        _musicPlayer.Load(_song.Text);
                        _musicPlayer.Play(false, _pomodoroEngineSettings.Volume);
                        _playButton.Content = "Stop";
                        AutomationProperties.SetName(_playButton, $"Stop {_songName}");
                        _isPlaying = true;
                    }
                }
                else
                {
                    _musicPlayer.Stop();
                    _playButton.Content = "Play";
                    AutomationProperties.SetName(_playButton, $"Play {_songName}");
                    _isPlaying = false;
                }
            }

            public event EventHandler CanExecuteChanged;
        }

        public class BrowseSong : ICommand
        {
            private readonly TextBox _output;

            public BrowseSong(TextBox output)
            {
                _output = output;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    DefaultExt = ".wav",
                    Filter = "MP3 (*.mp3)|*.mp3|WAVE (*.wav)|*.wav|All Files(*.*)|*.*",
                    InitialDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\sounds")
                };

                var result = dlg.ShowDialog();


                if (result.HasValue && result == true)
                {
                    _output.Text = dlg.FileName;
                }

            }

            public event EventHandler CanExecuteChanged;
        }
    }
}
