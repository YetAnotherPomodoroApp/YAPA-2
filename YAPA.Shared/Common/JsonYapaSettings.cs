using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using YAPA.Shared.Contracts;

namespace YAPA.Shared.Common
{
    public class JsonYapaSettings : ISettings
    {
        private readonly IEnvironment _enviroment;

        private readonly YapaSettingFile _settings;
        private readonly YapaSettingFile _localSettings;

        public JsonYapaSettings(IEnvironment enviroment, IJson json)
        {
            _enviroment = enviroment;

            _settings = new YapaSettingFile(json);
            _localSettings = new YapaSettingFile(json);

            _settings.PropertyChanged += _settings_PropertyChanged;
            _localSettings.PropertyChanged += _settings_PropertyChanged;
            Load();
        }

        public void Load()
        {
            _settings.Load(_enviroment.GetSettings());
            _localSettings.Load(_enviroment.GetLocalSettings());

            HasUnsavedChanges = false;
        }

        private void _settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HasUnsavedChanges))
            {
                HasUnsavedChanges = _settings.HasUnsavedChanges || _localSettings.HasUnsavedChanges;
            }
            else
            {
                // ReSharper disable once ExplicitCallerInfoArgument
                OnPropertyChanged(e.PropertyName);
            }
        }

        public T Get<T>(string name, T defaultValue, string plugin, bool defer, bool local = false)
        {
            var settingStore = local ? _localSettings : _settings;

            return settingStore.Get(name, defaultValue, plugin, defer);
        }

        public void Update(string name, object value, string plugin, bool defer, bool local = false)
        {
            var settingStore = local ? _localSettings : _settings;

            settingStore.Update(name, value, plugin, defer);

            if (defer == false)
            {
                if (local)
                {
                    SaveLocalSettings();
                }
                else
                {
                    SaveSettings();
                }
            }
        }

        public string GetRawSettingsForComponent(string plugin)
        {
            return _settings.GetRawSettingsForComponent(plugin);
        }

        public void SetRawSettingsForComponent(string plugin, string setting)
        {
            var current =  _settings.SetRawSettingsForComponent(plugin, setting);

            _enviroment.SaveSettings(current);
        }

        public ISettingsForComponent GetSettingsForComponent(string plugin)
        {
            return new SettingForPlugin(this, plugin);
        }

        public void Save()
        {
            SaveSettings();
            SaveLocalSettings();

            Load();
        }

        private void SaveSettings()
        {
            var settings = _settings.Commit();

            _enviroment.SaveSettings(settings);
        }

        private void SaveLocalSettings()
        {
            var localSettings = _localSettings.Commit();

            _enviroment.SaveLocalSettings(localSettings);
        }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
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

        public class SettingForPlugin : ISettingsForComponent
        {
            private readonly ISettings _settings;
            private readonly string _plugin;
            private bool _defer;

            public SettingForPlugin(ISettings settings, string plugin)
            {
                _settings = settings;
                _plugin = plugin;
                _defer = false;
            }

            public T Get<T>(string name, T defaultValue, bool local = false)
            {
                return _settings.Get(name, defaultValue, _plugin, _defer, local);
            }

            public void Update(string name, object value, bool local = false)
            {
                _settings.Update(name, value, _plugin, _defer, local);
            }

            public void DeferChanges()
            {
                _defer = true;
            }
        }

    }

    public class SettingsDictionary : Dictionary<string, Dictionary<string, object>>
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

        public void RemoveKey(string plugin)
        {
            if (ContainsKey(plugin))
            {
                Remove(plugin);
            }
        }

    }

    public class YapaSettingFile : INotifyPropertyChanged
    {
        private readonly IJson _json;
        private SettingsDictionary _settings;
        private readonly SettingsDictionary _modifiedSettings;

        public YapaSettingFile(IJson json)
        {
            _json = json;
            _settings = new SettingsDictionary();
            _modifiedSettings = new SettingsDictionary();
        }

        public string GetRawSettingsForComponent(string plugin)
        {
            if (!_settings.ContainsKey(plugin))
            {
                return string.Empty;
            }
            return _json.Serialize(_settings[plugin]);
        }

        public string SetRawSettingsForComponent(string plugin, string settings)
        {
            if (!_settings.ContainsKey(plugin))
            {
                return string.Empty;
            }

            _settings[plugin] = _json.Deserialize<Dictionary<string, object>>(settings);

            foreach (var pair in _settings[plugin])
            {
                OnPropertyChanged($"{plugin}.{pair.Key}");
            }

            return _json.Serialize(_settings);
        }

        public T Get<T>(string name, T defaultValue, string plugin, bool defer)
        {
            object value;
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

            if (value != null && value.Equals(defaultValue))
            {
                return (T)value;
            }

            return _json.ConvertToType<T>(value);
        }


        public void Update(string name, object value, string plugin, bool defer)
        {
            if (defer)
            {
                //if value is changed back to original value, just remove modification
                if (_json.AreEqual(value, _settings.GetValue(name, plugin, null)))
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
                // ReSharper disable once ExplicitCallerInfoArgument
                OnPropertyChanged($"{plugin}.{name}");
            }
        }

        public string Commit()
        {
            var settings = SaveToFile();
            _modifiedSettings.Clear();
            HasUnsavedChanges = false;
            return settings;
        }

        private string SaveToFile()
        {
            if (HasUnsavedChanges)
            {
                var changedProperties = new List<string>();
                foreach (var setting in _modifiedSettings)
                {
                    foreach (var value in setting.Value)
                    {
                        _settings.SetValue(value.Key, setting.Key, value.Value);
                        // ReSharper disable once ExplicitCallerInfoArgument
                        changedProperties.Add($"{setting.Key}.{value.Key}");
                    }
                }

                foreach (var item in changedProperties)
                {
                    OnPropertyChanged(item);
                }
            }

            var serialized = _json.Serialize(_settings);
            return serialized;
        }

        public void Load(string settings)
        {
            _modifiedSettings.Clear();
            HasUnsavedChanges = false;

            if (string.IsNullOrEmpty(settings))
            {
                _settings = new SettingsDictionary();
            }
            else
            {
                _settings = _json.Deserialize<SettingsDictionary>(settings);
                ApplyMigration();
            }
        }

        private bool _hasUnsavedChanges;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
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

        public void ApplyMigration()
        {
            var migrations = new List<Tuple<string, string>> { Tuple.Create("MinimizeToTray", "SystemTray") };
            var anyMigrationApplied = false;

            foreach (var migration in migrations)
            {
                if (!_settings.ContainsKey(migration.Item1))
                {
                    continue;
                }
                _settings[migration.Item2] = _settings[migration.Item1];
                _settings.RemoveKey(migration.Item1);
                anyMigrationApplied = true;
            }

            if (anyMigrationApplied)
            {
                Commit();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}