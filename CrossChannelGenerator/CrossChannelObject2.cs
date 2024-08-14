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
            ssb.AppendLine($"private readonly Channel<{this.LocalName}> channel;");
            using (ssb.ScopeBrace($"public {this.ClassName}(object channel)"))
            {
                ssb.AppendLine($"this.channel = (Channel<{this.LocalName}>)channel;");
            }

            if (this.Methods is not null)
            {
                foreach (var x in this.Methods)
                {
                    if (x.ReturnType == ServiceMethod.Type.Void)
                    {
                    }
                }
            }
        }
    }
}
