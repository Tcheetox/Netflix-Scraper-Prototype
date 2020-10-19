
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
        private CancellableTask monitorTask;
        public TestTask()
        {
            monitorTask = new CancellableTask((token) =>
            {
                while (!((CancellationToken)token).IsCancellationRequested)
                {
                    Debug.WriteLine($"> WORKING IN THE TASK (CANCEL: {((CancellationToken)token).IsCancellationRequested})");
                    monitorTask.SleepOrExit(1000);
                }
                Debug.WriteLine($"TASK ABOUT TO FINISH (CANCEL: {((CancellationToken)token).IsCancellationRequested})");
            });
        }

        public void Start()
        {
            Debug.WriteLine($"TASK SHOULD START");
            monitorTask.Start();
        }

        public void Restart()
        {
            Debug.WriteLine($"TASK SHOULD RESTART");
            monitorTask.Restart();
        }

        public void Stop()
        {
            Debug.WriteLine($"TASK STOPPING");
            monitorTask.Stop();
        }

        public void Terminate()
        {
            Debug.WriteLine($"TASK TERMINATING");
            monitorTask.Terminate();
        }
    }
}
