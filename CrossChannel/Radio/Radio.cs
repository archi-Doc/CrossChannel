// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Diagnostics.CodeAnalysis;
global using System.Linq;
global using System.Runtime.CompilerServices;
global using System.Threading.Tasks;
using Arc.Collections;
using CrossChannel.Internal;

namespace CrossChannel;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

/// <summary>
/// Represents a static radio class for facilitating communication between instances through an interface.
/// </summary>
public static class Radio
{// CrossChannel by Romeo
    #region Cache

    internal static class ChannelCache<TService>
        where TService : class, IRadioService
    {
        private static readonly Channel<TService> channel;

        static ChannelCache()
        {
            // channel = new();
            channel = (Channel<TService>)typeToChannel.GetOrAdd(typeof(TService), a => ChannelRegistry.Get<TService>().NewChannel());
        }

        public static Channel<TService> Channel => channel;
    }

    private static readonly ThreadsafeTypeKeyHashtable<Channel> typeToChannel = new();
    private static readonly ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap = new(); // UnorderedMap<TKey, object> // object is Channel<TService>

    #endregion

    /// <summary>
    /// Gets the channel for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>The channel for the specified service type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
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
        => typeToChannel.GetOrAdd(serviceType, a => ChannelRegistry.Get(serviceType).NewChannel());

    /// <summary>
    /// Tries to get the channel for the specified service type and key.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="channel">When this method returns, contains the channel associated with the specified service type and key, if the key is found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the channel for the specified service type and key is found; otherwise, <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public static bool TryGetChannel<TService, TKey>(TKey key, [MaybeNullWhen(false)] out Channel<TService> channel)
        where TService : class, IRadioService
        where TKey : notnull
        => RadioHelper.TryGetChannel(twoTypeToMap, key, out channel);

    /// <summary>
    /// Tries to get the channel for the specified service type and key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="serviceType">The type of the service.</param>
    /// <param name="key">The key.</param>
    /// <param name="channel">When this method returns, contains the channel associated with the specified service type and key, if the key is found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the channel for the specified service type and key is found; otherwise, <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public static bool TryGetChannel<TKey>(Type serviceType, TKey key, [MaybeNullWhen(false)] out Channel channel)
        where TKey : notnull
        => RadioHelper.TryGetChannel(twoTypeToMap, serviceType, key, out channel);

    /// <summary>
    /// Opens a channel for the specified service type and registers the instance.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <param name="instance">The instance to register.</param>
    /// <param name="weakReference">Indicates whether to use a weak reference for the instance.</param>
    /// <returns>A link to the opened channel.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public static Channel<TService>.Link Open<TService>(TService instance, bool weakReference = false)
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
    /// <param name="weakReference">Indicates whether to use a weak reference for the instance.</param>
    /// <returns>A link to the opened channel.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public static Channel<TService>.Link Open<TService, TKey>(TService instance, TKey key, bool weakReference = false)
        where TService : class, IRadioService
        where TKey : notnull
    {
        return RadioHelper.GetOrAddChannel(twoTypeToMap, instance, key).Open(instance, weakReference);
    }

    /// <summary>
    /// Retrieves an instance of a broker corresponding to a specific service type.<br/>
    /// When a broker function is called, the methods of the registered instances are invoked.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>The broker of the channel for the specified service type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
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
    public static TService Send<TService, TKey>(TKey key)
        where TService : class, IRadioService
        where TKey : notnull
    {
        if (TryGetChannel<TService, TKey>(key, out var channel))
        {
            return channel.Broker;
        }
        else
        {
            return ChannelRegistry.GetEmptyChannel<TService>().Broker;
        }
    }
}
