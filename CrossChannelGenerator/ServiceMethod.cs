// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace CrossChannel.Generator;

#pragma warning disable SA1602 // Enumeration items should be documented

public class ServiceMethod
{
    public const string VoidName = "void";
    public const string RadioResultName = "CrossChannel.RadioResult<T>";
    public const string TaskName = "System.Threading.Tasks.Task";
    public const string TaskRadioResultName = "System.Threading.Tasks.Task<TResult>";
    public const string CancellationTokenName = "System.Threading.CancellationToken";

    public enum Type
    {
        Other,
        Void,
        RadioResult,
        Task,
        TaskRadioResult,
    }

    public static ServiceMethod? Create(CrossChannelObject obj, CrossChannelObject method)
    {
        var returnObject = method.Method_ReturnObject;
        if (returnObject == null)
        {
            return null;
        }

        var returnType = Type.Other;
        CrossChannelObject? resultObject = null;
        if (returnObject.FullName == VoidName)
        {
            returnType = Type.Void;
        }
        else
        {
            var originalName = returnObject.OriginalDefinition?.FullName ?? string.Empty;
            if (originalName == RadioResultName)
            {
                returnType = Type.RadioResult;
                resultObject = returnObject.Generics_Arguments[0];
            }
            else if (originalName == TaskName)
            {
                returnType = Type.Task;
            }
            else if (originalName == TaskRadioResultName)
            {
                resultObject = returnObject.Generics_Arguments[0];
                if (resultObject.OriginalDefinition?.FullName == RadioResultName)
                {
                    returnType = Type.TaskRadioResult;
                    resultObject = resultObject.Generics_Arguments[0];
                }
            }
        }

        if (returnType == Type.Other)
        {
            method.Body.ReportDiagnostic(CrossChannelBody.Error_MethodReturnType, method.Location);
            return null;
        }

        if (method.Body.Abort)
        {
            return null;
        }

        var serviceMethod = new ServiceMethod(obj, method, returnObject, returnType, resultObject);
        return serviceMethod;
    }

    public ServiceMethod(CrossChannelObject obj, CrossChannelObject method, CrossChannelObject returnObject, Type returnType, CrossChannelObject? resultObject)
    {
        this.method = method;

        // An explicit interface implementation has to be qualified with the interface which declares the method,
        // which is not necessarily the service interface itself (the method may be inherited from a base interface).
        var declaringObject = method.ContainingObject;
        this.DeclaringName = declaringObject is null || declaringObject == obj ? obj.LocalName : declaringObject.FullName;

        this.ReturnObject = returnObject;
        this.ReturnType = returnType;
        this.ResultObject = resultObject;

        // CrossChannelObject.FullName drops the nullable annotations, but the generated broker
        // implements the interface explicitly, so the annotations have to match exactly.
        this.method.GetRawInformation(out var symbol, out _, out _);
        if (symbol is IMethodSymbol ms)
        {
            this.ReturnName = this.method.Body.SymbolToFullName(ms.ReturnType, true);
            if (GetResultSymbol(ms.ReturnType, returnType) is { } resultSymbol)
            {
                this.ResultName = this.method.Body.SymbolToFullName(resultSymbol, true);
            }
        }
        else
        {
            this.ReturnName = returnObject.FullName;
            this.ResultName = resultObject?.FullName ?? string.Empty;
        }

        // this.CancellationTokenIndex = this.method.Method_Parameters.IndexOf(CancellationTokenName);

        static ITypeSymbol? GetResultSymbol(ITypeSymbol returnSymbol, Type returnType)
        {// RadioResult<T> -> T, Task<RadioResult<T>> -> T
            if (returnSymbol is not INamedTypeSymbol nts ||
                nts.TypeArguments.Length != 1)
            {
                return null;
            }

            if (returnType == Type.RadioResult)
            {
                return nts.TypeArguments[0];
            }
            else if (returnType == Type.TaskRadioResult)
            {
                return nts.TypeArguments[0] is INamedTypeSymbol inner && inner.TypeArguments.Length == 1 ?
                    inner.TypeArguments[0] : null;
            }

            return null;
        }
    }

    public Location Location => this.method.Location;

    public string SimpleName => this.method.SimpleName;

    public string LocalName => this.method.LocalName;

    /// <summary>
    /// Gets the name of the interface which declares the method (used to qualify the explicit interface implementation).
    /// </summary>
    public string DeclaringName { get; private set; }

    // public WithNullable<CrossChannelObject>? ReturnObject { get; internal set; }

    public string ParameterType { get; private set; } = string.Empty;

    public CrossChannelObject ReturnObject { get; private set; }

    /// <summary>
    /// Gets the full name of the return type, including the nullable annotations (e.g. CrossChannel.RadioResult&lt;string?&gt;).
    /// </summary>
    public string ReturnName { get; private set; }

    public Type ReturnType { get; private set; }

    public CrossChannelObject? ResultObject { get; private set; }

    /// <summary>
    /// Gets the full name of the result type T, including the nullable annotations (e.g. string?).
    /// </summary>
    public string ResultName { get; private set; } = string.Empty;

    // public int CancellationTokenIndex { get; private set; }

    private CrossChannelObject method;

    public string GetParameters()
    {// int a1, string a2
        var sb = new StringBuilder();
        for (var i = 0; i < this.method.Method_Parameters.Length; i++)
        {
            if (i != 0)
            {
                sb.Append(", ");
            }

            sb.Append(this.method.Method_Parameters[i]);
            sb.Append(" a");
            sb.Append(i + 1);
        }

        return sb.ToString();
    }

    public string GetParameterNames()
    {// a1, a2
        var parameters = this.method.Method_Parameters;
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

            sb.Append('a');
            sb.Append(i + 1);
        }

        return sb.ToString();
    }
}
