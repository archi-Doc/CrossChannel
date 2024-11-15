// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using CrossChannel.Internal;

namespace CrossChannel;

internal static class RadioHelper
{
    public static bool TryGetChannelWithKey<TService, TKey>(ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap, TKey key, [MaybeNullWhen(false)] out Channel<TService> channel)
        where TService : class, IRadioService
        where TKey : notnull
    {
        if (!twoTypeToMap.TryGetValue(typeof(TService), typeof(TKey), out var obj) ||
            obj is not UnorderedMapWithLock<TKey, object> map)
        {
            channel = default;
            return false;
        }

        using (map.LockObject.EnterScope())
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

    public static bool TryGetChannelWithKey<TKey>(ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap, Type serviceType, TKey key, [MaybeNullWhen(false)] out Channel channel)
        where TKey : notnull
    {
        if (twoTypeToMap.TryGetValue(serviceType, typeof(TKey), out var obj) ||
            obj is not UnorderedMapWithLock<TKey, object> map)
        {
            channel = default;
            return false;
        }

        using (map.LockObject.EnterScope())
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

    public static Channel<TService> GetOrAddChannelWithKey<TService, TKey>(ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap, TService instance, TKey key)
        where TService : class, IRadioService
        where TKey : notnull
    {
        var map = (UnorderedMapWithLock<TKey, object>)twoTypeToMap.GetOrAdd(typeof(TService), typeof(TKey), (x, y) => new UnorderedMapWithLock<TKey, object>());

        using (map.LockObject.EnterScope())
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
