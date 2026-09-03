// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;

namespace CrossChannel;

/// <summary>
/// Provides functionality to copy all fields (including private/readonly/backing fields) from one class instance to another.
/// </summary>
/// <remarks>
/// When the runtime supports dynamic code (JIT), the copy is performed by a delegate compiled once per type.<br/>
/// Under Native AOT, where no code can be emitted, an equivalent reflection-based delegate is used instead.
/// </remarks>
public static class GhostCopy
{
    /// <summary>
    /// The members of the copied type which must survive trimming.
    /// </summary>
    private const DynamicallyAccessedMemberTypes CopiedMembers = DynamicallyAccessedMemberTypes.AllFields;

    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Delegate for copying fields from one instance to another.
    /// </summary>
    /// <typeparam name="T">The class type to copy.</typeparam>
    /// <param name="from">The source instance.</param>
    /// <param name="to">The destination instance.</param>
    public delegate void CopyDelegate<T>(ref T from, ref T to)
        where T : class;

    /// <summary>
    /// Copies all fields from one instance to another, including private/readonly/backing fields.
    /// </summary>
    /// <typeparam name="T">The class type to copy.</typeparam>
    /// <param name="from">The source instance.</param>
    /// <param name="to">The destination instance.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Copy<[DynamicallyAccessedMembers(CopiedMembers)] T>(ref T from, ref T to)
        where T : class
    {
        CopyDelegateCache<T>.CopyDelegate(ref from, ref to);
    }

    /// <summary>
    /// Creates a delegate that copies all fields from one instance to another.
    /// </summary>
    /// <remarks>The delegate is cached per type and shared with <see cref="Copy{T}(ref T, ref T)"/>.</remarks>
    /// <typeparam name="T">The class type to copy.</typeparam>
    /// <returns>A delegate that copies fields from one instance to another.</returns>
    public static CopyDelegate<T> CreateDelegate<[DynamicallyAccessedMembers(CopiedMembers)] T>()
        where T : class
        => CopyDelegateCache<T>.CopyDelegate;

    private static CopyDelegate<T> CreateDelegateCore<[DynamicallyAccessedMembers(CopiedMembers)] T>()
        where T : class
    {
        // Do not copy properties, since the backing fields are copied directly.
        var fields = GetFields(typeof(T));
        if (fields.Length == 0)
        {
            return static (ref T from, ref T to) => { };
        }

        return RuntimeFeature.IsDynamicCodeSupported ?
            CreateCompiledDelegate<T>(fields) :
            CreateReflectionDelegate<T>(fields);
    }

    private static FieldInfo[] GetFields([DynamicallyAccessedMembers(CopiedMembers)] Type type)
    {
        var declaredFields = type.GetFields(InstanceFields);
        if (type.BaseType is null || type.BaseType == typeof(object))
        {
            return declaredFields;
        }

        var fields = new List<FieldInfo>(declaredFields);
        for (var current = type.BaseType; current is not null && current != typeof(object); current = current.BaseType)
        {
            // GetFields on a derived type omits private fields declared by its base classes.
            fields.AddRange(current.GetFields(InstanceFields));
        }

        return fields.ToArray();
    }

    [RequiresDynamicCode("Compiles a field-copying delegate. Native AOT uses CreateReflectionDelegate instead.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "The expression tree only accesses the fields of T, which are preserved by the DynamicallyAccessedMembers annotation on T.")]
    [UnconditionalSuppressMessage("Trimming", "IL2060:MakeGenericMethod", Justification = "SetReadonlyField<T> is instantiated over the field types of T, which are preserved by the DynamicallyAccessedMembers annotation on T.")]
    private static CopyDelegate<T> CreateCompiledDelegate<T>(FieldInfo[] fields)
        where T : class
    {
        var byref = typeof(T).MakeByRefType();
        var source = Expression.Parameter(byref, "from");
        var destination = Expression.Parameter(byref, "to");
        var expressionList = new List<Expression>(fields.Length);

        foreach (var field in fields)
        {
            var sourceField = Expression.Field(source, field);
            var destinationField = Expression.Field(destination, field);
            if (field.IsInitOnly)
            {// Expression.Assign rejects init-only fields, so go through a ref-taking helper.
                var method = CompiledDelegateCache.SetReadonlyMethods.GetOrAdd(field.FieldType, static t => CompiledDelegateCache.SetReadonlyMethod.MakeGenericMethod(t));
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

    private static CopyDelegate<T> CreateReflectionDelegate<T>(FieldInfo[] fields)
        where T : class
    {
        // FieldInfo.SetValue writes init-only instance fields as well, so no special case is needed here.
        return (ref T from, ref T to) =>
        {
            foreach (var field in fields)
            {
                field.SetValue(to, field.GetValue(from));
            }
        };
    }

    private static void SetReadonlyField<T>(ref T target, T value)
    {
        Unsafe.AsRef<T>(ref target) = value;
    }

    private static class CopyDelegateCache<[DynamicallyAccessedMembers(CopiedMembers)] T>
        where T : class
    {
        public static readonly CopyDelegate<T> CopyDelegate = CreateDelegateCore<T>();
    }

    private static class CompiledDelegateCache
    {
        // This cache is only referenced by the dynamic-code path and is removed by Native AOT.
        public static readonly MethodInfo SetReadonlyMethod = typeof(GhostCopy).GetMethod(nameof(SetReadonlyField), BindingFlags.Static | BindingFlags.NonPublic)!;
        public static readonly ConcurrentDictionary<Type, MethodInfo> SetReadonlyMethods = new();
    }
}
