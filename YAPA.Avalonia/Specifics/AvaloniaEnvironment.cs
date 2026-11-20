using System;
using System.IO;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Specifics
{
    /// <summary>
    /// Same files as the WPF app: Documents/YAPA2/settings.json and localSettings.json,
    /// so the two versions read and write one format. YAPA_HOME, when set, points somewhere else
    /// (a portable install, or a test run that must not touch the real data).
    /// </summary>
    public class AvaloniaEnvironment : IEnvironment
    {
        public static string BaseDir { get; } =
            Environment.GetEnvironmentVariable("YAPA_HOME") is string custom && custom.Length > 0
                ? custom
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YAPA2");

        private readonly string _settingsFileLocation = Path.Combine(BaseDir, "settings.json");
        private readonly string _localSettingsFileLocation = Path.Combine(BaseDir, "localSettings.json");

        public AvaloniaEnvironment()
        {
            Directory.CreateDirectory(BaseDir);
        }

        public string GetSettings() => Read(_settingsFileLocation);

        public void SaveSettings(string settings) => Write(_settingsFileLocation, settings);

        public string GetLocalSettings() => Read(_localSettingsFileLocation);

        public void SaveLocalSettings(string settings) => Write(_localSettingsFileLocation, settings);

        public string GetPluginDirectory() => Path.Combine(BaseDir, "Plugins");

        public string GetThemeDirectory() => Path.Combine(BaseDir, "Themes");

        public bool PreRelease() => File.Exists(Path.Combine(BaseDir, "PreRelease.txt"));

        private static string Read(string file)
        {
            return File.Exists(file) ? File.ReadAllText(file) : "{}";
        }

        private static void Write(string file, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, content);
        }
    }
}
