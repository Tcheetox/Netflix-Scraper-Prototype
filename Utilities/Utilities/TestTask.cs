
using System.Diagnostics;
using System.Threading;
using Utilities;

namespace SpacepirateMonitor
{
    /// <summary>
    /// Fake class to develop and test 'CancellableTask'
    /// </summary>
    public class TestTask
    {
        public TestTask()
        { }

        private CancellableTask monitorTask;
        public void Start()
        {
            monitorTask = new CancellableTask((token) =>
            {
                Debug.WriteLine($"TASK CREATED");
                while (!((CancellationToken)token).IsCancellationRequested)
                {
                    Debug.WriteLine($"> WORKING IN THE TASK (CANCEL: {((CancellationToken)token).IsCancellationRequested})");
                    monitorTask.SleepOrExit(1000);
                }
                Debug.WriteLine($"TASK ABOUT TO FINISH (CANCEL: {((CancellationToken)token).IsCancellationRequested})");
            });
            monitorTask.Start();
        }

        public void Restart()
        {
            Debug.WriteLine($"TASK SHOULD RESTART");
            monitorTask.Restart();
            Debug.WriteLine($"TASK SHOULD HAVE RESTARTED ?!");
        }

        public void Stop()
        {
            Debug.WriteLine($"TASK STOPPING");
            monitorTask.Terminate();
            Debug.WriteLine($"TASK STOPPED!");
        }
    }
}
