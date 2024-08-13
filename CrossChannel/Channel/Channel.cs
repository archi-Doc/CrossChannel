// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

public class Channel<TService>
    where TService : class
{
    public class Connection : XChannel //opt
    {
        private readonly Channel<TService> channel;
        private readonly WeakReference<TService> weakReference;

        public Connection(Channel<TService> channel, TService instance)
        {
            this.channel = channel;
            this.weakReference = new(instance);

            lock (this.channel.syncObject)
            {
                this.channel.list.Add(this);
            }
        }

        public bool TryGetInstance([MaybeNullWhen(false)] out TService instance)
            => this.weakReference.TryGetTarget(out instance);

        public void Close()
            => this.Dispose();

        public override void Dispose()
        {
            lock (this.channel.syncObject)
            {
                if (this.Index != -1)
                {
                    this.channel.list.Remove(this); // this.Index is set to -1
                }
            }
        }
    }

    internal TService Broker { get; }

    private readonly object syncObject = new();
    private readonly FastList<Connection> list = new(); // this.semaphoreLock

    public Channel()
    {
        this.Broker = (TService)RadioRegistry.Get<TService>().Constructor(this);
    }

    public Connection Open(TService instance)
    {
        return new Connection(this, instance);
    }

    public FastList<Connection> InternalGetList() => this.list;
}
