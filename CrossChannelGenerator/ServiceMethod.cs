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

        var serviceMethod = new ServiceMethod(method);
        serviceMethod.ReturnType = returnType;
        serviceMethod.ResultObject = resultObject;

        return serviceMethod;
    }

    public ServiceMethod(CrossChannelObject method)
    {
        this.method = method;
    }

    public Location Location => this.method.Location;

    public string SimpleName => this.method.SimpleName;

    public string LocalName => this.method.LocalName;

    // public WithNullable<CrossChannelObject>? ReturnObject { get; internal set; }

    public string ParameterType { get; private set; } = string.Empty;

    public Type ReturnType { get; private set; }

    public CrossChannelObject? ResultObject { get; private set; }

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
            sb.Append(" ");
            sb.Append(CrossChannelBody.ArgumentName);
            sb.Append(i + 1);
        }

        return sb.ToString();
    }

    public string GetParameterNames(string name, int decrement)
    {// string.Empty, a1, (a1, a2)
        var parameters = this.method.Method_Parameters;
        var length = parameters.Length - decrement;
        if (length <= 0)
        {
            return string.Empty;
        }
        else if (length == 1)
        {
            return name + "1";
        }
        else
        {
            var sb = new StringBuilder();
            sb.Append("(");
            for (var i = 0; i < length; i++)
            {
                if (i != 0)
                {
                    sb.Append(", ");
                }

                sb.Append(name);
                sb.Append(i + 1);
            }

            sb.Append(")");
            return sb.ToString();
        }
    }
}
