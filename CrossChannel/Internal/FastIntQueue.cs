// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CrossChannel;

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
