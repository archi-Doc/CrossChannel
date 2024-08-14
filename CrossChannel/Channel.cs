﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

namespace CrossChannel;

public class Channel<TService>
    where TService : class
{
    #region Link

    public class Link : IDisposable
    {
#pragma warning disable SA1401 // Fields should be private
        internal int Index = -1; // The index of FastList<T>, lock() required.
#pragma warning restore SA1401 // Fields should be private

        private readonly Channel<TService> channel;
        private readonly WeakReference<TService>? weakReference;
        private readonly TService? strongReference;

        public Link(Channel<TService> channel, TService instance, bool weakReference)
        {
            this.channel = channel;
            if (weakReference)
            {
                this.weakReference = new(instance);
            }
            else
            {
                this.strongReference = instance;
            }

            lock (this.channel.syncObject)
            {
                this.channel.list.Add(this);
            }
        }

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
            => this.Dispose();

        public void Dispose()
        {
            lock (this.channel.syncObject)
            {
                if (this.Index != -1)
                {
                    this.channel.list.Remove(this); // this.Index is set to -1
                }
            }
        }
    }

    #endregion

    #region FastList

    private sealed class FastList : IDisposable
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

        internal int CleanupCount { get; set; } // no lock, not thread safe

        public Link?[] GetValues() => this.values; // no lock, safe for iterate

        public (Link?[] Array, int CountHint) GetValuesAndCountHint() => (this.values, this.count); // no lock, safe for iterate

        public bool IsDisposed => this.freeIndex == null;

        public bool IsEmpty => this.count == 0;

        public int Add(Link value)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(FastList));
            }

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
                newValues[this.values.Length] = value;
                this.count++;
                Volatile.Write(ref this.values, newValues);
                return index;
            }
        }

        public bool Remove(Link value)
        {
            if (this.IsDisposed)
            {
                return true;
            }

            var index = value.Index;
            ref var v = ref this.values[index];
            if (v == null)
            {
                throw new KeyNotFoundException($"key index {index} is not found.");
            }

            v = default(Link);
            this.freeIndex.Enqueue(index);
            value.Index = -1;
            this.count--;

            return this.count == 0;
        }

        /// <summary>
        /// Shrink the list when there are too many unused objects.
        /// </summary>
        /// <returns>true if the list is empty.</returns>
        public bool TryShrink()
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

            var oldIndex = 0;
            var i = 0;
            for (i = 0; i < this.count; i++)
            {
                while (this.values[oldIndex] == null)
                {
                    oldIndex++;
                }

                ref var v = ref this.values[oldIndex]!;
                newValues[i] = v;
                v.Index = i;
                v = default(Link);
            }

            this.freeIndex = new FastIntQueue(newLength);
            for (; i < newLength; i++)
            {
                this.freeIndex.Enqueue(i);
            }

            Volatile.Write(ref this.values, newValues);

            return false;
        }

        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.freeIndex = null!;
            this.values = Array.Empty<Link?>();
            this.count = 0;
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

    private readonly object syncObject = new(); // -> Lock
    private readonly FastList list = new();

    public Channel()
    {
        this.Broker = (TService)RadioRegistry.Get<TService>().Constructor(this);
    }

    public Link Open(TService instance, bool weakReference)
    {
        return new Link(this, instance, weakReference);
    }

    public int Count => this.list.Count;

    public (Link?[] Array, int CountHint) InternalGetList() => this.list.GetValuesAndCountHint();
}
