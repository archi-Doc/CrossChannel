// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using CrossChannel.Internal;

namespace CrossChannel;

/// <summary>
/// The keyed channel operations shared by <see cref="Radio"/> and <see cref="LocalRadio"/>.<br/>
/// The keyed channels of a service type and a key type are held by an <see cref="UnorderedMapWithLock{TKey, TValue}"/>
/// (key -> <see cref="Channel{TService}"/>), which is stored in a <see cref="ThreadsafeTwoTypeKeyHashtable{TValue}"/> and never removed.
/// </summary>
internal static class RadioHelper
{
    /// <summary>
    /// Gets the keyed map of the service type and <typeparamref name="TKey"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="twoTypeToMap">The table of keyed maps.</param>
    /// <param name="serviceType">The type of the service.</param>
    /// <returns>The map, or <see langword="null"/> if no channel has been opened with the service type and the key type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnorderedMapWithLock<TKey, object>? TryGetMap<TKey>(ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap, Type serviceType)
        where TKey : notnull
        => twoTypeToMap.TryGetValue(serviceType, typeof(TKey), out var obj) ? obj as UnorderedMapWithLock<TKey, object> : null;

    /// <summary>
    /// Gets the keyed map of <typeparamref name="TService"/> and <typeparamref name="TKey"/>, adding it if it does not exist.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="twoTypeToMap">The table of keyed maps.</param>
    /// <returns>The map.</returns>
    public static UnorderedMapWithLock<TKey, object> GetOrAddMap<TService, TKey>(ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap)
        where TService : class, IRadioService
        where TKey : notnull
        => (UnorderedMapWithLock<TKey, object>)twoTypeToMap.GetOrAdd(typeof(TService), typeof(TKey), static (x, y) => new UnorderedMapWithLock<TKey, object>());

    /// <summary>
    /// Gets the channel associated with the key.<br/>
    /// The channel is a <see cref="Channel{TService}"/> of the service type of the map, and the caller casts it (outside the lock).
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="map">The keyed map, or <see langword="null"/> if it does not exist.</param>
    /// <param name="key">The key.</param>
    /// <returns>The channel, or <see langword="null"/> if it is not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? GetChannel<TKey>(UnorderedMapWithLock<TKey, object>? map, TKey key)
        where TKey : notnull
    {
        if (map is null)
        {
            return null;
        }

        using (map.LockObject.EnterScope())
        {
            return map.TryGetValue(key, out var channel) ? channel : null;
        }
    }

    /// <summary>
    /// Tries to get the channel associated with the key from the keyed map of <typeparamref name="TService"/> and <typeparamref name="TKey"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="map">The keyed map, or <see langword="null"/> if it does not exist.</param>
    /// <param name="key">The key.</param>
    /// <param name="channel">When this method returns, contains the channel, if it is found.</param>
    /// <returns><see langword="true"/> if the channel is found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetChannelWithKey<TService, TKey>(UnorderedMapWithLock<TKey, object>? map, TKey key, [MaybeNullWhen(false)] out Channel<TService> channel)
        where TService : class, IRadioService
        where TKey : notnull
    {
        channel = GetChannel(map, key) as Channel<TService>;
        return channel is not null;
    }

    /// <summary>
    /// Tries to get the channel associated with the service type and the key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="twoTypeToMap">The table of keyed maps.</param>
    /// <param name="serviceType">The type of the service.</param>
    /// <param name="key">The key.</param>
    /// <param name="channel">When this method returns, contains the channel, if it is found.</param>
    /// <returns><see langword="true"/> if the channel is found; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is null.</exception>
    public static bool TryGetChannelWithKey<TKey>(ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap, Type serviceType, TKey key, [MaybeNullWhen(false)] out Channel channel)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        channel = GetChannel(TryGetMap<TKey>(twoTypeToMap, serviceType), key) as Channel;
        return channel is not null;
    }

    /// <summary>
    /// Gets the broker of the channel associated with the key, or the broker of the empty channel if it is not found.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="map">The keyed map of <typeparamref name="TService"/> and <typeparamref name="TKey"/>, or <see langword="null"/> if it does not exist.</param>
    /// <param name="key">The key.</param>
    /// <returns>The broker.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TService SendWithKey<TService, TKey>(UnorderedMapWithLock<TKey, object>? map, TKey key)
        where TService : class, IRadioService
        where TKey : notnull
        => ((GetChannel(map, key) as Channel<TService>) ?? RadioServiceRegistry.GetEmptyChannel<TService>()).Broker;

    /// <summary>
    /// Registers the instance in the channel associated with the key, creating the channel if it does not exist.<br/>
    /// The caller validates <paramref name="instance"/> and <paramref name="key"/> before getting or adding <paramref name="map"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="map">The keyed map of <typeparamref name="TService"/> and <typeparamref name="TKey"/>.</param>
    /// <param name="key">The key.</param>
    /// <param name="instance">The instance to register.</param>
    /// <param name="useWeakReference">Indicates whether to use a weak reference for the instance.</param>
    /// <returns>A link, or <see langword="null"/> if the channel is full.</returns>
    public static Channel<TService>.Link? OpenWithKey<TService, TKey>(UnorderedMapWithLock<TKey, object> map, TKey key, TService instance, bool useWeakReference)
        where TService : class, IRadioService
        where TKey : notnull
    {
        using (map.LockObject.EnterScope())
        {// A keyed channel shares this lock with the map, so the node and the link must be added in a single scope.
         // Otherwise another thread could detach the channel (by closing its last link) before the link is added.
            Channel<TService> channel;
            if (map.TryGetValue(key, out var obj))
            {
                channel = (Channel<TService>)obj;
            }
            else
            {
                channel = new Channel<TService>(map);
                (channel.NodeIndex, _) = map.Add(key, channel);
            }

            var link = channel.OpenInternal(instance, useWeakReference);
            if (link is null &&
                channel.NodeIndex != -1 &&
                channel.Count == 0)
            {// Do not leave an empty channel in the map.
                map.RemoveNode(channel.NodeIndex);
                channel.NodeIndex = -1;
            }

            return link;
        }
    }
}
