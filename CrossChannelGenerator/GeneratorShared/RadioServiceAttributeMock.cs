// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Visceral;

namespace CrossChannel;

public sealed class RadioServiceAttributeMock
{
    public static readonly string SimpleName = "RadioService";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = "CrossChannel." + StandardName;

    public int MaxLinks { get; set; } = int.MaxValue;

    public bool AutoRegisterServiceAndSender { get; set; } = true;

    public static RadioServiceAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new RadioServiceAttributeMock();

        object? val;
        val = VisceralHelper.GetValue(-1, nameof(MaxLinks), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.MaxLinks = (int)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(AutoRegisterServiceAndSender), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.AutoRegisterServiceAndSender = (bool)val;
        }

        return attribute;
    }
}
