// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Diagnostics.CodeAnalysis;
global using System.Linq;
global using System.Runtime.CompilerServices;
global using System.Threading.Tasks;
using System.Threading;
using CrossChannel.Internal;

namespace CrossChannel;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

/// <summary>
/// The process-wide radio: instances subscribe to a service interface with <see cref="Open{TService}(TService, bool)"/>
/// and messages are sent through <see cref="Send{TService}"/>.<br/>
/// Use <see cref="LocalRadio"/> when several independent radios are needed.
/// </summary>
public static class Radio
{// CrossChannel by Romeo
    #region Cache

    internal static class ChannelCache<TService>
        where TService : class, IRadioService
    {
        private static Channel<TService>? channel;

        public static Channel<TService> Channel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref channel) ?? Initialize();
        }

        private static Channel<TService> Initialize()
        {
            var value = (Channel<TService>)typeToChannel.GetOrAdd(typeof(TService), static _ => RadioServiceRegistry.GetRegistration<TService>().ChannelFactory());
            Volatile.Write(ref channel, value);
            return value;
        }
    }

    /// <summary>
    /// Caches the keyed map of <typeparamref name="TService"/> and <typeparamref name="TKey"/>, so that keyed operations skip the two-type lookup.<br/>
    /// Maps are never removed from <see cref="twoTypeToMap"/>, so the cached map never goes stale. The map is added only by
    /// <see cref="GetOrAddMap"/>, which caches it before any channel is added to it, so a missing cache means that no channel exists.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    private static class KeyedMapCache<TService, TKey>
        where TService : class, IRadioService
        where TKey : notnull
    {
        private static UnorderedMapWithLock<TKey, object>? map;

        /// <summary>
        /// Gets the keyed map, or <see langword="null"/> if no channel has been opened with the service type and the key type.
        /// </summary>
        /// <returns>The map, or <see langword="null"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnorderedMapWithLock<TKey, object>? TryGetMap()
            => Volatile.Read(ref map);

        /// <summary>
        /// Gets the keyed map, adding it if it does not exist.
        /// </summary>
        /// <returns>The map.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnorderedMapWithLock<TKey, object> GetOrAddMap()
            => Volatile.Read(ref map) ?? Initialize();

        private static UnorderedMapWithLock<TKey, object> Initialize()
        {
            var value = RadioHelper.GetOrAddMap<TService, TKey>(twoTypeToMap);
            Volatile.Write(ref map, value);
            return value;
        }
    }

    private static readonly ThreadsafeTypeKeyHashtable<Channel> typeToChannel = new();
    private static readonly ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap = new(); // UnorderedMapWithLock<TKey, object> // object is Channel<TService>

    #endregion

    /// <summary>
    /// Gets the channel for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>The channel for the specified service type.</returns>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Channel<TService> GetChannel<TService>()
        where TService : class, IRadioService
        => ChannelCache<TService>.Channel;

    /// <summary>
    /// Gets the channel for the specified service type.
    /// </summary>
    /// <param name="serviceType">The type of the service.</param>
    /// <returns>The channel for the specified service type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    public static Channel GetChannel(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return typeToChannel.GetOrAdd(serviceType, static a => RadioServiceRegistry.GetRegistration(a).ChannelFactory());
    }

    /// <summary>
    /// Tries to get the keyed channel for the specified service type and key. No channel is created.<br/>
    /// A keyed channel is removed when its last link is closed, and a link opened on a removed channel is never reached by
    /// <see cref="SendWithKey{TService, TKey}(TKey)"/>; subscribe with <see cref="OpenWithKey{TService, TKey}(TService, TKey, bool)"/> instead.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="channel">When this method returns, contains the channel, if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the channel exists; otherwise, <see langword="false"/> (including when the service type is not registered).</returns>
    public static bool TryGetChannelWithKey<TService, TKey>(TKey key, [MaybeNullWhen(false)] out Channel<TService> channel)
        where TService : class, IRadioService
        where TKey : notnull
        => RadioHelper.TryGetChannelWithKey(KeyedMapCache<TService, TKey>.TryGetMap(), key, out channel);

    /// <summary>
    /// Tries to get the keyed channel for the specified service type and key. No channel is created.<br/>
    /// A keyed channel is removed when its last link is closed, and a link opened on a removed channel is never reached by
    /// <see cref="SendWithKey{TService, TKey}(TKey)"/>; subscribe with <see cref="OpenWithKey{TService, TKey}(TService, TKey, bool)"/> instead.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="serviceType">The type of the service.</param>
    /// <param name="key">The key.</param>
    /// <param name="channel">When this method returns, contains the channel, if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the channel exists; otherwise, <see langword="false"/> (including when the service type is not registered).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is null.</exception>
    public static bool TryGetChannelWithKey<TKey>(Type serviceType, TKey key, [MaybeNullWhen(false)] out Channel channel)
        where TKey : notnull
        => RadioHelper.TryGetChannelWithKey(twoTypeToMap, serviceType, key, out channel);

    /// <summary>
    /// Subscribes the instance to the channel of the specified service type.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <param name="instance">The instance which receives the messages.</param>
    /// <param name="useWeakReference">
    /// <see langword="true"/> to hold the instance with a weak reference, so that the link is closed
    /// during sending or subscription cleanup after the instance is collected.
    /// </param>
    /// <returns>A link which unsubscribes the instance when disposed, or <see langword="null"/> if the channel is full (see <see cref="RadioServiceAttribute.MaxLinks"/>).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    public static Channel<TService>.Link? Open<TService>(TService instance, bool useWeakReference = false)
        where TService : class, IRadioService
    {
        var channel = ChannelCache<TService>.Channel;
        return channel.Open(instance, useWeakReference);
    }

    /// <summary>
    /// Subscribes the instance to the keyed channel of the specified service type and key.<br/>
    /// The keyed channel is created on the first subscription and removed when its last link is closed.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="instance">The instance which receives the messages.</param>
    /// <param name="key">The key.</param>
    /// <param name="useWeakReference">
    /// <see langword="true"/> to hold the instance with a weak reference, so that the link is closed
    /// during sending or subscription cleanup after the instance is collected.
    /// </param>
    /// <returns>A link which unsubscribes the instance when disposed, or <see langword="null"/> if the channel is full (see <see cref="RadioServiceAttribute.MaxLinks"/>).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> or <paramref name="key"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    public static Channel<TService>.Link? OpenWithKey<TService, TKey>(TService instance, TKey key, bool useWeakReference = false)
        where TService : class, IRadioService
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(key);
        return RadioHelper.OpenWithKey(KeyedMapCache<TService, TKey>.GetOrAddMap(), key, instance, useWeakReference);
    }

    /// <summary>
    /// Gets the broker of the channel of the specified service type.<br/>
    /// Calling a method of the broker invokes that method on every subscribed instance.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>The broker.</returns>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TService Send<TService>()
        where TService : class, IRadioService
    {
        return ChannelCache<TService>.Channel.Broker;
    }

    /// <summary>
    /// Gets the broker of the keyed channel of the specified service type and key.<br/>
    /// Calling a method of the broker invokes that method on every instance subscribed with the key.
    /// The broker is bound to the channel found at call time; call this method again instead of caching the broker.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="key">The key.</param>
    /// <returns>The broker, or the broker of the shared empty channel (sending does nothing) if no channel exists for the key.</returns>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    public static TService SendWithKey<TService, TKey>(TKey key)
        where TService : class, IRadioService
        where TKey : notnull
        => RadioHelper.SendWithKey<TService, TKey>(KeyedMapCache<TService, TKey>.TryGetMap(), key);
}
