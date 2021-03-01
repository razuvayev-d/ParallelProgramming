using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Collections;
using ParLab4;

namespace ParLab1
{
    class Program
    {
        #region Определение неизменяемых параметров
        /// <summary>
        /// Порядок первого прогона
        /// </summary>
        static uint cN = 1;
        /// <summary>
        /// Массив количества потоков
        /// </summary>
        static readonly byte[] Marr = { 10 };      
     
        static int M = 10;
        
        /// <summary>
        /// Число прогонов в одном эксперименте
        /// </summary>
        const int Count = 10;

        //true, true, true, true, true
        //false, false, false, false, false
        static bool[] ExpBool = { true, true, true, true, true, true, true, true};
        #endregion

        #region Автоматически заполняемые поля
        
        static Action ParallelOperation;
        static Action Operation;

        #endregion

        

        enum Experiments
        {
            LSDsort
            
        }
        const uint ExpCount = 1;
     
        
        static void Main(string[] args)
        {        
            for (int i = 0; i < ExpCount; i++)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Эксперимент {0}. ", i + 1);
                Console.ResetColor();

                //if (ExpBool[i])
                {
                    DefineDelegates((Experiments)i);
                    Report();
                }
                //Console.ReadKey();
            }
            Console.WriteLine();
            
        }

        static void DefineDelegates(Experiments Ex)
        {
            switch (Ex)
            {
                case Experiments.LSDsort:
                    Console.WriteLine("Медленный");
                    Operation = RadixLSDSortSeq;
                    ParallelOperation = ParallelLSDSort;
                    break;
            }
 
        }

        #region Check

        static bool Check(int[] a, int[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        static void Fill(int n)
        {
            Random r = new Random();
            array = new int[n];
            for (int i = 0; i < n; i++)
            {
                array[i] = r.Next() % 1000;
            }                      
           // Array.Sort(array);
           // Array.Reverse(array);         
        }

    #endregion
    static void Report()
    {
        N = (int)cN;
            Fill(N);
            Operation();
        double Mean = 0;

        double[] ParallelMean = new double[Marr.Length];
        for (int i = 0; i < ParallelMean.Length; i++) ParallelMean[i] = 0;

        Stopwatch stopwatch = new Stopwatch();

        Console.Write(" № |       N       | Один поток ");
        for (int i = 0; i < Marr.Length; i++) Console.Write(new string("| потоков " + Marr[i]).PadRight(13));

        for (int i = 1; i < Count; i++)
        {
            N *= 10;
            Console.WriteLine();
          
            Fill(N);
           // int[] input = array.Clone() as int[];
            stopwatch.Start();
            //Operation();
            GFG.radixsort(array, array.Length);
            stopwatch.Stop();
            Mean = stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Reset();
            //int[] sorted = array.Clone() as int[];

            for (int t = 0; t < Marr.Length; t++)
            {
                M = Marr[t];
                    Fill(N);
               // array = input;
                stopwatch.Start();
                ParallelOperation();
               // GFG.radixsort(array, array.Length);
                stopwatch.Stop();
                ParallelMean[t] = stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Reset();
                //Check();
            }

            Console.Write(i.ToString().PadLeft(3) + "|" + N.ToString().PadLeft(15) + "|" + Mean.ToString().PadLeft(12));
            for (int t = 0; t < Marr.Length; t++)
            {
                Console.Write("|" + ParallelMean[t].ToString().PadLeft(12));
            }
          //  if (Check(array, sorted)) Console.Write("  OK"); else Console.Write("  FAIL");
            //Console.ReadKey();
        }
        Console.WriteLine();
        Console.WriteLine();

    }
       
        #region Sort
        static int[] array;
        
        static int N;
        static List<int>[] Baskets = new List<int>[10];
        static bool success = false;
        static int exp = 1;
        static Barrier barrier;

        static void RadixLSDSortSeq()
        {
            int max = array.Max().ToString().Length;
            int n = array.Length;
            int upperbound = (int)Math.Pow(10, max);
            int iter, lastdigit, i;
            int[] temp, input = array, IntermediateArray = new int[n];

            for (int exp = 1; exp < upperbound; exp *= 10)
            {
                iter = 0;
                for (lastdigit = 0; lastdigit < 10; lastdigit++)
                {
                    for (i = 0; i < n; i++)
                        if ((array[i] / exp) % 10 == lastdigit)
                            IntermediateArray[iter++] = array[i];
                }
                temp = array;
                array = IntermediateArray;
                IntermediateArray = temp;
            }

            for (i = 0; i < n; i++)
                input[i] = array[i];
        }

        static void ParallelLSDSort()
        {         
            N = array.Length;
            int max = array.Max().ToString().Length;
            int M = 10;
            Thread[] threads = new Thread[M];
            success = false;
            barrier = new Barrier(M, (bar) =>
            {
                int iter = 0;
                for (int digit = 0; digit < 10; digit++)
                {
                    for (int j = 0; j < Baskets[digit].Count; j++)
                        array[iter++] = Baskets[digit][j];
                }

                exp *= 10;
                if (--max == 0) success = true;
            });
            exp = 1;
            for (int i = 0; i < M; i++)
            {
                threads[i] = new Thread(ThreadLSDSort);
                threads[i].Start(i);
            }

            for (int i = 0; i < M; i++)
                threads[i].Join();
        }

        static void ThreadLSDSort(object o)
        {
            int digit = (int)o;
            Baskets[digit] = new List<int>();

            while (success == false)
            {
                for (int i = 0; i < N; i++)
                    if ((array[i] / exp) % 10 == digit)
                        Baskets[digit].Add(array[i]);

                barrier.SignalAndWait();
                Baskets[digit].Clear();
            }        
        }
        #endregion
    }
}

