using System;
using System.IO;
using YAPA.Shared.Contracts;

namespace YAPA.WPF
{
    public class WpfEnviroment : IEnviroment
    {
        static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        readonly string _settingsFileLocation = Path.Combine(BaseDir, @"YAPA2", @"settings.json");
        readonly string _themeLocation = Path.Combine(BaseDir, @"YAPA2", @"settings.json");

        public string GetSettings()
        {
            if (!File.Exists(_settingsFileLocation))
            {
                return null;
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
            return Path.Combine(BaseDir, @"YAPA2", @"Plugins");
        }

        public string GetThemeDirectory()
        {
            return Path.Combine(BaseDir, @"YAPA2", @"Themes");
        }
    }
}
