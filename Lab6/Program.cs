using System;
using System.Diagnostics;
using System.Threading;

namespace ParLab6
{
    class Program
    {

        static int[,] graph;
        static Random rnd = new Random(10);
        static int V = 9;
        static int U;

       
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            int x = 99;
            graph = new int[,] {
            {0,2,3,x,x,x,x,x,x },
            {x,0,x,x,x,1,x,x,x },
            {x,x,0,1,2,x,x,x,x },
            {x,x,x,0,x,x,2,x,x },
            {x,x,x,x,0,x,x,x,x },
            {x,x,x,x,x,0,2,3,2 },
            {x,x,x,x,1,x,0,1,x },
            {x,x,x,x,x,x,x,0,x },
            {x,x,x,x,x,x,x,1,0 }
            };

            V = 300; M = 8;
            FillGraph();
            A = graph.Clone() as int[,];
            C = graph.Clone() as int[,];

           // Parallel();
            //   Print(C);


            A = graph.Clone() as int[,];
            C = graph.Clone() as int[,];

            Stopwatch sw = new Stopwatch();
            sw.Start();
            SeqEffective();
            sw.Stop();
            Console.WriteLine("Sw " + sw.Elapsed.TotalMilliseconds);
            Console.WriteLine("graph  ");
           // Print(graph);
            Console.WriteLine("C  ");
           // Print(C);
            int[,] Gee = C.Clone() as int[,];
            /*
            graph = new int[,] {
            {0,2,3,x,x,x,x,x,x },
            {x,0,x,x,x,1,x,x,x },
            {x,x,0,1,2,x,x,x,x },
            {x,x,x,0,x,x,2,x,x },
            {x,x,x,x,0,x,x,x,x },
            {x,x,x,x,x,0,2,3,2 },
            {x,x,x,x,1,x,0,1,x },
            {x,x,x,x,x,x,x,0,x },
            {x,x,x,x,x,x,x,1,0 }
            };*/


            //FillGraph();
            A = graph.Clone() as int[,];
            C = graph.Clone() as int[,];
            sw.Reset();
            sw.Restart();
            Seq();
            sw.Stop();

            Console.WriteLine("Sw " + sw.Elapsed.TotalMilliseconds);


            A = graph.Clone() as int[,];
            C = graph.Clone() as int[,];
            sw.Reset();
            sw.Restart();
            Parallel();
            sw.Stop();
            Console.WriteLine("Sw PAR " + sw.Elapsed.TotalMilliseconds);
            if (!Check(Gee, C)) Console.WriteLine("FAIL"); else Console.WriteLine("OK");
        }

        static bool Check(int[,] A, int[,] B)
        {
            int n = A.GetLength(0);
            int m = A.GetLength(1);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (A[i, j] != B[i, j]) return false;
            return true;
        }

        static void FillGraph()
        {
            graph = new int[V, V];
            int value;
            for (int i = 0; i < V; i++)
            {
                for (int j = i; j < V; j++)
                {
                    if (i == j)
                        graph[i, j] = 0;
                    else
                    {
                        value = rnd.Next(1, 10);
                        if (value <= Density)
                            graph[i, j]  = graph[j, i] = value;
                        else graph[i, j] = graph[j, i] = 0;
                    }
                }
            }
            C = graph.Clone() as int[,]; A = graph.Clone() as int[,];
        }
        static int n;
        static int[,] C, A;
        /// <summary>
        /// Коэффициент плотности матрицы [0,10]
        /// 0 -- нулевая матрица
        /// 1 -- 10% ненулевых недиагональных элементов
        /// 2 -- 20% ненулевых недиагональных элементов
        /// ...
        /// </summary>
        static int Density = 10; 
        static void Mul(int[,] A, int[,] C)
        {
            int n = A.GetLength(0);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    for (int k = 0; k < n; k++)
                        C[i, j] = Math.Min(C[i, j], A[i, k] + A[k, j]);
        }

        static void MulEffective(int[,] A, int[,] C)
        {      
            int n = A.GetLength(0);
            // for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)                 
                    for (int k = 0; k < n; k++)
                        C[i, j] = Math.Min(C[i, j], A[i, k] + A[k, j]);
        }

        static void Seq()
        {
            int k = (int)Math.Round(Math.Log2(Convert.ToDouble(V))); //степень матрицы
            int[,] temp = A;

            for (int i = 0; i < k; i++)
            {
                Mul(temp, C);
                temp = C;
                C = A;
            }
        }

        static void SeqEffective()
        {
            int k = (int)Math.Round(Math.Log2(Convert.ToDouble(V))); //степень матрицы
                                                                     // Console.WriteLine("K = ", k);
            int[,] temp = A;

            for (int i = 0; i < k; i++)
            {
                MulEffective(temp, C);
                temp = C;
                C = A;
            }
        }

        static void Print(int[,] A)
        {
            int n = A.GetLength(0);
            int m = A.GetLength(1);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                    if (A[i, j] != 99)
                        Console.Write(A[i, j].ToString().PadLeft(2) + " ");
                    else Console.Write("?".ToString().PadLeft(2) + " ");
                Console.WriteLine();
            }
        }
        /// <summary>
        /// Количество потоков
        /// </summary>
        static int M = 2;
        static Barrier barrier;
        static bool success;
        static void Parallel()
        {
            int n = A.GetLength(0);
            int start = 0, step = n / M;

            int k = (int)Math.Round(Math.Log2(Convert.ToDouble(V)));
            int[,] temp = A;
            success = false;
            barrier = new Barrier(M, (bar) =>
            {
                temp = C;
                C = A;
                if (--k == 0) success = true;
            });


            Thread[] threads = new Thread[M];
            for (int i = 0; i < M - 1; i++)
            {
                threads[i] = new Thread(ThreadOperation);
                threads[i].Start(new int[] { start, start += step });
            }
            threads[M - 1] = new Thread(ThreadOperation);
            threads[M - 1].Start(new int[] { start, n });

            for (int i = 0; i < M; i++)
                threads[i].Join();
        }
        
        static void ThreadOperation(object o)
        {
            int start = ((int[])o)[0],
                end = ((int[])o)[1];

            int n = A.GetLength(0);
            while (success == false)
            {
                for (int i = start; i < end; i++)
                    for (int k = 0; k < n; k++)
                        for (int j = 0; j < n; j++)
                            C[i, j] = Math.Min(C[i, j], A[i, k] + A[k, j]);
                barrier.SignalAndWait();
            }
        }
    }
}
