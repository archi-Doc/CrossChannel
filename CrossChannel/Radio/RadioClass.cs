// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

using CrossChannel.Internal;

namespace CrossChannel;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

public class RadioClass
{
    private readonly ThreadsafeTypeKeyHashtable<Channel> typeToChannel = new();
    private readonly ThreadsafeTwoTypeKeyHashtable<object> twoTypeToMap = new(); // UnorderedMap<TKey, object> // object is Channel<TService>

    public RadioClass()
    {
    }

    /// <summary>
    /// Gets the channel for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>The channel for the specified service type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public Channel<TService> GetChannel<TService>()
        where TService : class, IRadioService
        => (Channel<TService>)this.typeToChannel.GetOrAdd(typeof(TService), a => ChannelRegistry.Get<TService>().NewChannel());

    /// <summary>
    /// Gets the channel for the specified service type.
    /// </summary>
    /// <param name="serviceType">The type of the service.</param>
    /// <returns>The channel for the specified service type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public Channel GetChannel(Type serviceType)
        => (Channel)this.typeToChannel.GetOrAdd(serviceType, a => ChannelRegistry.Get(serviceType).NewChannel());

    /// <summary>
    /// Tries to get the channel for the specified service type and key.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="channel">When this method returns, contains the channel associated with the specified service type and key, if the key is found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the channel for the specified service type and key is found; otherwise, <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public bool TryGetChannelWithKey<TService, TKey>(TKey key, [MaybeNullWhen(false)] out Channel<TService> channel)
        where TService : class, IRadioService
        where TKey : notnull
        => RadioHelper.TryGetChannelWithKey(this.twoTypeToMap, key, out channel);

    /// <summary>
    /// Tries to get the channel for the specified service type and key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="serviceType">The type of the service.</param>
    /// <param name="key">The key.</param>
    /// <param name="channel">When this method returns, contains the channel associated with the specified service type and key, if the key is found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the channel for the specified service type and key is found; otherwise, <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public bool TryGetChannelWithKey<TKey>(Type serviceType, TKey key, [MaybeNullWhen(false)] out Channel channel)
        where TKey : notnull
        => RadioHelper.TryGetChannelWithKey(this.twoTypeToMap, serviceType, key, out channel);

    /// <summary>
    /// Opens a channel for the specified service type and registers the instance.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <param name="instance">The instance to register.</param>
    /// <param name="weakReference">Indicates whether to use a weak reference for the instance.</param>
    /// <returns>A link to the opened channel.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public Channel<TService>.Link? Open<TService>(TService instance, bool weakReference = false)
        where TService : class, IRadioService
    {
        var channel = this.GetChannel<TService>();
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
    public Channel<TService>.Link? OpenWithKey<TService, TKey>(TService instance, TKey key, bool weakReference = false)
        where TService : class, IRadioService
        where TKey : notnull
    {
        return RadioHelper.GetOrAddChannelWithKey(this.twoTypeToMap, instance, key).Open(instance, weakReference);
    }

    /// <summary>
    /// Retrieves an instance of a broker corresponding to a specific service type.<br/>
    /// When a broker function is called, the methods of the registered instances are invoked.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>The broker of the channel for the specified service type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
    public TService Send<TService>()
        where TService : class, IRadioService
    {
        return this.GetChannel<TService>().Broker;
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
    public TService SendWithKey<TService, TKey>(TKey key)
        where TService : class, IRadioService
        where TKey : notnull
    {
        if (RadioHelper.TryGetChannelWithKey<TService, TKey>(this.twoTypeToMap, key, out var channel))
        {
            return channel.Broker;
        }
        else
        {
            return ChannelRegistry.GetEmptyChannel<TService>().Broker;
        }
    }
}
