using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Utilities.Logger;

namespace MediaSkraper
{
    static class Program
    {
        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]

        private static extern IntPtr GetConsoleWindow();
        private static DataManager DataManager;

        static void Main(string[] args)
        {
            // Application should be closed using CTRL-C for proper disposal, CTRL-Break attempts to recover DataManager
            Console.CancelKeyPress += Console_CancelKeyPress;
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);

            // Start logging information
            Log.Start(true, Path.Combine(Environment.CurrentDirectory,"Logs"), 10);
            Log.Write("Press CTRL-C to exit the application at any time!", LogEntry.SeverityType.High);
            // Start of the operations
            DataManager = new DataManager();
            DataManager.Scrape();
            DataManager.WaitAll();

            // Await proper disposal and exit
            while (!IsDisposed)
                Thread.Sleep(100);
            Environment.Exit(0); // Exit from main thread
        }

        private static bool IsDisposed;
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            switch (e.SpecialKey)
            {
                case ConsoleSpecialKey.ControlBreak:
                    // TODO: add recovery mechanic
                    // Log.Write("Green{DataManager will now attempt to recover!}", LogEntry.SeverityType.High);
                    break;

                case ConsoleSpecialKey.ControlC:
                    Log.Write("Application MediaSkraper has been Red{interrupted} and is now disposing...", LogEntry.SeverityType.High);
                    DataManager.Dispose();
                    Log.Stop(true);
                    IsDisposed = true;
                    break;
            }
        }
    }
}
