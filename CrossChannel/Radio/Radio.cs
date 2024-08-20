// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Diagnostics.CodeAnalysis;
global using System.Linq;
global using System.Runtime.CompilerServices;
global using System.Threading.Tasks;
using Arc.Collections;
using CrossChannel.Internal;

namespace CrossChannel;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

public static class Radio
{// CrossChannel by Romeo
    static Radio()
    {
    }

    public static Channel<TService> GetChannel<TService>()
        where TService : class, IRadioService
        => ChannelCache<TService>.Channel;

    public static Channel GetChannel(Type serviceType)
        => typeToChannel.GetOrAdd(serviceType, a => ChannelRegistry.Get(serviceType).NewChannel());

    public static bool TryGetChannel<TService, TKey>(TKey key, [MaybeNullWhen(false)] out Channel<TService> channel)
        where TService : class, IRadioService
        where TKey : notnull
    {
        if (!twoTypeToMap.TryGetValue(typeof(TService), typeof(TKey), out var obj) ||
            obj is not UnorderedMap<TKey, object> map)
        {
            channel = default;
            return false;
        }

        lock (map)
        {
            if (!map.TryGetValue(key, out obj))
            {
                channel = default;
                return false;
            }

            channel = obj as Channel<TService>;
            return channel is not null;
        }
    }

    public static bool TryGetChannel<TKey>(Type serviceType, TKey key, [MaybeNullWhen(false)] out Channel channel)
        where TKey : notnull
    {
        if (!twoTypeToMap.TryGetValue(serviceType, typeof(TKey), out var obj) ||
            obj is not UnorderedMap<TKey, object> map)
        {
            channel = default;
            return false;
        }

        lock (map)
        {
            if (!map.TryGetValue(key, out obj))
            {
                channel = default;
                return false;
            }

            channel = obj as Channel;
            return channel is not null;
        }
    }

    public static Channel<TService>.Link Open<TService>(TService instance, bool weakReference = false)
        where TService : class, IRadioService
    {
        var channel = ChannelCache<TService>.Channel;
        return channel.Open(instance, weakReference);
    }

    public static Channel<TService>.Link Open<TService, TKey>(TService instance, TKey key, bool weakReference = false)
        where TService : class, IRadioService
        where TKey : notnull
    {
        var map = (UnorderedMap<TKey, object>)twoTypeToMap.GetOrAdd(typeof(TService), typeof(TKey), (x, y) => new UnorderedMap<TKey, object>());

        lock (map)
        {
            Channel<TService>? channel;
            if (map.TryGetValue(key, out var obj))
            {
                channel = (Channel<TService>)obj;
            }
            else
            {
                channel = new Channel<TService>(map);
                (channel.NodeIndex, _) = map.Add(key, channel);
            }

            return channel.Open(instance, weakReference);
        }
    }

    public static TService Send<TService>()
        where TService : class, IRadioService
    {
        return ChannelCache<TService>.Channel.Broker;
    }

    public static TService Send<TService, TKey>(TKey key)
        where TService : class, IRadioService
        where TKey : notnull
    {
        if (TryGetChannel<TService, TKey>(key, out var channel))
        {
            return channel.Broker;
        }
        else
        {
            return ChannelRegistry.EmptyChannel<TService>().Broker;
        }
    }

    #region Cache

    internal static class ChannelCache<TService>
        where TService : class, IRadioService
    {
        private static readonly Channel<TService> channel;

        static ChannelCache()
        {
            // channel = new();
            channel = (Channel<TService>)typeToChannel.GetOrAdd(typeof(TService), a => ChannelRegistry.Get<TService>().NewChannel());
        }

        public static Channel<TService> Channel => channel;
    }

    private static readonly ThreadsafeTypeKeyHashtable<Channel> typeToChannel = new();
    private static readonly ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap = new(); // UnorderedMap<TKey, object> // object is Channel<TService>

    #endregion
}
