// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1649 // File name should match first type name

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;

namespace Arc.WeakDelegate
{
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

        public WeakAction(Action method, bool keepTargetAlive = false)
            : this(method.Target, method, keepTargetAlive)
        {
        }

        public WeakAction(object? target, Action method, bool keepTargetAlive = false)
            : base(target, method, keepTargetAlive)
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

        public WeakAction(Action<T> method, bool keepTargetAlive = false)
            : this(method.Target, method, keepTargetAlive)
        {
        }

        public WeakAction(object? target, Action<T> method, bool keepTargetAlive = false)
            : base(target, method, keepTargetAlive)
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

        public WeakFunc(Func<TResult> method, bool keepTargetAlive = false)
            : this(method.Target, method, keepTargetAlive)
        {
        }

        public WeakFunc(object? target, Func<TResult> method, bool keepTargetAlive = false)
            : base(target, method, keepTargetAlive)
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

        public WeakFunc(Func<T, TResult> method, bool keepTargetAlive = false)
            : this(method.Target, method, keepTargetAlive)
        {
        }

        public WeakFunc(object? target, Func<T, TResult> method, bool keepTargetAlive = false)
            : base(target, method, keepTargetAlive)
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
        /// <param name="delegate">The action that will be associated to this instance.</param>
        /// <param name="keepTargetAlive">If true, the target of the Action will be kept as a hard reference, which might cause a memory leak.</param>
        public WeakDelegate(Delegate @delegate, bool keepTargetAlive = false)
            : this(@delegate.Target, @delegate, keepTargetAlive)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakDelegate"/> class.
        /// </summary>
        /// <param name="target">The action's owner.</param>
        /// <param name="delegate">The action that will be associated to this instance.</param>
        /// <param name="keepTargetAlive">If true, the target of the Action will be kept as a hard reference, which might cause a memory leak.</param>
        public WeakDelegate(object? target, Delegate @delegate, bool keepTargetAlive = false)
        {
#if NETFX_CORE
            if (@delegate.GetMethodInfo().IsStatic)
#else
            if (@delegate.Method.IsStatic)
#endif
            {
                this.StaticDelegate = @delegate;

                if (target != null)
                {
                    // Keep a reference to the target to control the WeakAction's lifetime.
                    this.Reference = new WeakReference(target);
                }

                return;
            }

#if NETFX_CORE
            this.Method = @delegate.GetMethodInfo();
#else
            this.Method = @delegate.Method;
#endif

            this.DelegateReference = new WeakReference(@delegate.Target);
            this.HardReference = keepTargetAlive ? @delegate.Target : null;
            if (target == @delegate.Target)
            {
                this.Reference = this.DelegateReference;
            }
            else
            {
                this.Reference = new WeakReference(target);
            }
        }

        /// <summary>
        /// Gets the name of the method.
        /// </summary>
        public string MethodName
        {// Thread safe
            get
            {
                if (this.StaticDelegate is { } sd)
                {
#if NETFX_CORE
                    return sd.GetMethodInfo().Name;
#else
                    return sd.Method.Name;
#endif
                }

                if (this.Method is { } m)
                {
                    return m.Name;
                }

                return string.Empty;
            }
        }

        public object? Target => this.Reference?.Target; // Thread safe

        public bool IsAlive
        {// Thread safe
            get
            {
                if (this.HardReference != null)
                {
                    return true;
                }

                if (this.Reference is { } w)
                {
                    return w.IsAlive;
                }

                /*if (this.StaticDelegate != null)
                {
                    return true;
                }*/

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the WeakDelegate is static or not.
        /// </summary>
        public bool IsStatic => this.StaticDelegate != null; // Thread safe

        /// <summary>
        /// Gets the target of this delegate.
        /// </summary>
        protected object? DelegateTarget
        {// Thread safe
            get
            {
                if (this.HardReference != null)
                {
                    return this.HardReference;
                }

                return this.DelegateReference?.Target;
            }
        }

        /// <summary>
        /// Gets or sets a hard reference of this delegate. This property is used only when the delegate is static.
        /// </summary>
        protected Delegate? StaticDelegate { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="MethodInfo" /> corresponding to this WeakDelegate's method passed in the constructor.
        /// </summary>
        protected MethodInfo? Method { get; set; }

        /// <summary>
        /// Gets or sets a WeakReference to this delegate's target (new WeakReference(@delegate.Target)).
        /// </summary>
        protected WeakReference? DelegateReference { get; set; }

        /// <summary>
        /// Gets or sets a WeakReference to the target passed when constructing the WeakDelegate (new WeakReference(target)).
        /// </summary>
        protected WeakReference? Reference { get; set; }

        /// <summary>
        /// Gets or sets a hard reference to this delegate's target (keepTargetAlive ? @delegate.Target : null).
        /// </summary>
        protected object? HardReference { get; set; }

        /// <summary>
        /// Sets the reference that this instance stores to null. Thread safe.
        /// </summary>
        public void MarkForDeletion()
        {// Thread safe
            this.Reference = null;
            this.DelegateReference = null;
            this.HardReference = null;
            this.Method = null;
            this.StaticDelegate = null;
        }
    }
}
