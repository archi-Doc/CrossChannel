// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using CrossChannel.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace CrossChannel;

public static class RadioRegistry
{
    private static ConcurrentDictionary<Type, ChannelInformation> typeToInformation = new();
    // private static ThreadsafeTypeKeyHashtable<ChannelInformation> typeToInformation = new();

    internal static class InformationCache<TService>
        where TService : class, IRadioService
    {
        public static readonly ChannelInformation Information;

        static InformationCache()
        {
            if (!typeToInformation.TryGetValue(typeof(TService), out var information))
            {
                throw new InvalidOperationException($"ChannelInformation for type {typeof(TService).FullName} has not been registered.");
            }

            Information = information;
        }
    }

    public static bool Register(ChannelInformation information)
    {
        // typeToInformation.TryAdd(information.MachineType, information);
        return typeToInformation.TryAdd(information.ServiceType, information);
    }

    public static ChannelInformation Get<TService>()
        where TService : class, IRadioService
    {
        return InformationCache<TService>.Information;
    }

    public static ChannelInformation Get(Type serviceType)
    {
        if (typeToInformation.TryGetValue(serviceType, out var information))
        {
            return information;
        }
        else
        {
            throw new InvalidOperationException($"ChannelInformation for type {serviceType.FullName} has not been registered.");
        }
    }

    public static Channel<TService> GetEmptyChannel<TService>()
        where TService : class, IRadioService
    {
        return (Channel<TService>)InformationCache<TService>.Information.EmptyChannel;
    }

    public static void AddService(IServiceCollection services)
    {
        foreach (var x in typeToInformation.Values)
        {// ITestService => Radio.Send<ITestService>() or RadioClass.Send<>()
            // IKeyedService => Radio.Send<ITestService, TKey>(key) or RadioClass
            // services.Add(new(x.ServiceType, RadioRegistry.Get(x.ServiceType).NewChannel()));
        }
    }
}
