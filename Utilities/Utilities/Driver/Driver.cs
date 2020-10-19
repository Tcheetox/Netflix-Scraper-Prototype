using OpenQA.Selenium.Chrome;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using Utilities.Logger;
using System.Linq;

namespace Utilities.Driver
{
    public partial class Driver : ChromeDriver
    {
        public readonly int MinimalWaitBetweenAction;
        public static ChromeDriverService ChromeDriverService { get; private set; }
        public event EventHandler ExceptionRaised;

        private static List<KeyValuePair<int, string>> initProcesses;
        private readonly List<KeyValuePair<int, string>> relatedProcesses;
        private readonly CancellableTask cancellableTask;

        /// <summary>
        /// Create default service for chromedriver session
        /// Store the list of initial running processes (i.e. this list is used to Kill the associated session processes if the driver cannot dispose properly)
        /// </summary>
        /// <returns></returns>
        private static ChromeDriverService CreateDefaultService()
        {
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            ChromeDriverService = service;

            // Save processes related to current session
            initProcesses = new List<KeyValuePair<int, string>>();
            foreach (var proc in Process.GetProcesses())
                if (proc.ProcessName.ToLowerInvariant().Contains("chrome"))
                    initProcesses.Add(new KeyValuePair<int, string>(proc.Id, proc.ProcessName.ToLowerInvariant()));
            return service;
        }

        /// <summary>
        /// Create default options for chromedriver session (i.e. can be augmented by extraParameters if any)
        /// </summary>
        /// <param name="browserDataPath"></param>
        /// <param name="extraParameters"></param>
        /// <returns></returns>
        private static ChromeOptions CreateOptions(string browserDataPath, string[] extraParameters)
        {
            ChromeOptions options = new ChromeOptions();

            if (IOSupport.IsDirectoryWriteable(browserDataPath))
                options.AddArgument($"--user-data-dir={browserDataPath}");
            if (extraParameters != null)
                options.AddArguments(extraParameters);

            return options;
        }

        public Driver(int minimalWaitBetweenAction, string browserDataPath = "", string[] extraParameters = null, CancellableTask _cancellableTask = null) : base(CreateDefaultService(), CreateOptions(browserDataPath, extraParameters))
        {
            // Get all processes associated to this chromedriver session (includes both chrome and chromedriver)
            relatedProcesses = new List<KeyValuePair<int, string>>();
            foreach (var proc in Process.GetProcesses())
                if (proc.ProcessName.ToLowerInvariant().Contains("chrome") && !initProcesses.Any(x => x.Key == proc.Id))
                    relatedProcesses.Add(new KeyValuePair<int, string>(proc.Id, proc.ProcessName.ToLowerInvariant()));

            cancellableTask = _cancellableTask;
            MinimalWaitBetweenAction = minimalWaitBetweenAction;
            Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(minimalWaitBetweenAction);
            Log.Write($"ChromeDriver instantiated", LogEntry.SeverityType.Low);
        }

        public void DisposeOrKill()
        {
            try
            {
                Dispose();
                Log.Write($"ChromeDriver properly disposed", LogEntry.SeverityType.Low);
            }
            catch (Exception ex)
            {
                Kill();
                Log.Write(ex, $"Chromedriver couldn't dispose properly, {relatedProcesses.Count} processes have been killed!", LogEntry.SeverityType.Low);
            }
        }

        public void Kill()
        {
            if (relatedProcesses?.Count > 0)
                foreach (var proc in relatedProcesses)
                    ProcessSupport.KillProcess(proc.Key);
        }

        public void Sleep(int cautiousWaitTime = 0)
        {
            cancellableTask.SleepOrExit(cautiousWaitTime > 0? cautiousWaitTime : MinimalWaitBetweenAction);
        }

        public static bool IsEmptyEventHandler(EventHandler handler)
        {
            if (handler.Method.GetParameters().Length > 0 && handler.Method.GetParameters()[0].Name.ToLowerInvariant().Contains("p0"))
                return true;
            else return false;
        }
    }
}
