// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CrossChannel;

/// <summary>
/// Provides the dependency injection integration of CrossChannel.
/// </summary>
public static class CrossChannelServiceCollectionExtensions
{
    /// <summary>
    /// Registers the radio services present in <see cref="RadioServiceRegistry"/> at the time of the call; services registered later are not added.<br/>
    /// <see cref="IChannel{TService}"/> is registered for each service, and unless the service opts out with
    /// <see cref="RadioServiceAttribute.AutoRegisterServiceAndSender"/>, the service interface (resolved to
    /// its broker) and <see cref="ISender{TService}"/> are registered as well. All of them are singletons.<br/>
    /// Existing registrations are kept: a <see cref="LocalRadio"/> registered beforehand is used, a service interface registered beforehand
    /// keeps its own implementation instead of the broker, and calling this method again only adds the missing services. The first call decides the radio, so calls with different
    /// <paramref name="useLocalRadio"/> values are not supported.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="useLocalRadio">
    /// <see langword="true"/> to add a <see cref="LocalRadio"/> singleton (unless one is registered) and route the registrations through it;
    /// <see langword="false"/> to route them through the static <see cref="Radio"/>.
    /// </param>
    /// <returns>The service collection, so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddCrossChannel(this IServiceCollection services, bool useLocalRadio = true)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (useLocalRadio)
        {// Use a LocalRadio instance.
            services.TryAddSingleton<LocalRadio>();
            foreach (var x in RadioServiceRegistry.Registrations)
            {
                var serviceType = x.ServiceType;
                services.TryAdd(new ServiceDescriptor(typeof(IChannel<>).MakeGenericType(serviceType), sp => sp.GetRequiredService<LocalRadio>().GetChannel(serviceType), ServiceLifetime.Singleton)); // IChannel<ISomeService> -> Channel
                if (x.AutoRegisterServiceAndSender)
                {
                    services.TryAdd(new ServiceDescriptor(serviceType, sp => sp.GetRequiredService<LocalRadio>().GetChannel(serviceType).GetBroker(), ServiceLifetime.Singleton)); // ISomeService -> Broker
                    services.TryAdd(new ServiceDescriptor(typeof(ISender<>).MakeGenericType(serviceType), typeof(LocalRadioSender<>).MakeGenericType(serviceType), ServiceLifetime.Singleton)); // ISender<ISomeService> -> LocalRadioSender<ISomeService>
                }
            }
        }
        else
        {// Use the static Radio.
            foreach (var x in RadioServiceRegistry.Registrations)
            {
                var serviceType = x.ServiceType;
                services.TryAdd(new ServiceDescriptor(typeof(IChannel<>).MakeGenericType(serviceType), sp => Radio.GetChannel(serviceType), ServiceLifetime.Singleton)); // IChannel<ISomeService> -> Channel
                if (x.AutoRegisterServiceAndSender)
                {
                    services.TryAdd(new ServiceDescriptor(serviceType, sp => Radio.GetChannel(serviceType).GetBroker(), ServiceLifetime.Singleton)); // ISomeService -> Broker
                    services.TryAdd(new ServiceDescriptor(typeof(ISender<>).MakeGenericType(serviceType), typeof(StaticRadioSender<>).MakeGenericType(serviceType), ServiceLifetime.Singleton)); // ISender<ISomeService> -> StaticRadioSender<ISomeService>
                }
            }
        }

        return services;
    }
}
