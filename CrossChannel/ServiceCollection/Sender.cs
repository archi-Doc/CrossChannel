// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// Represents an interface that delegates function execution to the respective instance via a broker.
/// </summary>
/// <typeparam name="TService">The type of the service.</typeparam>
public interface ISender<TService>
    where TService : class, IRadioService
{
    /// <summary>
    /// Gets a broker instance corresponding to a specific service type.<br/>
    /// When a broker function is called, the methods of the registered instances are invoked.
    /// </summary>
    /// <returns>The sent message.</returns>
    TService Send();

    /// <summary>
    /// Gets a broker instance corresponding to a specific service type.<br/>
    /// When a broker function is called, the methods of the registered instances are invoked.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="key">The key.</param>
    /// <returns>The sent message.</returns>
    TService SendWithKey<TKey>(TKey key)
        where TKey : notnull;
}

internal class Sender<TService> : ISender<TService>
    where TService : class, IRadioService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Sender{TService}"/> class.
    /// </summary>
    public Sender()
    {
    }

    /// <inheritdoc/>
    public TService Send()
        => Radio.Send<TService>();

    /// <inheritdoc/>
    public TService SendWithKey<TKey>(TKey key)
        where TKey : notnull
        => Radio.SendWithKey<TService, TKey>(key);
}
