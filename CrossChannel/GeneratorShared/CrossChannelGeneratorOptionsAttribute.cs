// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// Configures the CrossChannel source generator.<br/>
/// Place it on any interface of the project; at most one per project takes effect.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class CrossChannelGeneratorOptionsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CrossChannelGeneratorOptionsAttribute"/> class.
    /// </summary>
    public CrossChannelGeneratorOptionsAttribute()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether debugger attachment is requested. Reserved; currently ignored.
    /// </summary>
    public bool AttachDebugger { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether source is written to an existing "Generated" folder
    /// beside the annotated file instead of added to the compilation in memory (default is false).
    /// If the folder does not exist, source is added in memory. Include disk output in compilation when using this option.
    /// </summary>
    public bool GenerateToFile { get; set; } = false;
}
