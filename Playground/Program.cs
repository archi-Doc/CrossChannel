using System;
using System.Threading.Tasks;
using CrossChannel;
using Microsoft.Extensions.DependencyInjection;

namespace Playground;

[RadioServiceInterface]
public interface ITestService : IRadioService
{
    void Test1();

    RadioResult<int> Test2(int x);

    Task Test3();

    Task<RadioResult<int>> Test4();
}

public partial class TestService : ITestService
{
    private partial class NestedClass
    {
        [RadioServiceInterface]
        interface INestedService : IRadioService
        {
            Task Message(string t);
        }
    }

    void ITestService.Test1()
    {
        Console.WriteLine("Test1");
    }

    RadioResult<int> ITestService.Test2(int x)
    {
        Console.WriteLine("Test2");
        return new(2);
    }

    async Task ITestService.Test3()
    {
        await Console.Out.WriteLineAsync("Test3");
    }

    async Task<RadioResult<int>> ITestService.Test4()
    {
        await Console.Out.WriteLineAsync("Test4");
        return new(3);
    }
}

public class TestClass
{
    private readonly ITestService service;

    public TestClass(ITestService service)
    {// service broker
        this.service = service;
    }
}

class Program
{
    static void Main(string[] args)
    {
        /*var c = Radio.Open<ITestService>(new TestService());

        var result = Radio.Send<ITestService>().Test2(2);
        c.Close();

        result = Radio.Send<ITestService>().Test2(2);*/

        var collection = new ServiceCollection();
        collection.AddCrossChannel();
        var provider = collection.BuildServiceProvider();

        var testService = provider.GetRequiredService<ITestService>();
        testService.Test1(); // No service

        var channel = provider.GetRequiredService<IChannel<ITestService>>();
        var link = channel.Open((ITestService)new TestService());

        testService.Test1();

        var sender = provider.GetRequiredService<ISender<ITestService>>();
        sender.Send().Test1();

        link?.Close();
        testService.Test1();// No service

    }
}
