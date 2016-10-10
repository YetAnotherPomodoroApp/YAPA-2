using System.ComponentModel;

namespace YAPA.Contracts
{
    public interface ISettings : INotifyPropertyChanged
    {
        T Get<T>(string name);
        T Get<T>(string name, T defaultValue);
        object Get(string name);

        bool HasUnsavedChanges { get; }

        void Update(string name, object value);
        void Update(string name, object value, bool imidiate);

        void Save();
        void Load();
    }
}
