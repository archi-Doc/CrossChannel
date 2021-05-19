using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            CrossChannel.Radio.Open<int>(x => { Console.WriteLine(x); });
            CrossChannel.Radio.Send(1);

            CrossChannel.Radio.Open<int>(x => { Console.WriteLine(x); });
            CrossChannel.Radio.Send(2);
        }
    }
}
