using Microsoft.Extensions.DependencyInjection;
using System;
using userspace_backend.Model;

namespace userspace_backend
{
    public static class BackEndComposer
    {
        public static IServiceProvider Compose(IServiceCollection services)
        {
            services.AddSingleton<ISystemDevicesRetriever, SystemDevicesRetriever>();
            services.AddSingleton<ISystemDevicesProvider, SystemDevicesProvider>();
            return services.BuildServiceProvider();
        }
    }
}
