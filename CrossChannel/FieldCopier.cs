// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;

namespace CrossChannel;

/// <summary>
/// Shallow-copies the instance fields of a class, including private, readonly, and inherited fields.
/// </summary>
/// <remarks>
/// When the runtime supports dynamic code (JIT), the copy is performed by a delegate compiled once per type; readonly pointer
/// and function pointer fields are copied via reflection within it, which boxes them on every copy.<br/>
/// Under Native AOT, where no code can be emitted, an equivalent reflection-based delegate is used instead.
/// Reference fields remain shared. Fields declared only by a runtime subtype are not copied. Both instances must be non-null.
/// <see cref="string"/> is not supported, since its length is part of its layout. Arrays have no instance fields, so their elements are not copied.
/// </remarks>
public static class FieldCopier
{
    /// <summary>
    /// The members of the copied type which must survive trimming.
    /// </summary>
    private const DynamicallyAccessedMemberTypes CopiedMembers = DynamicallyAccessedMemberTypes.AllFields;

    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Represents a method which copies the instance fields of <paramref name="source"/> to <paramref name="destination"/>.
    /// </summary>
    /// <typeparam name="T">The class type to copy.</typeparam>
    /// <param name="source">The source instance.</param>
    /// <param name="destination">The destination instance.</param>
    public delegate void CopyDelegate<T>(ref T source, ref T destination)
        where T : class;

    /// <summary>
    /// Copies the instance fields of <paramref name="source"/> to <paramref name="destination"/>, including inherited, private, readonly, and backing fields.
    /// </summary>
    /// <typeparam name="T">The class type to copy.</typeparam>
    /// <param name="source">The source instance; must not be null.</param>
    /// <param name="destination">The destination instance; must not be null.</param>
    /// <exception cref="NotSupportedException"><typeparamref name="T"/> is <see cref="string"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Copy<[DynamicallyAccessedMembers(CopiedMembers)] T>(ref T source, ref T destination)
        where T : class
    {
        CopyDelegateCache<T>.CopyDelegate(ref source, ref destination);
    }

    /// <summary>
    /// Gets the cached delegate which copies the instance fields of <typeparamref name="T"/>; <see cref="Copy{T}(ref T, ref T)"/> uses the same delegate.<br/>
    /// For <see cref="string"/>, the delegate throws <see cref="NotSupportedException"/> when invoked.
    /// </summary>
    /// <typeparam name="T">The class type to copy.</typeparam>
    /// <returns>The copy delegate.</returns>
    public static CopyDelegate<T> GetDelegate<[DynamicallyAccessedMembers(CopiedMembers)] T>()
        where T : class
        => CopyDelegateCache<T>.CopyDelegate;

    private static CopyDelegate<T> CreateDelegateCore<[DynamicallyAccessedMembers(CopiedMembers)] T>()
        where T : class
    {
        if (typeof(T) == typeof(string))
        {// The length field of a string determines its size, so copying it would corrupt the destination (and the heap).
            return static (ref T source, ref T destination) => throw new NotSupportedException("FieldCopier does not support System.String.");
        }

        // Do not copy properties, since the backing fields are copied directly.
        var fields = GetFields(typeof(T));
        if (fields.Length == 0)
        {
            return static (ref T source, ref T destination) => { };
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
        var source = Expression.Parameter(byref, "source");
        var destination = Expression.Parameter(byref, "destination");
        var expressionList = new List<Expression>(fields.Length);

        foreach (var field in fields)
        {
            var sourceField = Expression.Field(source, field);
            var destinationField = Expression.Field(destination, field);
            if (field.IsInitOnly)
            {// Expression.Assign rejects init-only fields.
                if (field.FieldType.IsPointer || field.FieldType.IsFunctionPointer)
                {// Pointer types cannot be generic arguments of the helper below, so these fields are copied via reflection (the other fields stay compiled).
                    expressionList.Add(Expression.Call(CompiledDelegateCache.CopyFieldValueMethod, Expression.Constant(field, typeof(FieldInfo)), destination, source));
                }
                else
                {// Go through a ref-taking helper.
                    var method = CompiledDelegateCache.SetReadonlyMethods.GetOrAdd(field.FieldType, static t => CompiledDelegateCache.SetReadonlyMethod.MakeGenericMethod(t));
                    expressionList.Add(Expression.Call(method, destinationField, sourceField));
                }
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
        return (ref T source, ref T destination) =>
        {
            foreach (var field in fields)
            {
                field.SetValue(destination, field.GetValue(source));
            }
        };
    }

    private static void SetReadonlyField<T>(ref T target, T value)
    {
        Unsafe.AsRef<T>(ref target) = value;
    }

    private static void CopyFieldValue(FieldInfo field, object destination, object source)
    {
        field.SetValue(destination, field.GetValue(source));
    }

    private static class CopyDelegateCache<[DynamicallyAccessedMembers(CopiedMembers)] T>
        where T : class
    {
        public static readonly CopyDelegate<T> CopyDelegate = CreateDelegateCore<T>();
    }

    private static class CompiledDelegateCache
    {
        // This cache is only referenced by the dynamic-code path and is removed by Native AOT.
        public static readonly MethodInfo SetReadonlyMethod = typeof(FieldCopier).GetMethod(nameof(SetReadonlyField), BindingFlags.Static | BindingFlags.NonPublic)!;
        public static readonly ConcurrentDictionary<Type, MethodInfo> SetReadonlyMethods = new();
        public static readonly MethodInfo CopyFieldValueMethod = typeof(FieldCopier).GetMethod(nameof(CopyFieldValue), BindingFlags.Static | BindingFlags.NonPublic)!;
    }
}
