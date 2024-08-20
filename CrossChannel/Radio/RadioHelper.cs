// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using CrossChannel.Internal;

namespace CrossChannel;

internal static class RadioHelper
{
    public static bool TryGetChannel<TService, TKey>(ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap, TKey key, [MaybeNullWhen(false)] out Channel<TService> channel)
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

    public static bool TryGetChannel<TKey>(ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap, Type serviceType, TKey key, [MaybeNullWhen(false)] out Channel channel)
        where TKey : notnull
    {
        if (twoTypeToMap.TryGetValue(serviceType, typeof(TKey), out var obj) ||
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

    public static Channel<TService> GetOrAddChannel<TService, TKey>(ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap, TService instance, TKey key)
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

            return channel;
        }
    }
}
