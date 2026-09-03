// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1214 // Readonly fields should appear before non-readonly fields
#pragma warning disable SA1401 // Fields should be private

using System.Threading;

namespace CrossChannel.Internal;

/// <summary>
/// A hashtable keyed by a pair of <see cref="Type"/> which is lock-free for readers and locked for writers.<br/>
/// Entries are never removed, so a reader can safely walk a bucket while another thread adds to it.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
internal sealed class ThreadsafeTwoTypeKeyHashtable<TValue>
{
    private const double LoadFactor = 0.75d;

    private readonly Lock lockObject = new();
    private Entry[] buckets;
    private int size; // only use in writer lock

    private sealed class Entry
    {
        public Entry(Type key, Type key2, TValue value, int hash)
        {
            this.Key = key;
            this.Key2 = key2;
            this.Value = value;
            this.Hash = hash;
        }

        internal readonly Type Key;
        internal readonly Type Key2;
        internal readonly TValue Value;
        internal readonly int Hash;
        internal Entry? Next;
    }

    public ThreadsafeTwoTypeKeyHashtable(int capacity = 4)
    {
        var tableSize = CalculateCapacity(capacity);
        this.buckets = new Entry[tableSize];
    }

    public bool TryAdd(Type key, Type key2, TValue value)
        => this.TryAdd(key, key2, (_, _) => value);

    public bool TryAdd(Type key, Type key2, Func<Type, Type, TValue> valueFactory)
        => this.TryAddInternal(key, key2, valueFactory, out TValue _);

    public TValue GetOrAdd(Type key, Type key2, Func<Type, Type, TValue> valueFactory)
    {
        if (this.TryGetValue(key, key2, out var v))
        {
            return v;
        }

        this.TryAddInternal(key, key2, valueFactory, out v);
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(Type key, Type key2, [MaybeNullWhen(false)] out TValue value)
    {
        var table = this.buckets;
        var entry = table[CombineHash(key, key2) & (table.Length - 1)];

        while (entry is not null)
        {
            if (entry.Key == key && entry.Key2 == key2)
            {
                value = entry.Value;
                return true;
            }

            entry = entry.Next;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Combines the hash codes of the two keys.<br/>
    /// The two are mixed asymmetrically, so that (A, B) and (B, A) do not land in the same bucket.
    /// </summary>
    /// <param name="key">The first key.</param>
    /// <param name="key2">The second key.</param>
    /// <returns>The combined hash code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CombineHash(Type key, Type key2)
        => unchecked((key.GetHashCode() * 397) ^ key2.GetHashCode());

    private static int CalculateCapacity(int collectionSize)
    {
        var initialCapacity = (int)(collectionSize / LoadFactor);
        var capacity = 1;
        while (capacity < initialCapacity)
        {
            capacity <<= 1;
        }

        return capacity < 8 ? 8 : capacity;
    }

    /// <summary>
    /// Appends the entry to the specified table, which must not be published to readers yet.
    /// </summary>
    /// <param name="buckets">The table to append to.</param>
    /// <param name="entry">The entry to append.</param>
    private static void Append(Entry[] buckets, Entry entry)
    {
        ref var bucket = ref buckets[entry.Hash & (buckets.Length - 1)];
        if (bucket is null)
        {
            bucket = entry;
            return;
        }

        var last = bucket;
        while (last.Next is not null)
        {
            last = last.Next;
        }

        last.Next = entry;
    }

    private static bool AddToBuckets(Entry[] buckets, Type newKey, Type newKey2, Func<Type, Type, TValue> valueFactory, out TValue resultingValue)
    {
        var hash = CombineHash(newKey, newKey2);
        ref var bucket = ref buckets[hash & (buckets.Length - 1)];
        if (bucket is null)
        {
            resultingValue = valueFactory(newKey, newKey2);
            Volatile.Write(ref bucket, new Entry(newKey, newKey2, resultingValue, hash));
            return true;
        }

        var last = bucket;
        while (true)
        {
            if (last.Key == newKey && last.Key2 == newKey2)
            {
                resultingValue = last.Value;
                return false;
            }

            if (last.Next is null)
            {
                resultingValue = valueFactory(newKey, newKey2);
                Volatile.Write(ref last.Next, new Entry(newKey, newKey2, resultingValue, hash));
                return true;
            }

            last = last.Next;
        }
    }

    private bool TryAddInternal(Type key, Type key2, Func<Type, Type, TValue> valueFactory, out TValue resultingValue)
    {
        using (this.lockObject.EnterScope())
        {
            var nextCapacity = CalculateCapacity(this.size + 1);

            if (this.buckets.Length < nextCapacity)
            {
                // Rehash. A fresh Entry is allocated for every existing one, so that rewiring 'Next'
                // does not disturb the readers which are still walking the current table.
                var nextBucket = new Entry[nextCapacity];
                foreach (var bucket in this.buckets)
                {
                    for (var e = bucket; e is not null; e = e.Next)
                    {
                        Append(nextBucket, new Entry(e.Key, e.Key2, e.Value, e.Hash));
                    }
                }

                // Add the entry (if the key pair is already present, only the resize is performed).
                var successAdd = AddToBuckets(nextBucket, key, key2, valueFactory, out resultingValue);

                // Replace the field (thread-safe for readers).
                Volatile.Write(ref this.buckets, nextBucket);

                if (successAdd)
                {
                    this.size++;
                }

                return successAdd;
            }
            else
            {
                // Add the entry (appending to the end of a bucket is thread-safe for readers).
                var successAdd = AddToBuckets(this.buckets, key, key2, valueFactory, out resultingValue);
                if (successAdd)
                {
                    this.size++;
                }

                return successAdd;
            }
        }
    }
}
