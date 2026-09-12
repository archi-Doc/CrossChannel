// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace CrossChannel;

/// <summary>
/// Provides the dependency injection integration of CrossChannel.
/// </summary>
public static class CrossChannelServiceCollectionExtensions
{
    /// <summary>
    /// Registers every radio service of the process in the specified <see cref="IServiceCollection"/>.<br/>
    /// <see cref="IChannel{TService}"/> is registered for each service, and unless the service opts out with
    /// <see cref="RadioServiceAttribute.AutoRegisterServiceAndSender"/>, the service interface (resolved to
    /// its broker) and <see cref="ISender{TService}"/> are registered as well. All of them are singletons.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="useLocalRadio">
    /// <see langword="true"/> to add a <see cref="LocalRadio"/> singleton and route the registrations through it;
    /// <see langword="false"/> to route them through the static <see cref="Radio"/>.
    /// </param>
    /// <returns>The service collection, so that calls can be chained.</returns>
    public static IServiceCollection AddCrossChannel(this IServiceCollection services, bool useLocalRadio = true)
    {
        if (useLocalRadio)
        {// Use a LocalRadio instance.
            services.AddSingleton<LocalRadio>();
            foreach (var x in RadioServiceRegistry.Registrations)
            {
                var serviceType = x.ServiceType;
                services.Add(new(typeof(IChannel<>).MakeGenericType(serviceType), sp => sp.GetRequiredService<LocalRadio>().GetChannel(serviceType), ServiceLifetime.Singleton)); // IChannel<ISomeService> -> Channel
                if (x.AutoRegisterServiceAndSender)
                {
                    services.Add(new(serviceType, sp => sp.GetRequiredService<LocalRadio>().GetChannel(serviceType).GetBroker(), ServiceLifetime.Singleton)); // ISomeService -> Broker
                    services.Add(new(typeof(ISender<>).MakeGenericType(serviceType), typeof(LocalRadioSender<>).MakeGenericType(serviceType), ServiceLifetime.Singleton)); // ISender<ISomeService> -> LocalRadioSender<ISomeService>
                }
            }
        }
        else
        {// Use the static Radio.
            foreach (var x in RadioServiceRegistry.Registrations)
            {
                var serviceType = x.ServiceType;
                services.Add(new(typeof(IChannel<>).MakeGenericType(serviceType), sp => Radio.GetChannel(serviceType), ServiceLifetime.Singleton)); // IChannel<ISomeService> -> Channel
                if (x.AutoRegisterServiceAndSender)
                {
                    services.Add(new(serviceType, sp => Radio.GetChannel(serviceType).GetBroker(), ServiceLifetime.Singleton)); // ISomeService -> Broker
                    services.Add(new(typeof(ISender<>).MakeGenericType(serviceType), typeof(StaticRadioSender<>).MakeGenericType(serviceType), ServiceLifetime.Singleton)); // ISender<ISomeService> -> StaticRadioSender<ISomeService>
                }
            }
        }

        return services;
    }
}
