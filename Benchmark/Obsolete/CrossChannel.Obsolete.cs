// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arc.WeakDelegate;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1649 // File name should match first type name

namespace Arc.CrossChannel.Obsolete;

/*public readonly struct ChannelIdentification
{
    public readonly Type MessageType; // The type of a message.
    public readonly Type? ResultType; // The type of a result.

    public ChannelIdentification(Type messageType)
    {
        this.MessageType = messageType;
        this.ResultType = null;
    }

    public ChannelIdentification(Type messageType, Type? resultType)
    {
        this.MessageType = messageType;
        this.ResultType = resultType;
    }

    public override int GetHashCode() => HashCode.Combine(this.MessageType, this.ResultType);

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != typeof(ChannelIdentification))
        {
            return false;
        }

        var x = (ChannelIdentification)obj;
        return this.MessageType == x.MessageType && this.ResultType == x.ResultType;
    }
}

public static class CrossChannel
{
    private const int CleanupThreshold = 16;

    private static object cs = new object();

    private static int cleanupCount = 0;

    static CrossChannel()
    {
    }

    private static XChannel AddXChannel(LinkedList<XChannel> list, ChannelIdentification identification, object? targetId, bool exclusiveChannel, IWeakDelegate weakDelegate)
    { // lock (cs) required.
        if (exclusiveChannel)
        {
            if (list.Count > 0)
            { // other channel already exists.
                throw new InvalidOperationException();
            }
        }
        else
        {
            if (list.First?.Value.ExclusiveChannel == true)
            { // Exclusive channel exists.
                throw new InvalidOperationException();
            }
        }

        // New XChannel
        var channel = new XChannel(identification, targetId, exclusiveChannel, weakDelegate);

        // list: Identification to XChannels.
        channel.Node = list.AddLast(channel);

        return channel;
    }

    private static void DecrementReferenceCount(XChannel[] array)
    {
        lock (cs)
        {
            foreach (var x in array)
            {
                if (x.ReferenceCount > 0)
                {
                    x.ReferenceCount--;
                }
            }
        }
    }

    public static XChannel Open<TMessage, TResult>(Func<TMessage, TResult> method, object? targetId = null, bool exclusiveChannel = false)
    {
        XChannel channel;
        var identification = new ChannelIdentification(typeof(TMessage), typeof(TResult));

        lock (cs)
        {
            var list = Cache_WeakFunction<TMessage, TResult>.List;
            channel = AddXChannel(list, identification, targetId, exclusiveChannel, new WeakFunc<TMessage, TResult>(method.Target!, method));
            CleanupList(list);
        }

        return channel;
    }

    public static XChannel Open<TMessage>(object weakReference, Action<TMessage> method, object? targetId = null, bool exclusiveChannel = false)
    {
        XChannel channel;
        var identification = new ChannelIdentification(typeof(TMessage));

        lock (cs)
        {
            var list = Cache_WeakAction<TMessage>.List;
            channel = AddXChannel(list, identification, targetId, exclusiveChannel, new WeakAction<TMessage>(weakReference, method));
            CleanupList(list);
        }

        return channel;
    }

    public static XChannel OpenAsync<TMessage, TResult>(Func<TMessage, Task<TResult>> method, object? targetId = null, bool exclusiveChannel = false)
    {
        XChannel channel;
        var identification = new ChannelIdentification(typeof(TMessage), typeof(Task<TResult>));

        lock (cs)
        {
            var list = Cache_WeakFunction<TMessage, Task<TResult>>.List;
            channel = AddXChannel(list, identification, targetId, exclusiveChannel, new WeakFunc<TMessage, Task<TResult>>(method.Target!, method));
            CleanupList(list);
        }

        return channel;
    }

    public static XChannel OpenAsync<TMessage>(Func<TMessage, Task> method, object? targetId = null, bool exclusiveChannel = false)
    {
        XChannel channel;
        var identification = new ChannelIdentification(typeof(TMessage), typeof(Task));

        lock (cs)
        {
            var list = Cache_WeakFunction<TMessage, Task>.List;
            channel = AddXChannel(list, identification, targetId, exclusiveChannel, new WeakFunc<TMessage, Task>(method.Target!, method));
            CleanupList(list);
        }

        return channel;
    }

    private static void CleanupList(LinkedList<XChannel> list)
    {
        if (++cleanupCount < CleanupThreshold)
        {
            return;
        }

        cleanupCount = 0; // Initialize.

        LinkedListNode<XChannel>? node = list.First;
        LinkedListNode<XChannel>? nextNode;

        while (node != null)
        {
            nextNode = node.Next;

            if (!node.Value.IsAlive && node.Value.ReferenceCount == 0)
            {
                CloseChannel(node.Value);
            }

            node = nextNode;
        }
    }

    public static void Close(XChannel channel)
    {
        if (channel.Disposed == true)
        {// already closed.
            return;
        }

        while (true)
        {
            lock (cs)
            {
                if (channel.ReferenceCount == 0)
                {// reference countが0（Send / Receive処理をしていない状態）になったら、Close
                    CloseChannel(channel);
                    break;
                }
            }
#if NETFX_CORE
            Task.Delay(50).Wait();
#else
            System.Threading.Thread.Sleep(50);
#endif
        }
    }

    private static void CloseChannel(XChannel channel)
    {// lock (cs) required. Reference count must be 0.
        // list: Identification to XChannels.
        if (channel.Node != null)
        {
            var list = channel.Node.List;
            if (list != null)
            {
                list.Remove(channel.Node);
            }
        }

        channel.MarkForDeletion();
    }

    private static XChannel[] PrepareXChannelArray(LinkedList<XChannel> list, object? targetId)
    { // lock (cs) required. Convert LinkedList to Array, release garbage collected object, and increment reference count.
        var array = new XChannel[list.Count];
        var arrayCount = 0;
        var node = list.First;
        LinkedListNode<XChannel>? nextNode;

        if (targetId == null)
        {
            while (node != null)
            {
                nextNode = node.Next;

                if (node.Value.IsAlive)
                {// The instance is still alive.
                    array[arrayCount++] = node.Value;
                }
                else if (node.Value.ReferenceCount == 0)
                {// The instance is garbage collected and reference count is 0.
                    CloseChannel(node.Value);
                }

                node = nextNode;
            }
        }
        else
        {
            while (node != null)
            {
                nextNode = node.Next;

                if (node.Value.IsAlive)
                {// The instance is still alive.
                    if (node.Value.TargetId == targetId)
                    {
                        array[arrayCount++] = node.Value;
                    }
                }
                else if (node.Value.ReferenceCount == 0)
                {// Garbage collected and reference count is 0.
                    CloseChannel(node.Value);
                }

                node = nextNode;
            }
        }

        if (array.Length != arrayCount)
        {
            Array.Resize(ref array, arrayCount);
        }

        foreach (var x in array)
        {
            x.ReferenceCount++;
        }

        return array;
    }

    /// <summary>
    /// Send a message to receivers.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the return value from receivers.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <returns>An array of the return values (TResult).</returns>
    public static TResult[] Send<TMessage, TResult>(TMessage message) => SendTarget<TMessage, TResult>(message, null);

    /// <summary>
    /// Send a message to receivers.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the return value.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="targetId">The receiver with the same target id will receive this message. Set null to broadcast.</param>
    /// <returns>An array of the return values (TResult).</returns>
    public static TResult[] SendTarget<TMessage, TResult>(TMessage message, object? targetId)
    {
        XChannel[] array;

        lock (cs)
        {
            array = PrepareXChannelArray(Cache_WeakFunction<TMessage, TResult>.List, targetId);
        }

        TResult[] results = new TResult[array.Length];
        int resultsCount = 0;

        try
        {
            foreach (var x in array)
            {
                var d = x.WeakDelegate as WeakFunc<TMessage, TResult>;
                if (d == null)
                {
                    continue;
                }

                var result = d.Execute(message, out var executed);
                if (executed)
                {
                    results[resultsCount++] = result!;
                }
            }
        }
        finally
        {
            DecrementReferenceCount(array);
        }

        if (results.Length != resultsCount)
        {
            Array.Resize(ref results, resultsCount);
        }

        return results;
    }

    /// <summary>
    /// Send a message to receivers.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <returns>A number of the receivers.</returns>
    public static int Send<TMessage>(TMessage message) => SendTarget<TMessage>(message, null);

    /// <summary>
    /// Send a message to receivers.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="targetId">The receiver with the same target id will receive this message. Set null to broadcast.</param>
    /// <returns>A number of the receivers.</returns>
    public static int SendTarget<TMessage>(TMessage message, object? targetId)
    {
        XChannel[] array;
        var numberReceived = 0;

        lock (cs)
        {
            array = PrepareXChannelArray(Cache_WeakAction<TMessage>.List, targetId);
        }

        try
        {
            foreach (var x in array)
            {
                var d = x.WeakDelegate as WeakAction<TMessage>;
                if (d == null)
                {
                    continue;
                }

                d.Execute(message, out var executed);
                if (executed)
                {
                    numberReceived++;
                }
            }
        }
        finally
        {
            DecrementReferenceCount(array);
        }

        return numberReceived;
    }

    /// <summary>
    /// Send a message to receivers asynchronously.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the return value from receivers.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <returns>A task that represents the completion of all of the sending tasks.</returns>
    public static Task<TResult[]> SendAsync<TMessage, TResult>(TMessage message) => SendTargetAsync<TMessage, TResult>(message, null);

    /// <summary>
    /// Send a message to receivers asynchronously.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the return value from receivers.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="targetId">The receiver with the same target id will receive this message. Set null to broadcast.</param>
    /// <returns>A task that represents the completion of all of the sending tasks.</returns>
    public static Task<TResult[]> SendTargetAsync<TMessage, TResult>(TMessage message, object? targetId)
    {
        XChannel[] array;

        lock (cs)
        {
            array = PrepareXChannelArray(Cache_WeakFunction<TMessage, Task<TResult>>.List, targetId);
        }

        var taskArray = new Task<TResult>[array.Length];
        int taskCount = 0;

        try
        {
            foreach (var x in array)
            {
                var d = x.WeakDelegate as WeakFunc<TMessage, Task<TResult>>;
                var task = d?.Execute(message);
                if (task != null)
                {
                    taskArray[taskCount++] = task;
                }
            }
        }
        finally
        {
            DecrementReferenceCount(array);
        }

        if (taskArray.Length != taskCount)
        {
            Array.Resize(ref taskArray, taskCount);
        }

        return Task.WhenAll(taskArray);
    }

    /// <summary>
    /// Send a message to receivers asynchronously.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <returns>A task that represents the completion of the sending task.</returns>
    public static Task SendAsync<TMessage>(TMessage message) => SendTargetAsync<TMessage>(message, null);

    /// <summary>
    /// Send a message to receivers asynchronously.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="targetId">The receiver with the same target id will receive this message. Set null to broadcast.</param>
    /// <returns>A task that represents the completion of the sending task.</returns>
    public static Task SendTargetAsync<TMessage>(TMessage message, object? targetId)
    {
        XChannel[] array;

        lock (cs)
        {
            array = PrepareXChannelArray(Cache_WeakFunction<TMessage, Task>.List, targetId);
        }

        var taskArray = new Task[array.Length];
        int taskCount = 0;

        try
        {
            foreach (var x in array)
            {
                var d = x.WeakDelegate as WeakFunc<TMessage, Task>;
                var task = d?.Execute(message);
                if (task != null)
                {
                    taskArray[taskCount++] = task;
                }
            }
        }
        finally
        {
            DecrementReferenceCount(array);
        }

        if (taskArray.Length != taskCount)
        {
            Array.Resize(ref taskArray, taskCount);
        }

        return Task.WhenAll(taskArray);
    }

#pragma warning disable SA1401 // Fields should be private
    private static class Cache_WeakAction<TMessage>
    {
        public static LinkedList<XChannel> List;

        static Cache_WeakAction()
        {
            List = new LinkedList<XChannel>();
        }
    }

    private static class Cache_WeakFunction<TMessage, TResult>
    {
        public static LinkedList<XChannel> List;

        static Cache_WeakFunction()
        {
            List = new LinkedList<XChannel>();
        }
    }
#pragma warning restore SA1401 // Fields should be private
}

public class XChannel : IDisposable
{
    public XChannel(ChannelIdentification channelIdentification, object? targetId, bool exclusiveChannel, IWeakDelegate weakDelegate)
    {
        this.Identification = channelIdentification;
        this.TargetId = targetId;
        this.ExclusiveChannel = exclusiveChannel;
        this.WeakDelegate = weakDelegate;
    }

    public bool Disposed { get; private set; } = false;

    internal LinkedListNode<XChannel>? Node { get; set; }

    internal int ReferenceCount { get; set; }

    public ChannelIdentification Identification { get; private set; }

    public object? TargetId { get; set; }

    public bool ExclusiveChannel { get; private set; }

    public IWeakDelegate WeakDelegate { get; private set; }

    public bool IsAlive => this.WeakDelegate.IsAlive;

    public void MarkForDeletion()
    {
        this.Node = null;
        this.ReferenceCount = 0;
        this.Identification = default(ChannelIdentification);
        this.TargetId = null;
        this.WeakDelegate.MarkForDeletion();

        this.Disposed = true;
    }

    public void Dispose()
    {
        this.Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.Disposed)
        {
            if (disposing)
            {
                CrossChannel.Close(this);
            }

            this.Disposed = true;
        }
    }
}*/
