using System;
using System.Collections.Generic;
using System.Threading;


namespace ReadWriteWithoutSync
{
    class Program
    {
        static string buffer;
        static List<int> Count = new List<int>();
        static void Main(string[] args)
        {
            int n = 10;
            Thread[] Readers = new Thread[n];
            Thread[] Writers = new Thread[n];

            for (int i = 0; i < n; i++)
            {
                Writers[i] = new Thread(Writer);
                Writers[i].Start();
                Readers[i] = new Thread(Reader);
                Readers[i].Start();
            }
            for (int i = 0; i < n; i++)
            {
                Writers[i].Join();
                Readers[i].Join();
            }

        }

        static void Reader()
        {
            Count.Add(buffer[buffer.Length - 1]);
            buffer = buffer.Remove(buffer.Length - 1);
        }

        static void Writer()
        {
            Console.WriteLine(buffer);
            Random r = new Random(255);
            buffer += (char)r.Next();
        }
    }
}
