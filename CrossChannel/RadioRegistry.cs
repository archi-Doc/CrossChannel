// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrossChannel.Internal;

namespace CrossChannel;

public static class RadioRegistry
{
    // private static ConcurrentDictionary<Type, ChannelInformation> typeToInformation = new();
    private static ThreadsafeTypeKeyHashtable<ChannelInformation> typeToInformation = new();

    public static void Register(ChannelInformation information)
    {
        // typeToInformation.TryAdd(information.MachineType, information);
        typeToInformation.TryAdd(information.ServiceType, information);
    }

    public static ChannelInformation Get<TService>()
    {
        if (typeToInformation.TryGetValue(typeof(TService), out var information))
        {
            return information;
        }
        else
        {
            throw new InvalidOperationException($"ChannelInformation for type {typeof(TService).FullName} has not been registered.");
        }
    }
}
