// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using Arc.WeakDelegate;

namespace CrossChannel;

/// <summary>
/// Channel class to receive a message.<br/>
/// You need to call <see cref="XChannel.Dispose()"/> when the channel is no longer necessary, unless the weak reference is specified.
/// </summary>
public abstract class XChannel : IDisposable
{
#pragma warning disable SA1401 // Fields should be private
    internal int Index = -1; // The index of FastList<T>. lock() required.
#pragma warning restore SA1401 // Fields should be private

    public virtual void Dispose()
    {
        if (this.Index != -1)
        {
            this.Index = -1;
        }
    }
}

internal class XChannel_Message<TMessage> : XChannel
{
    internal XChannel_Message(FastList<XChannel_Message<TMessage>> list, object? weakReference, Action<TMessage> method)
    {
        if (weakReference == null)
        {
            this.StrongDelegate = method;
        }
        else
        {
            this.WeakDelegate = new(weakReference, method);
        }

        this.List = list;
        lock (this.List)
        {
            this.List.Add(this);
        }
    }

    internal FastList<XChannel_Message<TMessage>> List { get; }

    internal Action<TMessage>? StrongDelegate { get; set; }

    internal WeakAction<TMessage>? WeakDelegate { get; set; }

    public override void Dispose()
    {
        lock (this.List)
        {
            if (this.Index != -1)
            {
                this.List.Remove(this);
                this.WeakDelegate?.MarkForDeletion();
            }
        }
    }
}

internal class XChannel_MessageResult<TMessage, TResult> : XChannel
{
    internal XChannel_MessageResult(FastList<XChannel_MessageResult<TMessage, TResult>> list, object? weakReference, Func<TMessage, TResult> method)
    {
        if (weakReference == null)
        {
            this.StrongDelegate = method;
        }
        else
        {
            this.WeakDelegate = new(weakReference, method);
        }

        this.List = list;
        lock (this.List)
        {
            this.List.Add(this);
        }
    }

    internal FastList<XChannel_MessageResult<TMessage, TResult>> List { get; }

    internal Func<TMessage, TResult>? StrongDelegate { get; set; }

    internal WeakFunc<TMessage, TResult>? WeakDelegate { get; set; }

    public override void Dispose()
    {
        lock (this.List)
        {
            if (this.Index != -1)
            {
                this.List.Remove(this);
                this.WeakDelegate?.MarkForDeletion();
            }
        }
    }
}

internal class XCollection_KeyMessage<TKey, TMessage>
    where TKey : notnull
{
    internal ConcurrentDictionary<TKey, FastList<XChannel_KeyMessage<TKey, TMessage>>> Dictionary { get; } = new();

    internal int Count { get; set; } // ConcurrentDictionary.Count is just slow.

    internal int CleanupCount { get; set; }
}

internal class XChannel_KeyMessage<TKey, TMessage> : XChannel
    where TKey : notnull
{
    public XChannel_KeyMessage(XCollection_KeyMessage<TKey, TMessage> collection, TKey key, object? weakReference, Action<TMessage> method)
    {
        this.Key = key;
        if (weakReference == null)
        {
            this.StrongDelegate = method;
        }
        else
        {
            this.WeakDelegate = new(weakReference, method);
        }

        this.Collection = collection;
        lock (this.Collection)
        {
            if (!this.Collection.Dictionary.TryGetValue(key, out this.List!))
            {
                this.List = new FastList<XChannel_KeyMessage<TKey, TMessage>>();
                this.Collection.Dictionary.TryAdd(key, this.List);
                this.Collection.Count++;
            }

            this.List.Add(this);
        }
    }

    public TKey Key { get; }

    internal XCollection_KeyMessage<TKey, TMessage> Collection { get; }

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1401 // Fields should be private
    internal FastList<XChannel_KeyMessage<TKey, TMessage>> List;
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore SA1201 // Elements should appear in the correct order

    internal Action<TMessage>? StrongDelegate { get; set; }

    internal WeakAction<TMessage>? WeakDelegate { get; set; }

    public override void Dispose()
    {
        lock (this.Collection)
        {
            if (this.Index != -1)
            {
                var empty = this.List.Remove(this);
                this.WeakDelegate?.MarkForDeletion();

                if (empty && this.Collection.Count >= CrossChannelConst.HoldDictionaryThreshold)
                {
                    this.Collection.Dictionary.TryRemove(this.Key, out _);
                    this.Collection.Count--;
                    this.List.Dispose();
                }
            }
        }
    }
}

internal class XCollection_KeyMessageResult<TKey, TMessage, TResult>
    where TKey : notnull
{
    internal ConcurrentDictionary<TKey, FastList<XChannel_KeyMessageResult<TKey, TMessage, TResult>>> Dictionary { get; } = new();

    internal int Count { get; set; } // ConcurrentDictionary.Count is just slow.

    internal int CleanupCount { get; set; }
}

internal class XChannel_KeyMessageResult<TKey, TMessage, TResult> : XChannel
    where TKey : notnull
{
    public XChannel_KeyMessageResult(XCollection_KeyMessageResult<TKey, TMessage, TResult> collection, TKey key, object? weakReference, Func<TMessage, TResult> method)
    {
        this.Key = key;
        if (weakReference == null)
        {
            this.StrongDelegate = method;
        }
        else
        {
            this.WeakDelegate = new(weakReference, method);
        }

        this.Collection = collection;
        lock (this.Collection)
        {
            if (!this.Collection.Dictionary.TryGetValue(key, out this.List!))
            {
                this.List = new FastList<XChannel_KeyMessageResult<TKey, TMessage, TResult>>();
                this.Collection.Dictionary.TryAdd(key, this.List);
                this.Collection.Count++;
            }

            this.List.Add(this);
        }
    }

    public TKey Key { get; }

    internal XCollection_KeyMessageResult<TKey, TMessage, TResult> Collection { get; }

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1401 // Fields should be private
    internal FastList<XChannel_KeyMessageResult<TKey, TMessage, TResult>> List;
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore SA1201 // Elements should appear in the correct order

    internal Func<TMessage, TResult>? StrongDelegate { get; set; }

    internal WeakFunc<TMessage, TResult>? WeakDelegate { get; set; }

    public override void Dispose()
    {
        lock (this.Collection)
        {
            if (this.Index != -1)
            {
                var empty = this.List.Remove(this);
                this.WeakDelegate?.MarkForDeletion();

                if (empty && this.Collection.Count >= CrossChannelConst.HoldDictionaryThreshold)
                {
                    this.Collection.Dictionary.TryRemove(this.Key, out _);
                    this.Collection.Count--;
                    this.List.Dispose();
                }
            }
        }
    }
}
