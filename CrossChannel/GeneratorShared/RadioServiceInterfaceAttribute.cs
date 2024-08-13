// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class RadioServiceInterfaceAttribute : Attribute
{
    public RadioServiceInterfaceAttribute()
    {
    }

    // public bool RequireUiThread { get; set; }
}
