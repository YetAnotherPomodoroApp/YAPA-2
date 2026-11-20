using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Infrastructure
{
    /// <summary>
    /// A small constructor-injection container. The WPF app used Autofac 4 and added
    /// registrations after the container was built (themes and plugins are found at start-up);
    /// newer Autofac no longer allows that, so this keeps the same behaviour in a few lines.
    /// Concrete classes that were never registered are created on demand, a new one each time.
    /// </summary>
    public class DependencyInjector : IDependencyInjector
    {
        private sealed class Registration
        {
            public Type Implementation;
            public bool Singleton;
            public object Instance;
        }

        private readonly Dictionary<Type, Registration> _registrations = new Dictionary<Type, Registration>();
        private readonly object _lock = new object();

        public DependencyInjector()
        {
            RegisterInstance(this, typeof(IDependencyInjector));
            RegisterInstance(this, typeof(DependencyInjector));
        }

        public void Register(Type type, bool singleInsntace = false)
        {
            Register(type, type, singleInsntace);
        }

        public void Register<TService, TImplementation>(bool singleInstance = false) where TImplementation : TService
        {
            Register(typeof(TService), typeof(TImplementation), singleInstance);
        }

        public void Register(Type service, Type implementation, bool singleInstance = false)
        {
            lock (_lock)
            {
                _registrations[service] = new Registration { Implementation = implementation, Singleton = singleInstance };
            }
        }

        public void RegisterInstance(object instance, Type asType = null)
        {
            lock (_lock)
            {
                _registrations[asType ?? instance.GetType()] = new Registration
                {
                    Implementation = instance.GetType(),
                    Singleton = true,
                    Instance = instance
                };
            }
        }

        public T Resolve<T>() => (T)Resolve(typeof(T));

        public object Resolve(Type type)
        {
            return Resolve(type, new Stack<Type>());
        }

        public bool IsRegistered(Type type)
        {
            lock (_lock)
            {
                return _registrations.ContainsKey(type);
            }
        }

        private object Resolve(Type type, Stack<Type> chain)
        {
            Registration registration;
            lock (_lock)
            {
                _registrations.TryGetValue(type, out registration);
            }

            if (registration == null)
            {
                if (type.IsInterface || type.IsAbstract)
                {
                    throw new InvalidOperationException($"No registration for {type.FullName} (needed by {string.Join(" <- ", chain.Select(x => x.Name))})");
                }
                return Create(type, chain);
            }

            if (!registration.Singleton)
            {
                return Create(registration.Implementation, chain);
            }

            if (registration.Instance != null)
            {
                return registration.Instance;
            }

            var instance = Create(registration.Implementation, chain);
            lock (_lock)
            {
                if (registration.Instance == null)
                {
                    registration.Instance = instance;
                }
                // The same singleton serves every service it was registered for
                foreach (var other in _registrations.Values.Where(x => x.Singleton && x.Instance == null && x.Implementation == registration.Implementation))
                {
                    other.Instance = registration.Instance;
                }
                return registration.Instance;
            }
        }

        private object Create(Type type, Stack<Type> chain)
        {
            if (chain.Contains(type))
            {
                throw new InvalidOperationException($"Circular dependency: {string.Join(" <- ", chain.Select(x => x.Name))} <- {type.Name}");
            }

            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(x => x.GetParameters().Length)
                .ToList();

            if (!constructors.Any())
            {
                throw new InvalidOperationException($"{type.FullName} has no public constructor");
            }

            chain.Push(type);
            try
            {
                Exception lastError = null;
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    if (!parameters.All(CanResolve))
                    {
                        continue;
                    }
                    try
                    {
                        var args = parameters.Select(p => Resolve(p.ParameterType, chain)).ToArray();
                        return constructor.Invoke(args);
                    }
                    catch (TargetInvocationException e) when (e.InnerException != null)
                    {
                        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    }
                    catch (Exception e)
                    {
                        lastError = e;
                    }
                }
                throw lastError ?? new InvalidOperationException($"Unable to resolve constructor parameters for {type.FullName}");
            }
            finally
            {
                chain.Pop();
            }
        }

        private bool CanResolve(ParameterInfo parameter)
        {
            var type = parameter.ParameterType;
            if (IsRegistered(type))
            {
                return true;
            }
            if (type.IsPrimitive || type == typeof(string) || type.IsInterface || type.IsAbstract)
            {
                return false;
            }
            return type.IsClass;
        }
    }
}
