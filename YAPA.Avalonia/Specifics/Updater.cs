using System;
using System.IO;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Specifics
{
    /// <summary>
    /// Updates with Velopack, the successor of the Squirrel updater the WPF version uses, the same
    /// way: look for a new version at start, download it in the background, apply it when the app
    /// closes, and meanwhile show "Restart application to apply updates" in the settings window.
    ///
    /// Releases come from the project's GitHub releases (pre-releases too when the PreRelease.txt
    /// file is in the data folder, as in WPF). YAPA_UPDATE_URL points at another feed: a web address
    /// or a local folder made by vpk, which is how the update tests run.
    /// </summary>
    public class Updater
    {
        public const string Repository = "https://github.com/YetAnotherPomodoroApp/YAPA-2";

        private readonly ISettingManager _settingManager;
        private readonly IEnvironment _environment;

        private UpdateManager _manager;
        private UpdateInfo _pending;

        /// <summary>Call as YAPA exits: hands a downloaded update to Velopack to install.</summary>
        public void ApplyOnExit()
        {
            if (_manager == null || _pending == null)
            {
                return;
            }
            try
            {
                _manager.WaitExitThenApplyUpdates(_pending.TargetFullRelease, silent: true, restart: false);
                Log($"closing, installing {_pending.TargetFullRelease.Version}");
            }
            catch (Exception e)
            {
                Log($"could not start installing the update: {e.Message}");
            }
        }

        public static string LogFile => Path.Combine(AvaloniaEnvironment.BaseDir, "update.log");

        public Updater(ISettingManager settingManager, IEnvironment environment)
        {
            _settingManager = settingManager;
            _environment = environment;
        }

        /// <summary>Release builds always look; debug builds only when a feed is given.</summary>
        public static bool ShouldCheck
        {
            get
            {
#if DEBUG
                return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("YAPA_UPDATE_URL"));
#else
                return true;
#endif
            }
        }

        private IUpdateSource Source()
        {
            var custom = Environment.GetEnvironmentVariable("YAPA_UPDATE_URL");
            if (!string.IsNullOrEmpty(custom))
            {
                var isWeb = Uri.TryCreate(custom, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
                return isWeb ? new SimpleWebSource(custom) : new SimpleFileSource(new DirectoryInfo(custom));
            }
            return new GithubSource(Repository, null, _environment.PreRelease());
        }

        public async Task CheckAsync()
        {
            try
            {
                var manager = new UpdateManager(Source());
                if (!manager.IsInstalled)
                {
                    Log("not installed by Velopack (a development build), not checking");
                    return;
                }

                Log($"running {manager.CurrentVersion}, checking for updates");
                var update = await manager.CheckForUpdatesAsync();
                if (update == null)
                {
                    Log("up to date");
                    return;
                }

                var version = update.TargetFullRelease.Version.ToString();
                Log($"found {version}, downloading");
                await manager.DownloadUpdatesAsync(update);

                // Installed when YAPA closes (ApplyOnExit). Velopack's helper only waits a minute for
                // the app to exit, so it is started at exit, not now; and should that be missed,
                // Velopack applies a downloaded update at the next start.
                _manager = manager;
                _pending = update;
                Log($"downloaded {version}, it is installed when YAPA closes");

                _settingManager.NewVersion = version;
                _settingManager.RestartNeeded = true;
            }
            catch (Exception e)
            {
                // no network, no releases yet, a feed without this platform: try again next start
                Log($"update failed: {e.GetType().Name}: {e.Message}");
            }
        }

        public static void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(AvaloniaEnvironment.BaseDir);
                // keep the last part only
                if (File.Exists(LogFile) && new FileInfo(LogFile).Length > 100_000)
                {
                    File.Delete(LogFile);
                }
                File.AppendAllText(LogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
            }
            catch
            {
                // logging must never stop the app
            }
        }
    }
}
