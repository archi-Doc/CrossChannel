// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// Provides the sending (publishing) side of a radio service, so that a class can send messages
/// without depending on <see cref="Radio"/> or <see cref="RadioClass"/> directly.<br/>
/// Registered in dependency injection by <see cref="ServiceCollectionExtensions.AddCrossChannel"/>.
/// </summary>
/// <typeparam name="TService">The type of the service.</typeparam>
public interface ISender<TService>
    where TService : class, IRadioService
{
    /// <summary>
    /// Gets the broker of the channel. Calling a method of the broker invokes that method on every subscribed instance.
    /// </summary>
    /// <returns>The broker.</returns>
    TService Send();

    /// <summary>
    /// Gets the broker of the channel associated with the specified key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="key">The key.</param>
    /// <returns>The broker, or the broker of an empty channel if no channel is associated with the key.</returns>
    TService SendWithKey<TKey>(TKey key)
        where TKey : notnull;
}

/// <summary>
/// An <see cref="ISender{TService}"/> which sends through the static <see cref="Radio"/>.
/// </summary>
/// <typeparam name="TService">The type of the service.</typeparam>
internal sealed class StaticRadioSender<TService> : ISender<TService>
    where TService : class, IRadioService
{
    public StaticRadioSender()
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

/// <summary>
/// An <see cref="ISender{TService}"/> which sends through a <see cref="RadioClass"/> instance.
/// </summary>
/// <typeparam name="TService">The type of the service.</typeparam>
internal sealed class RadioClassSender<TService> : ISender<TService>
    where TService : class, IRadioService
{
    private readonly RadioClass radio;

    public RadioClassSender(RadioClass radio)
    {
        this.radio = radio;
    }

    /// <inheritdoc/>
    public TService Send()
        => this.radio.Send<TService>();

    /// <inheritdoc/>
    public TService SendWithKey<TKey>(TKey key)
        where TKey : notnull
        => this.radio.SendWithKey<TService, TKey>(key);
}
