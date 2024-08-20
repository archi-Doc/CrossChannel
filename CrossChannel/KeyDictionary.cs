// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace CrossChannel;

#pragma warning disable SA1401 // Fields should be private

/*internal class KeyDictionary<TService, TKey>
    where TService : class, IRadioService
    where TKey : notnull
{
    public class Item
    {
        internal Channel<TService> Channel;

        internal int NodeIndex;

        public Item(object syncObject)
        {
            this.Channel = new(syncObject);
        }
    }

    private readonly UnorderedMap<TKey, Item> keyToItem = new();

    public KeyDictionary()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(TKey key, [MaybeNullWhen(false)] out Item item)
    {
        lock (this.keyToItem)
        {
            return this.keyToItem.TryGetValue(key, out item);
        }
    }

    public Item GetOrAdd(TKey key)
    {
        lock (this.keyToItem)
        {
            if (this.keyToItem.TryGetValue(key, out var item))
            {
                return item;
            }
            else
            {
                item = new(this.keyToItem);
                (var nodeINdex, var newlyAdded) = this.keyToItem.Add(key, item);
                item.NodeIndex = nodeINdex;
                return item;
            }
        }
    }

    public Remove(Item item)
    {
        lock (this.keyToItem)
        {
            this.keyToItem.RemoveNode(item.NodeIndex);
            item.Channel.
        }
    }
}*/
