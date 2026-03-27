// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// Represents a channel interface for a specific service.
/// </summary>
/// <typeparam name="TService">The type of the service.</typeparam>
public interface IChannel<TService>
    where TService : class, IRadioService
{
    /// <summary>
    /// Opens a channel for the specified service instance.
    /// </summary>
    /// <param name="instance">The service instance.</param>
    /// <param name="weakReference">Specifies whether to use a weak reference for the service instance.</param>
    /// <returns>The channel link if the channel is successfully opened; otherwise, null.</returns>
    Channel<TService>.Link? Open(TService instance, bool weakReference = false);
}
