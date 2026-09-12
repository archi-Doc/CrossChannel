// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace CrossChannel;

/// <summary>
/// Provides the dependency injection integration of CrossChannel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers every radio service of the process in the specified <see cref="IServiceCollection"/>.<br/>
    /// <see cref="IChannel{TService}"/> is registered for each service, and unless the service opts out with
    /// <see cref="RadioServiceAttribute.AutoRegisterServiceAndSender"/>, the service interface (resolved to
    /// its broker) and <see cref="ISender{TService}"/> are registered as well. All of them are singletons.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="useRadioClass">
    /// <see langword="true"/> to add a <see cref="RadioClass"/> singleton and route the registrations through it;
    /// <see langword="false"/> to route them through the static <see cref="Radio"/>.
    /// </param>
    /// <returns>The service collection, so that calls can be chained.</returns>
    public static IServiceCollection AddCrossChannel(this IServiceCollection services, bool useRadioClass = true)
    {
        if (useRadioClass)
        {// Use a RadioClass instance.
            services.AddSingleton<RadioClass>();
            foreach (var x in ChannelRegistry.Registrations)
            {
                var serviceType = x.ServiceType;
                services.Add(new(typeof(IChannel<>).MakeGenericType(serviceType), sp => sp.GetRequiredService<RadioClass>().GetChannel(serviceType), ServiceLifetime.Singleton)); // IChannel<ISomeService> -> Channel
                if (x.AutoRegisterServiceAndSender)
                {
                    services.Add(new(serviceType, sp => sp.GetRequiredService<RadioClass>().GetChannel(serviceType).GetBroker(), ServiceLifetime.Singleton)); // ISomeService -> Broker
                    services.Add(new(typeof(ISender<>).MakeGenericType(serviceType), typeof(RadioClassSender<>).MakeGenericType(serviceType), ServiceLifetime.Singleton)); // ISender<ISomeService> -> RadioClassSender<ISomeService>
                }
            }
        }
        else
        {// Use the static Radio.
            foreach (var x in ChannelRegistry.Registrations)
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
