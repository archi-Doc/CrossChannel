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

    private static readonly MethodInfo SetReadonlyMethodInfo = typeof(GhostCopy).GetMethod(nameof(SetReadonlyField), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly ConcurrentDictionary<Type, MethodInfo> SetReadonlyMethodCache = new();

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
        foreach (var field in classType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var sourceField = Expression.Field(source, field);
            var destinationField = Expression.Field(destination, field);
            if (field.IsInitOnly)
            {
                var method = SetReadonlyMethodCache.GetOrAdd(field.FieldType, static t => SetReadonlyMethodInfo.MakeGenericMethod(t));
                expressionList.Add(Expression.Call(method, destinationField, sourceField));
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

    private static void SetReadonlyField<T>(ref T target, T value)
    {
        Unsafe.AsRef<T>(ref target) = value;
    }

    private static class CopyDelegateCache<T>
        where T : class
    {
        public static readonly CopyDelegate<T> CopyDelegate = CreateDelegate<T>();
    }
}
