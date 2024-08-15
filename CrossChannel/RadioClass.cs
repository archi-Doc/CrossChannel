// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

using System.Collections.Concurrent;
using System.Collections.Generic;
using CrossChannel.Internal;

namespace CrossChannel;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

public class RadioClass
{
    private readonly ThreadsafeTypeKeyHashtable<object> typeToChannel = new(); // Type -> Channel<TService>
    private readonly ThreadsafeTypeKeyHashtable<object> typeToDictionary = new(); // Type -> ConcurrentDictionary<TKey, Channel<TService>>

    private class Item<TService>
        where TService : class, IRadioService
    {
        public Item()
        {
        }

        public Channel<TService> Channel = new();
    }

    public RadioClass()
    {
    }

    public Channel<TService>.Link Open<TService>(TService instance, bool weakReference = false)
        where TService : class, IRadioService
    {
        var xchannel = this.GetOrAdd<TService>();
        return xchannel.Open(instance, weakReference);
    }

    public Channel<TService>.Link Open<TService, TKey>(TService instance, TKey key, bool weakReference = false)
        where TService : class, IRadioService
        where TKey : notnull
    {
        var xchannel = this.GetOrAdd<TService, TKey>(key);
        return xchannel.Open(instance, weakReference);
    }

    public TService Send<TService>()
        where TService : class, IRadioService
    {
        return this.GetOrAdd<TService>().Broker;
    }

    public TService Send<TService, TKey>(TKey key)
        where TService : class, IRadioService
        where TKey : notnull
    {
        return this.GetOrAdd<TService, TKey>(key).Broker;
    }

    private Channel<TService> GetOrAdd<TService>()
        where TService : class, IRadioService
    {
        return (Channel<TService>)this.typeToChannel.GetOrAdd(typeof(TService), x => new Channel<TService>());
    }

    private Channel<TService> GetOrAdd<TService, TKey>(TKey key)
        where TService : class, IRadioService
        where TKey : notnull
    {
        var dictionary = (ConcurrentDictionary<TKey, Channel<TService>>)this.typeToDictionary.GetOrAdd(typeof(TService), x => new ConcurrentDictionary<TKey, Channel<TService>>());
        return dictionary.GetOrAdd(key, x => new Channel<TService>());
    }

    /*public Channel<TService> GetOrEmpty<TService, TKey>(TKey key)
        where TService : class, IRadioService
        where TKey : notnull
    {
        var dictionary = (ConcurrentDictionary<TKey, Channel<TService>>)this.typeToDictionary.GetOrAdd(typeof(TService), x => new ConcurrentDictionary<TKey, Channel<TService>>());
    }

    public static bool TryGet(TKey key, [MaybeNullWhen(false)] out Channel<TService> channel) => dictionary.TryGetValue(key, out channel);*/
}
