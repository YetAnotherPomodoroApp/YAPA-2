using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using YAPA.Avalonia.Specifics;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Plugins.SoundSettings.MusicPlayer
{
    public partial class MusicPlayerPluginSettingWindow : UserControl
    {
        private readonly IMusicPlayer _musicPlayer;
        private readonly PomodoroEngineSettings _pomodoroEngineSettings;

        public MusicPlayerPluginSettingWindow()
        {
            InitializeComponent();
        }

        public MusicPlayerPluginSettingWindow(MusicPlayerPluginSettings settings, IMusicPlayer musicPlayer, PomodoroEngineSettings pomodoroEngineSettings)
        {
            settings.DeferChanges();
            InitializeComponent();
            DataContext = settings;
            _musicPlayer = musicPlayer;
            _pomodoroEngineSettings = pomodoroEngineSettings;

            BrowseWorkSong.Click += (s, e) => Browse(WorkSong);
            BrowseBreakSong.Click += (s, e) => Browse(BreakSong);
            BrowseSessionBreakSong.Click += (s, e) => Browse(SessionBreakSong);

            WirePlay(PlayWorkSong, WorkSong, "work song");
            WirePlay(PlayBreakSong, BreakSong, "break song");
            WirePlay(PlaySessionBreakSong, SessionBreakSong, "session break song");
        }

        private void WirePlay(Button playButton, TextBox song, string songName)
        {
            var isPlaying = false;
            playButton.Click += (s, e) =>
            {
                if (!isPlaying)
                {
                    var path = SoundPlayer.ResolvePath(song.Text);
                    if (path != null)
                    {
                        _musicPlayer.Load(path);
                        _musicPlayer.Play(false, _pomodoroEngineSettings.Volume);
                        playButton.Content = "Stop";
                        global::Avalonia.Automation.AutomationProperties.SetName(playButton, $"Stop {songName}");
                        isPlaying = true;
                    }
                }
                else
                {
                    _musicPlayer.Stop();
                    playButton.Content = "Play";
                    global::Avalonia.Automation.AutomationProperties.SetName(playButton, $"Play {songName}");
                    isPlaying = false;
                }
            };
        }

        private async void Browse(TextBox output)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
            {
                return;
            }

            var soundsFolder = Path.Combine(AppContext.BaseDirectory, "Resources", "sounds");
            var options = new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("MP3 (*.mp3)") { Patterns = new[] { "*.mp3" } },
                    new FilePickerFileType("WAVE (*.wav)") { Patterns = new[] { "*.wav" } },
                    new FilePickerFileType("All Files(*.*)") { Patterns = new[] { "*" } },
                },
            };
            if (Directory.Exists(soundsFolder))
            {
                options.SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(soundsFolder);
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            var path = files.FirstOrDefault()?.TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
            {
                output.Text = path;
            }
        }
    }
}
