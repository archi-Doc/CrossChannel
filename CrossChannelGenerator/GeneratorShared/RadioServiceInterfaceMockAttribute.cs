// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class RadioServiceInterfaceMockAttribute : Attribute
{
    public static readonly string SimpleName = "RadioServiceInterface";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = "CrossChannel." + StandardName;

    public RadioServiceInterfaceMockAttribute()
    {
    }

    // public bool RequireUiThread { get; set; }
}
