// -------------------------------------------------------------------------------------------------------------------------
// <copyright>
// https://www.apache.org/licenses/LICENSE-2.0
// Copyright 2020 Chris Shawcross "cshawky", SHAWKY Electronics, Australia
// Please refer to LICENCE.txt in this project folder.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using System.Threading;
using CsTool.Logger;
using System.Diagnostics;

namespace MyApplication
{
    /// <summary>
    /// Measure the performance of CsTool.Logger.
    /// 24th May 2020 i7 @4.7GHz 6 core/12 threads
    /// Single producer ~ 330000/second
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            int count = 200000;
            int threads = 1;
            int bufferSize = count + 1000;
            string message;
            Logger.LogThresholdMaxLevel = DebugThresholdLevel.LogInfo;
            Console.Write("CsTool.Logger Test Programme. Please wait");
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(1000);
                Console.Write(".");
            }
            Console.WriteLine("");


            //
            // Testing async queueing
            //
            Logger.Instance.IsSynchronousLogger = false;        // Only available if DEBUG is defined: Default true
#if DEBUG_QUEUE
            Logger.Instance.IsQueueAsyncEnabled = true;        // Only available if DEBUG is defined: Default true
#endif
            Logger.Instance.AddMessageMaxTime = 0;
            message = "Testing CsTool.Logger Async File logging single thread producer, async queuing...";
            TestCsToolLogger1(count, bufferSize, threads, message);

            //
            // Standard setup test
            //
            message = "Testing CsTool.Logger Async File logging single thread producer...";
            Logger.Instance.IsSynchronousLogger = false;        // Only available if DEBUG is defined: Default true
#if DEBUG_QUEUE
            Logger.Instance.IsQueueAsyncEnabled = false;        // Only available if DEBUG is defined: Default true
#endif
            Logger.Instance.AddMessageMaxTime = 0;
            TestCsToolLogger1(count, bufferSize, threads, message);

            message = "Testing CsTool.Logger Async File logging single thread producer, repeat...";
            Logger.Instance.IsSynchronousLogger = false;
#if DEBUG_QUEUE
            Logger.Instance.IsQueueAsyncEnabled = false;
#endif
            Logger.Instance.AddMessageMaxTime = 0;
            TestCsToolLogger1(count, bufferSize, threads, message);

            //
            // Synchronous
            //
            message = "Testing CsTool.Logger Synchronised file logging...";
            Logger.Instance.IsSynchronousLogger = true;
            Logger.Instance.AddMessageMaxTime = 0;
            TestCsToolLogger1(count, bufferSize, threads, message);

            /*
            message = "Testing CsTool.Logger Async File logging with log delayed for debugging...";
            Logger.Instance.IsSynchronousLogger = false;
            Logger.Instance.AddMessageMaxTime = 0;
            Logger.Instance.DebugMessageWriteDelay = 50;
            count = 100;
            TestCsToolLogger2(count, bufferSize, threads, message);
            */

            Logger.DisplayLogFile();
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }

        static void TestCsToolLogger1(int count, int bufferSize, int threads, string message)
        {
            Console.WriteLine("\n*******************************");
            Console.WriteLine(message);
            Logger.LogThresholdMaxLevel = DebugThresholdLevel.LogInfo;
            ulong arg1 = 0x1ffff0000;
            string arg2 = "Hello World";
            bool arg3 = true;
            Logger.Write(message);
            Console.WriteLine("Log a message with ULONG, String and Bool to {0}", Logger.FullLogFileName);
            Logger.LogMessage(LogPriority.Info, "Information Test Message ULong({0}) String({1}) Bool({2})", arg1, arg2, arg3);
            // Test change of log file name, normally do this before performing any logging.
            Logger.LogMessage(LogPriority.Info, "Renaming log file...");
            //arg2 = "CsTool.Logger";
            //Logger.FilePrepend = arg2;
            //Logger.MaximumLogQueueSize = bufferSize;
            Logger.Write("Information Test Message ULong({0}) String({1}) Bool({2})", arg1, arg2, arg3);
            Logger.Write(LogPriority.Warning, "Information Test Message ULong({0}) String({1}) Bool({2})", arg1, arg2, arg3);
            Logger.Write(LogPriority.Fatal, "Information Test Message ULong({0}) String({1}) Bool({2})", arg1, arg2, arg3);
            Console.WriteLine($"CsTool.Logger: {Logger.FullLogFileName}");
            do
            {
                Thread.Sleep(5);
            } while (Logger.LogQueueCount > 0);

            //
            // Performance test
            //
            Console.WriteLine("Enqueue {0} messages...", count);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Restart();
            var task1 = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < count / threads; i++)
                {
                    Logger.Write("Information Test Message Count({0}) String({1}) Bool({2})", i, arg2, arg3);
                }
            });

            task1.Wait();
            //task2.Wait();
            //task3.Wait();
            //task4.Wait();
            Logger.Write("Enqueueing {0} messages took {1:0.00} seconds", count, stopwatch.Elapsed.TotalSeconds);
            Console.WriteLine("Enqueueing {0} messages took {1:0.00} seconds, {2}/second", count, stopwatch.Elapsed.TotalSeconds, (int)(count/ stopwatch.Elapsed.TotalSeconds));

            // We have visibility of the queue, so monitor it
            if (Logger.LogQueueCount > 0) Console.WriteLine("Waiting for queue flush...");
            do
            {
                Thread.Sleep(5);
            } while (Logger.LogQueueCount > 0);
            Logger.Write("Dequeue {0} messages took < {1:0.00} seconds, {2}/second", count, stopwatch.Elapsed.TotalSeconds, (int)(count / stopwatch.Elapsed.TotalSeconds));
            Logger.CloseAndFlush();
            stopwatch.Stop();
            if (Logger.CountLostMessagesTotal > 0)
            {
                Logger.Write(LogPriority.Fatal, $"Lost {Logger.CountLostMessagesTotal} messages");
                Console.WriteLine($"ERROR: Lost {Logger.CountLostMessagesTotal} messages");
            }
            Console.WriteLine("\nDequeue/Flush (write to file) {0} messages took {1:0.00} seconds, {2}/second", count, stopwatch.Elapsed.TotalSeconds, (int)(count / stopwatch.Elapsed.TotalSeconds));
            Console.WriteLine("Longest Queue Add {0:0.000} milliseconds", Logger.AddMessageMaxTime * 1000);
        }


        static void TestCsToolLogger2(int count, int bufferSize, int threads, string message)
        {
            Console.WriteLine("\n\n*******************************");
            Console.WriteLine(message);
            Logger.LogThresholdMaxLevel = DebugThresholdLevel.LogInfo;
            ulong arg1 = 0x1ffff0000;
            string arg2 = "Hello World";
            bool arg3 = true;
            Logger.Write(message);
            Console.WriteLine("Log a message with ULONG, String and Bool to {0}", Logger.LogFilePath);
            Logger.LogMessage(LogPriority.Info, "Information Test Message ULong({0}) String({1}) Bool({2})", arg1, arg2, arg3);
            Logger.Write("Information Test Message ULong({0}) String({1}) Bool({2})", arg1, arg2, arg3);
            Logger.Write(LogPriority.Warning, "Information Test Message ULong({0}) String({1}) Bool({2})", arg1, arg2, arg3);
            Logger.Write(LogPriority.Fatal, "Information Test Message ULong({0}) String({1}) Bool({2})", arg1, arg2, arg3);
            Console.WriteLine($"CsTool.Logger: {Logger.LogFilePath}");
            do
            {
                Thread.Sleep(5);
            } while (Logger.LogQueueCount > 0);

            //
            // Performance test
            //
            Console.WriteLine("Enqueue {0} messages...", count);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Restart();
            var task1 = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < count / threads; i++)
                {
                    message = String.Format("Information Test Message Count({0}) String({1}) Bool({2})", i, arg2, arg3);
                    Logger.Write(message);
                }
            });

            task1.Wait();
            //task2.Wait();
            //task3.Wait();
            //task4.Wait();
            Logger.Write("Enqueueing {0} messages took {1:0.00} seconds", count, stopwatch.Elapsed.TotalSeconds);
            Console.WriteLine("Enqueueing {0} messages took {1:0.00} seconds, {2}/second", count, stopwatch.Elapsed.TotalSeconds, (int)(count / stopwatch.Elapsed.TotalSeconds));

            // We have visibility of the queue, so monitor it
            if (Logger.LogQueueCount > 0) Console.WriteLine("Waiting for queue flush...");
            do
            {
                Thread.Sleep(5);
            } while (Logger.LogQueueCount > 0);
            Logger.Write("Dequeue {0} messages took < {1:0.00} seconds, {2}/second", count, stopwatch.Elapsed.TotalSeconds, (int)(count / stopwatch.Elapsed.TotalSeconds));
            Logger.CloseAndFlush();
            stopwatch.Stop();
            if (Logger.CountLostMessagesTotal > 0)
            {
                Logger.Write(LogPriority.Fatal, $"Lost {Logger.CountLostMessagesTotal} messages");
                Console.WriteLine($"ERROR: Lost {Logger.CountLostMessagesTotal} messages");
            }
            Console.WriteLine("\nDequeue/Flush (write to file) {0} messages took {1:0.00} seconds, {2}/second", count, stopwatch.Elapsed.TotalSeconds, (int)(count / stopwatch.Elapsed.TotalSeconds));
            Console.WriteLine("Longest Queue Add {0:0.000} milliseconds", Logger.AddMessageMaxTime * 1000);
        }

    }
}
