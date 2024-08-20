// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CrossChannel.Obsolete;

/// <summary>
/// RadioClass is a non-static version of <see cref="ObsoleteRadio"/>.<br/>
/// It's easy to use.<br/>
/// 1. Open a channel (register a subscriber) : <see cref="Open{TMessage}(Action{TMessage}, object?)"/>.<br/>
/// 2. Send a message (publish) : <see cref="Send{TMessage}(TMessage)"/>.
/// </summary>
public class ObsoleteRadioClass
{
    /// <summary>
    /// Open a channel to receive a message.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="method">The delegate that is called when the message is sent.</param>
    /// <param name="weakReference">The weak reference of the object.<br/>
    /// The channel will be automatically closed when the object is garbage collected.<br/>
    /// To achieve maximum performance, you can set this value to null (DON'T forget to close the channel manually).</param>
    /// <returns>A new instance of XChannel.<br/>
    /// You need to call <see cref="XChannel.Dispose()"/> when the channel is no longer necessary, unless the weak reference is specified.</returns>
    public XChannel Open<TMessage>(Action<TMessage> method, object? weakReference = null)
    {
        var list = (FastList<XChannel_Message<TMessage>>)this.dictionaryMessage.GetOrAdd(
            typeof(TMessage),
            x => new FastList<XChannel_Message<TMessage>>());

        if (list.CleanupCount++ >= ObsoleteRadioConstants.ChannelTrimThreshold)
        {
            lock (list)
            {
                list.CleanupCount = 1;
                list.Cleanup();
            }
        }

        var channel = new XChannel_Message<TMessage>(list, weakReference, method);
        return channel;
    }

    /// <summary>
    /// Open a channel to receive a message asynchronously.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="method">The delegate that is called when the message is sent.</param>
    /// <param name="weakReference">The weak reference of the object.<br/>
    /// The channel will be automatically closed when the object is garbage collected.<br/>
    /// To achieve maximum performance, you can set this value to null (DON'T forget to close the channel manually).</param>
    /// <returns>A new instance of XChannel.<br/>
    /// You need to call <see cref="XChannel.Dispose()"/> when the channel is no longer necessary, unless the weak reference is specified.</returns>
    public XChannel OpenAsync<TMessage>(Func<TMessage, Task> method, object? weakReference = null) => this.OpenTwoWay<TMessage, Task>(method, weakReference);

    /// <summary>
    /// Open a channel to receive a message asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="key">Specify a key to limit the channels to receive the message.</param>
    /// <param name="method">The delegate that is called when the message is sent.</param>
    /// <param name="weakReference">The weak reference of the object.<br/>
    /// The channel will be automatically closed when the object is garbage collected.<br/>
    /// To achieve maximum performance, you can set this value to null (DON'T forget to close the channel manually).</param>
    /// <returns>A new instance of XChannel.<br/>
    /// You need to call <see cref="XChannel.Dispose()"/> when the channel is no longer necessary, unless the weak reference is specified.</returns>
    public XChannel OpenAsyncKey<TKey, TMessage>(TKey key, Func<TMessage, Task> method, object? weakReference = null)
        where TKey : notnull => this.OpenTwoWayKey<TKey, TMessage, Task>(key, method, weakReference);

    /// <summary>
    /// Open a channel to receive a message.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="key">Specify a key to limit the channels to receive the message.</param>
    /// <param name="method">The delegate that is called when the message is sent.</param>
    /// <param name="weakReference">The weak reference of the object.<br/>
    /// The channel will be automatically closed when the object is garbage collected.<br/>
    /// To achieve maximum performance, you can set this value to null (DON'T forget to close the channel manually).</param>
    /// <returns>A new instance of XChannel.<br/>
    /// You need to call <see cref="XChannel.Dispose()"/> when the channel is no longer necessary, unless the weak reference is specified.</returns>
    public XChannel OpenKey<TKey, TMessage>(TKey key, Action<TMessage> method, object? weakReference = null)
        where TKey : notnull
    {
        var collection = (XCollection_KeyMessage<TKey, TMessage>)this.dictionaryKeyMessage.GetOrAdd(
            new Identifier_KeyMessage(typeof(TKey), typeof(TMessage)),
            x => new XCollection_KeyMessage<TKey, TMessage>());

        if (collection.CleanupCount++ >= ObsoleteRadioConstants.CleanupDictionaryThreshold)
        {
            lock (collection)
            {
                collection.CleanupCount = 1;
                collection.Cleanup();
            }
        }

        var channel = new XChannel_KeyMessage<TKey, TMessage>(collection, key, weakReference, method);
        return channel;
    }

    /// <summary>
    /// Open a channel to receive a message and send a result.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="method">The delegate that is called when the message is sent.</param>
    /// <param name="weakReference">The weak reference of the object.<br/>
    /// The channel will be automatically closed when the object is garbage collected.<br/>
    /// To achieve maximum performance, you can set this value to null (DON'T forget to close the channel manually).</param>
    /// <returns>A new instance of XChannel.<br/>
    /// You need to call <see cref="XChannel.Dispose()"/> when the channel is no longer necessary, unless the weak reference is specified.</returns>
    public XChannel OpenTwoWay<TMessage, TResult>(Func<TMessage, TResult> method, object? weakReference = null)
    {
        var list = (FastList<XChannel_MessageResult<TMessage, TResult>>)this.dictionaryMessageResult.GetOrAdd(
            new Identifier_MessageResult(typeof(TMessage), typeof(TResult)),
            x => new FastList<XChannel_MessageResult<TMessage, TResult>>());

        if (list.CleanupCount++ >= ObsoleteRadioConstants.ChannelTrimThreshold)
        {
            lock (list)
            {
                list.CleanupCount = 1;
                list.Cleanup();
            }
        }

        var channel = new XChannel_MessageResult<TMessage, TResult>(list, weakReference, method);
        return channel;
    }

    /// <summary>
    /// Open a channel to receive a message and send a result asynchronously.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="method">The delegate that is called when the message is sent.</param>
    /// <param name="weakReference">The weak reference of the object.<br/>
    /// The channel will be automatically closed when the object is garbage collected.<br/>
    /// To achieve maximum performance, you can set this value to null (DON'T forget to close the channel manually).</param>
    /// <returns>A new instance of XChannel.<br/>
    /// You need to call <see cref="XChannel.Dispose()"/> when the channel is no longer necessary, unless the weak reference is specified.</returns>
    public XChannel OpenTwoWayAsync<TMessage, TResult>(Func<TMessage, Task<TResult>> method, object? weakReference = null) => this.OpenTwoWay<TMessage, Task<TResult>>(method, weakReference);

    /// <summary>
    /// Open a channel to receive a message and send a result asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="key">Specify a key to limit the channels to receive the message.</param>
    /// <param name="method">The delegate that is called when the message is sent.</param>
    /// <param name="weakReference">The weak reference of the object.<br/>
    /// The channel will be automatically closed when the object is garbage collected.<br/>
    /// To achieve maximum performance, you can set this value to null (DON'T forget to close the channel manually).</param>
    /// <returns>A new instance of XChannel.<br/>
    /// You need to call <see cref="XChannel.Dispose()"/> when the channel is no longer necessary, unless the weak reference is specified.</returns>
    public XChannel OpenTwoWayAsyncKey<TKey, TMessage, TResult>(TKey key, Func<TMessage, Task<TResult>> method, object? weakReference = null)
         where TKey : notnull => this.OpenTwoWayKey<TKey, TMessage, Task<TResult>>(key, method, weakReference);

    /// <summary>
    /// Open a channel to receive a message and send a result.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="key">Specify a key to limit the channels to receive the message.</param>
    /// <param name="method">The delegate that is called when the message is sent.</param>
    /// <param name="weakReference">The weak reference of the object.<br/>
    /// The channel will be automatically closed when the object is garbage collected.<br/>
    /// To achieve maximum performance, you can set this value to null (DON'T forget to close the channel manually).</param>
    /// <returns>A new instance of XChannel.<br/>
    /// You need to call <see cref="XChannel.Dispose()"/> when the channel is no longer necessary, unless the weak reference is specified.</returns>
    public XChannel OpenTwoWayKey<TKey, TMessage, TResult>(TKey key, Func<TMessage, TResult> method, object? weakReference = null)
        where TKey : notnull
    {
        var collection = (XCollection_KeyMessageResult<TKey, TMessage, TResult>)this.dictionaryKeyMessageResult.GetOrAdd(
            new Identifier_KeyMessageResult(typeof(TKey), typeof(TMessage), typeof(TResult)),
            x => new XCollection_KeyMessageResult<TKey, TMessage, TResult>());

        if(collection.CleanupCount++ >= ObsoleteRadioConstants.CleanupDictionaryThreshold)
        {
            lock (collection)
            {
                collection.CleanupCount = 1;
                collection.Cleanup();
            }
        }

        var channel = new XChannel_KeyMessageResult<TKey, TMessage, TResult>(collection, key, weakReference, method);
        return channel;
    }

    /// <summary>
    /// Close the channel.
    /// </summary>
    /// <param name="channel">The channel to close.</param>
    public void Close(XChannel channel) => channel.Dispose();

    /// <summary>
    /// Send a message.<br/>
    /// Message: <typeparamref name="TMessage"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <returns>The number of channels that received the message.</returns>
    public int Send<TMessage>(TMessage message)
    {
        if (!this.dictionaryMessage.TryGetValue(typeof(TMessage), out var obj))
        {
            return 0;
        }

        var list = (FastList<XChannel_Message<TMessage>>)obj;
        return list.Send(message);
    }

    /// <summary>
    /// Send a message asynchronously.<br/>
    /// Message: <typeparamref name="TMessage"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <returns><see cref="Task"/>.</returns>
    public Task SendAsync<TMessage>(TMessage message)
    {
        if (!this.dictionaryMessageResult.TryGetValue(new Identifier_MessageResult(typeof(TMessage), typeof(Task)), out var obj))
        {
            return Task.CompletedTask;
        }

        var list = (FastList<XChannel_MessageResult<TMessage, Task>>)obj;
        return list.SendAsync(message);
    }

    /// <summary>
    /// Send a message to a channel with the same key asynchronously.<br/>
    /// Key: <typeparamref name="TKey"/>.<br/>
    /// Message: <typeparamref name="TMessage"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="key">The channel with the same key receives the message.</param>
    /// <param name="message">The message to send.</param>
    /// <returns><see cref="Task"/>.</returns>
    public Task SendAsyncKey<TKey, TMessage>(TKey key, TMessage message)
        where TKey : notnull
    {
        if (!this.dictionaryKeyMessageResult.TryGetValue(new Identifier_KeyMessageResult(typeof(TKey), typeof(TMessage), typeof(Task)), out var obj))
        {
            return Task.CompletedTask;
        }

        var collection = (XCollection_KeyMessageResult<TKey, TMessage, Task>)obj;
        if (!collection.Dictionary.TryGetValue(key, out var list))
        {
            return Task.CompletedTask;
        }

        return list.SendAsync(message);
    }

    /// <summary>
    /// Send a message to a channel with the same key.<br/>
    /// Key: <typeparamref name="TKey"/>.<br/>
    /// Message: <typeparamref name="TMessage"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="key">The channel with the same key receives the message.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>The number of channels that received the message.</returns>
    public int SendKey<TKey, TMessage>(TKey key, TMessage message)
        where TKey : notnull
    {
        if (!this.dictionaryKeyMessage.TryGetValue(new Identifier_KeyMessage(typeof(TKey), typeof(TMessage)), out var obj))
        {
            return 0;
        }

        var collection = (XCollection_KeyMessage<TKey, TMessage>)obj;
        if (!collection.Dictionary.TryGetValue(key, out var list))
        {
            return 0;
        }

        return list.Send(message);
    }

    /// <summary>
    /// Send a message and receive the result.<br/>
    /// Message: <typeparamref name="TMessage"/>.<br/>
    /// Result: <typeparamref name="TResult"/>.<br/>
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <returns>The result; An array of <typeparamref name="TResult"/>.</returns>
    public TResult[] SendTwoWay<TMessage, TResult>(TMessage message)
    {
        if (!this.dictionaryMessageResult.TryGetValue(new Identifier_MessageResult(typeof(TMessage), typeof(TResult)), out var obj))
        {
            return Array.Empty<TResult>();
        }

        var list = (FastList<XChannel_MessageResult<TMessage, TResult>>)obj;
        return list.Send(message);
    }

    /// <summary>
    /// Send a message and receive the result asynchronously.<br/>
    /// Message: <typeparamref name="TMessage"/>.<br/>
    /// Result: <typeparamref name="TResult"/>.<br/>
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <returns>The result; <see cref="Task"/>&lt;<typeparamref name="TResult"/>[]&gt;.</returns>
    public Task<TResult[]> SendTwoWayAsync<TMessage, TResult>(TMessage message)
    {
        if (!this.dictionaryMessageResult.TryGetValue(new Identifier_MessageResult(typeof(TMessage), typeof(Task<TResult>)), out var obj))
        {
            return Task.FromResult(Array.Empty<TResult>());
        }

        var list = (FastList<XChannel_MessageResult<TMessage, Task<TResult>>>)obj;
        return list.SendAsync(message);
    }

    /// <summary>
    /// Send a message to a channel with the same key, and receive the result asynchronously.<br/>
    /// Key: <typeparamref name="TKey"/>.<br/>
    /// Message: <typeparamref name="TMessage"/>.<br/>
    /// Result: <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="key">The channel with the same key receives the message.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>The result; <see cref="Task"/>&lt;<typeparamref name="TResult"/>[]&gt;.</returns>
    public Task<TResult[]> SendTwoWayAsyncKey<TKey, TMessage, TResult>(TKey key, TMessage message)
        where TKey : notnull
    {
        if (!this.dictionaryKeyMessageResult.TryGetValue(new Identifier_KeyMessageResult(typeof(TKey), typeof(TMessage), typeof(Task<TResult>)), out var obj))
        {
            return Task.FromResult(Array.Empty<TResult>());
        }

        var collection = (XCollection_KeyMessageResult<TKey, TMessage, Task<TResult>>)obj;
        if (!collection.Dictionary.TryGetValue(key, out var list))
        {
            return Task.FromResult(Array.Empty<TResult>());
        }

        return list.SendAsync(message);
    }

    /// <summary>
    /// Send a message to a channel with the same key, and receive the result.<br/>
    /// Key: <typeparamref name="TKey"/>.<br/>
    /// Message: <typeparamref name="TMessage"/>.<br/>
    /// Result: <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="key">The channel with the same key receives the message.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>The result; An array of <typeparamref name="TResult"/>.</returns>
    public TResult[] SendTwoWayKey<TKey, TMessage, TResult>(TKey key, TMessage message)
        where TKey : notnull
    {
        if (!this.dictionaryKeyMessageResult.TryGetValue(new Identifier_KeyMessageResult(typeof(TKey), typeof(TMessage), typeof(TResult)), out var obj))
        {
            return Array.Empty<TResult>();
        }

        var collection = (XCollection_KeyMessageResult<TKey, TMessage, TResult>)obj;
        if (!collection.Dictionary.TryGetValue(key, out var list))
        {
            return Array.Empty<TResult>();
        }

        return list.Send(message);
    }

    // private ConcurrentDictionary<Identifier_KeyMessageD, object> dictionaryKeyMessageDirect = new(); // FastList<XChannel_Message<TMessage>> // 20-30% Faster, but I could not solve the synchronization problem and the cleanup problem.
    private ConcurrentDictionary<Type, object> dictionaryMessage = new(); // FastList<XChannel_Message<TMessage>>
    private ConcurrentDictionary<Identifier_MessageResult, object> dictionaryMessageResult = new(); // FastList<XChannel_MessageResult<TMessage, TResult>>
    private ConcurrentDictionary<Identifier_KeyMessage, object> dictionaryKeyMessage = new(); // XCollection_KeyMessage<TKey, TMessage>
    private ConcurrentDictionary<Identifier_KeyMessageResult, object> dictionaryKeyMessageResult = new(); // XCollection_KeyMessageResult<TKey, TMessage, TResult>
}
