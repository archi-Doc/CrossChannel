// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class CrossChannelGeneratorOptionAttribute : Attribute
{
    public bool AttachDebugger { get; set; } = false;

    public bool GenerateToFile { get; set; } = false;

    public string? CustomNamespace { get; set; }

    public bool UseModuleInitializer { get; set; } = true;

    public CrossChannelGeneratorOptionAttribute()
    {
    }
}
