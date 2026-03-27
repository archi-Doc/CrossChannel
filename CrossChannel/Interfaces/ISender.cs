// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// Represents an interface that delegates function execution to the registered (opened) instance via a broker.
/// </summary>
/// <typeparam name="TService">The type of the service.</typeparam>
public interface ISender<TService>
    where TService : class, IRadioService
{
    /// <summary>
    /// Gets a broker instance corresponding to a specific service type.<br/>
    /// When a broker function is called, the methods of the registered instances are invoked.
    /// </summary>
    /// <returns>The broker instance.</returns>
    TService Get();

    /// <summary>
    /// Gets a broker instance corresponding to a specific service type.<br/>
    /// When a broker function is called, the methods of the registered instances are invoked.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="key">The key.</param>
    /// <returns>The broker instance.</returns>
    TService GetWithKey<TKey>(TKey key)
        where TKey : notnull;
}

internal class StaticBrokerProvider<TService> : ISender<TService>
    where TService : class, IRadioService
{
    public StaticBrokerProvider()
    {
    }

    /// <inheritdoc/>
    public TService Get()
        => Radio.Send<TService>();

    /// <inheritdoc/>
    public TService GetWithKey<TKey>(TKey key)
        where TKey : notnull
        => Radio.SendWithKey<TService, TKey>(key);
}

internal class NonStaticBrokerProvider<TService> : ISender<TService>
    where TService : class, IRadioService
{
    private readonly RadioClass radio;

    public NonStaticBrokerProvider(RadioClass radio)
    {
        this.radio = radio;
    }

    /// <inheritdoc/>
    public TService Get()
        => this.radio.Send<TService>();

    /// <inheritdoc/>
    public TService GetWithKey<TKey>(TKey key)
        where TKey : notnull
        => this.radio.SendWithKey<TService, TKey>(key);
}
