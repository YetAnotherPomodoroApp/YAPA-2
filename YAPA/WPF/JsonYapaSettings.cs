using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using YAPA.Contracts;
using YAPA.Shared.Contracts;

namespace YAPA.WPF
{
    public class JsonYapaSettings : ISettings
    {
        private readonly IEnviroment _enviroment;
        private SettingsDictionary _settings;
        private SettingsDictionary _modifiedSettings;

        public JsonYapaSettings(IEnviroment enviroment)
        {
            _enviroment = enviroment;
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

            if (value != null && value.Equals(defaultValue))
            {
                return (T)value;
            }

            if (typeof(T).IsValueType || value is string)
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            else if (value is JArray)
            {
                return ((JArray)value).ToObject<T>();
            }
            else
            {
                return (T)value;
            }
        }


        public void Update(string name, object value, string plugin, bool defer)
        {
            if (defer)
            {
                //if value is changed back to original value, just remove modification
                if (AreEqual(value, _settings.GetValue(name, plugin, null)))
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

        private bool AreEqual(object valA, object valB)
        {
            if (valA == null && valB == null)
            {
                return true;
            }

            if (valA == null && valB != null
                || valA != null && valB == null)
            {
                return false;
            }

            if (valA.GetType().IsValueType || valA is string)
            {
                return valA.Equals(valB);
            }
            else if (valA.GetType().GetInterface(nameof(IEnumerable)) != null)
            {
                var listA = (IEnumerable)valA;
                var listB = (JArray)valB;


                if (Count(listA) != listB.Count)
                {
                    return false;
                }

                foreach (var b in listB)
                {
                    var contains = false;
                    foreach (var a in listA)
                    {
                        contains |= b.ToString().Equals(a);
                    }

                    if (contains == false)
                    {
                        return false;
                    }
                }
                return true;

            }



            return false;
        }

        public int Count(IEnumerable source)
        {
            int c = 0;
            var e = source.GetEnumerator();
            while (e.MoveNext())
                c++;
            return c;
        }


        public ISettingsForComponent GetSettingsForComponent(string plugin)
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
            _enviroment.SaveSettings(serialized);
        }

        public void Load()
        {

            _modifiedSettings.Clear();
            HasUnsavedChanges = false;
            _settings = JsonConvert.DeserializeObject<SettingsDictionary>(_enviroment.GetSettings() ?? "[]");
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

            public T Get<T>(string name, T defaultValue)
            {
                return _settings.Get(name, defaultValue, _plugin, _defer);
            }

            public void Update(string name, object value)
            {
                _settings.Update(name, value, _plugin, _defer);
            }

            public void DeferChanges()
            {
                _defer = true;
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
