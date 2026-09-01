// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Diagnostics.CodeAnalysis;
global using System.Linq;
global using System.Runtime.CompilerServices;
global using System.Threading.Tasks;
using CrossChannel.Internal;

namespace CrossChannel;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

/// <summary>
/// The process-wide radio: instances subscribe to a service interface with <see cref="Open{TService}(TService, bool)"/>
/// and messages are sent through <see cref="Send{TService}"/>.<br/>
/// Use <see cref="RadioClass"/> when several independent radios are needed.
/// </summary>
public static class Radio
{// CrossChannel by Romeo
    #region Cache

    internal static class ChannelCache<TService>
        where TService : class, IRadioService
    {
        // A field initializer (instead of a static constructor) keeps the type 'beforefieldinit',
        // so the JIT can elide the class initialization check on the hot path.
        public static readonly Channel<TService> Channel =
            (Channel<TService>)typeToChannel.GetOrAdd(typeof(TService), static _ => ChannelRegistry.GetRegistration<TService>().CreateChannel());
    }

    private static readonly ThreadsafeTypeKeyHashtable<Channel> typeToChannel = new();
    private static readonly ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap = new(); // UnorderedMapWithLock<TKey, object> // object is Channel<TService>

    #endregion

    /// <summary>
    /// Gets the channel for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>The channel for the specified service type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Channel<TService> GetChannel<TService>()
        where TService : class, IRadioService
        => ChannelCache<TService>.Channel;

    /// <summary>
    /// Gets the channel for the specified service type.
    /// </summary>
    /// <param name="serviceType">The type of the service.</param>
    /// <returns>The channel for the specified service type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public static Channel GetChannel(Type serviceType)
        => typeToChannel.GetOrAdd(serviceType, static a => ChannelRegistry.GetRegistration(a).CreateChannel());

    /// <summary>
    /// Tries to get the channel for the specified service type and key.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="channel">When this method returns, contains the channel associated with the specified service type and key, if the key is found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the channel for the specified service type and key is found; otherwise, <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public static bool TryGetChannelWithKey<TService, TKey>(TKey key, [MaybeNullWhen(false)] out Channel<TService> channel)
        where TService : class, IRadioService
        where TKey : notnull
        => RadioHelper.TryGetChannelWithKey(twoTypeToMap, key, out channel);

    /// <summary>
    /// Tries to get the channel for the specified service type and key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="serviceType">The type of the service.</param>
    /// <param name="key">The key.</param>
    /// <param name="channel">When this method returns, contains the channel associated with the specified service type and key, if the key is found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the channel for the specified service type and key is found; otherwise, <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public static bool TryGetChannelWithKey<TKey>(Type serviceType, TKey key, [MaybeNullWhen(false)] out Channel channel)
        where TKey : notnull
        => RadioHelper.TryGetChannelWithKey(twoTypeToMap, serviceType, key, out channel);

    /// <summary>
    /// Opens a channel for the specified service type and registers the instance.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <param name="instance">The instance to register.</param>
    /// <param name="weakReference">
    /// <see langword="true"/> to hold the instance with a weak reference, so that the link is closed
    /// automatically once the instance is garbage collected.
    /// </param>
    /// <returns>A link which unsubscribes the instance when disposed, or <see langword="null"/> if the channel is full (see <see cref="RadioServiceAttribute.MaxLinks"/>).</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public static Channel<TService>.Link? Open<TService>(TService instance, bool weakReference = false)
        where TService : class, IRadioService
    {
        var channel = ChannelCache<TService>.Channel;
        return channel.Open(instance, weakReference);
    }

    /// <summary>
    /// Opens a channel for the specified service type and key, and registers the instance.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="instance">The instance to register.</param>
    /// <param name="key">The key.</param>
    /// <param name="weakReference">
    /// <see langword="true"/> to hold the instance with a weak reference, so that the link is closed
    /// automatically once the instance is garbage collected.
    /// </param>
    /// <returns>A link which unsubscribes the instance when disposed, or <see langword="null"/> if the channel is full (see <see cref="RadioServiceAttribute.MaxLinks"/>).</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public static Channel<TService>.Link? OpenWithKey<TService, TKey>(TService instance, TKey key, bool weakReference = false)
        where TService : class, IRadioService
        where TKey : notnull
    {
        return RadioHelper.OpenWithKey<TService, TKey>(twoTypeToMap, key, instance, weakReference);
    }

    /// <summary>
    /// Retrieves an instance of a broker corresponding to a specific service type.<br/>
    /// When a broker function is called, the methods of the registered instances are invoked.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>The broker of the channel for the specified service type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TService Send<TService>()
        where TService : class, IRadioService
    {
        return ChannelCache<TService>.Channel.Broker;
    }

    /// <summary>
    /// Retrieves an instance of a broker corresponding to a specific service type.<br/>
    /// When a broker function is called, the methods of the registered instances are invoked.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="key">The key.</param>
    /// <returns>The broker of the channel for the specified service type and key, or the broker of an empty channel if the channel is not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public static TService SendWithKey<TService, TKey>(TKey key)
        where TService : class, IRadioService
        where TKey : notnull
    {
        if (TryGetChannelWithKey<TService, TKey>(key, out var channel))
        {
            return channel.Broker;
        }
        else
        {
            return ChannelRegistry.GetEmptyChannel<TService>().Broker;
        }
    }
}
