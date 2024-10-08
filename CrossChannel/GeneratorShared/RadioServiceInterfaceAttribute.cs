﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// Represents an attribute added to the interface used as a Radio service.<br/>
/// The requirements are to add the <see cref="RadioServiceInterfaceAttribute" /> and to derive from the <see cref="IRadioService" />.<br/>
/// The return type of the interface function must be either <see cref="void"/>, <see cref="Task"/>, <see cref="RadioResult{T}"/>, <see cref="Task{T}"/>(where TResult is <see cref="RadioResult{T}"/>).
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

    /// <summary>
    /// Gets or sets the maximum number of links of the channel (default is <see cref="int.MaxValue"/>).
    /// </summary>
    public int MaxLinks { get; set; } = int.MaxValue;
}
