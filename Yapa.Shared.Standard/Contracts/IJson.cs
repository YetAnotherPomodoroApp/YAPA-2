namespace YAPA.Shared.Contracts
{
    public interface IJson
    {
        string Serialize(object obj);

        T Deserialize<T>(string obj);

        T ConvertToType<T>(object obj);

        bool AreEqual(object a, object b);
    }
}
