// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace CrossChannel;

/// <summary>
/// Represents information about a channel.<br/>
/// Constructor: new broker(channel).<br/>
/// Radio.Send&lt;TService&gt;().
/// </summary>
public class ChannelInformation
{
    public Type ServiceType { get; }

    public Func<Channel, object> NewBroker { get; }

    public Func<Channel> NewChannel { get; }

    public Func<IUnorderedMap, Channel> NewChannel2 { get; }

    public int MaxLinks { get; private set; }

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

    public ChannelInformation(Type serviceType, Func<Channel, object> newBroker, Func<Channel> newChannel, Func<IUnorderedMap, Channel> newChannel2, int maxLinks)
    {
        this.ServiceType = serviceType;
        this.NewBroker = newBroker;
        this.NewChannel = newChannel;
        this.NewChannel2 = newChannel2;
        this.MaxLinks = maxLinks;
    }
}
