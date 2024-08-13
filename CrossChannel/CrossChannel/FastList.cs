// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CrossChannel;

// NOT thread safe, highly customized for XChannel.
public sealed class FastList<T> : IDisposable
    where T : XChannel
{
    private const int InitialCapacity = 4;
    private const int MinShrinkStart = 8;

    // public delegate ref int ObjectToIndexDelegete(T obj);

    private T?[] values = default!;
    private int count;
    private FastIntQueue freeIndex = default!;

    public FastList()
    {
        // this.objectToIndex = objectToIndex;
        this.Initialize();
    }

    // public int Count => this.count; // Deprecated because it may lead to inconsistent results between 'count' and 'values'.

    internal int CleanupCount { get; set; } // no lock, not thread safe

    public T?[] GetValues() => this.values; // no lock, safe for iterate

    public (T?[] Array, int CountHint) GetValuesAndCountHint() => (this.values, this.count); // no lock, safe for iterate

    public bool IsDisposed => this.freeIndex == null;

    public bool IsEmpty => this.count == 0;

    public int Add(T value)
    {
        if (this.IsDisposed)
        {
            throw new ObjectDisposedException(nameof(FastList<T>));
        }

        if (this.freeIndex.Count != 0)
        {
            var index = this.freeIndex.Dequeue();
            // this.objectToIndex(value) = index;
            value.Index = index;
            this.values[index] = value;
            this.count++;
            return index;
        }
        else
        {
            // resize
            var newValues = new T[this.values.Length * 2];
            Array.Copy(this.values, 0, newValues, 0, this.values.Length);
            this.freeIndex.EnsureNewCapacity(newValues.Length);
            for (var i = this.values.Length; i < newValues.Length; i++)
            {
                this.freeIndex.Enqueue(i);
            }

            var index = this.freeIndex.Dequeue();
            // this.objectToIndex(value) = index;
            value.Index = index;
            newValues[this.values.Length] = value;
            this.count++;
            Volatile.Write(ref this.values, newValues);
            return index;
        }
    }

    public bool Remove(T value)
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

        v = default(T);
        this.freeIndex.Enqueue(index);
        value.Index = -1;
        this.count--;

        return this.count == 0;
    }

    /// <summary>
    /// Shrink the list when there are too many unused objects.
    /// </summary>
    /// <returns>true if the list is empty.</returns>
    public bool Shrink()
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
        var newValues = new T[newLength];

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
            v = default(T);
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
        this.values = Array.Empty<T?>();
        this.count = 0;
    }

    // private ObjectToIndexDelegete objectToIndex;

    private void Initialize()
    {
        this.freeIndex = new FastIntQueue(InitialCapacity);
        for (int i = 0; i < InitialCapacity; i++)
        {
            this.freeIndex.Enqueue(i);
        }

        this.count = 0;
        var v = new T?[InitialCapacity];
        Volatile.Write(ref this.values, v);
    }
}

internal class FastIntQueue
{
    private int[] array;
    private int head;
    private int tail;
    private int size;

    public FastIntQueue(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        this.array = new int[capacity];
        this.head = 0;
        this.tail = 0;
        this.size = 0;
    }

    public int Count => this.size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(int item)
    {
        if (this.size == this.array.Length)
        {
            throw new InvalidOperationException("Queue is full.");
        }

        this.array[this.tail] = item;
        this.size++;
        this.tail++;
        if (this.tail == this.array.Length)
        {
            this.tail = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Dequeue()
    {
        if (this.size == 0)
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        var removed = this.array[this.head];
        this.array[this.head] = default!;
        this.size--;

        this.head++;
        if (this.head == this.array.Length)
        {
            this.head = 0;
        }

        return removed;
    }

    public void EnsureNewCapacity(int capacity)
    {
        var newarray = new int[capacity];
        if (this.size > 0)
        {
            if (this.head < this.tail)
            {
                Array.Copy(this.array, this.head, newarray, 0, this.size);
            }
            else
            {
                Array.Copy(this.array, this.head, newarray, 0, this.array.Length - this.head);
                Array.Copy(this.array, 0, newarray, this.array.Length - this.head, this.tail);
            }
        }

        this.array = newarray;
        this.head = 0;
        this.tail = (this.size == capacity) ? 0 : this.size;
    }
}

/*internal class FastQueue<T>
{
    private T[] array;
    private int head;
    private int tail;
    private int size;

    public FastQueue(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        this.array = new T[capacity];
        this.head = 0;
        this.tail = 0;
        this.size = 0;
    }

    public int Count => this.size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(T item)
    {
        if (this.size == this.array.Length)
        {
            throw new InvalidOperationException("Queue is full.");
        }

        this.array[this.tail] = item;
        this.size++;
        this.tail++;
        if(this.tail == this.array.Length)
        {
            this.tail = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Dequeue()
    {
        if (this.size == 0)
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        var removed = this.array[this.head];
        this.array[this.head] = default!;
        this.size--;

        this.head++;
        if (this.head == this.array.Length)
        {
            this.head = 0;
        }

        return removed;
    }

    public void EnsureNewCapacity(int capacity)
    {
        var newarray = new T[capacity];
        if (this.size > 0)
        {
            if (this.head < this.tail)
            {
                Array.Copy(this.array, this.head, newarray, 0, this.size);
            }
            else
            {
                Array.Copy(this.array, this.head, newarray, 0, this.array.Length - this.head);
                Array.Copy(this.array, 0, newarray, this.array.Length - this.head, this.tail);
            }
        }

        this.array = newarray;
        this.head = 0;
        this.tail = (this.size == capacity) ? 0 : this.size;
    }
}*/
