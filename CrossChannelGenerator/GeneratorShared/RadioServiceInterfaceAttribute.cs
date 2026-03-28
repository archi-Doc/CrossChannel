// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace CrossChannel;

public sealed class RadioServiceAttributeMock
{
    public static readonly string SimpleName = "RadioService";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = "CrossChannel." + StandardName;

    public RadioServiceAttributeMock(Location location)
    {
        this.Location = location;
    }

    public Location Location { get; }

    public int MaxLinks { get; set; } = int.MaxValue;

    public bool AutoRegisterRadioServiceAndSender { get; set; } = true;

    public static RadioServiceAttributeMock FromArray(Location location, object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new RadioServiceAttributeMock(location);

        object? val;
        val = VisceralHelper.GetValue(-1, nameof(MaxLinks), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.MaxLinks = (int)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(AutoRegisterRadioServiceAndSender), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.AutoRegisterRadioServiceAndSender = (bool)val;
        }

        return attribute;
    }
}
