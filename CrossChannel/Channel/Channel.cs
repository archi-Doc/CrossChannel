// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;
using System.Threading;

namespace CrossChannel;

/// <summary>
/// Represents the non-generic base of <see cref="Channel{TService}"/>.
/// </summary>
public abstract class Channel
{
    /// <summary>
    /// The number of open operations between two trim operations.
    /// </summary>
    public const int TrimThreshold = 32;

    /// <summary>
    /// The number of trim operations between two sweeps for dead weak references.
    /// </summary>
    public const int WeakReferenceCheckThreshold = 32;

    /// <summary>
    /// Gets the maximum number of links the channel can hold.
    /// </summary>
    public int MaxLinks { get; internal set; }

    /// <summary>
    /// Gets or sets the index of the node of this channel in its keyed map, or -1 if it is not in a map.
    /// </summary>
    internal int NodeIndex { get; set; }

    /// <summary>
    /// Gets the broker of the channel. Calling a method of the broker invokes that method on every linked instance.
    /// </summary>
    /// <returns>The broker.</returns>
    public abstract object GetBroker();
}

/// <summary>
/// Delivers messages to the instances linked to it.<br/>
/// Instances subscribe with <see cref="Open(TService, bool)"/>, and messages are sent through <see cref="GetBroker"/>.
/// </summary>
/// <typeparam name="TService">The type of the service.</typeparam>
public sealed class Channel<TService> : Channel, IChannel<TService>
    where TService : class, IRadioService
{
    #region Link

    /// <summary>
    /// Represents the registration of a single instance in a <see cref="Channel{TService}"/>.<br/>
    /// Disposing the link unsubscribes the instance.
    /// </summary>
    public sealed class Link : IDisposable
    {
#pragma warning disable SA1401 // Fields should be private
        internal int Index = -1; // The index of FastList<T>, lock() required.
#pragma warning restore SA1401 // Fields should be private

        private readonly Channel<TService> channel;
        private readonly WeakReference<TService>? weakReference;
        private readonly TService? strongReference;

        internal Link(Channel<TService> channel, TService instance, bool useWeakReference)
        {// Valid link
            this.channel = channel;
            if (useWeakReference)
            {
                this.weakReference = new(instance);
            }
            else
            {
                this.strongReference = instance;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the link is open (still registered in the channel).
        /// </summary>
        public bool IsOpen => Volatile.Read(ref this.Index) != -1;

        /// <summary>
        /// Tries to get the linked instance. Fails when the instance was held by a weak reference and has been collected.
        /// </summary>
        /// <param name="instance">When this method returns, contains the linked instance, if it is still alive.</param>
        /// <returns><see langword="true"/> if the instance was retrieved; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetInstance([MaybeNullWhen(false)] out TService instance)
        {
            if (this.strongReference is not null)
            {
                instance = this.strongReference;
                return true;
            }
            else
            {
                return this.weakReference!.TryGetTarget(out instance);
            }
        }

        /// <summary>
        /// Unsubscribes the instance. Equivalent to <see cref="Dispose"/> and safe to call more than once.
        /// </summary>
        public void Close()
            => this.channel.Remove(this);

        /// <inheritdoc/>
        public void Dispose()
            => this.channel.Remove(this);
    }

    #endregion

    #region FastList

    private sealed class FastList
    {
        private const int InitialCapacity = 1;
        private const int MinShrinkStart = 8;

        private Link?[] values = default!;
        private int count;
        private FastIntQueue freeIndex = default!;

        public FastList()
        {
            this.Initialize();
        }

        public int Count => Volatile.Read(ref this.count); // It may lead to inconsistent results between 'count' and 'values'.

        public Link?[] GetValues() => this.values; // no lock, safe for iterate

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (Link?[] Array, int CountHint) GetValuesAndCountHint()
        {// no lock, safe for iterate
            // The array and count are separate observations. Concurrent mutation can make the hint
            // either too small or too large; it is only an initial allocation size for brokers.
            var values = Volatile.Read(ref this.values);
            var countHint = Volatile.Read(ref this.count);
            return (values, countHint);
        }

        public int Add(Link value)
        {
            if (this.freeIndex.Count != 0)
            {
                var index = this.freeIndex.Dequeue();
                value.Index = index;
                Volatile.Write(ref this.values[index], value);
                Volatile.Write(ref this.count, this.count + 1);
                return index;
            }
            else
            {// Resize
                var newValues = new Link[this.values.Length * 2];
                Array.Copy(this.values, 0, newValues, 0, this.values.Length);
                this.freeIndex.EnsureNewCapacity(newValues.Length);
                for (var i = this.values.Length; i < newValues.Length; i++)
                {
                    this.freeIndex.Enqueue(i);
                }

                var index = this.freeIndex.Dequeue();
                value.Index = index;
                newValues[index] = value;
                Volatile.Write(ref this.count, this.count + 1);
                Volatile.Write(ref this.values, newValues);
                return index;
            }
        }

        public void Remove(Link value)
        {
            var index = value.Index;
            ref var v = ref this.values[index];
            if (v == null)
            {
                return;
            }

            Volatile.Write(ref v, null);
            this.freeIndex.Enqueue(index);
            Volatile.Write(ref value.Index, -1);
            Volatile.Write(ref this.count, this.count - 1);
        }

        /// <summary>
        /// Shrink the list when there are too many unused objects.
        /// </summary>
        /// <returns>true if the list is empty.</returns>
        public bool TryTrim()
        {
            if (this.count == 0)
            {// Empty
                if (this.values.Length > MinShrinkStart)
                {
                    this.Initialize();
                }

                return true;
            }

            if (this.values.Length <= MinShrinkStart)
            {
                return false;
            }
            else if (this.count * 2 >= this.values.Length)
            {
                return false;
            }

            var newLength = this.values.Length >> 1;
            while (this.count < newLength)
            {
                newLength >>= 1;
            }

            newLength <<= 1;
            newLength = (newLength < InitialCapacity) ? InitialCapacity : newLength;
            var newValues = new Link[newLength];

            var oldValues = this.values;
            var oldIndex = 0;
            var i = 0;
            for (i = 0; i < this.count; i++)
            {
                while (oldValues[oldIndex] is null)
                {
                    oldIndex++;
                }

                // The old array is not cleared, since it may be being enumerated by other threads (send).
                var link = oldValues[oldIndex++]!;
                newValues[i] = link;
                link.Index = i;
            }

            this.freeIndex = new FastIntQueue(newLength);
            for (; i < newLength; i++)
            {
                this.freeIndex.Enqueue(i);
            }

            Volatile.Write(ref this.values, newValues);

            return false;
        }

        private void Initialize()
        {
            this.freeIndex = new FastIntQueue(InitialCapacity);
            for (int i = 0; i < InitialCapacity; i++)
            {
                this.freeIndex.Enqueue(i);
            }

            this.count = 0;
            var v = new Link?[InitialCapacity];
            Volatile.Write(ref this.values, v);
        }
    }

    #endregion

    internal TService Broker { get; }

    internal Lock LockObject { get; }

    private readonly IUnorderedMapWithLock? map; // Not null if the channel is registered in a keyed map.
    private readonly FastList list = new();
    private int trimCount;
    private int checkReferenceCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="Channel{TService}"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    public Channel()
    {
        this.LockObject = new Lock();
        this.NodeIndex = -1;

        var registration = RadioServiceRegistry.GetRegistration<TService>();
        this.MaxLinks = registration.MaxLinks;
        this.Broker = (TService)registration.BrokerFactory(this);
    }

    internal Channel(IUnorderedMapWithLock map)
    {
        this.map = map;
        this.LockObject = map.LockObject; // Shared with the map (the node is added/removed while holding this lock).
        this.NodeIndex = -1;

        var registration = RadioServiceRegistry.GetRegistration<TService>();
        this.MaxLinks = registration.MaxLinks;
        this.Broker = (TService)registration.BrokerFactory(this);
    }

    /// <inheritdoc/>
    public Link? Open(TService instance, bool useWeakReference = false)
    {
        using (this.LockObject.EnterScope())
        {
            return this.OpenInternal(instance, useWeakReference);
        }
    }

    /// <summary>
    /// Registers the instance. <see cref="LockObject"/> must be held by the caller.<br/>
    /// A keyed channel shares the lock with its map, so the caller can add the node and the link atomically.
    /// </summary>
    /// <param name="instance">The instance to register.</param>
    /// <param name="useWeakReference">Indicates whether to use a weak reference for the instance.</param>
    /// <returns>A link to the opened channel, or null if the channel is full.</returns>
    internal Link? OpenInternal(TService instance, bool useWeakReference)
    {// using (this.LockObject.EnterScope()) is required
        ArgumentNullException.ThrowIfNull(instance);
        if (this.list.Count >= this.MaxLinks)
        {
            this.RemoveDeadLinks();
            if (this.list.Count >= this.MaxLinks)
            {// Invalid link
                return default; // new(this);
            }
        }

        var link = new Link(this, instance, useWeakReference);
        this.list.Add(link);
        if (++this.trimCount >= TrimThreshold)
        {
            this.trimCount = 0;
            this.TrimInternal();
        }

        return link;
    }

    /// <summary>
    /// Gets the current number of registered links, including weak links not yet cleaned up.
    /// </summary>
    public int Count => this.list.Count;

    /// <summary>
    /// Gets the internal link array together with a hint of the number of links it holds.<br/>
    /// Intended for the generated broker code: the array is shared, must be treated as read-only,
    /// and contains null entries for unused slots. CountHint is an allocation hint, not a snapshot
    /// or an iteration limit. Links may be added or removed concurrently.
    /// </summary>
    /// <returns>The shared link array and an approximate link count.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Link?[] Links, int CountHint) DangerousGetLinks() => this.list.GetValuesAndCountHint();

    /// <inheritdoc/>
    public override TService GetBroker() => this.Broker;

    private void Remove(Link link)
    {
        using (this.LockObject.EnterScope())
        {
            if (link.Index != -1)
            {
                this.list.Remove(link); // this.Index is set to -1
            }

            if (this.map is not null &&
                this.NodeIndex != -1 &&
                this.Count == 0)
            {
                this.map.RemoveNode(this.NodeIndex);
                this.NodeIndex = -1;
            }
        }
    }

    private void TrimInternal()
    {// using (this.LockObject.EnterScope()) is required
        if (++this.checkReferenceCount >= WeakReferenceCheckThreshold)
        {
            this.checkReferenceCount = 0;

            this.RemoveDeadLinks();
        }

        this.list.TryTrim();
    }

    private void RemoveDeadLinks()
    {
        foreach (var link in this.list.GetValues())
        {
            if (link is not null && !link.TryGetInstance(out _))
            {
                this.list.Remove(link);
            }
        }
    }
}
