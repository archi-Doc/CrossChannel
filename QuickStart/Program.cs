using System;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace QuickStart;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("CrossChannel Sample:");
        Console.WriteLine();

        Sample.QuickStart();
    }
}
