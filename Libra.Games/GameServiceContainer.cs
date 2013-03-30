#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Games
{
    public sealed class GameServiceContainer : IServiceProvider
    {
        Dictionary<Type, object> services;

        public GameServiceContainer()
        {
            services = new Dictionary<Type, object>();
        }

        public void AddService<T>(T service) where T : class
        {
            AddService(typeof(T), service);
        }

        public void AddService(Type serviceType, object service)
        {
            if (serviceType == null) throw new ArgumentNullException("serviceType");
            if (service == null) throw new ArgumentNullException("service");

            services.Add(serviceType, service);
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException("serviceType");

            object service;
            if (services.TryGetValue(serviceType, out service))
                return service;

            return null;
        }

        public void RemoveService<T>() where T : class
        {
            RemoveService(typeof(T));
        }

        public void RemoveService(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException("serviceType");

            services.Remove(serviceType);
        }
    }
}
