// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Arc.WeakDelegate;

namespace CrossChannel
{
    internal class XChannel_Key<TKey, TMessage> : XChannel
        where TKey : notnull
    {
        public XChannel_Key(Dictionary<TKey, FastList<XChannel_Key<TKey, TMessage>>> map, TKey key, object? weakReference, Action<TMessage> method)
        {
            this.Map = map;
            lock (this.Map)
            {
                if (!map.TryGetValue(key, out var list))
                {
                    list = new();
                    map[key] = list;
                }

                this.List = list;
                this.Key = key;
                this.Index = this.List.Add(this);
            }

            if (weakReference == null)
            {
                this.StrongDelegate = method;
            }
            else
            {
                this.WeakDelegate = new(weakReference, method);
            }
        }

        public TKey Key { get; }

        internal Dictionary<TKey, FastList<XChannel_Key<TKey, TMessage>>> Map { get; }

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1401 // Fields should be private
        internal FastList<XChannel_Key<TKey, TMessage>> List;
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore SA1201 // Elements should appear in the correct order

        internal Action<TMessage>? StrongDelegate { get; set; }

        internal WeakAction<TMessage>? WeakDelegate { get; set; }

        public override void Dispose()
        {
            if (this.Index != -1)
            {
                lock (this.Map)
                {
                    var empty = this.List.Remove(this);
                    if (empty)
                    {
                        this.Map.Remove(this.Key);
                    }
                }

                this.Index = -1;
                this.WeakDelegate?.MarkForDeletion();
            }
        }
    }

    internal class XChannel_Key2<TKey, TMessage> : XChannel
        where TKey : notnull
    {
        public XChannel_Key2(ConcurrentDictionary<TKey, FastList<XChannel_Key2<TKey, TMessage>>> map, TKey key, object? weakReference, Action<TMessage> method)
        {
            this.Map = map;
            lock (map)
            {
                // this.List = map.GetOrAdd(key, x => new FastList<XChannel_Key2<TKey, TMessage>>());
                if (!map.TryGetValue(key, out this.List))
                {
                    this.List = new FastList<XChannel_Key2<TKey, TMessage>>();
                    map.TryAdd(key, this.List);
                    Interlocked.Increment(ref mapCount);
                }

                this.Key = key;
                this.Index = this.List.Add(this);
            }

            if (weakReference == null)
            {
                this.StrongDelegate = method;
            }
            else
            {
                this.WeakDelegate = new(weakReference, method);
            }
        }

        public TKey Key { get; }

        internal ConcurrentDictionary<TKey, FastList<XChannel_Key2<TKey, TMessage>>> Map { get; }

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1401 // Fields should be private
        internal FastList<XChannel_Key2<TKey, TMessage>> List;
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore SA1201 // Elements should appear in the correct order

        internal Action<TMessage>? StrongDelegate { get; set; }

        internal WeakAction<TMessage>? WeakDelegate { get; set; }

        private static int mapCount;

        public override void Dispose()
        {
            if (this.Index != -1)
            {
                // this.List.Remove(this.Index);

                if (mapCount < 16)
                {
                    this.List.Remove(this);
                }
                else
                {
                    lock (this.Map)
                    {
                        var empty = this.List.Remove(this);
                        if (empty)
                        {
                            this.Map.TryRemove(this.Key, out _);
                            Interlocked.Decrement(ref mapCount);
                        }
                    }
                }

                /*lock (this.Map)
                {
                    var empty = this.List.Remove(this.Index);
                    if (empty)
                    {
                        this.Map.TryRemove(this.Key, out _);
                    }
                }*/

                this.Index = -1;
                this.WeakDelegate?.MarkForDeletion();
            }
        }
    }

    internal class XChannel_Key3<TKey, TMessage> : XChannel
        where TKey : notnull
    {
        public XChannel_Key3(Hashtable table, TKey key, object? weakReference, Action<TMessage> method)
        {
            this.Table = table;
            lock (this.Table)
            {
                var list = this.Table[key] as FastList<XChannel_Key3<TKey, TMessage>>;
                if (list == null)
                {
                    list = new();
                    this.Table[key] = list;
                }

                this.List = list;
                this.Key = key;
                this.Index = this.List.Add(this);
            }

            if (weakReference == null)
            {
                this.StrongDelegate = method;
            }
            else
            {
                this.WeakDelegate = new(weakReference, method);
            }
        }

        public TKey Key { get; }

        internal Hashtable Table { get; }

        internal FastList<XChannel_Key3<TKey, TMessage>> List { get; }

        internal Action<TMessage>? StrongDelegate { get; set; }

        internal WeakAction<TMessage>? WeakDelegate { get; set; }

        public override void Dispose()
        {
            if (this.Index != -1)
            {
                lock (this.Table)
                {
                    var empty = this.List.Remove(this);
                    if (empty)
                    {
                        this.Table.Remove(this.Key);
                    }
                }
            }

            this.Index = -1;
            this.WeakDelegate?.MarkForDeletion();
        }
    }

#pragma warning disable SA1204 // Static elements should appear before instance elements
    internal static partial class CrossChannelExtensions
#pragma warning restore SA1204 // Static elements should appear before instance elements
    {
        internal static int Send<TKey, TMessage>(this FastList<XChannel_Key<TKey, TMessage>> list, TMessage message)
            where TKey : notnull
        {
            var array = list.GetValues();
            var numberReceived = 0;
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] is { } channel)
                {
                    if (channel.StrongDelegate != null)
                    {
                        channel.StrongDelegate(message);
                        numberReceived++;
                    }
                    else if (channel.WeakDelegate!.IsAlive)
                    {
                        channel.WeakDelegate!.Execute(message);
                        numberReceived++;
                    }
                    else
                    {
                        channel.Dispose();
                    }
                }
            }

            return numberReceived;
        }

        internal static int Send<TKey, TMessage>(this FastList<XChannel_Key2<TKey, TMessage>> list, TMessage message)
            where TKey : notnull
        {
            var array = list.GetValues();
            var numberReceived = 0;
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] is { } channel)
                {
                    if (channel.StrongDelegate != null)
                    {
                        channel.StrongDelegate(message);
                        numberReceived++;
                    }
                    else if (channel.WeakDelegate!.IsAlive)
                    {
                        channel.WeakDelegate!.Execute(message);
                        numberReceived++;
                    }
                    else
                    {
                        channel.Dispose();
                    }
                }
            }

            return numberReceived;
        }

        internal static int Send<TKey, TMessage>(this FastList<XChannel_Key3<TKey, TMessage>> list, TMessage message)
            where TKey : notnull
        {
            var array = list.GetValues();
            var numberReceived = 0;
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] is { } channel)
                {
                    if (channel.StrongDelegate != null)
                    {
                        channel.StrongDelegate(message);
                        numberReceived++;
                    }
                    else if (channel.WeakDelegate!.IsAlive)
                    {
                        channel.WeakDelegate!.Execute(message);
                        numberReceived++;
                    }
                    else
                    {
                        channel.Dispose();
                    }
                }
            }

            return numberReceived;
        }
    }
}
