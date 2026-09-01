// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

namespace CrossChannel;

/// <summary>
/// Describes a radio service registered in <see cref="ChannelRegistry"/>.<br/>
/// One instance is created per service interface by the generated module initializer.
/// </summary>
public class ChannelRegistration
{
    /// <summary>
    /// Gets the service interface this registration describes.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the factory which creates the broker of a channel.<br/>
    /// The broker forwards each method call to every instance linked to that channel.
    /// </summary>
    public Func<Channel, object> CreateBroker { get; }

    /// <summary>
    /// Gets the factory which creates a new, empty channel.
    /// </summary>
    public Func<Channel> CreateChannel { get; }

    /// <summary>
    /// Gets the maximum number of links a channel of this service can hold.
    /// </summary>
    public int MaxLinks { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="ServiceCollectionExtensions.AddCrossChannel"/> registers
    /// the service interface and <see cref="ISender{TService}"/> in dependency injection.<br/>
    /// <see cref="IChannel{TService}"/> is registered regardless of this value.
    /// </summary>
    public bool AutoRegisterServiceAndSender { get; }

    /// <summary>
    /// Gets the shared channel which accepts no link (<see cref="MaxLinks"/> is 0).<br/>
    /// Sending to its broker is a no-op, which is used when a keyed channel is not found.
    /// </summary>
    public Channel EmptyChannel
        => this.emptyChannel ?? this.PrepareEmptyChannel();

    private Channel? emptyChannel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelRegistration"/> class.
    /// </summary>
    /// <param name="serviceType">The service interface.</param>
    /// <param name="createBroker">The factory which creates the broker of a channel.</param>
    /// <param name="createChannel">The factory which creates a new channel.</param>
    /// <param name="maxLinks">The maximum number of links a channel can hold.</param>
    /// <param name="autoRegisterServiceAndSender">Whether to register the service interface and the sender in dependency injection.</param>
    public ChannelRegistration(Type serviceType, Func<Channel, object> createBroker, Func<Channel> createChannel, int maxLinks, bool autoRegisterServiceAndSender)
    {
        this.ServiceType = serviceType;
        this.CreateBroker = createBroker;
        this.CreateChannel = createChannel;
        this.MaxLinks = maxLinks;
        this.AutoRegisterServiceAndSender = autoRegisterServiceAndSender;
    }

    private Channel PrepareEmptyChannel()
    {
        var channel = this.CreateChannel();
        channel.MaxLinks = 0; // Set before publishing the instance.
        return Interlocked.CompareExchange(ref this.emptyChannel, channel, null) ?? channel;
    }
}
