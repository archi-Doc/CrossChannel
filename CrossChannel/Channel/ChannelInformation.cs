// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

public record ChannelInformation(Type ServiceType, Func<object, object> Constructor);
