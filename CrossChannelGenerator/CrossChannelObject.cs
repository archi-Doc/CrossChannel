﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1602 // Enumeration items should be documented

namespace CrossChannel.Generator;

public enum DeclarationCondition
{
    NotDeclared, // Not declared
    ImplicitlyDeclared, // declared (implicitly)
    ExplicitlyDeclared, // declared (explicitly interface)
}

[Flags]
public enum CrossChannelObjectFlag
{
    Configured = 1 << 0,
    RelationConfigured = 1 << 1,
    Checked = 1 << 2,

    RadioServiceInterface = 1 << 10, // RadioServiceInterface
}

public class CrossChannelObject : VisceralObjectBase<CrossChannelObject>
{
    public CrossChannelObject()
    {
    }

    public new CrossChannelBody Body => (CrossChannelBody)((VisceralObjectBase<CrossChannelObject>)this).Body;

    public CrossChannelObjectFlag ObjectFlag { get; private set; }

    public RadioServiceInterfaceAttributeMock? RadioServiceInterfaceAttribute { get; private set; }

    public List<CrossChannelObject>? Children { get; private set; } // The opposite of ContainingObject

    public List<CrossChannelObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

    // public VisceralIdentifier Identifier { get; private set; } = VisceralIdentifier.Default;

    public string ClassName { get; set; } = string.Empty;

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
        if (this.ObjectFlag.HasFlag(CrossChannelObjectFlag.Configured))
        {
            return;
        }

        this.ObjectFlag |= CrossChannelObjectFlag.Configured;

        foreach (var x in this.AllAttributes)
        {
            if (x.FullName == RadioServiceInterfaceAttributeMock.FullName)
            {// [RadioServiceInterface]
                this.RadioServiceInterfaceAttribute = RadioServiceInterfaceAttributeMock.FromArray(x.Location, x.ConstructorArguments, x.NamedArguments);
                this.ObjectFlag |= CrossChannelObjectFlag.RadioServiceInterface;
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
        if (this.ObjectFlag.HasFlag(CrossChannelObjectFlag.RelationConfigured))
        {
            return;
        }

        this.ObjectFlag |= CrossChannelObjectFlag.RelationConfigured;

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
        if (this.ObjectFlag.HasFlag(CrossChannelObjectFlag.Checked))
        {
            return;
        }

        this.ObjectFlag |= CrossChannelObjectFlag.Checked;

        if (this.ObjectFlag.HasFlag(CrossChannelObjectFlag.RadioServiceInterface))
        {// [RadioServiceInterface]
            // Must be derived from IRadioService
            if (!this.AllInterfaces.Any(x => x == IRadioService.FullName))
            {
                this.Body.AddDiagnostic(CrossChannelBody.Error_IRadioService, this.Location);
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

            this.ClassName = $"__{this.SimpleName}_Broker_{(uint)FarmHash.Hash64(this.FullName):x8}__";

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

    internal void GenerateFrontend(ScopingStringBuilder ssb)
    {
        using (var cls = ssb.ScopeBrace($"private class {this.ClassName} : {this.FullName}")) // {this.AccessibilityName}
        {
        }
    }
}
