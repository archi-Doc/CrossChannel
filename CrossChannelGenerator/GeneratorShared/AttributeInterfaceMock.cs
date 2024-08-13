// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CrossChannel.Generator;

public static class AttributeHelper
{
    public static object? GetValue(int constructorIndex, string? name, object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        if (constructorIndex >= 0 && constructorIndex < constructorArguments.Length)
        {// Constructor Argument.
            return constructorArguments[constructorIndex];
        }
        else if (name != null)
        {// Named Argument.
            var pair = namedArguments.FirstOrDefault(x => x.Key == name);
            if (pair.Equals(default(KeyValuePair<string, object?>)))
            {
                return null;
            }

            return pair.Value;
        }
        else
        {
            return null;
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class CrossChannelGeneratorOptionAttributeMock : Attribute
{
    public static readonly string SimpleName = "CrossChannelGeneratorOption";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = "CrossChannel." + StandardName;

    public bool AttachDebugger { get; set; } = false;

    public bool GenerateToFile { get; set; } = false;

    public string? CustomNamespace { get; set; }

    public bool UseModuleInitializer { get; set; } = true;

    public CrossChannelGeneratorOptionAttributeMock()
    {
    }

    public static CrossChannelGeneratorOptionAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new CrossChannelGeneratorOptionAttributeMock();
        object? val;

        val = AttributeHelper.GetValue(-1, nameof(AttachDebugger), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.AttachDebugger = (bool)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(GenerateToFile), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.GenerateToFile = (bool)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(CustomNamespace), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.CustomNamespace = (string)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(UseModuleInitializer), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.UseModuleInitializer = (bool)val;
        }

        return attribute;
    }
}
