using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

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
        static readonly byte[] Marr = { 8 };

        /// <summary>
        /// Число прогонов в одном эксперименте
        /// </summary>
        const int Count = 3;
        /// <summary>
        /// Число повторений для усреднения
        /// </summary>
        static int ReplayCount = 1;

        static int K = 10;
        //true, true, true, true, true
        //false, false, false, false, false
        static bool[] ExpBool = { false, true, false, false, false };
        #endregion

        #region Автоматически заполняемые поля
        static uint N = 100;
        static int M = 2;
        static int[] vector;
        static int[] vector2;

        static int[] Numbers;
        static bool[] EratFlags;

        static List<int> BasePrime = new List<int>();

        static int current_index = 0;
        static int current_prime = 0;

        static int SqrtN = (int)Math.Sqrt(N);

        static Action Operation;
        static Action ParallelOperation;
        static ParameterizedThreadStart ThreadOperation;
        #endregion

        enum Experiments
        {
            EratosfenModifyRange,
            EratosfenBasicRange,
            EratosfenThreadPool,
            EratosfenBasicSeq
        }
        const uint ExpCount = 4;

        static void Main(string[] args)
        {
            /*
            EratFlags = new bool[N];
            Numbers = new int[N];

            Console.WriteLine();
            FillArrays();


            EratosfenClassic();


            for (int i = 0; i < N; i++)
            {
                if (EratFlags[i] == false) Console.Write(Numbers[i] + " ");
            }

            FillArrays();
            BasicPrimaryNumbers();
            DefineDelegates(Experiments.EratosfenModifyRange);
            ParallelOperationRange();
            Console.WriteLine();

            for (int i = 0; i < N; i++)
            {
                if (EratFlags[i] == false) Console.Write(Numbers[i] + " ");
            }
            Console.ReadKey();
            */

            for (int i = 0; i < ExpCount; i++)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Эксперимент {0}. ", i + 1);
                Console.ResetColor();

                if (ExpBool[i])
                {
                    DefineDelegates((Experiments)i);
                    Report();
                }
            }
            Console.WriteLine();
        }

        static void DefineDelegates(Experiments Ex)
        {
            Operation = EratosfenClassic;
            switch (Ex)
            {
                case Experiments.EratosfenModifyRange:
                    Console.WriteLine("Модифицированный последовательный алгоритм поиска: декомпозиция по данным");

                    ParallelOperation = ParallelOperationRange;
                    ThreadOperation = ThreadEratosfenModify;
                    break;
                case Experiments.EratosfenBasicRange:
                    Console.WriteLine("Модифицированный последовательный алгоритм поиска: применение пула потоков");

                    ParallelOperation = ParallelOperationBasicRange;
                    ThreadOperation = ThreadEratosfenBasicModify;
                    break;
                case Experiments.EratosfenThreadPool:
                    Console.WriteLine("Модифицированный последовательный алгоритм поиска: декомпозиция по данным");

                    ParallelOperation = ParallelOperationThreadPool;
                    ThreadOperation = ThreadPoolOperation;
                    break;
                case Experiments.EratosfenBasicSeq:
                    Console.WriteLine("Последовательный перебор простых чисел");

                    ParallelOperation = ParallelOperationBasicSeq;
                    ThreadOperation = ThreadOperationBasicSeq;
                    break;
            }
        }



        static void Report()
        {
            N = cN;
            Int64 Mean = 0;
            Int64[] ParallelMean = new Int64[Marr.Length];
            for (int i = 0; i < ParallelMean.Length; i++) ParallelMean[i] = 0;

            Stopwatch stopwatch = new Stopwatch();

            Console.Write(" № |       N       | Один поток ");
            for (int i = 0; i < Marr.Length; i++) Console.Write(new string("| потоков " + Marr[i]).PadRight(13));

            for (int i = 1; i <= Count; i++)
            {
                Console.WriteLine();
                Numbers = new int[N *= 10];
                EratFlags = new bool[N];

                //System.GC.Collect();
                for (int j = 0; j < ReplayCount; j++)
                {
                    FillArrays();

                    stopwatch.Start();
                    Operation();
                    stopwatch.Stop();

                    Mean += stopwatch.ElapsedTicks;
                    stopwatch.Reset();

                    for (int t = 0; t < Marr.Length; t++)
                    {
                        M = Marr[t];
                        stopwatch.Start();
                        ParallelOperation();
                        stopwatch.Stop();
                        ParallelMean[t] += stopwatch.ElapsedTicks;
                        stopwatch.Reset();
                    }
                }

                Mean /= ReplayCount;
                Console.Write(i.ToString().PadLeft(3) + "|" + N.ToString().PadLeft(15) + "|" + Mean.ToString().PadLeft(12));
                for (int t = 0; t < Marr.Length; t++)
                {
                    ParallelMean[t] /= ReplayCount;
                    Console.Write("|" + ParallelMean[t].ToString().PadLeft(12));
                }
            }

            Console.WriteLine();
            Console.WriteLine();
        }

        static void FillArrays()
        {
            Random R = new Random();

            for (int j = 0; j < N; j++)
            {
                Numbers[j] = j;
                EratFlags[j] = false;
            }
        }

        #region Простые числа, последовательно

        static void EratosfenClassic()
        {
            int sqrtN = (int)Math.Sqrt(N + 1);
            for (int i = 2; i < sqrtN; i++)
            {
                if (!EratFlags[i])
                    for (int j = i * i; j < N; j += i)
                    {
                        EratFlags[j] = true;
                    }
            }
        }

        static void BasicPrimaryNumbers()
        {
            int sqrtN = (int)Math.Sqrt(N + 1);
            for (int i = 2; i < sqrtN; i++)
            {
                if (!EratFlags[i])
                {
                    BasePrime.Add(Numbers[i]);
                    for (int j = i * i; j < sqrtN; j += i)
                    {
                        EratFlags[j] = true;
                    }
                }
            }
            /*
            for (int i = 2; i < sqrtN; i++)
            {
                if (!EratFlags[i]) BasePrime.Add(Numbers[i]);
            }*/
        }
        #endregion




        #region Функции разбиения
        static void ParallelOperationRange()
        {
            BasicPrimaryNumbers();
            Thread[] threads = new Thread[M];
            int step = Numbers.Length / M;
            int start = 0;

            for (int i = 0; i < M - 1; i++)
            {
                threads[i] = new Thread(ThreadOperation);
                threads[i].Start(new int[] { start, start += step });
            }
            threads[M - 1] = new Thread(ThreadOperation);
            threads[M - 1].Start(new int[] { start, Numbers.Length });

            for (int i = 0; i < M; i++) threads[i].Join();
        }

        static void ParallelOperationBasicRange()
        {
            BasicPrimaryNumbers();
            Thread[] threads = new Thread[M];
            int step = BasePrime.Count / M;
            int start = SqrtN;

            for (int i = 0; i < M - 1; i++)
            {
                threads[i] = new Thread(ThreadOperation);
                threads[i].Start(new int[] { start, start += step });
            }
            threads[M - 1] = new Thread(ThreadOperation);
            threads[M - 1].Start(new int[] { start, BasePrime.Count });

            for (int i = 0; i < M; i++) threads[i].Join();
        }


        static void ParallelOperationThreadPool()
        {
            ManualResetEvent[] events = new ManualResetEvent[BasePrime.Count];

            for (int i = 0; i < BasePrime.Count; i++)
            {
                events[i] = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(ThreadPoolOperation, new object[] { BasePrime[i], events[i] });
            }

            for (int i = 0; i < BasePrime.Count; i++)
            {
                events[i].WaitOne();
            }

        }


        static void ParallelOperationBasicSeq()
        {
            BasicPrimaryNumbers();
            Thread[] threads = new Thread[M];

            for (int i = 0; i < M; i++)
            {
                threads[i] = new Thread(ThreadOperation);
                threads[i].Start(null);
            }

            for (int i = 0; i < M; i++) threads[i].Join();
        }
        #endregion

        #region Функции вычисления для потоков
        static void ThreadEratosfenModify(object interval)
        {
            int start = ((int[])interval)[0];
            int end = ((int[])interval)[1];
            if (start == 0) start = 2;
            for (int i = start; i < end; i++)
            {
                foreach (var num in BasePrime)
                    if (Numbers[i] % num == 0)
                    {
                        EratFlags[i] = true;
                        break;
                    }
            }
        }

        static void ThreadEratosfenBasicModify(object interval)
        {
            int start = ((int[])interval)[0];
            int end = ((int[])interval)[1];


            for (int i = SqrtN; i < N; i++)
            {
                for (int j = start; j < end; j++)
                {
                    if (Numbers[i] % BasePrime[j] == 0) EratFlags[i] = true;
                }
            }
        }

        static void ThreadPoolOperation(object o)
        {
            int prime = (int)((object[])o)[0];
            ManualResetEvent ev = ((object[])o)[1] as ManualResetEvent;

            for (int i = SqrtN; i < N; i++)
                if (Numbers[i] % prime == 0) EratFlags[i] = true;
            ev.Set();
        }

        static void ThreadOperationBasicSeq(object o)
        {
            while (true)
            {
                if (current_index >= BasePrime.Count) break;
                lock ("ThreadOperationBasicSeq")
                {
                    current_prime = BasePrime[current_index];
                    current_index++;
                }

                // Обработка текущего простого числа
                for (int i = SqrtN; i < N; i++)
                    if (!EratFlags[i])
                        if (Numbers[i] % current_prime == 0) EratFlags[i] = true;
            }

        }
        #endregion
    }
}
