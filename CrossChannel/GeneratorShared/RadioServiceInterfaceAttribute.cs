// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// Represents an attribute added to the interface used as a Radio service.<br/>
/// The requirements are to add the <see cref="RadioServiceInterfaceAttribute" /> and to derive from the <see cref="IRadioService" />.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class RadioServiceInterfaceAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioServiceInterfaceAttribute"/> class.
    /// </summary>
    public RadioServiceInterfaceAttribute()
    {
    }

    // public bool RequireUiThread { get; set; }
}
