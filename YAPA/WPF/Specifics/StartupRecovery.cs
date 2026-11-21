using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using YAPA.Shared.Common;

namespace YAPA.WPF.Specifics
{
    /// <summary>
    /// When a freshly updated version crashes while it starts, the previous version is started in its place.
    /// The user keeps a working timer, and the previous version installs the fix as soon as one is published.
    ///
    /// A version that can't start can't update itself either, so without this it stays broken until it is reinstalled.
    ///
    /// Nothing is shown: the timer simply appears, as on any other start. What happened is in the log.
    ///
    /// Kept free of everything else in the application on purpose: it has to work when the rest does not.
    /// </summary>
    public static class StartupRecovery
    {
        private static bool _startupCompleted;
        private static bool _handled;

        /// <summary>
        /// Call once the main window is up. A crash after this is an ordinary crash, not a failed start.
        /// </summary>
        public static void MarkStartupCompleted()
        {
            _startupCompleted = true;
        }

        public static bool StartupCompleted => _startupCompleted;

        /// <summary>
        /// Starts the previous installed version. Returns false when there is none, when this copy was not
        /// installed by the installer, or when startup had already completed.
        /// </summary>
        public static bool TryStartPreviousVersion(Exception crash, Action<string> log)
        {
            if (_startupCompleted || _handled)
            {
                return false;
            }

            _handled = true;

            try
            {
                var exe = Assembly.GetEntryAssembly()?.Location;
                if (string.IsNullOrEmpty(exe))
                {
                    return false;
                }

                var versionFolder = Path.GetDirectoryName(exe);
                var installFolder = Path.GetDirectoryName(versionFolder);

                //The installer puts Update.exe above the version folders; without it this is a developer's copy
                if (string.IsNullOrEmpty(installFolder) || !File.Exists(Path.Combine(installFolder, "Update.exe")))
                {
                    return false;
                }

                var exeName = Path.GetFileName(exe);
                var current = Path.GetFileName(versionFolder);
                var candidates = Directory.GetDirectories(installFolder)
                    .Select(Path.GetFileName)
                    .Where(folder => File.Exists(Path.Combine(installFolder, folder, exeName)));

                var previous = InstalledVersions.Previous(candidates, current);
                if (previous == null)
                {
                    return false;
                }

                Log(log, $"{current} failed to start ({crash?.GetType().Name}: {crash?.Message}). Starting {previous} instead");

                var previousFolder = Path.Combine(installFolder, previous);
                Process.Start(new ProcessStartInfo(Path.Combine(previousFolder, exeName), ArgumentsOfThisProcess())
                {
                    WorkingDirectory = previousFolder,
                    UseShellExecute = false
                });

                return true;
            }
            catch (Exception e)
            {
                Log(log, $"Could not start the previous version: {e.Message}");
                return false;
            }
        }

        private static string ArgumentsOfThisProcess()
        {
            return string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg));
        }

        private static void Log(Action<string> log, string message)
        {
            try
            {
                log?.Invoke(message);
            }
            catch (Exception)
            {
                //Logging may be part of what is broken
            }
        }
    }
}
