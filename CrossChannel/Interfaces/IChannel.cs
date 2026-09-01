// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// Provides the subscribing side of a radio service, so that a class can subscribe without
/// depending on <see cref="Radio"/> or <see cref="RadioClass"/> directly.<br/>
/// Registered in dependency injection by <see cref="ServiceCollectionExtensions.AddCrossChannel"/>.
/// </summary>
/// <typeparam name="TService">The type of the service.</typeparam>
public interface IChannel<TService>
    where TService : class, IRadioService
{
    /// <summary>
    /// Subscribes the specified instance to the channel.
    /// </summary>
    /// <param name="instance">The instance which receives the messages.</param>
    /// <param name="weakReference">
    /// <see langword="true"/> to hold the instance with a weak reference, so that the link is closed
    /// automatically once the instance is garbage collected.
    /// </param>
    /// <returns>A link which unsubscribes the instance when disposed, or <see langword="null"/> if the channel is full.</returns>
    Channel<TService>.Link? Open(TService instance, bool weakReference = false);
}
