// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class RadioServiceInterfaceAttribute : Attribute
{
    public RadioServiceInterfaceAttribute()
    {
    }

    public bool RequireUiThread { get; set; }
}
