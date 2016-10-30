namespace YAPA.Shared.Contracts
{
    public interface IEnviroment
    {
        string GetSettings();
        void SaveSettings(string settings);

        string GetPluginDirectory();
        string GetThemeDirectory();
    }
}
