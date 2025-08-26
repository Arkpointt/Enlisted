using System;
using System.Collections.Generic;
using Enlisted.Core.Logging;

namespace Enlisted.Core.DependencyInjection
{
    /// <summary>
    /// Simple service container implementing dependency injection patterns.
    /// Replaces static singleton patterns as outlined in ADR-004.
    /// Provides constructor injection and service lifecycle management.
    /// </summary>
    public interface IServiceContainer
    {
        /// <summary>Register a singleton service instance.</summary>
        void RegisterSingleton<TInterface, TImplementation>(TImplementation instance)
            where TImplementation : class, TInterface;

        /// <summary>Register a singleton service with factory function.</summary>
        void RegisterSingleton<TInterface>(Func<IServiceContainer, TInterface> factory);

        /// <summary>Get a registered service instance.</summary>
        TInterface GetService<TInterface>();

        /// <summary>Check if a service is registered.</summary>
        bool IsRegistered<TInterface>();
    }

    /// <summary>
    /// Production implementation of service container.
    /// Provides simple dependency injection without external dependencies.
    /// Follows blueprint principle of preferring simple, self-contained solutions.
    /// </summary>
    public class ServiceContainer : IServiceContainer
    {
        private readonly Dictionary<Type, object> _singletonInstances = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<IServiceContainer, object>> _singletonFactories = new Dictionary<Type, Func<IServiceContainer, object>>();

        public void RegisterSingleton<TInterface, TImplementation>(TImplementation instance)
            where TImplementation : class, TInterface
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            
            var interfaceType = typeof(TInterface);
            _singletonInstances[interfaceType] = instance;
        }

        public void RegisterSingleton<TInterface>(Func<IServiceContainer, TInterface> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            
            var interfaceType = typeof(TInterface);
            _singletonFactories[interfaceType] = container => factory(container);
        }

        public TInterface GetService<TInterface>()
        {
            var interfaceType = typeof(TInterface);
            
            // Check for existing instance
            if (_singletonInstances.TryGetValue(interfaceType, out var instance))
            {
                return (TInterface)instance;
            }
            
            // Check for factory
            if (_singletonFactories.TryGetValue(interfaceType, out var factory))
            {
                var newInstance = factory(this);
                _singletonInstances[interfaceType] = newInstance;
                return (TInterface)newInstance;
            }
            
            throw new InvalidOperationException($"Service of type {interfaceType.Name} is not registered");
        }

        public bool IsRegistered<TInterface>()
        {
            var interfaceType = typeof(TInterface);
            return _singletonInstances.ContainsKey(interfaceType) || _singletonFactories.ContainsKey(interfaceType);
        }
    }

    /// <summary>
    /// Global service locator for transition period.
    /// Provides access to services during migration from static patterns.
    /// Will be replaced with proper constructor injection in behaviors.
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceContainer _container;

        /// <summary>Initialize the service container during mod startup.</summary>
        public static void Initialize(IServiceContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>Get a service instance. Throws if not initialized or service not found.</summary>
        public static TInterface GetService<TInterface>()
        {
            if (_container == null)
                throw new InvalidOperationException("ServiceLocator not initialized. Call Initialize() during mod startup.");
            
            return _container.GetService<TInterface>();
        }

        /// <summary>Check if container is initialized and service is available.</summary>
        public static bool TryGetService<TInterface>(out TInterface service)
        {
            service = default(TInterface);
            
            if (_container == null || !_container.IsRegistered<TInterface>())
                return false;
            
            try
            {
                service = _container.GetService<TInterface>();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
