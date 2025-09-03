// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq.Expressions;
using System.Reflection;

namespace CrossChannel;

public static class GhostCopy
{
    public delegate void CopyDelegate<T>(ref T from, ref T to)
        where T : class;

    public static void Copy<T>(ref T from, ref T to)
        where T : class
    {
        CopyDelegateCache<T>.CopyDelegate(ref from, ref to);
    }

    public static CopyDelegate<T> CreateDelegate<T>()
        where T : class
    {
        var classType = typeof(T);
        var byref = classType.MakeByRefType();
        var source = Expression.Parameter(byref, "from");
        var destination = Expression.Parameter(byref, "to");

        var expressionList = new List<Expression>();
        /*foreach (var property in classType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (property.GetIndexParameters().Length > 0)
            {// Exclude index parameters.
                continue;
            }

            var setter = property.GetSetMethod(true);
            var getter = property.GetGetMethod(true);
            if (getter is null || setter is null)
            {
                continue;
            }

            expressionList.Add(Expression.Call(
                destination,
                setter,
                Expression.Convert(
                    Expression.Call(source, getter),
                    property.PropertyType)));
        }*/

        MethodInfo? helperSetReadonly = default;
        foreach (var field in classType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var sourceField = Expression.Field(source, field);
            var destinationField = Expression.Field(destination, field);
            if (field.IsInitOnly)
            {
                helperSetReadonly ??= typeof(GhostCopy)
            .GetMethod(nameof(SetReadonlyFieldViaReflection), BindingFlags.Static | BindingFlags.NonPublic)!;

                expressionList.Add(Expression.Call(
                    helperSetReadonly.MakeGenericMethod(field.FieldType),
                    Expression.Convert(destination, typeof(object)),
                    Expression.Constant(field, typeof(FieldInfo)),
                    Expression.Convert(sourceField, typeof(object))));
            }
            else
            {
                expressionList.Add(Expression.Assign(destinationField, sourceField));
            }
        }

        var body = Expression.Block(expressionList);
        var lambda = Expression.Lambda<CopyDelegate<T>>(body, source, destination);
        return lambda.Compile();
    }

    private static void SetReadonlyFieldViaReflection<TField>(object target, FieldInfo field, object valueBoxed)
    {
        try
        {
            field.SetValue(target, (TField)valueBoxed);
        }
        catch
        {
        }
    }

    private static class CopyDelegateCache<T>
        where T : class
    {
        public static readonly CopyDelegate<T> CopyDelegate;

        static CopyDelegateCache()
        {
            CopyDelegate = CreateDelegate<T>();
        }
    }
}
