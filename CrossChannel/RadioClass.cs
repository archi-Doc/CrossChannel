// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CrossChannel
{
    public class RadioClass
    {
        public XChannel Open<TMessage>(Action<TMessage> method, object? weakReference = null)
        {
            var list = (FastList<XChannel_Message<TMessage>>)this.dictionaryMessage.GetOrAdd(
                typeof(TMessage),
                x => new FastList<XChannel_Message<TMessage>>());

            if (list.CleanupCount++ >= Radio.Const.CleanupListThreshold)
            {
                lock (list)
                {
                    list.CleanupCount = 0;
                    list.Cleanup();
                }
            }

            var channel = new XChannel_Message<TMessage>(list, weakReference, method);
            return channel;
        }

        public XChannel OpenAsync<TMessage>(Func<TMessage, Task> method, object? weakReference = null) => this.OpenTwoWay<TMessage, Task>(method, weakReference);

        public XChannel OpenAsyncKey<TKey, TMessage>(TKey key, Func<TMessage, Task> method, object? weakReference = null)
            where TKey : notnull => this.OpenTwoWayKey<TKey, TMessage, Task>(key, method, weakReference);

        public XChannel OpenKey<TKey, TMessage>(TKey key, Action<TMessage> method, object? weakReference = null)
            where TKey : notnull
        {
            var collection = (XCollection_KeyMessage<TKey, TMessage>)this.dictionaryKeyMessage.GetOrAdd(
                new Identifier_KeyMessage(typeof(TKey), typeof(TMessage)),
                x => new XCollection_KeyMessage<TKey, TMessage>());

            if (collection.CleanupCount++ >= Radio.Const.CleanupDictionaryThreshold)
            {
                lock (collection)
                {
                    collection.CleanupCount = 0;
                    collection.Cleanup();
                }
            }

            var channel = new XChannel_KeyMessage<TKey, TMessage>(collection, key, weakReference, method);
            return channel;
        }

        public XChannel OpenTwoWay<TMessage, TResult>(Func<TMessage, TResult> method, object? weakReference = null)
        {
            var list = (FastList<XChannel_MessageResult<TMessage, TResult>>)this.dictionaryMessageResult.GetOrAdd(
                new Identifier_MessageResult(typeof(TMessage), typeof(TResult)),
                x => new FastList<XChannel_MessageResult<TMessage, TResult>>());

            if (list.CleanupCount++ >= Radio.Const.CleanupListThreshold)
            {
                lock (list)
                {
                    list.CleanupCount = 0;
                    list.Cleanup();
                }
            }

            var channel = new XChannel_MessageResult<TMessage, TResult>(list, weakReference, method);
            return channel;
        }

        public XChannel OpenTwoWayAsync<TMessage, TResult>(Func<TMessage, Task<TResult>> method, object? weakReference = null) => this.OpenTwoWay<TMessage, Task<TResult>>(method, weakReference);

        public XChannel OpenTwoWayAsyncKey<TKey, TMessage, TResult>(TKey key, Func<TMessage, Task<TResult>> method, object? weakReference = null)
             where TKey : notnull => this.OpenTwoWayKey<TKey, TMessage, Task<TResult>>(key, method, weakReference);

        public XChannel OpenTwoWayKey<TKey, TMessage, TResult>(TKey key, Func<TMessage, TResult> method, object? weakReference = null)
            where TKey : notnull
        {
            var collection = (XCollection_KeyMessageResult<TKey, TMessage, TResult>)this.dictionaryKeyMessageResult.GetOrAdd(
                new Identifier_KeyMessageResult(typeof(TKey), typeof(TMessage), typeof(TResult)),
                x => new XCollection_KeyMessageResult<TKey, TMessage, TResult>());

            if(collection.CleanupCount++ >= Radio.Const.CleanupDictionaryThreshold)
            {
                lock (collection)
                {
                    collection.CleanupCount = 0;
                    collection.Cleanup();
                }
            }

            var channel = new XChannel_KeyMessageResult<TKey, TMessage, TResult>(collection, key, weakReference, method);
            return channel;
        }

        public void Close(XChannel channel) => channel.Dispose();

        public int Send<TMessage>(TMessage message)
        {
            if (!this.dictionaryMessage.TryGetValue(typeof(TMessage), out var obj))
            {
                return 0;
            }

            var list = (FastList<XChannel_Message<TMessage>>)obj;
            return list.Send(message);
        }

        public Task SendAsync<TMessage>(TMessage message)
        {
            if (!this.dictionaryMessageResult.TryGetValue(new Identifier_MessageResult(typeof(TMessage), typeof(Task)), out var obj))
            {
                return Task.CompletedTask;
            }

            var list = (FastList<XChannel_MessageResult<TMessage, Task>>)obj;
            return list.SendAsync(message);
        }

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

        public TResult[] SendTwoWay<TMessage, TResult>(TMessage message)
        {
            if (!this.dictionaryMessageResult.TryGetValue(new Identifier_MessageResult(typeof(TMessage), typeof(TResult)), out var obj))
            {
                return Array.Empty<TResult>();
            }

            var list = (FastList<XChannel_MessageResult<TMessage, TResult>>)obj;
            return list.Send(message);
        }

        public Task<TResult[]> SendTwoWayAsync<TMessage, TResult>(TMessage message)
        {
            if (!this.dictionaryMessageResult.TryGetValue(new Identifier_MessageResult(typeof(TMessage), typeof(Task<TResult>)), out var obj))
            {
                return Task.FromResult(Array.Empty<TResult>());
            }

            var list = (FastList<XChannel_MessageResult<TMessage, Task<TResult>>>)obj;
            return list.SendAsync(message);
        }

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
}
