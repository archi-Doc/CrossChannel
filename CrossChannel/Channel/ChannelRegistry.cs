// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace CrossChannel;

public static class ChannelRegistry
{
    private static readonly ConcurrentDictionary<Type, ChannelInformation> TypeToInformation = new();

    private static class InformationCache<TService>
        where TService : class, IRadioService
    {
        // A field initializer (instead of a static constructor) keeps the type 'beforefieldinit',
        // so the JIT can elide the class initialization check on the hot path.
        public static readonly ChannelInformation Information = Get(typeof(TService));
    }

    public static bool Register(ChannelInformation information)
    {
        return TypeToInformation.TryAdd(information.ServiceType, information);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ChannelInformation Get<TService>()
        where TService : class, IRadioService
    {
        return InformationCache<TService>.Information;
    }

    public static ChannelInformation Get(Type serviceType)
    {
        if (TypeToInformation.TryGetValue(serviceType, out var information))
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
        => (Channel<TService>)InformationCache<TService>.Information.EmptyChannel;

    public static ICollection<ChannelInformation> Channels => TypeToInformation.Values;
}
