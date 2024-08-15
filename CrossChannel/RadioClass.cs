// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CrossChannel.Internal;

namespace CrossChannel;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

public class RadioClass
{
    private readonly ThreadsafeTypeKeyHashtable<object> typeToChannel = new(); // ServiceType -> Channel<TService>
    private readonly ThreadsafeTypeKeyHashtable<object> typeToKeyDictionary = new(); // ServiceType -> KeyDictionary<TKey>

    public RadioClass()
    {
    }

    public Channel<TService>.Link Open<TService>(TService instance, bool weakReference = false)
        where TService : class, IRadioService
    {
        var channel = this.GetChannel<TService>();
        return channel.Open(instance, weakReference);
    }

    public Channel<TService>.Link Open<TService, TKey>(TService instance, TKey key, bool weakReference = false)
        where TService : class, IRadioService
        where TKey : notnull
    {
        var xchannel = this.GetChannel<TService, TKey>(key);
        return xchannel.Open(instance, weakReference);
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

    public Channel<TService> GetChannel<TService>()
        where TService : class, IRadioService
    {
        return (Channel<TService>)this.typeToChannel.GetOrAdd(typeof(TService), x => new Channel<TService>(default));
    }

    public bool TryGetChannel<TService, TKey>(TKey key, [MaybeNullWhen(false)] out Channel<TService> channel)
        where TService : class, IRadioService
        where TKey : notnull
    {
        if (this.typeToKeyDictionary.TryGetValue(typeof(TService), out var obj) &&
            obj is KeyDictionary<TKey> keyDictionary)
        {
            lock (keyDictionary)
            {
                if (keyDictionary.TryGet(key, out var item))
                {
                    channel = item.Channel as Channel<TService>;
                    return channel is not null;
                }
            }
        }

        channel = default;
        return false;
    }

    private Channel<TService> GetChannel<TService, TKey>(TKey key)
        where TService : class, IRadioService
        where TKey : notnull
    {
        var keyDictionary = (KeyDictionary<TKey>)this.typeToKeyDictionary.GetOrAdd(typeof(TService), x => new KeyDictionary<TKey>());

        lock (keyDictionary)
        {
            var item = keyDictionary.Get(key);
            if (item.Channel is Channel<TService> c)
            {
                return c;
            }
            else
            {
                c = new Channel<TService>();
                item.Channel = c;
            }

            return c;
        }
    }
}
