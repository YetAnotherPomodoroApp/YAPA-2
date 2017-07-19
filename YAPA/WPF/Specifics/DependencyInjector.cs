using System;
using Autofac;
using YAPA.Shared.Contracts;

namespace YAPA.WPF.Specifics
{
    public class DependencyInjector : IDependencyInjector
    {
        private readonly IContainer _autofac;

        public DependencyInjector(IContainer autofac)
        {
            _autofac = autofac;
        }

        public object Resolve(Type type)
        {
            return _autofac.Resolve(type);
        }

        public void Register(Type type, bool singleInsntace = false)
        {
            var updater = new ContainerBuilder();

            var registration = updater.RegisterType(type);


            if (singleInsntace)
            {
                registration.SingleInstance();
            }

            updater.Update(_autofac);
        }

        public void RegisterInstance(object instance, Type asType = null)
        {
            var updater = new ContainerBuilder();

            var registraction = updater.RegisterInstance(instance);

            if (asType != null)
            {
                registraction.As(asType);
            }
            updater.Update(_autofac);
        }
    }
}
