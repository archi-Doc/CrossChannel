// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;

namespace CrossChannel;

/// <summary>
/// Provides functionality to copy all fields (including private/readonly/backing fields) from one class instance to another.
/// </summary>
public static class GhostCopy
{
    /// <summary>
    /// Delegate for copying fields from one instance to another.
    /// </summary>
    /// <typeparam name="T">The class type to copy.</typeparam>
    /// <param name="from">The source instance.</param>
    /// <param name="to">The destination instance.</param>
    public delegate void CopyDelegate<T>(ref T from, ref T to)
        where T : class;

    // private static MethodInfo helperSetReadonly;
    private static MethodInfo setReadonlyMethod;
    private static ConcurrentDictionary<Type, MethodInfo> setReadonlyMethodCache = new();
    // private static MethodInfo unsafeAsRef;

    static GhostCopy()
    {
        // helperSetReadonly = typeof(GhostCopy).GetMethod(nameof(SetReadonlyFieldViaReflection), BindingFlags.Static | BindingFlags.NonPublic)!;
        setReadonlyMethod = typeof(GhostCopy).GetMethod(nameof(SetReadonlyField), BindingFlags.Static | BindingFlags.NonPublic)!;
        /*unsafeAsRef = typeof(Unsafe).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(Unsafe.AsRef)
            && m.IsGenericMethodDefinition
            && m.GetParameters().Length == 1
            && m.GetParameters()[0].ParameterType.IsByRef);*/
    }

    /// <summary>
    /// Copies all fields from one instance to another, including private/readonly/backing fields.
    /// </summary>
    /// <typeparam name="T">The class type to copy.</typeparam>
    /// <param name="from">The source instance.</param>
    /// <param name="to">The destination instance.</param>
    public static void Copy<T>(ref T from, ref T to)
        where T : class
    {
        CopyDelegateCache<T>.CopyDelegate(ref from, ref to);
    }

    /// <summary>
    /// Creates a delegate that copies all fields from one instance to another.
    /// </summary>
    /// <typeparam name="T">The class type to copy.</typeparam>
    /// <returns>A delegate that copies fields from one instance to another.</returns>
    public static CopyDelegate<T> CreateDelegate<T>()
        where T : class
    {
        var classType = typeof(T);
        var byref = classType.MakeByRefType();
        var source = Expression.Parameter(byref, "from");
        var destination = Expression.Parameter(byref, "to");
        var expressionList = new List<Expression>();

        // Do not copy properties, since the backing fields are copied directly.
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

        foreach (var field in classType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var sourceField = Expression.Field(source, field);
            var destinationField = Expression.Field(destination, field);
            if (field.IsInitOnly)
            {
                var method = setReadonlyMethodCache.GetOrAdd(field.FieldType, t => setReadonlyMethod.MakeGenericMethod(t));
                expressionList.Add(Expression.Call(method, destinationField, sourceField));

                /*var unsafeAsRefMethod = unsafeAsRef.MakeGenericMethod(field.FieldType);
                var callAsRef = Expression.Call(unsafeAsRefMethod, destinationField);
                expressionList.Add(Expression.Assign(callAsRef, sourceField));*/

                /*expressionList.Add(Expression.Call(
                    helperSetReadonly.MakeGenericMethod(field.FieldType),
                    Expression.Convert(destination, typeof(object)),
                    Expression.Constant(field, typeof(FieldInfo)),
                    Expression.Convert(sourceField, typeof(object))));*/
            }
            else
            {
                expressionList.Add(Expression.Assign(destinationField, sourceField));
            }
        }

        var body = Expression.Block(expressionList);
        var lambda = Expression.Lambda<CopyDelegate<T>>(body, source, destination);
        return lambda.CompileFast();
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

    private static void SetReadonlyField<T>(ref T target, T value)
    {
        Unsafe.AsRef<T>(ref target) = value;
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
