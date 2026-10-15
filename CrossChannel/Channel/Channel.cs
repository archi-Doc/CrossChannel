// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace CrossChannel;

/// <summary>
/// Represents the non-generic base of <see cref="Channel{TService}"/>.
/// </summary>
public abstract class Channel
{
    /// <summary>
    /// The number of link changes (opens and closes) between two trim operations.
    /// </summary>
    public const int TrimThreshold = 32;

    /// <summary>
    /// The number of trim operations between two sweeps for dead weak references.
    /// </summary>
    public const int WeakReferenceCheckThreshold = 32;

    /// <summary>
    /// Gets the maximum number of links the channel can hold (<see cref="RadioServiceAttribute.MaxLinks"/>).<br/>
    /// A value of 0 or less accepts no links; the shared empty channel uses 0.
    /// </summary>
    public int MaxLinks { get; internal set; }

    /// <summary>
    /// Gets or sets the index of the node of this channel in its keyed map, or -1 if it is not in a map.
    /// </summary>
    internal int NodeIndex { get; set; }

    /// <summary>
    /// Gets the broker of the channel. Calling a method of the broker invokes that method on every subscribed instance.
    /// </summary>
    /// <returns>The broker, which implements the service interface.</returns>
    public abstract object GetBroker();
}

/// <summary>
/// Delivers messages to the instances subscribed to it.<br/>
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
        internal int Index = -1; // The index in FastList, or -1 if closed. Written only under the channel lock.
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
        /// Gets a value indicating whether the link is still registered in the channel.<br/>
        /// A weak link whose instance has been collected stays open until the channel removes it.
        /// </summary>
        public bool IsOpen => Volatile.Read(ref this.Index) != -1;

        /// <summary>
        /// Tries to get the linked instance. Fails only when the instance was held by a weak reference and has been collected.<br/>
        /// It does not check <see cref="IsOpen"/>: a closed link still returns its instance if it is alive.
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
        /// Unsubscribes the instance. Equivalent to <see cref="Dispose"/> and safe to call more than once.<br/>
        /// Closing the last link of a keyed channel removes the channel from its radio.
        /// </summary>
        public void Close()
            => this.channel.Remove(this);

        /// <summary>
        /// Unsubscribes the instance. Equivalent to <see cref="Close"/> and safe to call more than once.
        /// </summary>
        public void Dispose()
            => this.channel.Remove(this);
    }

    #endregion

    #region FastList

    private sealed class FastList
    {
        private const int InitialCapacity = 1;
        private const int MinShrinkStart = 8;

        private readonly FastIntQueue freeIndex = new(InitialCapacity);
        private Link?[] values;
        private int count;
        private int peakCount; // The largest count since the last trim (>= count).

        public FastList()
        {
            this.values = new Link?[InitialCapacity];
            this.freeIndex.Reset(InitialCapacity, 0, InitialCapacity);
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

        public void Add(Link value)
        {
            if (this.count >= this.peakCount)
            {
                this.peakCount = this.count + 1;
            }

            if (this.freeIndex.Count != 0)
            {
                var index = this.freeIndex.Dequeue();
                value.Index = index;
                Volatile.Write(ref this.values[index], value);
                Volatile.Write(ref this.count, this.count + 1);
            }
            else
            {// Resize
                var index = this.values.Length; // The first new slot is used by this link, and the rest are free.
                var newValues = new Link[index * 2];
                Array.Copy(this.values, 0, newValues, 0, index);
                this.freeIndex.Reset(newValues.Length, index + 1, newValues.Length);

                value.Index = index;
                newValues[index] = value;
                Volatile.Write(ref this.count, this.count + 1);
                Volatile.Write(ref this.values, newValues);
            }
        }

        public void Remove(Link value)
        {// The caller guarantees that the link is in the list (link.Index != -1).
            var index = value.Index;
            ref var v = ref this.values[index];
            Debug.Assert(v == value, "The link must be stored at its index.");

            Volatile.Write(ref v, null);
            this.freeIndex.Enqueue(index);
            Volatile.Write(ref value.Index, -1);
            Volatile.Write(ref this.count, this.count - 1);
        }

        /// <summary>
        /// Shrink the list when there are too many unused objects.<br/>
        /// The list is sized for the largest count since the previous trim, not just the current count,
        /// so that a list whose links are repeatedly opened and closed does not shrink only to grow again.
        /// </summary>
        public void TryTrim()
        {
            var size = this.peakCount;
            this.peakCount = this.count;
            Debug.Assert(size > 0, "A trim follows an addition or a removal, so the peak count is positive.");

            if (this.values.Length <= MinShrinkStart)
            {
                return;
            }
            else if (size * 2 >= this.values.Length)
            {
                return;
            }

            var newLength = this.values.Length >> 1;
            while (size < newLength)
            {
                newLength >>= 1;
            }

            newLength <<= 1; // newLength >= 2, since size >= 1.
            var newValues = new Link[newLength];

            var oldValues = this.values;
            var oldIndex = 0;
            for (var i = 0; i < this.count; i++)
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

            this.freeIndex.Reset(newLength, this.count, newLength);
            Volatile.Write(ref this.values, newValues);
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
    /// Initializes a new instance of the <see cref="Channel{TService}"/> class.<br/>
    /// The channel is standalone, independent of <see cref="Radio"/> and <see cref="LocalRadio"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    public Channel()
        : this(null)
    {
    }

    internal Channel(IUnorderedMapWithLock? map)
    {
        this.map = map;
        this.LockObject = map?.LockObject ?? new Lock(); // A keyed channel shares the lock with its map (the node is added/removed while holding this lock).
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
    /// <returns>The link, or <see langword="null"/> if the channel is full.</returns>
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
        this.TrimPeriodically();

        return link;
    }

    /// <summary>
    /// Gets the number of registered links, including weak links whose instances were collected but not yet removed.
    /// </summary>
    public int Count => this.list.Count;

    /// <summary>
    /// Gets the shared link array and a hint of the number of links. Intended for the generated broker code.<br/>
    /// Treat the array as read-only and read each entry with <c>Volatile.Read</c>: unused slots are <see langword="null"/>,
    /// and links may be added or closed concurrently, so check <see cref="Link.IsOpen"/>.
    /// CountHint is only an allocation hint, not a snapshot or an iteration limit.
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
                this.list.Remove(link); // link.Index is set to -1
                this.TrimPeriodically();
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

    /// <summary>
    /// Trims the list once every <see cref="Channel.TrimThreshold"/> link changes.<br/>
    /// Closing counts as well as opening: sending scans the whole array, so a channel whose links were
    /// mostly closed must not keep its peak capacity until enough new links are opened.
    /// </summary>
    private void TrimPeriodically()
    {// using (this.LockObject.EnterScope()) is required
        if (++this.trimCount < TrimThreshold)
        {
            return;
        }

        this.trimCount = 0;
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
