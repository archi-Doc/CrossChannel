// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

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
    /// Gets the maximum number of links for the channel.
    /// </summary>
    public int MaxLinks { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the radio service interface and sender<br/>
    /// should be automatically registered in dependency injection (default is true).
    /// </summary>
    public bool AutoRegisterRadioServiceAndSender { get; set; } = true;

    /// <summary>
    /// Gets an empty channel (a channel which does not accept any link).
    /// </summary>
    public Channel EmptyChannel
        => this.emptyChannel ?? this.PrepareEmptyChannel();

    private Channel? emptyChannel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelInformation"/> class.
    /// </summary>
    /// <param name="serviceType">The service type associated with the channel.</param>
    /// <param name="newBroker">The function that creates a new broker for the channel.</param>
    /// <param name="newChannel">The function that creates a new channel.</param>
    /// <param name="maxLinks">The maximum number of links for the channel.</param>
    /// <param name="autoRegisterRadioServiceAndSender">A value indicating whether the radio service interface and sender should be automatically registered in dependency injection.</param>
    public ChannelInformation(Type serviceType, Func<Channel, object> newBroker, Func<Channel> newChannel, int maxLinks, bool autoRegisterRadioServiceAndSender)
    {
        this.ServiceType = serviceType;
        this.NewBroker = newBroker;
        this.NewChannel = newChannel;
        this.MaxLinks = maxLinks;
        this.AutoRegisterRadioServiceAndSender = autoRegisterRadioServiceAndSender;
    }

    private Channel PrepareEmptyChannel()
    {
        var channel = this.NewChannel();
        channel.MaxLinks = 0; // Set before publishing the instance.
        return Interlocked.CompareExchange(ref this.emptyChannel, channel, null) ?? channel;
    }
}
