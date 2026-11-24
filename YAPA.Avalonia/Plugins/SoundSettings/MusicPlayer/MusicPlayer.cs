using System;
using System.IO;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Plugins.SoundSettings.MusicPlayer
{
    [BuiltInPlugin(Hide = true)]
    public class MusicPlayerPlugin : IPluginMeta
    {
        public string Title => "Music player";

        public string Id => "MusicPlayer";

        public Type Plugin => typeof(MusicPlayer);

        public Type Settings => typeof(MusicPlayerPluginSettings);

        public Type SettingEditWindow => typeof(MusicPlayerPluginSettingWindow);
    }

    public class MusicPlayer : IPlugin
    {
        private readonly IPomodoroEngine _engine;
        private readonly MusicPlayerPluginSettings _settings;
        private readonly IMusicPlayer _musicPlayer;
        private readonly PomodoroEngineSettings _engineSettings;

        public MusicPlayer(IPomodoroEngine engine, MusicPlayerPluginSettings settings, IMusicPlayer musicPlayer, PomodoroEngineSettings engineSettings)
        {
            _engine = engine;
            _settings = settings;
            _musicPlayer = musicPlayer;
            _engineSettings = engineSettings;

            _engine.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_engine.Phase))
                {
                    _musicPlayer.Stop();
                    Play();
                }
            };
        }

        private void Play()
        {
            if (_engineSettings.DisableSoundNotifications)
            {
                return;
            }

            var songToPlay = string.Empty;
            var repeat = false;

            switch (_engine.Phase)
            {
                case PomodoroPhase.Work:
                    songToPlay = _settings.WorkSong;
                    repeat = _settings.RepeatWorkSong;
                    break;
                case PomodoroPhase.Break:
                    if (_engine.Index == _engineSettings.PomodorosBeforeLongBreak)
                    {
                        songToPlay = _settings.SessionBreakSong;
                        repeat = _settings.RepeatSessionBreakSong;
                    }
                    else
                    {
                        songToPlay = _settings.BreakSong;
                        repeat = _settings.RepeatBreakSong;
                    }
                    break;
            }

            if (File.Exists(songToPlay))
            {
                _musicPlayer.Load(songToPlay);
                _musicPlayer.Play(repeat, _engineSettings.Volume);
            }
        }
    }

    public class MusicPlayerPluginSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public string WorkSong
        {
            get => _settings.Get<string>(nameof(WorkSong), null, true);
            set => _settings.Update(nameof(WorkSong), value, true);
        }

        public bool RepeatWorkSong
        {
            get => _settings.Get(nameof(RepeatWorkSong), false, true);
            set => _settings.Update(nameof(RepeatWorkSong), value, true);
        }

        public string BreakSong
        {
            get => _settings.Get<string>(nameof(BreakSong), null, true);
            set => _settings.Update(nameof(BreakSong), value, true);
        }

        public bool RepeatBreakSong
        {
            get => _settings.Get(nameof(RepeatBreakSong), false, true);
            set => _settings.Update(nameof(RepeatBreakSong), value, true);
        }

        public string SessionBreakSong
        {
            get => _settings.Get<string>(nameof(SessionBreakSong), null, true);
            set => _settings.Update(nameof(SessionBreakSong), value, true);
        }

        public bool RepeatSessionBreakSong
        {
            get => _settings.Get(nameof(RepeatSessionBreakSong), false, true);
            set => _settings.Update(nameof(RepeatSessionBreakSong), value, true);
        }

        public MusicPlayerPluginSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(new MusicPlayerPlugin().Id);
        }

        public void DeferChanges() => _settings.DeferChanges();
    }
}
