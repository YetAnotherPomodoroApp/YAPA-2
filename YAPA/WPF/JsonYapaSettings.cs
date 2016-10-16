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
        private DictionaryDefaultNull<string> _settings;
        private DictionaryDefaultNull<string> _modifiedSettings;

        public JsonYapaSettings()
        {
            _settings = new DictionaryDefaultNull<string>();
            _modifiedSettings = new DictionaryDefaultNull<string>();
            Load();
        }

        public T Get<T>(string name)
        {
            var isClass = typeof(T).IsClass;
            if (!isClass)
            {
                return (T)Convert.ChangeType(_settings[name], typeof(T));
            }

            return (T)_settings[name];
        }

        public T Get<T>(string name, T defaultValue)
        {
            var value = defaultValue;
            if (_settings.ContainsKey(name))
            {
                value = Get<T>(name);
            }

            return value;
        }

        public object Get(string name)
        {
            return Get<object>(name);
        }

        public void Update(string name, object value)
        {
            Update(name, value, false);
        }

        public void Update(string name, object value, bool imidiate)
        {
            if (imidiate)
            {
                _settings[name] = value;
                Save();
            }
            else
            {
                _modifiedSettings[name] = value;

                if (_modifiedSettings[name].Equals(_settings[name]))
                {
                    _modifiedSettings.Remove(name);
                }

                HasUnsavedChanges = _modifiedSettings.Any();
            }
        }

        public void Save()
        {
            SaveSettings();
            _modifiedSettings.Clear();
            HasUnsavedChanges = false;
        }

        private void SaveSettings()
        {
            var settingDir = Path.GetDirectoryName(SettingsFilePath());
            if (!Directory.Exists(settingDir))
            {
                Directory.CreateDirectory(settingDir);
            }

            if (HasUnsavedChanges)
            {
                foreach (var modified in _modifiedSettings)
                {
                    _settings[modified.Key] = modified.Value;
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
                _settings = JsonConvert.DeserializeObject<DictionaryDefaultNull<string>>(file.ReadToEnd());
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

        protected class DictionaryDefaultNull<TKey> : Dictionary<TKey, object>
        {
            public new object this[TKey key]
            {
                get
                {
                    return ContainsKey(key) ? base[key] : null;
                }

                set { base[key] = value; }
            }
        }
    }
}
