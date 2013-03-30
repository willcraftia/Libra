#region Using

using System;

#endregion

namespace Libra
{
    public static class ServiceProviderExtension
    {
        public static T GetService<T>(this IServiceProvider serviceProvider) where T : class
        {
            return serviceProvider.GetService(typeof(T)) as T;
        }

        public static T GetRequiredService<T>(this IServiceProvider serviceProvider) where T : class
        {
            return serviceProvider.GetRequiredService(typeof(T)) as T;
        }

        public static object GetRequiredService(this IServiceProvider serviceProvider, Type serviceType)
        {
            var service = serviceProvider.GetService(serviceType);
            if (service == null)
                throw new InvalidOperationException("Service not found: " + serviceType);

            return service;
        }
    }
}
