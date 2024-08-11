using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.WeakDelegate;

namespace CrossChannel;

public readonly struct RadioResult<T>
{
    public readonly T[] ResultArray;

    public RadioResult(T result)
    {
        this.ResultArray = [result];
    }

    public int NumberOfResults => this.ResultArray.Length;
}

public interface IRadioService
{
}

internal class XCollection<TService>
{
    public XCollection()
    {
    }

    public TService Broker { get; } = default!;
}

public static class NewRadio
{
    static NewRadio()
    {
    }

    public static void Open<TService>(TService instance, object? weakReference = default)
        where TService : IRadioService
    {
        var broker = Cache_Service<TService>.Collection;
    }

    public static TService Send<TService>()
        where TService : IRadioService
    {
        return Cache_Service<TService>.Collection.Broker;
    }

    internal static class Cache_Service<TService>
        where TService : IRadioService
    {
        public static XCollection<TService> Collection;

        static Cache_Service()
        {
            Collection = new();
        }
    }
}
