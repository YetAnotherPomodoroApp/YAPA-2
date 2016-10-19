using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using YAPA.Contracts;

namespace YAPA.WPF
{
    public class JsonYapaSettings : ISettings
    {
        private SettingsDictionary _settings;
        private SettingsDictionary _modifiedSettings;

        public JsonYapaSettings()
        {
            _settings = new SettingsDictionary();
            _modifiedSettings = new SettingsDictionary();
            Load();
        }

        public T Get<T>(string name, T defaultValue, string plugin, bool defer)
        {
            object value = defaultValue;
            var modValue = _modifiedSettings.GetValue(name, plugin, null);
            var settingValue = _settings.GetValue(name, plugin, null);

            if (defer)
            {
                value = modValue ?? settingValue ?? defaultValue;
            }
            else
            {
                value = settingValue ?? defaultValue;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }


        public void Update(string name, object value, string plugin, bool defer)
        {
            if (defer)
            {
                //if value is changed back to original value, just remove modification
                if (value == null || value.Equals(_settings.GetValue(name, plugin, null)))
                {
                    _modifiedSettings.RemoveKey(name, plugin);
                }
                else
                {
                    _modifiedSettings.SetValue(name, plugin, value);
                }

                HasUnsavedChanges = _modifiedSettings.Any();
            }
            else
            {
                _settings.SetValue(name, plugin, value);
                OnPropertyChanged($"{plugin}.{name}");

                SaveToFile();
            }
        }

        public ISettingsForPlugin GetSettingsForPlugin(string plugin)
        {
            return new SettingForPlugin(this, plugin);
        }

        public void Save()
        {
            SaveToFile();
            _modifiedSettings.Clear();
            HasUnsavedChanges = false;
        }

        private void SaveToFile()
        {
            var settingDir = Path.GetDirectoryName(SettingsFilePath());
            if (!Directory.Exists(settingDir))
            {
                Directory.CreateDirectory(settingDir);
            }

            if (HasUnsavedChanges)
            {
                foreach (var setting in _modifiedSettings)
                {
                    foreach (var value in setting.Value)
                    {
                        _settings.SetValue(value.Key, setting.Key, value.Value);
                        OnPropertyChanged($"{setting.Key}.{value.Key}");
                    }
                }
            }

            var serialized = JsonConvert.SerializeObject(_settings);
            using (var file = new StreamWriter(SettingsFilePath()))
            {
                file.Write(serialized);
            }

        }

        public void Load()
        {
            if (!File.Exists(SettingsFilePath()))
            {
                return;
            }
            _modifiedSettings.Clear();
            HasUnsavedChanges = false;
            using (var file = new StreamReader(SettingsFilePath()))
            {
                _settings = JsonConvert.DeserializeObject<SettingsDictionary>(file.ReadToEnd());
            }
        }

        private static string SettingsFilePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"YAPA2", @"settings.json");
        }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get { return _hasUnsavedChanges; }
            set
            {
                if (_hasUnsavedChanges == value)
                {
                    return;
                }
                _hasUnsavedChanges = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class SettingForPlugin : ISettingsForPlugin
        {
            private readonly ISettings _settings;
            private readonly string _plugin;
            private bool defer;

            public SettingForPlugin(ISettings settings, string plugin)
            {
                _settings = settings;
                _plugin = plugin;
                defer = false;
            }

            public T Get<T>(string name, T defaultValue)
            {
                return _settings.Get(name, defaultValue, _plugin, defer);
            }

            public void Update(string name, object value)
            {
                _settings.Update(name, value, _plugin, defer);

            }

            public void DeferChanges()
            {
                defer = true;
            }
        }

        protected class SettingsDictionary : Dictionary<string, Dictionary<string, object>>
        {
            public Dictionary<string, object> GetSettingsFor(string plugin)
            {
                Dictionary<string, object> settings;
                if (ContainsKey(plugin) == false)
                {
                    settings = new Dictionary<string, object>();
                    this[plugin] = settings;
                }
                else
                {
                    settings = this[plugin];
                }

                return settings;
            }

            public object GetValue(string name, string plugin, object defaultValue)
            {
                if (ContainsKey(plugin) == false)
                {
                    return defaultValue;
                }

                var settings = GetSettingsFor(plugin);
                var val = defaultValue;

                if (settings.ContainsKey(name))
                {
                    val = settings[name];
                }

                return val;
            }

            public void SetValue(string name, string plugin, object value)
            {
                var settings = GetSettingsFor(plugin);
                settings[name] = value;
            }

            public void RemoveKey(string name, string plugin)
            {
                var settings = GetSettingsFor(plugin);
                if (settings.ContainsKey(name))
                {
                    settings.Remove(name);
                }

                if (settings.Count == 0)
                {
                    Remove(plugin);
                }
            }

        }
    }
}
