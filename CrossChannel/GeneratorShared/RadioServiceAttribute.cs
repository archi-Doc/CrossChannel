// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// Marks an interface as a radio service, so that the source generator emits a broker for it.<br/>
/// The interface must also derive from <see cref="IRadioService"/>, the return type of each of its abstract or virtual
/// methods (including inherited ones) must be <see langword="void"/>, <see cref="Task"/>, <see cref="RadioResult{T}"/>,
/// or <see cref="Task{TResult}"/> of <see cref="RadioResult{T}"/>, and the types containing it must be partial.<br/>
/// Not supported: generic or file-local interfaces (or ones nested in such a type), properties, events,
/// generic, static abstract, or protected abstract methods, ref returns, and ref, out, or in parameters.
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
    /// When the limit is reached, dead weak links are removed first; if the channel is still full, opening a link returns <see langword="null"/>.
    /// A value of 0 or less accepts no subscriptions.
    /// </summary>
    public int MaxLinks { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="CrossChannelServiceCollectionExtensions.AddCrossChannel"/>
    /// registers the service interface and <see cref="ISender{TService}"/> in dependency injection (default is <see langword="true"/>).<br/>
    /// <see cref="IChannel{TService}"/> is registered regardless of this value.
    /// </summary>
    public bool AutoRegisterServiceAndSender { get; set; } = true;
}
