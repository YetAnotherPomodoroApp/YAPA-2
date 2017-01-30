using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using YAPA.Contracts;
using YAPA.Shared.Contracts;

namespace YAPA.Shared
{
    public class JsonYapaSettings : ISettings
    {
        private readonly IEnviroment _enviroment;
        private readonly IJson _json;

        private YapaSettingFile _settings;
        private YapaSettingFile _localSettings;

        public JsonYapaSettings(IEnviroment enviroment, IJson json)
        {
            _enviroment = enviroment;
            _json = json;

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

        public ISettingsForComponent GetSettingsForComponent(string plugin)
        {
            return new SettingForPlugin(this, plugin);
        }

        public void Save()
        {
            SaveSettings();
            SaveLocalSettings();
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

    }

    public class YapaSettingFile : INotifyPropertyChanged
    {
        private readonly IJson _json;
        private SettingsDictionary _settings;
        private SettingsDictionary _modifiedSettings;

        public YapaSettingFile(IJson json)
        {
            _json = json;
            _settings = new SettingsDictionary();
            _modifiedSettings = new SettingsDictionary();
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
                foreach (var setting in _modifiedSettings)
                {
                    foreach (var value in setting.Value)
                    {
                        _settings.SetValue(value.Key, setting.Key, value.Value);
                        OnPropertyChanged($"{setting.Key}.{value.Key}");
                    }
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
            }
        }

        private bool _hasUnsavedChanges;

        public event PropertyChangedEventHandler PropertyChanged;

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

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}