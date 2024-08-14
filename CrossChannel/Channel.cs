// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

public class Channel<TService>
    where TService : class
{
    public class Link : XChannel //opt
    {
        private readonly Channel<TService> channel;
        private readonly WeakReference<TService>? weakReference;
        private readonly TService? strongReference;

        public Link(Channel<TService> channel, TService instance, bool weakReference)
        {
            this.channel = channel;
            if (weakReference)
            {
                this.weakReference = new(instance);
            }
            else
            {
                this.strongReference = instance;
            }

            lock (this.channel.syncObject)
            {
                this.channel.list.Add(this);
            }
        }

        public bool TryGetInstance([MaybeNullWhen(false)] out TService instance)
        {
            if (this.strongReference is not null)
            {
                instance = this.strongReference;
                return true;
            }
            else
            {
                return this.weakReference!.TryGetTarget(out instance);
            }
        }

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

    private readonly object syncObject = new(); // -> Lock
    private readonly FastList<Link> list = new();

    public Channel()
    {
        this.Broker = (TService)RadioRegistry.Get<TService>().Constructor(this);
    }

    public Link Open(TService instance, bool weakReference)
    {
        return new Link(this, instance, weakReference);
    }

    public FastList<Link> InternalGetList() => this.list;
}
