// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace CrossChannel;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the CrossChannel services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="useRadioClass">If true,use the non-static RadioClass; otherwise, use the static Radio.</param>
    public static void AddCrossChannel(this IServiceCollection services, bool useRadioClass = true)
    {
        if (useRadioClass)
        {// Use a RadioClass instance.
            services.AddSingleton<RadioClass>();
            foreach (var x in ChannelRegistry.Channels)
            {
                var serviceType = x.ServiceType;
                services.Add(new(typeof(IChannel<>).MakeGenericType(serviceType), sp => sp.GetRequiredService<RadioClass>().GetChannel(serviceType), ServiceLifetime.Singleton)); // IChannel<ISomeService> -> Channel
                if (x.AutoRegisterRadioServiceAndSender)
                {
                    services.Add(new(serviceType, sp => sp.GetRequiredService<RadioClass>().GetChannel(serviceType).GetBroker(), ServiceLifetime.Singleton)); // ISomeService -> Broker
                    services.Add(new(typeof(ISender<>).MakeGenericType(serviceType), typeof(NonStaticBrokerProvider<>).MakeGenericType(serviceType), ServiceLifetime.Singleton)); // ISender<ISomeService> -> NonStaticBrokerProvider<ISomeService>
                }
            }
        }
        else
        {// Use the static Radio.
            foreach (var x in ChannelRegistry.Channels)
            {
                var serviceType = x.ServiceType;
                services.Add(new(typeof(IChannel<>).MakeGenericType(serviceType), sp => Radio.GetChannel(serviceType), ServiceLifetime.Singleton)); // IChannel<ISomeService> -> Channel
                if (x.AutoRegisterRadioServiceAndSender)
                {
                    services.Add(new(serviceType, sp => Radio.GetChannel(serviceType).GetBroker(), ServiceLifetime.Singleton)); // ISomeService -> Broker
                    services.Add(new(typeof(ISender<>).MakeGenericType(serviceType), typeof(StaticBrokerProvider<>).MakeGenericType(serviceType), ServiceLifetime.Singleton)); // ISender<ISomeService> -> StaticBrokerProvider<ISomeService>
                }
            }
        }
    }
}
