// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1214 // Readonly fields should appear before non-readonly fields
#pragma warning disable SA1401 // Fields should be private

using System.Threading;

namespace CrossChannel.Internal;

/*public readonly record struct TwoTypeKey(Type Key, Type Key2)
{
    public override int GetHashCode()
        => HashCode.Combine(this.Key, this.Key2);
}*/

internal class ThreadsafeTwoTypeKeyHashtable<TValue>
{
    private const double LoadFactor = 0.75d;

    private readonly Lock lockObject = new();
    private Entry[] buckets;
    private int size; // only use in writer lock

    private class Entry
    {
        public Entry(Type key, Type key2, TValue value, int hash)
        {
            this.Key = key;
            this.Key2 = key2;
            this.Value = value;
            this.Hash = hash;
        }

        internal Type Key;
        internal Type Key2;
        internal TValue Value;
        internal int Hash;
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
        TValue? v;
        if (this.TryGetValue(key, key2, out v))
        {
            return v;
        }

        this.TryAddInternal(key, key2, valueFactory, out v);
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(Type key, Type key2, [MaybeNullWhen(false)] out TValue value)
    {
        Entry[] table = this.buckets;
        var hash = key.GetHashCode() ^ key2.GetHashCode();
        Entry? entry = table[hash & table.Length - 1];

        while (entry != null)
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

    private bool TryAddInternal(Type key, Type key2, Func<Type, Type, TValue> valueFactory, out TValue resultingValue)
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
                        var newEntry = new Entry(e.Key, e.Key2, e.Value, e.Hash);
                        this.AddToBuckets(nextBucket, key, key2, newEntry, null!, out resultingValue);
                        e = e.Next;
                    }
                }

                // add entry(if failed to add, only do resize)
                var successAdd = this.AddToBuckets(nextBucket, key, key2, null, valueFactory, out resultingValue);

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
                var successAdd = this.AddToBuckets(this.buckets, key, key2, null, valueFactory, out resultingValue);
                if (successAdd)
                {
                    this.size++;
                }

                return successAdd;
            }
        }
    }

    private bool AddToBuckets(Entry[] buckets, Type newKey, Type newKey2, Entry? newEntryOrNull, Func<Type, Type, TValue> valueFactory, out TValue resultingValue)
    {
        var h = (newEntryOrNull != null) ? newEntryOrNull.Hash : (newKey.GetHashCode() ^ newKey2.GetHashCode());
        if (buckets[h & (buckets.Length - 1)] == null)
        {
            if (newEntryOrNull != null)
            {
                resultingValue = newEntryOrNull.Value;
                System.Threading.Volatile.Write(ref buckets[h & (buckets.Length - 1)], newEntryOrNull);
            }
            else
            {
                resultingValue = valueFactory(newKey, newKey2);
                System.Threading.Volatile.Write(ref buckets[h & (buckets.Length - 1)], new Entry(newKey, newKey2, resultingValue, h));
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
                        resultingValue = valueFactory(newKey, newKey2);
                        System.Threading.Volatile.Write(ref searchLastEntry.Next!, new Entry(newKey, newKey2, resultingValue, h));
                    }

                    break;
                }

                searchLastEntry = searchLastEntry.Next;
            }
        }

        return true;
    }
}
