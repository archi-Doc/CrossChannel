// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrossChannel;

public interface ISender<TService>
    where TService : class, IRadioService
{
    TService Send();

    TService Send<TKey>(TKey key)
        where TKey : notnull;
}

internal class Sender<TService> : ISender<TService>
    where TService : class, IRadioService
{
    public Sender()
    {
    }

    public TService Send()
        => Radio.Send<TService>();

    public TService Send<TKey>(TKey key)
        where TKey : notnull
        => Radio.Send<TService, TKey>(key);
}
