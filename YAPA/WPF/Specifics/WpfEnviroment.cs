using System;
using System.IO;
using YAPA.Shared.Contracts;

namespace YAPA.WPF
{
    public class WpfEnviroment : IEnviroment
    {
        static readonly string BaseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"YAPA2");
        readonly string _settingsFileLocation = Path.Combine(BaseDir, @"settings.json");
        readonly string _localSettingsFileLocation = Path.Combine(BaseDir, @"localSettings.json");
        readonly string _themeLocation = Path.Combine(BaseDir, @"settings.json");

        public WpfEnviroment()
        {
            if (!Directory.Exists(BaseDir))
            {
                Directory.CreateDirectory(BaseDir);
            }
        }

        public string GetSettings()
        {
            if (!File.Exists(_settingsFileLocation))
            {
                return "{}";
            }
            using (var file = new StreamReader(_settingsFileLocation))
            {
                return file.ReadToEnd();
            }
        }

        public void SaveSettings(string settings)
        {
            var settingDir = Path.GetDirectoryName(_settingsFileLocation);
            if (!Directory.Exists(settingDir))
            {
                Directory.CreateDirectory(settingDir);
            }

            using (var file = new StreamWriter(_settingsFileLocation))
            {
                file.Write(settings);
            }
        }

        public string GetPluginDirectory()
        {
            return Path.Combine(BaseDir, @"Plugins");
        }

        public string GetThemeDirectory()
        {
            return Path.Combine(BaseDir, @"Themes");
        }

        public string GetLocalSettings()
        {
            if (!File.Exists(_localSettingsFileLocation))
            {
                return "{}";
            }
            using (var file = new StreamReader(_localSettingsFileLocation))
            {
                return file.ReadToEnd();
            }
        }

        public void SaveLocalSettings(string settings)
        {
            var settingDir = Path.GetDirectoryName(_localSettingsFileLocation);
            if (!Directory.Exists(settingDir))
            {
                Directory.CreateDirectory(settingDir);
            }

            using (var file = new StreamWriter(_localSettingsFileLocation))
            {
                file.Write(settings);
            }
        }
    }
}
