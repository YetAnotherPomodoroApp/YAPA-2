using System;
using System.Reflection;
using YAPA.Shared.Contracts;
using YAPA.WPF;

namespace YAPA.Plugins.ReleaseNotes
{
    [BuiltInPlugin(Hide = true)]
    public class ReleaseNotesPlugin : IPluginMeta
    {
        public string Title => "Release notes";

        public string Id => "ReleaseNotes";

        public Type Plugin => typeof(ReleaseNotes);

        public Type Settings => typeof(ReleaseNotesSettings);

        public Type SettingEditWindow => null;
    }

    public class ReleaseNotes : IPlugin
    {
        public ReleaseNotes(IEnvironment environment, ReleaseNotesSettings settings)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var currentVersion = assembly.GetName().Version.ToString(3);

            var showDialog = true || !string.IsNullOrEmpty(settings.Version)
                && currentVersion != settings.Version
                && settings.ShowNotification;

            settings.Version = currentVersion;

            if (showDialog)
            {
                var window = new ReleaseNotesWindow(settings, environment.PreRelease());

                window.ShowDialog();
            }
        }
    }

    public class ReleaseNotesSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public ReleaseNotesSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(new ReleaseNotesPlugin().Id);
        }

        public string Version
        {
            get => _settings.Get<string>(nameof(Version), null);
            set => _settings.Update(nameof(Version), value);
        }

        public bool ShowNotification
        {
            get => _settings.Get<bool>(nameof(ShowNotification), true);
            set => _settings.Update(nameof(ShowNotification), value);
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}
