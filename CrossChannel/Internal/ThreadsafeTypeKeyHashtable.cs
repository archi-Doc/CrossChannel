// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1214 // Readonly fields should appear before non-readonly fields
#pragma warning disable SA1401 // Fields should be private

using System.Threading;

namespace CrossChannel.Internal;

internal class ThreadsafeTypeKeyHashtable<TValue>
{
    private const double LoadFactor = 0.75d;

    private readonly Lock lockObject = new();
    private Entry[] buckets;
    private int size; // only use in writer lock

    private class Entry
    {
        public Entry(Type key, TValue value, int hash)
        {
            this.Key = key;
            this.Value = value;
            this.Hash = hash;
        }

        internal Type Key;
        internal TValue Value;
        internal int Hash;
        internal Entry? Next;
    }

    public ThreadsafeTypeKeyHashtable(int capacity = 4)
    {
        var tableSize = CalculateCapacity(capacity);
        this.buckets = new Entry[tableSize];
    }

    public bool TryAdd(Type key, TValue value)
        => this.TryAdd(key, _ => value);

    public bool TryAdd(Type key, Func<Type, TValue> valueFactory)
        => this.TryAddInternal(key, valueFactory, out TValue _);

    public TValue GetOrAdd(Type key, Func<Type, TValue> valueFactory)
    {
        TValue? v;
        if (this.TryGetValue(key, out v))
        {
            return v;
        }

        this.TryAddInternal(key, valueFactory, out v);
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(Type key, [MaybeNullWhen(false)] out TValue value)
    {
        Entry[] table = this.buckets;
        var hash = key.GetHashCode();
        Entry? entry = table[hash & table.Length - 1];

        while (entry != null)
        {
            if (entry.Key == key)
            {
                value = entry.Value;
                return true;
            }

            entry = entry.Next;
        }

        value = default;
        return false;
    }

    private static int CalculateCapacity(int collectionSize)
    {
        var initialCapacity = (int)(collectionSize / LoadFactor);
        var capacity = 1;
        while (capacity < initialCapacity)
        {
            capacity <<= 1;
        }

        if (capacity < 8)
        {
            return 8;
        }

        return capacity;
    }

    private bool TryAddInternal(Type key, Func<Type, TValue> valueFactory, out TValue resultingValue)
    {
        using (this.lockObject.EnterScope())
        {
            var nextCapacity = CalculateCapacity(this.size + 1);

            if (this.buckets.Length < nextCapacity)
            {
                // rehash
                var nextBucket = new Entry[nextCapacity];
                for (int i = 0; i < this.buckets.Length; i++)
                {
                    Entry? e = this.buckets[i];
                    while (e != null)
                    {
                        var newEntry = new Entry(e.Key, e.Value, e.Hash);
                        this.AddToBuckets(nextBucket, key, newEntry, null!, out resultingValue);
                        e = e.Next;
                    }
                }

                // add entry(if failed to add, only do resize)
                var successAdd = this.AddToBuckets(nextBucket, key, null, valueFactory, out resultingValue);

                // replace field(threadsafe for read)
                System.Threading.Volatile.Write(ref this.buckets, nextBucket);

                if (successAdd)
                {
                    this.size++;
                }

                return successAdd;
            }
            else
            {
                // add entry(insert last is thread safe for read)
                var successAdd = this.AddToBuckets(this.buckets, key, null, valueFactory, out resultingValue);
                if (successAdd)
                {
                    this.size++;
                }

                return successAdd;
            }
        }
    }

    private bool AddToBuckets(Entry[] buckets, Type newKey, Entry? newEntryOrNull, Func<Type, TValue> valueFactory, out TValue resultingValue)
    {
        var h = (newEntryOrNull != null) ? newEntryOrNull.Hash : newKey.GetHashCode();
        if (buckets[h & (buckets.Length - 1)] == null)
        {
            if (newEntryOrNull != null)
            {
                resultingValue = newEntryOrNull.Value;
                System.Threading.Volatile.Write(ref buckets[h & (buckets.Length - 1)], newEntryOrNull);
            }
            else
            {
                resultingValue = valueFactory(newKey);
                System.Threading.Volatile.Write(ref buckets[h & (buckets.Length - 1)], new Entry(newKey, resultingValue, h));
            }
        }
        else
        {
            Entry searchLastEntry = buckets[h & (buckets.Length - 1)];
            while (true)
            {
                if (searchLastEntry.Key == newKey)
                {
                    resultingValue = searchLastEntry.Value;
                    return false;
                }

                if (searchLastEntry.Next == null)
                {
                    if (newEntryOrNull != null)
                    {
                        resultingValue = newEntryOrNull.Value;
                        System.Threading.Volatile.Write(ref searchLastEntry.Next!, newEntryOrNull);
                    }
                    else
                    {
                        resultingValue = valueFactory(newKey);
                        System.Threading.Volatile.Write(ref searchLastEntry.Next!, new Entry(newKey, resultingValue, h));
                    }

                    break;
                }

                searchLastEntry = searchLastEntry.Next;
            }
        }

        return true;
    }
}
