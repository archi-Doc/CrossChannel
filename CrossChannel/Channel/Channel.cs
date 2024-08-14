// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

public class Channel<TService>
    where TService : class
{
    public class Link : XChannel //opt
    {
        private readonly Channel<TService> channel;
        // private readonly WeakReference<TService> weakReference;
        private readonly TService instance;

        public Link(Channel<TService> channel, TService instance)
        {
            this.channel = channel;
            // this.weakReference = new(instance);
            this.instance = instance;

            lock (this.channel.syncObject)
            {
                this.channel.list.Add(this);
            }
        }

        // public bool TryGetInstance([MaybeNullWhen(false)] out TService instance)
        //    => this.weakReference.TryGetTarget(out instance);

        public TService Instance => this.instance;//

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

    public Link Open(TService instance)
    {
        return new Link(this, instance);
    }

    public FastList<Link> InternalGetList() => this.list;
}
