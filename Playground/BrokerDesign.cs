using System;
using System.Linq;
using System.Threading.Tasks;
using CrossChannel;

namespace Playground;

/*public class TestServiceBroker : ITestService
{
    private readonly Channel<ITestService> channel;

    public TestServiceBroker(object channel)
    {
        this.channel = (Channel<ITestService>)channel;
    }

    public void Test1()
    {
        (var array, var countHint) = this.channel.InternalGetList().GetValuesAndCountHint();
        foreach (var x in array)
        {
            if (x is null) continue;
            if (!x.TryGetInstance(out var instance)) { x.Dispose(); continue; }
            instance.Test1();
        }
    }

    public RadioResult<int> Test2(int a0)
    {
        (var array, var countHint) = this.channel.InternalGetList().GetValuesAndCountHint();
        int firstResult = default;
        int[]? results = default;
        var count = 0;
        foreach (var x in array)
        {
            if (x is null) continue;
            if (!x.TryGetInstance(out var instance)) { x.Dispose(); continue; }

            if (instance.Test2(a0).TryGetSingleResult(out var r))
            {
                if (count == 0)
                {
                    count = 1;
                    firstResult = r;
                }
                else
                {// count >= 1
                    if (results is null)
                    {// count == 1
                        results = new int[countHint];
                        results[0] = firstResult;
                    }

                    if (count < countHint) results[count++] = r;
                    else break;
                }
            }
        }

        if (count == 0) return default;
        else if (count == 1) return new(firstResult);
        else if (countHint != count) Array.Resize(ref results, count);
        return new(results!);
    }

    public async Task Test3()
    {
        (var array, var countHint) = this.channel.InternalGetList().GetValuesAndCountHint();
        var tasks = new Task[countHint];
        var count = 0;
        foreach (var x in array)
        {
            if (x is null) continue;
            if (!x.TryGetInstance(out var instance)) { x.Dispose(); continue; }
            if (count < countHint) tasks[count++] = instance.Test3();
            else break;
        }

        if (countHint != count) Array.Resize(ref tasks, count);
        if (count == 0) return;
        else if (count == 1) await tasks[0].ConfigureAwait(false);
        else await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task<RadioResult<int>> Test4()
    {
        (var array, var countHint) = this.channel.InternalGetList().GetValuesAndCountHint();
        var tasks = new Task<RadioResult<int>>[countHint];
        var count = 0;
        foreach (var x in array)
        {
            if (x is null) continue;
            if (!x.TryGetInstance(out var instance)) { x.Dispose(); continue; }
            if (count < countHint) tasks[count++] = instance.Test4();
            else break;
        }

        if (countHint != count) Array.Resize(ref tasks, count);
        if (count == 0) return default;
        else if (count == 1) return await tasks[0].ConfigureAwait(false);
        else return new((await Task.WhenAll(tasks).ConfigureAwait(false)).Select(x => x.TryGetSingleResult(out var r) ? r : default).ToArray());
    }
}*/
