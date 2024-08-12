// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

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

            this.channel.semaphoreLock.Enter();
            this.channel.list.Add(this);
            this.channel.semaphoreLock.Exit();
        }

        public bool TryGetInstance([MaybeNullWhen(false)] out TService instance)
            => this.weakReference.TryGetTarget(out instance);

        public void Close()
            => this.Dispose();

        public override void Dispose()
        {
            this.channel.semaphoreLock.Enter();
            this.InternalDispose();
            this.channel.semaphoreLock.Exit();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InternalDispose()
        {
            if (this.Index != -1)
            {
                this.channel.list.Remove(this); // this.Index is set to -1
            }
        }
    }

    internal TService Broker { get; } = default!;

    private readonly SemaphoreLock semaphoreLock = new();
    private readonly FastList<Connection> list = new(); // this.semaphoreLock

    public Channel()
    {
    }

    public Connection Open(TService instance)
    {
        return new Connection(this, instance);
    }

    public FastList<Connection> InternalGetList() => this.list;

    #region Lock

    public void EnterScope() => this.semaphoreLock.Enter();

    public Task<bool> EnterScopeAsync() => this.semaphoreLock.EnterAsync();

    public void ExitScope() => this.semaphoreLock.Exit();

    #endregion
}
