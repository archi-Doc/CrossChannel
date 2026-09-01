// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

namespace CrossChannel;

/// <summary>
/// Represents an abstract channel.
/// </summary>
public abstract class Channel
{
    /// <summary>
    /// The threshold value for trimming the channel list.
    /// </summary>
    public const int TrimThreshold = 32;

    /// <summary>
    /// The threshold value for checking weak references in the channel list.
    /// </summary>
    public const int CheckReferenceThreshold = 32;

    /// <summary>
    /// Gets the maximum number of links allowed in the channel.
    /// </summary>
    public int MaxLinks { get; internal set; }

    /// <summary>
    /// Gets or sets the index of the node in the channel.
    /// </summary>
    internal int NodeIndex { get; set; }

    /// <summary>
    /// Gets the broker object associated with the channel.
    /// </summary>
    /// <returns>The broker object.</returns>
    public abstract object GetBroker();
}

public sealed class Channel<TService> : Channel, IChannel<TService>
    where TService : class, IRadioService
{
    #region Link

    public sealed class Link : IDisposable
    {
#pragma warning disable SA1401 // Fields should be private
        internal int Index = -1; // The index of FastList<T>, lock() required.
#pragma warning restore SA1401 // Fields should be private

        private readonly Channel<TService> channel;
        private readonly WeakReference<TService>? weakReference;
        private readonly TService? strongReference;

        internal Link(Channel<TService> channel, TService instance, bool weakReference)
        {// Valid link
            this.channel = channel;
            if (weakReference)
            {
                this.weakReference = new(instance);
            }
            else
            {
                this.strongReference = instance;
            }
        }

        public bool IsValid => this.Index != -1;

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

        public void Close()
            => this.channel.Remove(this);

        public void Dispose()
            => this.channel.Remove(this);
    }

    #endregion

    #region FastList

    private sealed class FastList
    {
        private const int InitialCapacity = 4;
        private const int MinShrinkStart = 8;

        private Link?[] values = default!;
        private int count;
        private FastIntQueue freeIndex = default!;

        public FastList()
        {
            this.Initialize();
        }

        public int Count => this.count; // It may lead to inconsistent results between 'count' and 'values'.

        public Link?[] GetValues() => this.values; // no lock, safe for iterate

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (Link?[] Array, int CountHint) GetValuesAndCountHint()
        {// no lock, safe for iterate
            // 'count' is read before 'values' (acquire) so that CountHint can never under-report
            // the number of links held by the returned array. Enumerators rely on this in order to
            // stop as soon as CountHint links have been processed, instead of scanning the whole array.
            var countHint = Volatile.Read(ref this.count);
            return (this.values, countHint);
        }

        public int Add(Link value)
        {
            if (this.freeIndex.Count != 0)
            {
                var index = this.freeIndex.Dequeue();
                value.Index = index;
                this.values[index] = value;
                this.count++;
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
                this.count++;
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

            v = default(Link);
            this.freeIndex.Enqueue(index);
            value.Index = -1;
            this.count--;
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

    public Channel()
    {
        this.LockObject = new Lock();
        this.NodeIndex = -1;

        var info = ChannelRegistry.Get<TService>();
        this.MaxLinks = info.MaxLinks;
        this.Broker = (TService)info.NewBroker(this);
    }

    public Channel(IUnorderedMapWithLock map)
    {
        this.map = map;
        this.LockObject = map.LockObject; // Shared with the map (the node is added/removed while holding this lock).
        this.NodeIndex = -1;

        var info = ChannelRegistry.Get<TService>();
        this.MaxLinks = info.MaxLinks;
        this.Broker = (TService)info.NewBroker(this);
    }

    public Link? Open(TService instance, bool weakReference = false)
    {
        using (this.LockObject.EnterScope())
        {
            if (this.list.Count >= this.MaxLinks)
            {// Invalid link
                return default; // new(this);
            }

            var link = new Link(this, instance, weakReference);
            this.list.Add(link);
            if (this.trimCount++ >= TrimThreshold)
            {
                this.trimCount = 0;
                this.TrimInternal();
            }

            return link;
        }
    }

    public int Count => this.list.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Link?[] Array, int CountHint) InternalGetList() => this.list.GetValuesAndCountHint();

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
        if (this.checkReferenceCount++ >= CheckReferenceThreshold)
        {
            this.checkReferenceCount = 0;

            var array = this.list.GetValues();
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] is { } link
                    && !link.TryGetInstance(out _))
                {
                    this.list.Remove(link);
                }
            }
        }

        this.list.TryTrim();
    }
}
