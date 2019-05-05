using DispatcherHelper;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DispatcherTestApp
{
    class Program
    {
        static Dispatcher mainDispatcher_;
        static EventLoop loop_;

        [STAThread]
        static void Main()
        {
            Log("start");

            mainDispatcher_ = Dispatcher.CurrentDispatcher;
            Console.CancelKeyPress += Console_CancelKeyPress;

            mainDispatcher_.BeginInvoke(DispatcherPriority.Normal, new Action(OnLoad));

            Dispatcher.Run();

            loop_.Dispose();

            Console.WriteLine("Press Enter key to quit.");
            Console.ReadLine();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Log("ctrl-c");
            mainDispatcher_.BeginInvokeShutdown(DispatcherPriority.Normal);
            e.Cancel = true;
        }

        private static void Log(string message)
        {
            Console.WriteLine("[{0}]{1}", Thread.CurrentThread.ManagedThreadId, message);
        }

        private static void OnLoad()
        {
            loop_ = new EventLoop();
            loop_.Run();
            loop_.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            loop_.Dispatcher.BeginInvoke(new Action(OnTick));
        }

        private static void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log(e.Exception.ToString());
            e.Handled = true;
        }

        private static async void OnTick()
        {
            var sw = new Stopwatch();
            sw.Start();
            while (!loop_.Dispatcher.HasShutdownStarted)
            {
                Log(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff", null));
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(true);

                if (sw.Elapsed > TimeSpan.FromSeconds(15))
                {
                    throw new Exception("Elapsed too long.");
                }
            }
        }
    }
}
