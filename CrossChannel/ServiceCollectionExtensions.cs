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
                services.Add(new(typeof(IChannel<>).MakeGenericType(x.ServiceType), sp => sp.GetRequiredService<RadioClass>().GetChannel(x.ServiceType), ServiceLifetime.Singleton)); // IChannel<ISomeService> -> Channel
                services.Add(new(x.ServiceType, sp => sp.GetRequiredService<RadioClass>().GetChannel(x.ServiceType).GetBroker(), ServiceLifetime.Singleton)); // ISomeService -> Broker
                services.AddSingleton(typeof(ISender<>), typeof(NonStaticBrokerProvider<>)); // ISender<ISomeService> -> NonStaticBrokerProvider<ISomeService>
            }
        }
        else
        {// Use the static Radio.
            foreach (var x in ChannelRegistry.Channels)
            {
                services.Add(new(typeof(IChannel<>).MakeGenericType(x.ServiceType), sp => Radio.GetChannel(x.ServiceType), ServiceLifetime.Singleton)); // IChannel<ISomeService> -> Channel
                services.Add(new(x.ServiceType, sp => Radio.GetChannel(x.ServiceType).GetBroker(), ServiceLifetime.Singleton)); // ISomeService -> Broker
                services.Add(new(typeof(ISender<>).MakeGenericType(x.ServiceType), sp => Activator.CreateInstance(typeof(StaticBrokerProvider<>).MakeGenericType(x.ServiceType))!, ServiceLifetime.Singleton)); // ISender<ISomeService> -> StaticBrokerProvider<ISomeService>
            }
        }
    }
}

/*public interface ICrossChannelBuilder
{
    IServiceCollection Services { get; }
}

public class CrossChannelBuilder : ICrossChannelBuilder
{
    public IServiceCollection Services { get; }

    public CrossChannelBuilder(IServiceCollection services)
    {
        this.Services = services;
    }
}*/
