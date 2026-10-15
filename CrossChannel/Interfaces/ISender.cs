// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// Provides the sending (publishing) side of a radio service, so that a class can send messages
/// without depending on <see cref="Radio"/> or <see cref="LocalRadio"/> directly.<br/>
/// Registered in dependency injection by <see cref="CrossChannelServiceCollectionExtensions.AddCrossChannel"/>
/// unless the service opts out with <see cref="RadioServiceAttribute.AutoRegisterServiceAndSender"/>.
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
    /// Gets the broker of the keyed channel associated with the specified key.<br/>
    /// The broker is bound to the channel found at call time; call this method again instead of caching the broker.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="key">The key.</param>
    /// <returns>The broker, or the broker of the shared empty channel (sending does nothing) if no channel exists for the key.</returns>
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
/// An <see cref="ISender{TService}"/> which sends through a <see cref="LocalRadio"/> instance.
/// </summary>
/// <typeparam name="TService">The type of the service.</typeparam>
internal sealed class LocalRadioSender<TService> : ISender<TService>
    where TService : class, IRadioService
{
    private readonly LocalRadio radio;
    private readonly TService broker; // The channel of a LocalRadio is never replaced, so its broker is constant.

    public LocalRadioSender(LocalRadio radio)
    {
        this.radio = radio;
        this.broker = radio.Send<TService>();
    }

    /// <inheritdoc/>
    public TService Send()
        => this.broker;

    /// <inheritdoc/>
    public TService SendWithKey<TKey>(TKey key)
        where TKey : notnull
        => this.radio.SendWithKey<TService, TKey>(key);
}
