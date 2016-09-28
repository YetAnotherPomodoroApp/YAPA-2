namespace YAPA.Contracts
{
    public interface ISettings
    {
        T Get<T>(string name);
        T Get<T>(string name, T defaultValue);
        object Get(string name);

        void Update(string name, object value);

        void Save();
        void Load();
    }
}
