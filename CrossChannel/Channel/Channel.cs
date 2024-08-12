// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

public class Channel<TService>
{
    public class Connection : XChannel //opt
    {
        private readonly Channel<TService> channel;
        private readonly WeakReference? weakReference;

        public Connection(Channel<TService> channel, object? weakReference)
        {
            this.channel = channel;
            if (weakReference is not null)
            {
                this.weakReference = new(weakReference);
            }
        }

        public void Close()
            => this.Dispose();

        public override void Dispose()
        {
            lock (this.channel.list)
            {
                if (this.Index != -1)
                {
                    this.channel.list.Remove(this);
                }
            }
        }
    }

    public TService Broker { get; } = default!;

    private readonly FastList<Connection> list = new();

    public Channel()
    {
    }

    public Connection Open(TService instance, object? weakReference)
    {
        return new Connection(this, weakReference);
    }
}
