using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Utilities.Logger
{
    public static class Log
    {
        public static bool Started { get; private set; }

        private static ConcurrentQueue<LogEntry> logEntries;
        private static CancellableTask task;
        private static bool useConsole;
        private static int intervalBetweenPurge;
        private static string colorPattern;

        public static void Start(bool _useConsole, string directoryPath, int _intervalBetweenPurge)
        {
            // Create color pattern
            foreach (var color in Enum.GetValues(typeof(ConsoleColor)))
                colorPattern += $"({color}" + "{)" + "|";
            colorPattern += "(})";

            // Save variable and instantiate logging
            intervalBetweenPurge = _intervalBetweenPurge;
            useConsole = _useConsole;
            if (IOSupport.IsDirectoryWriteable(directoryPath))
            {
                logEntries = new ConcurrentQueue<LogEntry>();
                Started = true;
                Write($"Application {Process.GetCurrentProcess().ProcessName} started (logging enabled)");
                task = new CancellableTask((token) =>
                {
                    while (!((CancellationToken)token).IsCancellationRequested)
                    {
                        PurgeQueue(directoryPath);
                        Thread.Sleep(intervalBetweenPurge);
                    }
                });
                task.Start();
            }
            else
                Write($"Application {Process.GetCurrentProcess().ProcessName} started (logging disabled)");
        }

        public static void Stop(bool applicationExiting = false)
        {
            if (applicationExiting)
                Write($"Application {Process.GetCurrentProcess().ProcessName} terminated", LogEntry.SeverityType.High);
            if (Started)
            {
                Thread.Sleep(intervalBetweenPurge);
                task.Stop(true);
            }
        }

        private static void PurgeQueue(string directoryPath)
        {
            try
            {
                FileStream fs = new FileStream(Path.Combine(directoryPath, $"{DateTime.Now:yyyyMMdd}.log"), FileMode.Append, FileAccess.Write, FileShare.Read);
                using (StreamWriter sw = new StreamWriter(fs))
                    while (!logEntries.IsEmpty)
                        if (logEntries.TryDequeue(out LogEntry logEntry))
                        {
                            sw.WriteLine(CreateString(logEntry, false)); 
                            Print(logEntry);
                        }
            }
            catch (Exception ex)
            {
                Print(new LogEntry(ex, "Error while writing to log file", LogEntry.SeverityType.Medium, false, string.Empty));
            }
        }

        public static void Write(Exception _exception, string _caption, LogEntry.SeverityType _severity = LogEntry.SeverityType.Low, bool _isLogOnly = false, string _message = "")
        {
            Write(new LogEntry(_exception, _caption, _severity, _isLogOnly, _message));
        }

        public static void Write(string _caption, LogEntry.SeverityType _severity = LogEntry.SeverityType.None, bool _isLogOnly = false, string _message = "")
        {
            Write(new LogEntry(_caption, _severity, _isLogOnly, _message));
        }

        private static void Write(LogEntry logEntry)
        {
            if (Started)
                logEntries.Enqueue(logEntry);
            else
                Print(logEntry);
        }

        private static string CreateString(LogEntry logEntry, bool keepColor)
        {
            string output = logEntry.ToString(!keepColor);
            if (!Started)
            {
                if (logEntry.Severity == LogEntry.SeverityType.None)
                    output = $"{logEntry.ToString()} [NO LOG]";
                else
                    output = $"{logEntry.ToString()} *{logEntry.Severity}* [NO LOG]";
            }

            if (keepColor)
                return output;
            else
                return Regex.Replace(output, colorPattern, string.Empty);
        }

        private static readonly object consoleLock = new object();
        private static void Print(LogEntry logEntry) 
        {
            if (logEntry.IsLogOnly)
                return;

            // Print to console with color
            if (!useConsole)
                Debug.WriteLine(CreateString(logEntry, false));
            else
            {
                lock (consoleLock)
                {
                    // Define default output color
                    ConsoleColor defaultColor = ConsoleColor.Gray;
                    switch (logEntry.Severity)
                    {
                        case LogEntry.SeverityType.Low:
                            defaultColor = ConsoleColor.DarkGray;
                            break;
                        case LogEntry.SeverityType.Medium:
                            defaultColor = ConsoleColor.White;
                            break;
                        case LogEntry.SeverityType.High:
                            defaultColor = ConsoleColor.DarkYellow;
                            break;
                        case LogEntry.SeverityType.Critical:
                            defaultColor = ConsoleColor.Red;
                            break;
                    }
                    Console.ForegroundColor = defaultColor;

                    // Print stuff
                    bool colorChanged = false;
                    foreach (string line in Regex.Split(CreateString(logEntry, true), colorPattern, RegexOptions.IgnoreCase))
                    {
                        if (line.Contains("{") && Enum.TryParse(line.Replace("{", string.Empty), true, out ConsoleColor pick))
                        {
                            Console.ForegroundColor = pick;
                            colorChanged = true;
                        }
                        else if (line.Contains("}") && colorChanged)
                            Console.ForegroundColor = defaultColor;
                        else
                            Console.Write(line);
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
