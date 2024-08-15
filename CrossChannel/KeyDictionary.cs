// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace CrossChannel;

#pragma warning disable SA1401 // Fields should be private

internal class KeyDictionary<TKey>
    where TKey : notnull
{

    public class Item
    {
        internal object? Channel;
        private int nodeINdex;
    }

    private Item newItem = new();
    private readonly UnorderedMap<TKey, Item> keyToItem = new();
    private readonly LinkedList<Item> emptyItems = new();

    public KeyDictionary()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(TKey key, [MaybeNullWhen(false)] out Item item)
    {
        return this.keyToItem.TryGetValue(key, out item);
    }

    public Item GetOrAdd(TKey key)
    {
        (var nodeINdex, var newlyAdded) = this.keyToItem.Add(key, this.newItem);
        if (newlyAdded)
        {
        }
        if (this.keyToItem.TryGetValue(key, out var item))
        {
            return item;
        }
        else
        {
            item = new();

        }
    }

    public void TryTrim(Item item)
    {

    }
}
