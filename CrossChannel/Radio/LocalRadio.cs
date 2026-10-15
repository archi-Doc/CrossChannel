// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

using CrossChannel.Internal;

namespace CrossChannel;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

/// <summary>
/// A radio with its own set of channels, so that several independent radios can coexist in one process.<br/>
/// Use the static <see cref="Radio"/> instead when a single, process-wide radio is enough (it is slightly faster).
/// </summary>
public class LocalRadio
{
    private readonly ThreadsafeTypeKeyHashtable<Channel> typeToChannel = new();
    private readonly ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap = new(); // UnorderedMapWithLock<TKey, object> // object is Channel<TService>

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalRadio"/> class.
    /// </summary>
    public LocalRadio()
    {
    }

    /// <summary>
    /// Gets the channel for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>The channel for the specified service type.</returns>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    public Channel<TService> GetChannel<TService>()
        where TService : class, IRadioService
        => (Channel<TService>)this.typeToChannel.GetOrAdd(typeof(TService), static _ => RadioServiceRegistry.GetRegistration<TService>().ChannelFactory());

    /// <summary>
    /// Gets the channel for the specified service type.
    /// </summary>
    /// <param name="serviceType">The type of the service.</param>
    /// <returns>The channel for the specified service type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    public Channel GetChannel(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return this.typeToChannel.GetOrAdd(serviceType, static a => RadioServiceRegistry.GetRegistration(a).ChannelFactory());
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
    public bool TryGetChannelWithKey<TService, TKey>(TKey key, [MaybeNullWhen(false)] out Channel<TService> channel)
        where TService : class, IRadioService
        where TKey : notnull
        => RadioHelper.TryGetChannelWithKey(RadioHelper.TryGetMap<TKey>(this.twoTypeToMap, typeof(TService)), key, out channel);

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
    public bool TryGetChannelWithKey<TKey>(Type serviceType, TKey key, [MaybeNullWhen(false)] out Channel channel)
        where TKey : notnull
        => RadioHelper.TryGetChannelWithKey(this.twoTypeToMap, serviceType, key, out channel);

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
    public Channel<TService>.Link? Open<TService>(TService instance, bool useWeakReference = false)
        where TService : class, IRadioService
    {
        var channel = this.GetChannel<TService>();
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
    public Channel<TService>.Link? OpenWithKey<TService, TKey>(TService instance, TKey key, bool useWeakReference = false)
        where TService : class, IRadioService
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(key);
        return RadioHelper.OpenWithKey(RadioHelper.GetOrAddMap<TService, TKey>(this.twoTypeToMap), key, instance, useWeakReference);
    }

    /// <summary>
    /// Gets the broker of the channel of the specified service type.<br/>
    /// Calling a method of the broker invokes that method on every subscribed instance.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>The broker.</returns>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    public TService Send<TService>()
        where TService : class, IRadioService
    {
        return this.GetChannel<TService>().Broker;
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
    public TService SendWithKey<TService, TKey>(TKey key)
        where TService : class, IRadioService
        where TKey : notnull
        => RadioHelper.SendWithKey<TService, TKey>(RadioHelper.TryGetMap<TKey>(this.twoTypeToMap, typeof(TService)), key);
}
