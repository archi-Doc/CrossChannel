// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Globalization;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1611

namespace CrossChannel.Generator;

[Flags]
public enum CrossChannelObjectFlags
{
    Configured = 1 << 0,
    RelationConfigured = 1 << 1,
    Checked = 1 << 2,

    RadioService = 1 << 10, // RadioService
}

public partial class CrossChannelObject : VisceralObjectBase<CrossChannelObject>
{
    public CrossChannelObject()
    {
    }

    public new CrossChannelBody Body => (CrossChannelBody)((VisceralObjectBase<CrossChannelObject>)this).Body;

    public CrossChannelObjectFlags ObjectFlags { get; private set; }

    public RadioServiceAttributeMock? RadioServiceAttribute { get; private set; }

    public List<CrossChannelObject>? Children { get; private set; } // The opposite of ContainingObject

    // public VisceralIdentifier Identifier { get; private set; } = VisceralIdentifier.Default;

    public string BrokerClassName { get; set; } = string.Empty;

    public List<ServiceMethod>? Methods { get; private set; }

    /// <summary>
    /// Gets the global:: qualified name of the type, as referenced by the generated code.
    /// </summary>
    public string QualifiedName => this.qualifiedName ??= this.symbol!.ToDisplayString(ServiceMethod.QualifiedFormat);

    /// <summary>
    /// Gets a value indicating whether the type hosts nested services, so that its partial declaration and initializer are generated.<br/>
    /// Every type which hosts a service is partial (CCG001 aborts the generation otherwise).
    /// </summary>
    public bool IsContainer => this.Children is { Count: > 0 };

    private string? qualifiedName;

    public void Configure()
    {
        if (this.ObjectFlags.HasFlag(CrossChannelObjectFlags.Configured))
        {
            return;
        }

        this.ObjectFlags |= CrossChannelObjectFlags.Configured;

        foreach (var x in this.AllAttributes)
        {
            if (x.FullName == RadioServiceAttributeMock.FullName)
            {// [RadioService]
                this.RadioServiceAttribute = RadioServiceAttributeMock.FromArray(x.ConstructorArguments, x.NamedArguments);
                this.ObjectFlags |= CrossChannelObjectFlags.RadioService;
            }
        }

        // Generic type is not supported.
        /*if (this.Generics_Kind != VisceralGenericsKind.NotGeneric)
        {
            this.Body.AddDiagnostic(CrossChannelBody.Error_GenericType, this.Location);
            return;
        }*/

        // Used keywords
        // this.Identifier = new VisceralIdentifier("__gen_cc_identifier__");

        // Methods
    }

    public void ConfigureRelation()
    {// Create an object tree.
        if (this.ObjectFlags.HasFlag(CrossChannelObjectFlags.RelationConfigured))
        {
            return;
        }

        this.ObjectFlags |= CrossChannelObjectFlags.RelationConfigured;

        if (!this.Kind.IsType())
        {// Not type
            return;
        }

        var originalDefinition = this.OriginalDefinition;
        if (originalDefinition == null)
        {
            return;
        }
        else if (originalDefinition != this)
        {
            originalDefinition.ConfigureRelation();
        }

        if (originalDefinition.ContainingObject == null)
        {// Root object
            List<CrossChannelObject>? list;
            if (!this.Body.Namespaces.TryGetValue(this.Namespace, out list))
            {// Create a new namespace.
                list = new();
                this.Body.Namespaces[this.Namespace] = list;
            }

            if (!list.Contains(originalDefinition))
            {
                list.Add(originalDefinition);
            }
        }
        else
        {// Child object
            var parent = originalDefinition.ContainingObject;
            parent.ConfigureRelation();
            if (parent.Children == null)
            {
                parent.Children = new();
            }

            if (!parent.Children.Contains(originalDefinition))
            {
                parent.Children.Add(originalDefinition);
            }
        }
    }

    public void Check()
    {
        if (this.ObjectFlags.HasFlag(CrossChannelObjectFlags.Checked))
        {
            return;
        }

        this.ObjectFlags |= CrossChannelObjectFlags.Checked;

        if (this.ObjectFlags.HasFlag(CrossChannelObjectFlags.RadioService) &&
            this.symbol is INamedTypeSymbol serviceSymbol)
        {// [RadioService]
            for (var type = serviceSymbol; type is not null; type = type.ContainingType)
            {
                if (type.IsGenericType)
                {
                    this.Body.ReportDiagnostic(CrossChannelBody.Error_UnsupportedMember, this.Location);
                    return;
                }
                else if (type.IsFileLocal)
                {// The generated code lives in another file, so it cannot reference a file-local type.
                    this.Body.ReportDiagnostic(CrossChannelBody.Error_FileLocal, this.Location, type.Name);
                    return;
                }
            }

            // Must be derived from IRadioService
            if (!this.AllInterfaces.Any(x => x == IRadioServiceMock.FullName))
            {
                this.Body.AddDiagnostic(CrossChannelBody.Error_NotRadioService, this.Location);
                return;
            }

            // Parent class also needs to be a partial class.
            var parent = this.ContainingObject;
            while (parent != null)
            {
                if (!parent.IsPartial)
                {
                    this.Body.ReportDiagnostic(CrossChannelBody.Error_NotPartialParent, parent.Location, parent.FullName);
                }

                parent = parent.ContainingObject;
            }

            this.BrokerClassName = $"__{this.SimpleName}_Broker_{(uint)FarmHash.Hash64(this.FullName):x8}__";

            // The members are read from the symbols: Arc.Visceral skips the methods of any namespace named System (e.g. IDisposable).
            foreach (var type in serviceSymbol.AllInterfaces.Prepend(serviceSymbol))
            {
                foreach (var member in type.GetMembers())
                {
                    if (member is IMethodSymbol method)
                    {
                        if (ServiceMethod.Create(this, method) is { } serviceMethod)
                        {
                            this.Methods ??= new();
                            this.Methods.Add(serviceMethod);
                        }
                    }
                    else if (member is IPropertySymbol or IEventSymbol)
                    {
                        this.Body.ReportDiagnostic(CrossChannelBody.Error_UnsupportedMember, this.GetLocation(member));
                    }
                }
            }
        }
    }

    public static void GenerateInitializer(ScopingStringBuilder ssb, List<CrossChannelObject> list)
    {
        ssb.AppendLine(CrossChannelBody.ObsoleteContext);
        using (var m = ssb.ScopeBrace($"internal static void {CrossChannelBody.InitializerName}()"))
        {
            foreach (var x in list)
            {// Register services.
                x.GenerateRegister(ssb);
            }

            foreach (var x in list)
            {// Children
                if (x.IsContainer)
                {
                    ssb.AppendLine($"{x.QualifiedName}.{CrossChannelBody.InitializerName}();");
                }
            }
        }
    }

    /// <summary>
    /// Gets the source location of a member, or the location of this type if the member is declared in metadata.
    /// </summary>
    /// <param name="member">The member of this type.</param>
    /// <returns>The location to report a diagnostic at.</returns>
    internal Location GetLocation(ISymbol member)
        => member.Locations.FirstOrDefault(x => x.IsInSource) ?? this.Location;

    /// <summary>
    /// Generates the broker and, for a type which hosts nested services, its partial declaration.
    /// </summary>
    internal void GenerateObject(ScopingStringBuilder ssb)
    {
        if (this.RadioServiceAttribute is null && !this.IsContainer)
        {
            return;
        }

        /*else if (this.IsAbstractOrInterface)
        {
            return;
        }*/

        this.GenerateOutObject(ssb);
        this.GenerateContainer(ssb);
    }

    /// <summary>
    /// Out-object code generation.
    /// </summary>
    internal void GenerateOutObject(ScopingStringBuilder ssb)
    {
        if (this.RadioServiceAttribute is not null)
        {
            this.GenerateBrokerClass(ssb);
        }
    }

    /// <summary>
    /// Generates the partial declaration which hosts the brokers of the nested services and their initializer.
    /// </summary>
    internal void GenerateContainer(ScopingStringBuilder ssb)
    {
        if (this.Children is not { Count: > 0 } children)
        {
            return;
        }

        using (var cls = ssb.ScopeBrace($"{this.AccessibilityName} partial {this.KindName} {this.symbol!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}"))
        {// Generate children and loader.
            var isFirst = true;
            foreach (var x in children)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    ssb.AppendLine();
                }

                x.GenerateObject(ssb);
            }

            GenerateInitializer(ssb, children);
        }
    }

    /// <summary>
    /// Generate the registration code.
    /// </summary>
    internal void GenerateRegister(ScopingStringBuilder ssb)
    {
        if (this.RadioServiceAttribute is null)
        {// Container
            return;
        }

        var serviceName = this.QualifiedName;
        var maxLinks = this.RadioServiceAttribute.MaxLinks.ToString(CultureInfo.InvariantCulture);
        var autoRegisterServiceAndSender = this.RadioServiceAttribute.AutoRegisterServiceAndSender ? "true" : "false";
        ssb.AppendLine($"global::CrossChannel.RadioServiceRegistry.Register(new(typeof({serviceName}), static x => new {this.BrokerClassName}(x), static () => new global::CrossChannel.Channel<{serviceName}>(), {maxLinks}, {autoRegisterServiceAndSender}));");
    }
}
