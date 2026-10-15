// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;

namespace CrossChannel.Generator;

#pragma warning disable SA1602 // Enumeration items should be documented

public class ServiceMethod
{
    public const string RadioResultName = "CrossChannel.RadioResult<T>";
    public const string TaskName = "System.Threading.Tasks.Task";
    public const string GenericTaskName = "System.Threading.Tasks.Task<TResult>";
    public const string CancellationTokenName = "System.Threading.CancellationToken";

    /// <summary>
    /// The format of the type names emitted in the generated code: global:: qualified, keywords escaped, and nullable annotations kept.
    /// </summary>
    public static readonly SymbolDisplayFormat QualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public enum MethodReturnKind
    {
        Other,
        Void,
        RadioResult,
        Task,
        TaskRadioResult,
    }

    public static ServiceMethod? Create(CrossChannelObject serviceObject, IMethodSymbol symbol)
    {
        if (symbol.AssociatedSymbol is not null)
        {// Property and event accessors (the property or event itself is reported).
            return null;
        }
        else if (symbol.IsStatic)
        {
            if (!symbol.IsAbstract)
            {// Static helpers keep their implementation (static abstract members are reported below).
                return null;
            }
        }
        else if (symbol.MethodKind != MethodKind.Ordinary || !(symbol.IsAbstract || symbol.IsVirtual))
        {// Explicit implementations, reabstractions, and private or sealed methods are not implemented by the broker.
            return null;
        }

        // A protected member cannot be called through another instance, so a protected default method keeps its implementation.
        var isProtected = symbol.DeclaredAccessibility is Accessibility.Protected or Accessibility.ProtectedAndInternal;
        if (isProtected && !symbol.IsAbstract)
        {
            return null;
        }

        var location = serviceObject.GetLocation(symbol);
        if (symbol.IsStatic || isProtected || symbol.IsGenericMethod || symbol.ReturnsByRef || symbol.ReturnsByRefReadonly ||
            symbol.Parameters.Any(x => x.RefKind != RefKind.None))
        {
            serviceObject.Body.ReportDiagnostic(CrossChannelBody.Error_UnsupportedMember, location);
            return null;
        }

        // The kind is detected from the symbol, since Arc.Visceral drops type arguments it cannot wrap (delegates, dynamic).
        var returnKind = GetReturnKind(symbol.ReturnType, out var resultType);
        if (returnKind == MethodReturnKind.Other)
        {
            serviceObject.Body.ReportDiagnostic(CrossChannelBody.Error_MethodReturnType, location);
            return null;
        }

        if (serviceObject.Body.Abort)
        {
            return null;
        }

        return new ServiceMethod(symbol, returnKind, resultType);
    }

    public ServiceMethod(IMethodSymbol symbol, MethodReturnKind returnKind, ITypeSymbol? resultType)
    {
        this.symbol = symbol;

        // An explicit interface implementation has to be qualified with the interface which declares the method,
        // which is not necessarily the service interface itself (the method may be inherited from a base interface).
        this.DeclaringName = symbol.ContainingType.ToDisplayString(QualifiedFormat);

        this.ReturnKind = returnKind;

        // The generated broker implements the interface explicitly, so the nullable annotations have to match exactly.
        var returnType = symbol.ReturnType;
        this.ReturnName = returnType.ToDisplayString(QualifiedFormat);
        this.IsReturnNullable = returnType.NullableAnnotation == NullableAnnotation.Annotated ||
            symbol.GetReturnTypeAttributes().Any(static x => x.AttributeClass is { Name: "MaybeNullAttribute" } c && c.ContainingNamespace.ToDisplayString() == "System.Diagnostics.CodeAnalysis");
        this.RequiresUnsafe = symbol.Parameters.Any(static x => IsUnsafeType(x.Type));
        this.ElementName = this.IsReturnNullable ? returnType.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(QualifiedFormat) : this.ReturnName;
        if (resultType is not null)
        {
            this.ResultName = resultType.ToDisplayString(QualifiedFormat);
        }

        // this.CancellationTokenIndex = this.method.Method_Parameters.IndexOf(CancellationTokenName);
    }

    public string SimpleName => this.symbol.Name;

    /// <summary>
    /// Gets the name of the interface which declares the method (used to qualify the explicit interface implementation).
    /// </summary>
    public string DeclaringName { get; }

    // public WithNullable<CrossChannelObject>? ReturnObject { get; internal set; }

    /// <summary>
    /// Gets the full name of the return type, including the nullable annotations (e.g. global::CrossChannel.RadioResult&lt;string?&gt;).
    /// </summary>
    public string ReturnName { get; }

    /// <summary>
    /// Gets the full name of the return type without the top-level nullable annotation (used for the local task variables).
    /// </summary>
    public string ElementName { get; }

    /// <summary>
    /// Gets a value indicating whether the return type is nullable (e.g. Task? or [return: MaybeNull]), so a receiver may return null.
    /// </summary>
    public bool IsReturnNullable { get; }

    /// <summary>
    /// Gets a value indicating whether a parameter has a pointer or function pointer type, so the broker needs an unsafe context.
    /// </summary>
    public bool RequiresUnsafe { get; }

    public MethodReturnKind ReturnKind { get; }

    /// <summary>
    /// Gets the full name of the result type T, including the nullable annotations (e.g. string?).
    /// </summary>
    public string ResultName { get; } = string.Empty;

    // public int CancellationTokenIndex { get; private set; }

    private readonly IMethodSymbol symbol;

    public string GetParameterDeclarations()
    {// int a1, string a2
        var parameters = this.symbol.Parameters;
        var sb = new StringBuilder();
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i != 0)
            {
                sb.Append(", ");
            }

            sb.Append(parameters[i].Type.ToDisplayString(QualifiedFormat));
            sb.Append(" a");
            sb.Append(i + 1);
        }

        return sb.ToString();
    }

    public string GetParameterNames()
    {// a1, a2
        var parameters = this.symbol.Parameters;
        var length = parameters.Length;
        if (length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            if (i != 0)
            {
                sb.Append(", ");
            }

            var type = parameters[i].Type;
            if (type.TypeKind == TypeKind.Dynamic)
            {// A dynamic argument would bind the call at runtime.
                sb.Append(type.NullableAnnotation == NullableAnnotation.Annotated ? "(object?)" : "(object)");
            }

            sb.Append('a');
            sb.Append(i + 1);
        }

        return sb.ToString();
    }

    private static bool IsUnsafeType(ITypeSymbol type)
        => type switch
        {
            IPointerTypeSymbol or IFunctionPointerTypeSymbol => true,
            IArrayTypeSymbol array => IsUnsafeType(array.ElementType),
            _ => false,
        };

    private static MethodReturnKind GetReturnKind(ITypeSymbol returnType, out ITypeSymbol? resultType)
    {// void, RadioResult<T>, Task, Task<RadioResult<T>> (T: the result type)
        resultType = null;
        if (returnType.SpecialType == SpecialType.System_Void)
        {
            return MethodReturnKind.Void;
        }

        if (returnType is not INamedTypeSymbol namedType)
        {// dynamic, arrays, pointers
            return MethodReturnKind.Other;
        }

        var originalName = namedType.OriginalDefinition.ToDisplayString();
        if (originalName == RadioResultName)
        {
            resultType = namedType.TypeArguments[0];
        }
        else if (originalName == TaskName)
        {
            return MethodReturnKind.Task;
        }
        else if (originalName == GenericTaskName &&
            namedType.TypeArguments[0] is INamedTypeSymbol resultObject &&
            resultObject.OriginalDefinition.ToDisplayString() == RadioResultName)
        {
            resultType = resultObject.TypeArguments[0];
        }
        else
        {
            return MethodReturnKind.Other;
        }

        // dynamic is not supported: 'new(firstResult)' would bind at runtime and treat an array value as multiple results.
        if (resultType.TypeKind == TypeKind.Dynamic)
        {
            resultType = null;
            return MethodReturnKind.Other;
        }

        return originalName == RadioResultName ? MethodReturnKind.RadioResult : MethodReturnKind.TaskRadioResult;
    }
}
