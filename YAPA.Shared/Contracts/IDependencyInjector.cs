using System;

namespace YAPA.Contracts
{
    public interface IDependencyInjector
    {
        object Resolve(Type type);
        void Register(Type type, bool singleInsntace = false);
    }
}
