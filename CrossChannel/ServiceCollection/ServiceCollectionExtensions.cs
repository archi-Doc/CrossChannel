// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace CrossChannel;

public static class ServiceCollectionExtensions
{
    public static ICrossChannelBuilder AddCrossChannel(this IServiceCollection services)
    {
        ChannelRegistry.AddService(services);
        // services.Add(new(typeof(IRadioService), Radio.Send<IRadioService>()));
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
