using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        }

        public T Get<T>(string name)
        {
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
            _modifiedSettings[name] = value;

            if (_modifiedSettings[name] == _settings[name])
            {
                _modifiedSettings.Remove(name);
            }

            HasUnsavedChanges = _modifiedSettings.Any();
        }

        public void Save()
        {
            throw new NotImplementedException();

            _modifiedSettings.Clear();
        }

        public void Load()
        {
            _modifiedSettings.Clear();
            throw new NotImplementedException();
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
