// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Diagnostics.CodeAnalysis;
global using System.Linq;
global using System.Runtime.CompilerServices;
global using System.Threading.Tasks;

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace CrossChannel;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

public static class Radio
{// CrossChannel by Romeo
    static Radio()
    {
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
        var channel = ChannelCache<TService, TKey>.GetOrAdd(key);
        return channel.Open(instance, weakReference);
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
        return ChannelCache<TService, TKey>.GetOrEmpty(key).Broker;
    }

    #region Cache

    internal static class ChannelCache<TService>
        where TService : class, IRadioService
    {
        private static readonly Channel<TService> channel;

        static ChannelCache()
        {
            channel = new();
        }

        public static Channel<TService> Channel => channel;
    }

    internal static class ChannelCache<TService, TKey>
        where TService : class, IRadioService
        where TKey : notnull
    {
        private static readonly ConcurrentDictionary<TKey, Channel<TService>> dictionary;
        private static readonly Channel<TService> empty;

        static ChannelCache()
        {
            dictionary = new();
            empty = new();
        }

        public static Channel<TService> GetOrAdd(TKey key) => dictionary.GetOrAdd(key, x => new());

        public static Channel<TService> GetOrEmpty(TKey key) => dictionary.TryGetValue(key, out var channel) ? channel : empty;
    }

    #endregion
}
