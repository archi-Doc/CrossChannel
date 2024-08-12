﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Diagnostics.CodeAnalysis;
global using System.Linq;
global using System.Runtime.CompilerServices;
global using System.Threading.Tasks;

using System.Collections.Concurrent;

namespace CrossChannel;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

public static class NewRadio
{
    static NewRadio()
    {
    }

    public static Channel<TService>.Connection Open<TService>(TService instance, object? weakReference = default)
        where TService : IRadioService
    {
        var xchannel = ChannelCache<TService>.Channel;
        return xchannel.Open(instance, weakReference);
    }

    public static Channel<TService>.Connection Open<TService, TKey>(TService instance, TKey key, object? weakReference = default)
        where TService : IRadioService
        where TKey : notnull
    {
        var xchannel = ChannelCache<TService, TKey>.Channel(key);
        return xchannel.Open(instance, weakReference);
    }

    public static TService Send<TService>()
        where TService : IRadioService
    {
        return ChannelCache<TService>.Channel.Broker;
    }

    public static TService Send<TService, TKey>(TKey key)
        where TService : IRadioService
        where TKey : notnull
    {
        return ChannelCache<TService, TKey>.Channel(key).Broker;
    }

    #region Cache

    internal static class ChannelCache<TService>
        where TService : IRadioService
    {
        private static readonly Channel<TService> channel;

        static ChannelCache()
        {
            channel = new();
        }

        public static Channel<TService> Channel => channel;
    }

    internal static class ChannelCache<TService, TKey>
        where TService : IRadioService
        where TKey : notnull
    {
        private static readonly ConcurrentDictionary<TKey, Channel<TService>> dictionary;

        static ChannelCache()
        {
            dictionary = new();
        }

        public static Channel<TService> Channel(TKey key) => dictionary.GetOrAdd(key, x => new());
    }

    #endregion
}
