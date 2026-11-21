using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using YAPA.Shared.Common;

namespace YAPA.WPF.Specifics
{
    /// <summary>
    /// When a freshly updated version crashes while it starts, the previous version is started in its place.
    /// The user keeps a working timer, and the previous version installs the fix as soon as one is published.
    ///
    /// A version that can't start can't update itself either, so without this it stays broken until it is reinstalled.
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

                TellUserOnce(installFolder, current, previous);
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TellUserOnce(string installFolder, string current, string previous)
        {
            try
            {
                //One message per failed version, not one every time the application is started
                var marker = Path.Combine(installFolder, $"started-previous-instead-of-{current}.txt");
                if (File.Exists(marker))
                {
                    return;
                }

                File.WriteAllText(marker, $"{DateTime.Now:u} {current} failed to start, {previous} was started instead.");

                MessageBox.Show(
                    $"The latest update of YAPA 2 ({Display(current)}) could not start, so the previous version ({Display(previous)}) was started instead.{Environment.NewLine}{Environment.NewLine}" +
                    "You don't need to do anything: a fixed version will be installed automatically when it is available.",
                    "YAPA 2", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception)
            {
                //The previous version is running; the message is a courtesy
            }
        }

        private static string Display(string versionFolder)
        {
            return InstalledVersions.TryParse(versionFolder, out var version) ? version.ToString() : versionFolder;
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
