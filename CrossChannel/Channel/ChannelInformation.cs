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

    public Func<object, object> NewBroker { get; }

    public Func<object> NewChannel { get; }

    public Func<IUnorderedMap, object> NewChannel2 { get; }

    public int MaxChannels { get; private set; }

    public object EmptyChannel { get; }

    public ChannelInformation(Type serviceType, Func<object, object> newBroker, Func<object> newChannel, Func<IUnorderedMap, object> newChannel2, bool singleChannel)
    {
        this.ServiceType = serviceType;
        this.NewBroker = newBroker;
        this.NewChannel = newChannel;
        this.NewChannel2 = newChannel2;
        this.MaxChannels = singleChannel ? 1 : int.MaxValue;

        var empty = this.NewChannel();
        empty.MaxChannels = 0;
        this.EmptyChannel = empty;
    }
}
