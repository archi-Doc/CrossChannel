// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1649 // File name should match first type name

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;

namespace Arc.WeakDelegate;

/// <summary>
/// Stores a delegate without causing a hard reference to be created. The owner can be garbage collected at any time.
/// </summary>
public interface IWeakDelegate
{
    /// <summary>
    /// Gets the Delegate's owner. This object is stored as a <see cref="WeakReference" />.
    /// </summary>
    object? Target { get; }

    /// <summary>
    /// Gets a value indicating whether the Delegate's owner is still alive.
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Deletes all references, which notifies the cleanup method that this entry must be deleted.
    /// </summary>
    void MarkForDeletion();
}

/// <summary>
/// A key which stores a type of the Delegate's owner and MethodInfo.
/// Method (e.g. Action&lt;T&gt;) is not suitable for a key (cannot cache properly).
/// </summary>
public struct DelegateKey
{
    public Type InstanceType;
    public MethodInfo Method;

    public DelegateKey(Type instanceType, MethodInfo method)
    {
        this.InstanceType = instanceType;
        this.Method = method;
    }

    public override int GetHashCode() => HashCode.Combine(this.InstanceType, this.Method);

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != typeof(DelegateKey))
        {
            return false;
        }

        var x = (DelegateKey)obj;
        return this.InstanceType == x.InstanceType && this.Method == x.Method;
    }
}

public class WeakAction : WeakDelegate
{
    private static Hashtable delegateCache = new Hashtable();
    private Action<object>? compiledDelegate;

    public WeakAction(object target, Action method)
        : base(target, method)
    {
        if (method.Target == null)
        { // Static delegate.
            return;
        }

        var type = method.Target.GetType();
        var key = new DelegateKey(type, method.Method);

        this.compiledDelegate = delegateCache[key] as Action<object>;
        if (this.compiledDelegate == null)
        {
            var targetParam = Expression.Parameter(typeof(object));
            this.compiledDelegate = Expression.Lambda<Action<object>>(
                Expression.Call(
                    Expression.Convert(targetParam, type),
                    method.Method),
                targetParam)
                .CompileFast();

            lock (delegateCache)
            {
                delegateCache[key] = this.compiledDelegate;
            }
        }
    }

    public void Execute(out bool executed)
    {// Thread safe
        if (!this.IsAlive)
        {
            executed = false;
            return;
        }
        else if (this.StaticDelegate is Action method)
        {
            executed = true;
            method();
            return;
        }

        var delegateTarget = this.DelegateTarget;
        if (this.compiledDelegate != null && delegateTarget != null)
        {
            executed = true;
            this.compiledDelegate(delegateTarget);
            return;
        }

        executed = false;
        return;
    }

    public void Execute()
    {// Thread safe
        if (!this.IsAlive)
        {
            return;
        }
        else if (this.StaticDelegate is Action method)
        {
            method();
            return;
        }

        var delegateTarget = this.DelegateTarget;
        if (this.compiledDelegate != null && delegateTarget != null)
        {
            this.compiledDelegate(delegateTarget);
        }

        return;
    }
}

public class WeakAction<T> : WeakDelegate
{
    private static ConcurrentDictionary<DelegateKey, Action<object, T>> delegateCache = new();
    private Action<object, T>? compiledDelegate;

    public WeakAction(object target, Action<T> method)
        : base(target, method)
    {
        if (method.Target == null)
        { // Static delegate.
            return;
        }

        var type = method.Target.GetType();
        var key = new DelegateKey(type, method.Method);

        // this.compiledDelegate = delegateCache[key] as Action<object, T>;
        if (!delegateCache.TryGetValue(key, out this.compiledDelegate))
        {
            var targetParam = Expression.Parameter(typeof(object));
            var t = Expression.Parameter(typeof(T));
            this.compiledDelegate = Expression.Lambda<Action<object, T>>(
                Expression.Call(
                    Expression.Convert(targetParam, type),
                    method.Method,
                    t),
                targetParam,
                t)
                .CompileFast();

            delegateCache.TryAdd(key, this.compiledDelegate);

            /*lock (delegateCache)
            {
                delegateCache[key] = this.compiledDelegate;
            }*/
        }
    }

    public void Execute(T t, out bool executed)
    {// Thread safe
        if (!this.IsAlive)
        {
            executed = false;
            return;
        }
        else if (this.StaticDelegate is Action<T> method)
        {
            executed = true;
            method(t);
            return;
        }

        var delegateTarget = this.DelegateTarget;
        if (this.compiledDelegate != null && delegateTarget != null)
        {
            executed = true;
            this.compiledDelegate(delegateTarget, t);
            return;
        }

        executed = false;
        return;
    }

    public void Execute(T t)
    {// Thread safe
        if (!this.IsAlive)
        {
            return;
        }
        else if (this.StaticDelegate is Action<T> method)
        {
            method(t);
            return;
        }

        var delegateTarget = this.DelegateTarget;
        if (this.compiledDelegate != null && delegateTarget != null)
        {
            this.compiledDelegate(delegateTarget, t);
        }

        return;
    }
}

public class WeakFunc<TResult> : WeakDelegate
{
    private static Hashtable delegateCache = new Hashtable();
    private Func<object, TResult>? compiledDelegate;

    public WeakFunc(object target, Func<TResult> method)
        : base(target, method)
    {
        if (method.Target == null)
        { // Static delegate.
            return;
        }

        var type = method.Target.GetType();
        var key = new DelegateKey(type, method.Method);

        this.compiledDelegate = delegateCache[key] as Func<object, TResult>;
        if (this.compiledDelegate == null)
        {
            var targetParam = Expression.Parameter(typeof(object));
            this.compiledDelegate = Expression.Lambda<Func<object, TResult>>(
                Expression.Call(
                    Expression.Convert(targetParam, type),
                    method.Method),
                targetParam)
                .CompileFast();

            lock (delegateCache)
            {
                delegateCache[key] = this.compiledDelegate;
            }
        }
    }

    [return: MaybeNull]
    public TResult Execute(out bool executed)
    {// Thread safe
        if (!this.IsAlive)
        {
            executed = false;
            return default;
        }
        else if (this.StaticDelegate is Func<TResult> method)
        {
            executed = true;
            return method();
        }

        var delegateTarget = this.DelegateTarget;
        if (this.compiledDelegate != null && delegateTarget != null)
        {
            executed = true;
            return this.compiledDelegate(delegateTarget);
        }

        executed = false;
        return default;
    }

    [return: MaybeNull]
    public TResult Execute()
    {// Thread safe
        if (!this.IsAlive)
        {
            return default;
        }
        else if (this.StaticDelegate is Func<TResult> method)
        {
            return method();
        }

        var delegateTarget = this.DelegateTarget;
        if (this.compiledDelegate != null && delegateTarget != null)
        {
            return this.compiledDelegate(delegateTarget);
        }

        return default;
    }
}

public class WeakFunc<T, TResult> : WeakDelegate
{
    private static Hashtable delegateCache = new Hashtable();
    private Func<object, T, TResult>? compiledDelegate;

    public WeakFunc(object target, Func<T, TResult> method)
        : base(target, method)
    {
        if (method.Target == null)
        { // Static delegate.
            return;
        }

        var type = method.Target.GetType();
        var key = new DelegateKey(type, method.Method);

        this.compiledDelegate = delegateCache[key] as Func<object, T, TResult>;
        if (this.compiledDelegate == null)
        {
            var targetParam = Expression.Parameter(typeof(object));
            var t = Expression.Parameter(typeof(T));
            this.compiledDelegate = Expression.Lambda<Func<object, T, TResult>>(
                Expression.Call(
                    Expression.Convert(targetParam, type),
                    method.Method,
                    t),
                targetParam,
                t)
                .CompileFast();

            lock (delegateCache)
            {
                delegateCache[key] = this.compiledDelegate;
            }
        }
    }

    [return: MaybeNull]
    public TResult Execute(T t, out bool executed)
    {// Thread safe
        if (!this.IsAlive)
        {
            executed = false;
            return default;
        }
        else if (this.StaticDelegate is Func<T, TResult> method)
        {
            executed = true;
            return method(t);
        }

        var delegateTarget = this.DelegateTarget;
        if (this.compiledDelegate != null && delegateTarget != null)
        {
            executed = true;
            return this.compiledDelegate(delegateTarget, t);
        }

        executed = false;
        return default;
    }

    [return: MaybeNull]
    public TResult Execute(T t)
    {// Thread safe
        if (!this.IsAlive)
        {
            return default;
        }
        else if (this.StaticDelegate is Func<T, TResult> method)
        {
            return method(t);
        }

        var delegateTarget = this.DelegateTarget;
        if (this.compiledDelegate != null && delegateTarget != null)
        {
            return this.compiledDelegate(delegateTarget, t);
        }

        return default;
    }
}

public class WeakDelegate : IWeakDelegate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeakDelegate"/> class.
    /// </summary>
    /// <param name="target">The action's owner.</param>
    /// <param name="delegate">The action that will be associated to this instance.</param>
    public WeakDelegate(object target, Delegate @delegate)
    {
        this.Reference = new WeakReference(target);

#if NETFX_CORE
        if (@delegate.GetMethodInfo().IsStatic)
#else
        if (@delegate.Method.IsStatic)
#endif
        {
            this.StaticDelegate = @delegate;
            return;
        }

#if NETFX_CORE
        this.Method = @delegate.GetMethodInfo();
#else
        this.methodInfo = @delegate.Method;
#endif

        if (target == @delegate.Target)
        {
            this.weakDelegateTarget = this.Reference;
        }
        else
        {
            this.hardDelegateTarget = @delegate.Target;
        }
    }

    /// <summary>
    /// Gets the name of the method.
    /// </summary>
    public string MethodName
    {// Thread safe
        get
        {
            if (this.StaticDelegate is { } staticDelegate)
            {
#if NETFX_CORE
                return staticDelegate.GetMethodInfo().Name;
#else
                return staticDelegate.Method.Name;
#endif
            }

            if (this.methodInfo is { } methodInfo)
            {
                return methodInfo.Name;
            }

            return string.Empty;
        }
    }

    public object? Target => this.Reference?.Target;

    public bool IsAlive => this.Reference?.IsAlive == true;

    /// <summary>
    /// Gets a value indicating whether the WeakDelegate is static or not.
    /// </summary>
    public bool IsStatic => this.StaticDelegate != null;

    /// <summary>
    /// Gets the target of this delegate.
    /// </summary>
    protected object? DelegateTarget
        => this.hardDelegateTarget ?? this.weakDelegateTarget?.Target;

    /// <summary>
    /// Gets or sets a hard reference of this delegate. This property is used only when the delegate is static.
    /// </summary>
    protected Delegate? StaticDelegate { get; set; }

    /// <summary>
    /// Gets or sets a WeakReference to the target passed when constructing the WeakDelegate (new WeakReference(target)).
    /// </summary>
    protected WeakReference? Reference { get; set; }

    private MethodInfo? methodInfo;
    private WeakReference? weakDelegateTarget;
    private object? hardDelegateTarget;

    /// <summary>
    /// Sets the reference that this instance stores to null. Thread safe.
    /// </summary>
    public void MarkForDeletion()
    {// Thread safe
        this.StaticDelegate = null;
        this.Reference = null;
        this.methodInfo = null;
        this.weakDelegateTarget = null;
        this.hardDelegateTarget = null;
    }
}
