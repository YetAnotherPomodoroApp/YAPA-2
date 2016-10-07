using System;
using System.Collections.Generic;
using YAPA.Contracts;

namespace YAPA.WPF
{
    public class JsonYapaSettings : ISettings
    {
        private Dictionary<string, object> _settings;

        public JsonYapaSettings()
        {
            _settings = new Dictionary<string, object>();
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
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Load()
        {
            throw new NotImplementedException();
        }
    }
}
