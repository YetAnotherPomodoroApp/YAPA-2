using System.ComponentModel;

namespace YAPA.Shared.Contracts
{
    public interface ISettings : INotifyPropertyChanged
    {
        T Get<T>(string name, T defaultValue, string plugin, bool defer, bool local = false);

        bool HasUnsavedChanges { get; }

        void Update(string name, object value, string plugin, bool defer, bool local = false);

        ISettingsForComponent GetSettingsForComponent(string plugin);

        void SetRawSettingsForComponent(string plugin, string settings);
        string GetRawSettingsForComponent(string plugin);

        void Save();
        void Load();
    }

    public interface ISettingsForComponent
    {
        T Get<T>(string name, T defaultValue, bool local = false);

        void Update(string name, object value, bool local = false);

        void DeferChanges();
    }
}
