// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

/// <summary>
/// Configures the CrossChannel source generator.<br/>
/// Place it on any interface of the project; at most one per project takes effect.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class CrossChannelGeneratorOptionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CrossChannelGeneratorOptionAttribute"/> class.
    /// </summary>
    public CrossChannelGeneratorOptionAttribute()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the generator attaches a debugger when it runs (default is <see langword="false"/>).<br/>
    /// For debugging the generator itself.
    /// </summary>
    public bool AttachDebugger { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the generated code is written to a "Generated" folder
    /// next to the annotated file instead of being added to the compilation in memory (default is <see langword="false"/>).<br/>
    /// For inspecting the generated code.
    /// </summary>
    public bool GenerateToFile { get; set; } = false;
}
