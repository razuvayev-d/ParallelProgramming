using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

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
        static readonly byte[] Marr = { 8, 10, 16 };

        static string[] files = Directory.GetFiles(Directory.GetCurrentDirectory() + @"\Dir1");

        static Dictionary<char, int> chars;
      
        
        static int M = 2;
        static int WritersM = M/2;
        static int ReadersM = M/2;

        static int ReaderCount = 4;
        static int WriterCount = 4;
        static int MessageLength_start = 10;
        static int MessageCount_start = 1;


        static string CurrentDirectory = Directory.GetCurrentDirectory();

        static Dictionary<string, int> wordsEtalon = new Dictionary<string, int>();
        static Dictionary<char, int> symbolsEtalon = new Dictionary<char, int>();

        static Dictionary<string, int>[] wordsLocals = new Dictionary<string, int>[M];
        static Dictionary<char, int>[] symbolsLocals = new Dictionary<char, int>[M];

        static Dictionary<string, int> wordsRes = new Dictionary<string, int>();
        static Dictionary<char, int> symbolsRes = new Dictionary<char, int>();

        static ConcurrentDictionary<string, int> wordsGlobalBuff = new ConcurrentDictionary<string, int>();
        static ConcurrentDictionary<char, int> symbolsGlobalBuff = new ConcurrentDictionary<char, int>();

        static BlockingCollection<string> ReadBuffer = new BlockingCollection<string>();


        static string[] Directories = { "Dir1", "Dir2", "Dir3", "Dir4" };
        /// <summary>
        /// Число прогонов в одном эксперименте
        /// </summary>
        const int Count = 4;


        //true, true, true, true, true
        //false, false, false, false, false
        static bool[] ExpBool = { true, true, true, true, true, true, true, true};
        #endregion

        #region Автоматически заполняемые поля


        string DirName;

        
        static Action ParallelOperation;
        static Action Operation;
        static Action Check;
        static ParameterizedThreadStart ThreadOperation;
        static ParameterizedThreadStart Reader;


        static Action ConcatDictionaryes;
        #endregion

        

        enum Experiments
        {
            FilesLocalBufferSym,
            FileLocalBuffSymLinq,
            FilesGlobalBufferSym,
            TaskSym,

            FilesLocalBufferWords,
            FilesGlobalBufferWords,
            TaskWords,

            FilesLocalNum,
            FilesGlobalNum,
            TaskNum
            
        }
        const uint ExpCount = 2;
        static bool Success = true;
        static void ConcatDirWords()
        {
            wordsRes = wordsLocals[0];
            for (int i = 1; i<wordsLocals.Length; i++)
            {
                if (wordsLocals[i] == null) continue;
                foreach(var word in wordsLocals[i])
                {
                    if (wordsRes.ContainsKey(word.Key)) wordsRes[word.Key] += word.Value;
                    else wordsRes.Add(word.Key, word.Value);
                }
            }
        }
        static void ConcatDirSym()
        {
            symbolsRes = symbolsLocals[0];
            for (int i = 1; i < symbolsLocals.Length; i++)
            {
                if (symbolsLocals[i] == null) continue;
                foreach (var symbol in symbolsLocals[i])
                {
                    if (symbolsRes.ContainsKey(symbol.Key)) symbolsRes[symbol.Key] += symbol.Value;
                    else symbolsRes.Add(symbol.Key, symbol.Value);
                }
            }
        }
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
                case Experiments.FilesLocalBufferWords:
                    Console.WriteLine("Локальные буферы, слова");
                    Operation = SequenceWords;
                    ThreadOperation = LocalBuffersWords;
                    ParallelOperation = FileDecomposition;
                    ConcatDictionaryes = ConcatDirWords;
                    Check = CheckWords;
                    break;
                case Experiments.FilesGlobalBufferWords:
                    Console.WriteLine("Глобальный буфер, слова");
                    Operation = SequenceWords;
                    ThreadOperation = GlobalBufferWords;
                    ParallelOperation = FileDecomposition;
                    ConcatDictionaryes = () => { };
                    Check = CheckConWords;
                    break;
                case Experiments.FilesLocalBufferSym:
                    Console.WriteLine("Локальные буферы, символы");
                    Operation = SequenceSymbols;

                    ThreadOperation = LocalBuffersSymbols;
                    ParallelOperation = FileDecomposition;
                    ConcatDictionaryes = ConcatDirSym;
                    Check = CheckSym;
                    break;
                case Experiments.FilesGlobalBufferSym:
                    Console.WriteLine("Глобальный буфер, символы");

                    Operation = SequenceSymbols;
                    Check = CheckConSym;
                    ThreadOperation = GlobalBufferSymbols;
                    ParallelOperation = FileDecomposition;
                    ConcatDictionaryes = () => { };
                    break;

                case Experiments.TaskSym:
                    Console.WriteLine("Декомпозиция по задачам, символы");
                    Operation = SequenceSymbols;
                    Reader = ReaderSymbol;
                    Check = CheckSym;
                    ParallelOperation = TaskDecomposition;
                    ConcatDictionaryes = ConcatDirSym;
                    break;
                case Experiments.TaskWords:
                    Console.WriteLine("Декомпозиция по задачам, слова");
                    Operation = SequenceWords;
                    Reader = ReaderWord;
                    Check = CheckWords;
                    ParallelOperation = TaskDecomposition;
                    ConcatDictionaryes = ConcatDirWords;
                    break;

                case Experiments.FilesLocalNum:
                    Console.WriteLine("Локальные буферы, числа");
                    Operation = SeqNumbers;
                    ThreadOperation = LocalNumbers;
                    ParallelOperation = FileDecomposition;
                    ConcatDictionaryes = ConcatDirWords;
                    Check = CheckWords;
                    break;

                case Experiments.FilesGlobalNum:
                    Console.WriteLine("Глобальный буфер, числа");
                    Operation = SeqNumbers;
                    ThreadOperation = GlobalNumbers;
                    ParallelOperation = FileDecomposition;
                    ConcatDictionaryes = () => { };
                    Check = CheckConWords;
                    break;

                case Experiments.TaskNum:
                    Console.WriteLine("Декомпозиция по задачам, числа");
                    Operation = SeqNumbers;
                    Reader = ReaderNumber;
                    Check = CheckWords;
                    ParallelOperation = TaskDecomposition;
                    ConcatDictionaryes = ConcatDirWords;
                    break;


                case Experiments.FileLocalBuffSymLinq:
                    Console.WriteLine("LINQ Локальные буферы, символы");
                    Operation = SequenceSymbolsLinq;
                    ThreadOperation = LocalBuffersSymbolsLinq;
                    ParallelOperation = FileDecomposition;
                    ConcatDictionaryes = ConcatDirSym;
                    Check = CheckSym;
                    break;

            }
        }

        static void FillArrays()
        {
            Success = true;
            wordsRes.Clear();
            symbolsRes.Clear();
            symbolsGlobalBuff.Clear();
            wordsGlobalBuff.Clear();

            wordsLocals = new Dictionary<string, int>[M];
            symbolsLocals = new Dictionary<char, int>[M];
            ReadBuffer = new BlockingCollection<string>();
        }

        #region Check
        static void CheckSym()        
        {
            bool b = 
                symbolsEtalon.Values.Sum() == symbolsRes.Values.Sum();
            int x = symbolsEtalon.Values.Sum();
            int y = symbolsRes.Values.Sum();
            Success &= b;
        }
        static void CheckWords()
        {
            bool b = wordsEtalon.Count == wordsRes.Count &&
               wordsEtalon.Values.Sum() == wordsRes.Values.Sum();
            Success &= b;/*
            if (b) Console.WriteLine("OK");
            else Console.WriteLine("FAIL");*/
        }

        static void CheckConSym()
        {
            bool b = symbolsEtalon.Count == symbolsGlobalBuff.Count &&
              symbolsEtalon.Values.Sum() == symbolsGlobalBuff.Values.Sum();
            Success &= b;
        }
        static void CheckConWords()
        {
            bool b = wordsEtalon.Count == wordsGlobalBuff.Count &&
              wordsEtalon.Values.Sum() == wordsGlobalBuff.Values.Sum();
            Success &= b;
        }
        #endregion
        static void Report()
        {
            
            //N = cN;
            int N;
            double Mean = 0;
            
            double[] ParallelMean = new double[Marr.Length];
            for (int i = 0; i < ParallelMean.Length; i++) ParallelMean[i] = 0;

            Stopwatch stopwatch = new Stopwatch();

            Console.Write(" № |  Число файлов | Один поток ");
            for (int i = 0; i < Marr.Length; i++) Console.Write(new string("| потоков " + Marr[i]).PadRight(13));

            for (int i = 0; i < Count; i++)
            {
                files = Directory.GetFiles(Directory.GetCurrentDirectory() + @"\" + Directories[i]);
                Console.WriteLine();

                symbolsEtalon.Clear();
                wordsEtalon.Clear();

                stopwatch.Start();
                Operation();
                stopwatch.Stop();
                Mean = stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Reset();

                for (int t = 0; t < Marr.Length; t++)
                {
                    M = Marr[t];
                    WritersM = Math.Min(M / 4, 2);
                    ReadersM = M - WritersM;

                    FillArrays();

                    stopwatch.Start();
                    ParallelOperation();
                    stopwatch.Stop();
                    ParallelMean[t] = stopwatch.Elapsed.TotalMilliseconds;
                    stopwatch.Reset();
                    Check();
                }
                

                Console.Write(i.ToString().PadLeft(3) + "|" + files.Length.ToString().PadLeft(15) + "|" + Mean.ToString().PadLeft(12));
                for (int t = 0; t < Marr.Length; t++)
                {
                    Console.Write("|" + ParallelMean[t].ToString().PadLeft(12));
                }
                if (Success) Console.Write("  |  OK"); else Console.Write("  | FAIL");
                //Console.Write(" SE C = " + symbolsEtalon.Values.Sum() + " SR C = " + symbolsRes.Values.Sum() + " COn = " + symbolsGlobalBuff.Values.Sum());

                //Console.ReadKey();
            }
            Console.WriteLine();
            Console.WriteLine();
            
        }
        #region anon types
        static void SequenceWords_AT()
        {
            string pattern = "[.?!)(,:«»;]";
            var wordCounts = files
                 .SelectMany(path => File.ReadLines(path))
                 .SelectMany(line => Regex.Replace(line, pattern, "").Split(' '))
                 .GroupBy(word => word)
                 .Select(group => new { group.Key, Value = group.Count() })
                 .OrderBy(pair => -pair.Value)
                 .ToList();          
        }

        static void SequenceSymbols_AT()
        {
            var wordCounts = files
                 .SelectMany(path => File.ReadLines(path))
                 .SelectMany(line => line)
                 .GroupBy(symbols => symbols)
                 .Select(group => new { group.Key, Value = group.Count() })
                 .OrderBy(pair => -pair.Value)
                 .ToList();
        }
        #endregion

        #region sequenceLinq 
        static void SequenceWordsLinq()
        {
            string pattern = "[.?!)(,:«»;]";
            wordsEtalon = files
                 .SelectMany(path => File.ReadLines(path))
                 .SelectMany(line => Regex.Replace(line, pattern, "").Split(' '))
                 .GroupBy(word => word)
                 .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
                 .OrderBy(pair => -pair.Value)
                 .ToDictionary(group => group.Key, group => group.Value);
        }

        static void SequenceSymbolsLinq()
        {
            symbolsEtalon = files
                 .SelectMany(path => File.ReadLines(path))
                 .SelectMany(line => line)
                 .GroupBy(symbols => symbols)
                 .Select(group => new KeyValuePair<char, int>(group.Key, group.Count()))
                 .OrderBy(pair => -pair.Value)
                 .ToDictionary(group => group.Key, group => group.Value);
        }
        #endregion

        #region Local Buffers Linq       

        static void LocalBuffersWordsLinq(object num)
        {
            string pattern = "[.?!)(,:«»;]";
            int start = (num as int[])[0];
            int end = (num as int[])[1];
            int i = (num as int[])[2];

            wordsLocals[i] = files
                 .Skip(start).SkipLast(files.Length - end)
                 .SelectMany(path => File.ReadLines(path))                
                 .SelectMany(line => Regex.Replace(line, pattern, "").Split(' '))
                 .GroupBy(word => word)
                 .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
                 .OrderBy(pair => -pair.Value)
                 .ToDictionary(group => group.Key, group => group.Value);
        }

        static void LocalBuffersSymbolsLinq(object num)
        {         
            int start = (num as int[])[0];
            int end = (num as int[])[1];
            int i = (num as int[])[2];

            symbolsLocals[i] = files
                 .Skip(start).SkipLast(files.Length - end)
                 .SelectMany(path => File.ReadLines(path))
                 .SelectMany(line => line)
                 .GroupBy(symbols => symbols)
                 .Select(group => new KeyValuePair<char, int>(group.Key, group.Count()))
                 .OrderBy(pair => -pair.Value)
                 .ToDictionary(group => group.Key, group => group.Value);
        }

        #endregion

        #region Sequence
        static void SequenceWords()
        {
            string pattern = "[.?!)(,:«»;]";
            foreach (var file in files)
            {
                string[] lines = File.ReadLines(file).ToArray();
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] words = Regex.Replace(lines[i], pattern, "").Split(' ');
                    for (int j = 0; j < words.Length; j++)
                        if (wordsEtalon.ContainsKey(words[j])) wordsEtalon[words[j]] += 1;
                        else wordsEtalon.Add(words[j], 1);
                }
            }
        }

        static void SequenceSymbols()
        {
            foreach (var file in files)
            {
                string[] lines = File.ReadLines(file).ToArray();
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    for (int j = 0; j < line.Length; j++)
                        if (symbolsEtalon.ContainsKey(line[j])) symbolsEtalon[line[j]] += 1;
                        else symbolsEtalon.Add(line[j], 1);
                }
            }
        }
        #endregion

        #region Local Buffers

        static void FileDecomposition()
        {
            int start = 0;
            int step = files.Length / M;

            Thread[] threads = new Thread[M];

            for (int i = 0; i < M - 1; i++)
            {
                threads[i] = new Thread(ThreadOperation);
                threads[i].Start(new int[] { start, start += step, i });
            }
            threads[M - 1] = new Thread(ThreadOperation);
            threads[M - 1].Start(new int[] { start, files.Length, M - 1 });

            for (int i = 0; i < M; i++) threads[i].Join();
            ConcatDictionaryes();
        }

        static void LocalBuffersWords(object num)
        {
            string pattern = "[.?!)(,:«»;]";
            int start = (num as int[])[0];
            int end = (num as int[])[1];
            int n = (num as int[])[2];
            wordsLocals[n] = new Dictionary<string, int>();

            for (int i = start; i < end; i++)
            {
                string[] lines = File.ReadLines(files[i]).ToArray();
                for (int k = 0; k < lines.Length; k++)
                {
                    string[] words = Regex.Replace(lines[k], pattern, "").Split(' ');
                    for (int j = 0; j < words.Length; j++)
                        if (wordsLocals[n].ContainsKey(words[j])) wordsLocals[n][words[j]] += 1;
                        else wordsLocals[n].Add(words[j], 1);
                }
            }           
        }

        static void LocalBuffersSymbols(object num)
        {
            int start = (num as int[])[0];
            int end = (num as int[])[1];
            int n = (num as int[])[2];
            symbolsLocals[n] = new Dictionary<char, int>();

            for (int i = start; i < end; i++) 
            {
                string[] lines = File.ReadLines(files[i]).ToArray();
                for (int k = 0; k < lines.Length; k++)
                {
                    string line = lines[k];
                    for (int j = 0; j < line.Length; j++)
                        if (symbolsLocals[n].ContainsKey(line[j])) symbolsLocals[n][line[j]] += 1;
                        else symbolsLocals[n].Add(line[j], 1);
                }
            }        
        }

        #endregion

        #region Global Buffer

        static void GlobalBufferWords(object num)
        {
            string pattern = "[.?!)(,:«»;]";
            int start = (num as int[])[0];
            int end = (num as int[])[1];


            for (int i = start; i < end; i++)
            {
                string[] lines = File.ReadLines(files[i]).ToArray();
                for (int k = 0; k < lines.Length; k++)
                {
                    string[] words = Regex.Replace(lines[k], pattern, "").Split(' ');
                    for (int j = 0; j < words.Length; j++)
                        wordsGlobalBuff.AddOrUpdate(words[j], 1, (key, value) => value + 1);
                }
            }
            
        }

        static void GlobalBufferSymbols(object num)
        {
            int start = (num as int[])[0];
            int end = (num as int[])[1];
            int n = (num as int[])[2];
            symbolsLocals[n] = new Dictionary<char, int>();

            for (int i = start; i < end; i++)
            {
                string[] lines = File.ReadLines(files[i]).ToArray();
                for (int k = 0; k < lines.Length; k++)
                {
                    string line = lines[k];
                    for (int j = 0; j < line.Length; j++)
                        symbolsGlobalBuff.AddOrUpdate(line[j], 1, (k, v) => v + 1);
                }
            }
        }
        #endregion

        #region Task Decomposition
        static void TaskDecomposition()
        {
            Thread[] readers = new Thread[ReadersM];
            Thread[] writers = new Thread[WritersM];

            int start = 0;
            int step = files.Length / ReadersM;

            for (int i = 0; i < WritersM - 1; i++)
            {
                writers[i] = new Thread(WriterColection);
                writers[i].Start(new int[] { start, start += step });
            }
            writers[WritersM - 1] = new Thread(WriterColection);
            writers[WritersM - 1].Start(new int[] { start, files.Length });

            for (int i = 0; i < ReadersM; i++)
            {
                readers[i] = new Thread(Reader);
                readers[i].Start(i);
            }

            for (int i = 0; i < WritersM; i++)
            {
                writers[i].Join();
            }
            ReadBuffer.CompleteAdding();
            for (int i = 0; i < ReadersM; i++)
            {
                readers[i].Join();
            }
            ConcatDictionaryes();
        }

        static void WriterColection(object o)
        {
            int start = (o as int[])[0];
            int end = (o as int[])[1];

            for (int i = start; i < end; i++)
            {
                string[] lines = File.ReadLines(files[i]).ToArray();
                for (int j = 0; j < lines.Length; j++)
                    ReadBuffer.Add(lines[j]);
            }
        }

        static void ReaderWord(object o)
        {
            int n = (int)o;
            string pattern = "[.?!)(,:«»;]";         
            wordsLocals[n] = new Dictionary<string, int>();
            string line;
            bool flag;
            while (true)
            {
                flag = ReadBuffer.TryTake(out line); 
                if (!flag && ReadBuffer.Count == 0 && ReadBuffer.IsAddingCompleted) return;
                else if (!flag) continue;
                string[] words = Regex.Replace(line, pattern, "").Split(' ');
                for (int j = 0; j < words.Length; j++)
                    if (wordsLocals[n].ContainsKey(words[j])) wordsLocals[n][words[j]] += 1;
                    else wordsLocals[n].Add(words[j], 1);
            }
        }

        static void ReaderSymbol(object o)
        {
            int n = (int)o;       
            symbolsLocals[n] = new Dictionary<char, int>();
            string line;
            bool flag;
            while (true)
            {
                flag = ReadBuffer.TryTake(out line);
                if (!flag && ReadBuffer.Count == 0 && ReadBuffer.IsAddingCompleted) return;
                else if (!flag) continue;
                for (int j = 0; j < line.Length; j++)
                    if (symbolsLocals[n].ContainsKey(line[j])) symbolsLocals[n][line[j]] += 1;
                    else symbolsLocals[n].Add(line[j], 1);
            }
        }
        #endregion

        #region My

        static void LocalNumbers(object num)
        {
            string pattern = "[.?!)(,:«»;]";
            string NumPattern = @"\d+";
            int start = (num as int[])[0];
            int end = (num as int[])[1];
            int n = (num as int[])[2];
            wordsLocals[n] = new Dictionary<string, int>();
            
            for (int i = start; i < end; i++)
            {
                string[] lines = File.ReadLines(files[i]).ToArray();
                for (int k = 0; k < lines.Length; k++)
                {
                    MatchCollection matches = Regex.Matches(Regex.Replace(lines[k], pattern, ""), NumPattern);
                    if (matches == null) continue;
                    for (int j = 0; j < matches.Count; j++)
                        if (wordsLocals[n].ContainsKey(matches[j].Value)) wordsLocals[n][matches[j].Value] += 1;
                        else wordsLocals[n].Add(matches[j].Value, 1);
                }
            }
        }

        static void GlobalNumbers(object num)
        {
            string pattern = "[.?!)(,:«»;]";
            int start = (num as int[])[0];
            int end = (num as int[])[1];
            string NumPattern = @"\d+";

            for (int i = start; i < end; i++)
            {
                string[] lines = File.ReadLines(files[i]).ToArray();
                for (int k = 0; k < lines.Length; k++)
                {
                    MatchCollection matches = Regex.Matches(Regex.Replace(lines[k], pattern, ""), NumPattern);
                    if (matches == null) continue;
                    for (int j = 0; j < matches.Count; j++)
                        wordsGlobalBuff.AddOrUpdate(matches[j].Value, 1, (key, value) => value + 1);

                }
            }
        }

        static void ReaderNumber(object o)
        {
            int n = (int)o;
            string pattern = "[.?!)(,:«»;]";
            string NumPattern = @"\d+";

            wordsLocals[n] = new Dictionary<string, int>();
            string line;
            bool flag;
            while (true)
            {
                flag = ReadBuffer.TryTake(out line);
                if (!flag && ReadBuffer.Count == 0 && ReadBuffer.IsAddingCompleted) return;
                else if (!flag) continue;
                MatchCollection matches = Regex.Matches(Regex.Replace(line, pattern, ""), NumPattern);
                if (matches == null) continue;
                for (int j = 0; j < matches.Count; j++)
                    if (wordsLocals[n].ContainsKey(matches[j].Value)) wordsLocals[n][matches[j].Value] += 1;
                    else wordsLocals[n].Add(matches[j].Value, 1);
            }
        }
        #endregion

        static void SeqNumbers()
        {
            string pattern = "[.?!)(,:«»;]";
            string NumPattern = @"\d+";

            foreach (var file in files)
            {
                string[] lines = File.ReadLines(file).ToArray();
                for (int k = 0; k < lines.Length; k++)
                {
                    MatchCollection matches = Regex.Matches(Regex.Replace(lines[k], pattern, ""), NumPattern);
                    for (int j = 0; j < matches.Count; j++)
                        if (wordsEtalon.ContainsKey(matches[j].Value)) wordsEtalon[matches[j].Value] += 1;
                        else wordsEtalon.Add(matches[j].Value, 1);
                }
            }
        }
    }
}

