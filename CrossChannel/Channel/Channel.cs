// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

public class Channel<TService>
{
    public class Connection : IDisposable
    {
        private readonly Channel<TService> channel;

        public Connection(Channel<TService> channel)
        {
            this.channel = channel;
        }

        public void Close()
            => this.Dispose();

        #region IDisposable Support

        private bool disposed = false; // To detect redundant calls.

        /// <summary>
        /// Finalizes an instance of the <see cref="Connection"/> class.
        /// </summary>
        ~Connection()
        {
            this.Dispose(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// free managed/native resources.
        /// </summary>
        /// <param name="disposing">true: free managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // free managed resources.
                }

                // free native resources here if there are any.
                this.disposed = true;
            }
        }
        #endregion
    }

    public TService Broker { get; } = default!;

    public Channel()
    {
    }

    public Connection Open(TService instance, object? weakReference)
    {
        return new Connection(this);
    }
}
