// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Visceral;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1611

namespace CrossChannel.Generator;

public partial class CrossChannelObject
{
    internal void GenerateBrokerClass(ScopingStringBuilder ssb)
    {
        var accessModifier = this.ContainingObject is null ? "internal" : "private";
        using (ssb.ScopeBrace($"{accessModifier} class {this.ClassName}"))
        {
        }
    }
}
