using System;

namespace YAPA.Shared.Contracts
{
    public interface IDependencyInjector
    {
        object Resolve(Type type);
        void Register(Type type, bool singleInsntace = false);
        void RegisterInstance(object instance, Type asType = null);
    }
}
