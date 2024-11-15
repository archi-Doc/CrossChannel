// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace CrossChannel;

/// <summary>
/// Represents information about a channel.
/// </summary>
public class ChannelInformation
{
    /// <summary>
    /// Gets the service type associated with the channel.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the function that creates a new broker for the channel.
    /// </summary>
    public Func<Channel, object> NewBroker { get; }

    /// <summary>
    /// Gets the function that creates a new channel.
    /// </summary>
    public Func<Channel> NewChannel { get; }

    /// <summary>
    /// Gets the function that creates a new keyed channel.
    /// </summary>
    public Func<IUnorderedMapWithLock, Channel> NewChannel2 { get; }

    /// <summary>
    /// Gets the maximum number of links for the channel.
    /// </summary>
    public int MaxLinks { get; private set; }

    /// <summary>
    /// Gets an empty channel.
    /// </summary>
    public Channel EmptyChannel
    {
        get
        {
            if (this.emptyChannel is null)
            {
                this.emptyChannel = this.NewChannel();
                this.emptyChannel.MaxLinks = 0;
            }

            return this.emptyChannel;
        }
    }

    private Channel? emptyChannel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelInformation"/> class.
    /// </summary>
    /// <param name="serviceType">The service type associated with the channel.</param>
    /// <param name="newBroker">The function that creates a new broker for the channel.</param>
    /// <param name="newChannel">The function that creates a new channel.</param>
    /// <param name="newChannel2">The function that creates a new channel with an unordered map.</param>
    /// <param name="maxLinks">The maximum number of links for the channel.</param>
    public ChannelInformation(Type serviceType, Func<Channel, object> newBroker, Func<Channel> newChannel, Func<IUnorderedMapWithLock, Channel> newChannel2, int maxLinks)
    {
        this.ServiceType = serviceType;
        this.NewBroker = newBroker;
        this.NewChannel = newChannel;
        this.NewChannel2 = newChannel2;
        this.MaxLinks = maxLinks;
    }
}
