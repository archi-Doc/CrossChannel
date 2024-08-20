// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace CrossChannel;

/// <summary>
/// Represents information about a channel.<br/>
/// Constructor: new broker(channel).<br/>
/// Radio.Send&lt;TService&gt;().
/// </summary>
public record ChannelInformation(Type ServiceType, Func<object, object> NewBroker, Func<object> NewChannel, Func<IUnorderedMap, int, object> NewChannel2, bool SingleChannel);
