// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;

namespace CrossChannel;

/// <summary>
/// A FIFO queue of free slot indices, used by the link list of <see cref="Channel{TService}"/>.<br/>
/// It never grows on Enqueue; <see cref="Reset"/> sets a new capacity and refills it.
/// The caller keeps the queue within its capacity (the free and the used slots add up to the capacity).
/// </summary>
internal sealed class FastIntQueue
{
    private int[] array;
    private int head;
    private int tail;
    private int size;

    public FastIntQueue(int capacity)
    {
        Debug.Assert(capacity >= 0, "The capacity must not be negative.");
        this.array = new int[capacity];
    }

    public int Count => this.size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(int item)
    {
        Debug.Assert(this.size < this.array.Length, "Queue is full.");
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
        Debug.Assert(this.size > 0, "Queue is empty.");
        var removed = this.array[this.head];
        this.size--;

        this.head++;
        if (this.head == this.array.Length)
        {
            this.head = 0;
        }

        return removed;
    }

    /// <summary>
    /// Discards all items and fills the queue with the consecutive integers [<paramref name="start"/>, <paramref name="end"/>).<br/>
    /// The buffer is reallocated only when <paramref name="capacity"/> differs from the current one.
    /// </summary>
    /// <param name="capacity">The new capacity.</param>
    /// <param name="start">The first integer to enqueue.</param>
    /// <param name="end">The integer after the last one to enqueue.</param>
    public void Reset(int capacity, int start, int end)
    {
        Debug.Assert(start >= 0 && start <= end && end - start <= capacity, "The range must fit in the capacity.");
        if (this.array.Length != capacity)
        {
            this.array = new int[capacity];
        }

        var n = end - start;
        for (var i = 0; i < n; i++)
        {
            this.array[i] = start + i;
        }

        this.head = 0;
        this.size = n;
        this.tail = (n == capacity) ? 0 : n;
    }
}
