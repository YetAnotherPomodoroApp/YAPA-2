using System.ComponentModel;

namespace YAPA.Contracts
{
    public interface ISettings : INotifyPropertyChanged
    {
        T Get<T>(string name, T defaultValue, string plugin, bool defer);

        bool HasUnsavedChanges { get; }

        void Update(string name, object value, string plugin, bool defer);

        ISettingsForComponent GetSettingsForComponent(string plugin);

        void Save();
        void Load();
    }

    public interface ISettingsForComponent
    {
        T Get<T>(string name, T defaultValue);

        void Update(string name, object value);

        void DeferChanges();
    }
}
