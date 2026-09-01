// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// Marks an interface as a radio service, so that the source generator emits a broker for it.<br/>
/// The interface must also derive from <see cref="IRadioService"/>, and the return type of each of its
/// methods must be <see cref="void"/>, <see cref="Task"/>, <see cref="RadioResult{T}"/>,
/// or <see cref="Task{TResult}"/> of <see cref="RadioResult{T}"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class RadioServiceAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioServiceAttribute"/> class.
    /// </summary>
    public RadioServiceAttribute()
    {
    }

    /// <summary>
    /// Gets or sets the maximum number of instances which can subscribe to a single channel (default is <see cref="int.MaxValue"/>).<br/>
    /// Opening a link beyond this limit returns <see langword="null"/>.
    /// </summary>
    public int MaxLinks { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ServiceCollectionExtensions.AddCrossChannel"/>
    /// registers the service interface and <see cref="ISender{TService}"/> in dependency injection (default is <see langword="true"/>).<br/>
    /// <see cref="IChannel{TService}"/> is registered regardless of this value.
    /// </summary>
    public bool AutoRegisterServiceAndSender { get; set; } = true;
}
