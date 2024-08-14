// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.CodeAnalysis;

namespace CrossChannel;

public sealed class RadioServiceInterfaceAttributeMock
{
    public static readonly string SimpleName = "RadioServiceInterface";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = "CrossChannel." + StandardName;

    public RadioServiceInterfaceAttributeMock(Location location)
    {
        this.Location = location;
    }

    public Location Location { get; }

    // public bool RequireUiThread { get; set; }

    public static RadioServiceInterfaceAttributeMock FromArray(Location location, object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new RadioServiceInterfaceAttributeMock(location);

        /*object? val;
        val = AttributeHelper.GetValue(-1, nameof(ImplicitKeyAsName), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ImplicitKeyAsName = (bool)val;
        }*/

        return attribute;
    }
}
