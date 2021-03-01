using System;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace ParLab1
{
    class Program
    {
        #region Определение неизменяемых параметров
        /// <summary>
        /// Порядок первого прогона
        /// </summary>
        static uint cN = 10000;
        /// <summary>
        /// Массив количества потоков
        /// </summary>
        static readonly byte[] Marr = { 2, 8, 16, 24};

        /// <summary>
        /// Число прогонов в одном эксперименте
        /// </summary>
        const int Count = 5;
        /// <summary>
        /// Число повторений для усреднения
        /// </summary>
        static int ReplayCount = 1;

        static int K = 10;
        //true, true, true, true, true
        //false, false, false, false, false
        static bool[] ExpBool = { true, true, true, true, true };

        const int dev = 100;
        #endregion

        #region Автоматически заполняемые поля
        static uint N;
        static int M;
        static int[] vector;
        static int[] vector2;
        
        
        delegate void Action();
       
        static Action Operation;
        static Action ParallelOperation;      
        static ParameterizedThreadStart ThreadOperation;
        #endregion

        enum Experiments
        {
            SimpleRange, //Простые вычисления при равномерной сложности на диапазоне
            HardRange, //Сложные вычисления при равномерной сложности на диапазоне
            SimpleIrregularRange, //Простые вычисления при неравномерной сложности на диапазоне
            SimpleIrregularCircular, //Простые вычисления при неравномерной сложности на круговом разбиении
            SimpleCircular //Простые вычисления при равномерной сложности на круговом разбиении
        }
        const uint ExpCount = 5;

        static void Main(string[] args)
        {
            int qqqq = -12 % 10;
            //Console.WriteLine(System.Environment.ProcessorCount);
            if (qqqq == -2) Console.WriteLine("sas");
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
            ReportHardCalc();
        }

        static void DefineDelegates(Experiments Ex)
        {
            switch (Ex)
            {
                case Experiments.SimpleRange:
                    Console.WriteLine("Простые вычисления при равномерной сложности на диапазоне");                   
                    Operation = OperationSimple;
                    ParallelOperation = ParallelOperationRange;
                    ThreadOperation = ThreadOperationSimple;
                    break;
                case Experiments.HardRange:
                    Console.WriteLine("Сложные вычисления при равномерной сложности на диапазоне");
                    Operation = OperationHard;                
                    ParallelOperation = ParallelOperationRange;
                    ThreadOperation = ThreadOrepationHard;
                    break;
                case Experiments.SimpleCircular:
                    Console.WriteLine("Простые вычисления при равномерной сложности и круговом разделении");
                    Operation = OperationSimple;
                    ParallelOperation = ParralelOperationCircular;
                    ThreadOperation = ThreadOperationRegularCircular;
                    break;
                case Experiments.SimpleIrregularRange:
                    Console.WriteLine("Вычисления при неравномерной сложности на диапазоне");
                    cN = 1;
                    ReplayCount = 1;
                    Operation = OperationIrregular;
                    ParallelOperation = ParallelOperationRange;
                    ThreadOperation = ThreadOperationIrregularRange;
                    break;
                case Experiments.SimpleIrregularCircular:
                    cN = 1;
                    ReplayCount = 1;
                    Console.WriteLine("Вычисления при неравномерной сложности и круговом разделении");
                    Operation = OperationIrregular;
                    ParallelOperation = ParralelOperationCircular;
                    ThreadOperation = ThreadOperationIrregularCircular;
                    break;
                
            }          
        }


        static void ReportHardCalc()
        {
            DefineDelegates((Experiments)1);
            N = 1000000;

            vector = new int[N];
            vector2 = new int[N];

            Int64 Mean = 0;
            Int64[] ParallelMean = new Int64[Marr.Length];

            Stopwatch stopwatch = new Stopwatch();
            Console.Write("при N = " + N); Console.WriteLine();
            Console.Write(" № |       K       | Один поток ");
            for (int i = 0; i < Marr.Length; i++) Console.Write(new string("| потоков " + Marr[i]).PadRight(13));

            for (int i = 1; i <= Count; i++)
            {             
                Console.WriteLine();
                FillArrays();

                stopwatch.Start();
                Operation();
                stopwatch.Stop();

                
                Mean = stopwatch.ElapsedTicks;
                stopwatch.Reset();

                for (int t = 0; t < Marr.Length; t++)
                {
                    M = Marr[t];
                    stopwatch.Start();
                    ParallelOperation();
                    stopwatch.Stop();
                    ParallelMean[t] = stopwatch.ElapsedTicks;
                    stopwatch.Reset();
                }
                stopwatch.Reset();

                Console.Write(i.ToString().PadLeft(3) + "|" + K.ToString().PadLeft(15) + "|" + Mean.ToString().PadLeft(12));
                for (int t = 0; t < Marr.Length; t++)
                {
                    ParallelMean[t] /= ReplayCount;
                    Console.Write("|" + ParallelMean[t].ToString().PadLeft(12));
                }
                K *= 10;
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
                vector = new int[N*=10];
                vector2 = new int[N];
                System.GC.Collect();
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
            
            for (int j = 0; j < vector.Length; j++) vector[j] = R.Next();
            for (int j = 0; j < vector.Length; j++) vector2[j] = R.Next();
        }

        #region Однопоточные функции
        static void OperationSimple()
        {
            for (int i = 0; i < vector.Length; i++) vector[i] *= 8;
        }
        static void OperationHard()
        {
            for (int i = 0; i < vector.Length; i++)
            {
                for (int j = 0; j < K; j++)
                    vector[i] += (int)Math.Pow(vector2[i], 2.548);
            }
        }

        static void OperationIrregular()
        {
            for (int i = 0; i < vector.Length; i++)
                for (int j = 0; j < i; j++)
                    vector[i] += (int)Math.Pow(vector[i], 2);
        }      
        #endregion

        #region Функции разбиения
        static void ParallelOperationRange()
        {
            Thread[] threads = new Thread[M];
            int step = vector.Length / M;
            int start = 0;

            for (int i = 0; i < M - 1; i++)
            {
                threads[i] = new Thread(ThreadOperation);
                threads[i].Start(new int[] { start, start += step });
            }
            threads[M - 1] = new Thread(ThreadOperation);
            threads[M - 1].Start(new int[] { start, vector.Length });

            for (int i = 0; i < M; i++) threads[i].Join();
        }


        static void ParralelOperationCircular()
        {
            Thread[] threads = new Thread[M];
           
            for (int i = 0; i < M; i++)
            {
                threads[i] = new Thread(ThreadOperation);
               
                    threads[i].Start(i);
            }

            for (int i = 0; i < M; i++) threads[i].Join();
        }
        #endregion

        #region Функции вычисления для потоков
        static void ThreadOperationSimple(object interval)
        {
            int start = ((int[])interval)[0];
            int end = ((int[])interval)[1];
            for (int i = start; i < end; i++) vector[i] *= 8;
        }

        static void ThreadOrepationHard(object interval)
        {
            int start = ((int[])interval)[0];
            int end = ((int[])interval)[1];

            for (int i = start; i < end; i++)
            {
                for (int j = 0; j < K; j++)
                    vector[i] += (int)Math.Pow(vector2[i], 2.548);
            }           
        }

        static void ThreadOperationIrregularRange(object interval)
        {
            int start = ((int[])interval)[0];
            int end = ((int[])interval)[1];

            for (int i = start; i < end; i++)
            {
                for (int j = 0; j < i; j++)
                    vector[i] += (int)Math.Pow(vector2[i], 2.561);
            }
        }

        static void ThreadOperationIrregularCircular(object ThNumber)
        {
            for (int i = (int)ThNumber; i < vector.Length; i += M)
            {
                for (int j = 0; j < i; j++)
                    vector[i] += (int)Math.Pow(vector2[i], 2.548);
            }
        }

        static void ThreadOperationRegularCircular(object ThNumber)
        {
            for (int i = (int)ThNumber; i < vector.Length; i += M)
            {
                vector[i] *= 8;
            }
        }

        #endregion
    }
}
