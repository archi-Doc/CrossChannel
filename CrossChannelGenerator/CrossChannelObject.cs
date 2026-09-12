// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1611

namespace CrossChannel.Generator;

public enum DeclarationCondition
{
    NotDeclared, // Not declared
    ImplicitlyDeclared, // declared (implicitly)
    ExplicitlyDeclared, // declared (explicitly interface)
}

[Flags]
public enum CrossChannelObjectFlags
{
    Configured = 1 << 0,
    RelationConfigured = 1 << 1,
    Checked = 1 << 2,
    InitializerGenerated = 1 << 3,

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

    public List<CrossChannelObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

    // public VisceralIdentifier Identifier { get; private set; } = VisceralIdentifier.Default;

    public string BrokerClassName { get; set; } = string.Empty;

    public List<ServiceMethod>? Methods { get; private set; }

    public Arc.Visceral.NullableAnnotation NullableAnnotationIfReferenceType
    {
        get
        {
            if (this.TypeObject?.Kind.IsReferenceType() == true)
            {
                if (this.symbol is IFieldSymbol fs)
                {
                    return (Arc.Visceral.NullableAnnotation)fs.NullableAnnotation;
                }
                else if (this.symbol is IPropertySymbol ps)
                {
                    return (Arc.Visceral.NullableAnnotation)ps.NullableAnnotation;
                }
            }

            return Arc.Visceral.NullableAnnotation.None;
        }
    }

    public string QuestionMarkIfReferenceType
    {
        get
        {
            if (this.Kind.IsReferenceType())
            {
                return "?";
            }
            else
            {
                return string.Empty;
            }
        }
    }

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
                this.RadioServiceAttribute = RadioServiceAttributeMock.FromArray(x.Location, x.ConstructorArguments, x.NamedArguments);
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

        if (originalDefinition.ConstructedObjects == null)
        {
            originalDefinition.ConstructedObjects = new();
        }

        if (!originalDefinition.ConstructedObjects.Contains(this))
        {
            originalDefinition.ConstructedObjects.Add(this);
        }
    }

    public void Check()
    {
        if (this.ObjectFlags.HasFlag(CrossChannelObjectFlags.Checked))
        {
            return;
        }

        this.ObjectFlags |= CrossChannelObjectFlags.Checked;

        if (this.ObjectFlags.HasFlag(CrossChannelObjectFlags.RadioService))
        {// [RadioService]
            this.GetRawInformation(out var rawSymbol, out _, out _);
            if (rawSymbol is INamedTypeSymbol serviceSymbol)
            {
                for (var type = serviceSymbol; type is not null; type = type.ContainingType)
                {
                    if (type.IsGenericType)
                    {
                        this.Body.ReportDiagnostic(CrossChannelBody.Error_UnsupportedMember, this.Location);
                        return;
                    }
                }

                foreach (var type in serviceSymbol.AllInterfaces.Prepend(serviceSymbol))
                {
                    foreach (var member in type.GetMembers())
                    {
                        if (member is IPropertySymbol or IEventSymbol)
                        {
                            this.Body.ReportDiagnostic(CrossChannelBody.Error_UnsupportedMember, member.Locations.FirstOrDefault() ?? this.Location);
                        }
                    }
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

            foreach (var x in this.GetMembers(VisceralTarget.Method))
            {
                AddMethod(this, x);
            }

            foreach (var @interface in this.AllInterfaceObjects)
            {
                foreach (var x in @interface.GetMembers(VisceralTarget.Method).Where(y => y.ContainingObject == @interface))
                {
                    AddMethod(this, x);
                }
            }
        }

        static void AddMethod(CrossChannelObject obj, CrossChannelObject method)
        {
            var serviceMethod = ServiceMethod.Create(obj, method);
            if (serviceMethod == null)
            {
                return;
            }

            obj.Methods ??= new();
            obj.Methods.Add(serviceMethod);
        }
    }

    public static void GenerateInitializer(ScopingStringBuilder ssb, CrossChannelObject? parent, List<CrossChannelObject> list)
    {
        if (parent?.Generics_Kind == VisceralGenericsKind.OpenGeneric)
        {
            return;
        }

        var list2 = list.SelectMany(x => x.ConstructedObjects).Where(x => x.RadioServiceAttribute != null).ToArray();

        if (parent != null)
        {
            parent.ObjectFlags |= CrossChannelObjectFlags.InitializerGenerated;
        }

        using (var m = ssb.ScopeBrace($"internal static void {CrossChannelBody.InitializerName}()"))
        {
            foreach (var x in list2)
            {
                if (x.RadioServiceAttribute == null)
                {
                    continue;
                }

                if (x.Generics_Kind != VisceralGenericsKind.OpenGeneric)
                {// Register fixed types.
                    x.GenerateRegister(ssb);
                }
            }

            foreach (var x in list.Where(a => a.ObjectFlags.HasFlag(CrossChannelObjectFlags.InitializerGenerated)))
            {// Children
                ssb.AppendLine($"{x.FullName}.{CrossChannelBody.InitializerName}();");
            }
        }
    }

    /// <summary>
    /// Generates the broker, nested declarations, and registration initializer.
    /// </summary>
    internal void GenerateObject(ScopingStringBuilder ssb)
    {
        if (this.ConstructedObjects == null)
        {
            return;
        }

        /*else if (this.IsAbstractOrInterface)
        {
            return;
        }*/

        this.GenerateOutObject(ssb);
        if (!this.IsPartial)
        {
            return;
        }

        using (var cls = ssb.ScopeBrace($"{this.AccessibilityName} partial {this.KindName} {this.LocalName}"))
        {
            if (this.RadioServiceAttribute is not null)
            {
                this.GenerateInObject(ssb);
            }

            if (this.Children?.Count > 0)
            {// Generate children and loader.
                var isFirst = true;
                foreach (var x in this.Children)
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

                GenerateInitializer(ssb, this, this.Children);
            }
        }
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
    /// In-object code generation.
    /// </summary>
    internal void GenerateInObject(ScopingStringBuilder ssb)
    {
        if (this.Generics_Kind == VisceralGenericsKind.OpenGeneric)
        {
            this.GenerateRegister(ssb);
        }
    }

    /// <summary>
    /// Generate the registration code.
    /// </summary>
    internal void GenerateRegister(ScopingStringBuilder ssb)
    {
        if (this.Generics_Kind == VisceralGenericsKind.OpenGeneric ||
            this.RadioServiceAttribute is null)
        {
            return;
        }

        var autoRegisterServiceAndSender = this.RadioServiceAttribute.AutoRegisterServiceAndSender ? "true" : "false";
        ssb.AppendLine($"RadioServiceRegistry.Register(new(typeof({this.FullName}), static x => new {this.BrokerClassName}(x), static () => new Channel<{this.FullName}>(), {this.RadioServiceAttribute.MaxLinks.ToString()}, {autoRegisterServiceAndSender}));");
    }
}
