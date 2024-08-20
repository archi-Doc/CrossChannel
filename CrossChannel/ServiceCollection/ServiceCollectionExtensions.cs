﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace CrossChannel;

public static class ServiceCollectionExtensions
{
    public static ICrossChannelBuilder AddCrossChannel(this IServiceCollection services)
    {
        foreach (var x in ChannelRegistry.Channels)
        {
            // Radio
            services.Add(new(x.ServiceType, a => Radio.GetChannel(x.ServiceType).GetBroker(), ServiceLifetime.Singleton)); // ISomeService
            services.Add(new(typeof(IChannel<>).MakeGenericType(x.ServiceType), a => Radio.GetChannel(x.ServiceType), ServiceLifetime.Singleton)); // IChannel<ISomeService>
            services.Add(new(typeof(ISender<>).MakeGenericType(x.ServiceType), a => Activator.CreateInstance(typeof(Sender<>).MakeGenericType(x.ServiceType))!, ServiceLifetime.Singleton)); // ISender<ISomeService>
        }

        return new CrossChannelBuilder(services);
    }
}

public interface ICrossChannelBuilder
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
}
