// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

using System.Collections.Concurrent;
using CrossChannel.Internal;

namespace CrossChannel;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

public class NewRadioClass
{
    private readonly ThreadsafeTypeKeyHashtable<object> typeToChannel = new(); // Type -> Channel<TService>
    private readonly ThreadsafeTypeKeyHashtable<object> typeToDictionary = new(); // Type -> ConcurrentDictionary<TKey, Channel<TService>>

    public NewRadioClass()
    {
    }

    public Channel<TService>.Link Open<TService>(TService instance)
        where TService : class, IRadioService
    {
        var xchannel = this.GetChannel<TService>();
        return xchannel.Open(instance);
    }

    public Channel<TService>.Link Open<TService, TKey>(TService instance, TKey key)
        where TService : class, IRadioService
        where TKey : notnull
    {
        var xchannel = this.GetChannel<TService, TKey>(key);
        return xchannel.Open(instance);
    }

    public TService Send<TService>()
        where TService : class, IRadioService
    {
        return this.GetChannel<TService>().Broker;
    }

    public TService Send<TService, TKey>(TKey key)
        where TService : class, IRadioService
        where TKey : notnull
    {
        return this.GetChannel<TService, TKey>(key).Broker;
    }

    private Channel<TService> GetChannel<TService>()
        where TService : class, IRadioService
    {
        return (Channel<TService>)this.typeToChannel.GetOrAdd(typeof(TService), x => new Channel<TService>());
    }

    private Channel<TService> GetChannel<TService, TKey>(TKey key)
        where TService : class, IRadioService
        where TKey : notnull
    {
        var dictionary = (ConcurrentDictionary<TKey, Channel<TService>>)this.typeToDictionary.GetOrAdd(typeof(TService), x => new ConcurrentDictionary<TKey, Channel<TService>>());
        return dictionary.GetOrAdd(key, x => new Channel<TService>());
    }
}
