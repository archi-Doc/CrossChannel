using System;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("CrossChannel Sample:");
            Console.WriteLine();

            await Sample.Other();
        }
    }
}
