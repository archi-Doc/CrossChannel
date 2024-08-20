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

    public Func<int, object> NewChannel { get; }

    public Func<int, IUnorderedMap, object> NewChannel2 { get; }

    public int MaxLinks { get; private set; }

    public object EmptyChannel { get; }

    public ChannelInformation(Type serviceType, Func<object, object> newBroker, Func<int, object> newChannel, Func<int, IUnorderedMap, object> newChannel2, bool singleLink)
    {
        this.ServiceType = serviceType;
        this.NewBroker = newBroker;
        this.NewChannel = newChannel;
        this.NewChannel2 = newChannel2;
        this.MaxLinks = singleLink ? 1 : int.MaxValue;

        var empty = this.NewChannel(0);
        this.EmptyChannel = empty;
    }
}
