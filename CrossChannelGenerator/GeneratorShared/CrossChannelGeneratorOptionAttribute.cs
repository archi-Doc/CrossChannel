// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Visceral;

namespace CrossChannel.Generator;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class CrossChannelGeneratorOptionAttributeMock : Attribute
{
    public static readonly string SimpleName = "CrossChannelGeneratorOption";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = "CrossChannel." + StandardName;

    public bool AttachDebugger { get; set; } = false;

    public bool GenerateToFile { get; set; } = false;

    public CrossChannelGeneratorOptionAttributeMock()
    {
    }

    public static CrossChannelGeneratorOptionAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new CrossChannelGeneratorOptionAttributeMock();
        object? val;

        val = VisceralHelper.GetValue(-1, nameof(AttachDebugger), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.AttachDebugger = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(GenerateToFile), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.GenerateToFile = (bool)val;
        }

        return attribute;
    }
}
