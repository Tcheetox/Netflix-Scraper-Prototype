using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Logger;

namespace Utilities
{
    /// <summary>
    /// Support class to create tasks that can be canceled or restarted
    /// </summary>
    public class CancellableTask 
    {
        public string TaskOwner;
        public event EventHandler ExceptionRaised;
        public event EventHandler StatusChanged;
        public Task Task { get; private set; }
        private CancellationTokenSource cancellationTokenSource { get; set; }
        private readonly object taskLock = new object();
        private Action<object> action;

        protected virtual void OnExceptionRaised(EventArgs e)
        {
            ExceptionRaised?.Invoke(this, e);
        }

        public void SleepOrExit(int ms)
        {
            try
            {
                if (!cancellationTokenSource.IsCancellationRequested)
                    Task.Wait(ms, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Log.Write($"Task '{TaskOwner}' cancelled while awaiting for completion");
                //Log.Write(oce, $"Task '{TaskOwner}' cancelled while awaiting for completion", LogEntry.SeverityType.Low);
            }
        }

        public bool IsCancelled
        {
            get
            {
                if (cancellationTokenSource != null)
                    return cancellationTokenSource.IsCancellationRequested;
                else return true;
            }
        }

        private Exception exception;
        public Exception Exception
        {
            get
            {
                return exception;
            }
            private set
            {
                exception = value;
                OnExceptionRaised(EventArgs.Empty);
            }
        }

        public CancellableTask(Action<object> _action)
        {
            action = _action;
            TaskOwner = new StackTrace().GetFrame(1).GetMethod().ReflectedType.Name;
            Initialize();
            Status = CancellableTaskStatus.Created;
        }

        private CancellableTaskStatus status;
        public CancellableTaskStatus Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
                OnStatusChanged(EventArgs.Empty);
            }
        }

        protected virtual void OnStatusChanged(EventArgs e)
        {
            Log.Write($"Task '{TaskOwner}' {Status.ToString().ToLowerInvariant()}");
            StatusChanged?.Invoke(this, e);
        }

        public enum CancellableTaskStatus
        {
            Created,
            Started,
            Stopped,
            Faulted,
            Terminated
        }

        private void Initialize()
        {
            if (Status != CancellableTaskStatus.Created || Task == null)
            {
                Task = null;
                cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;
                Action<object> cancellableAction = new Action<object>((cancelled) =>
                {
                    try
                    {
                        action.Invoke(cancelled);
                        if (mustTerminate)
                            Status = CancellableTaskStatus.Terminated;
                        else
                            Status = CancellableTaskStatus.Stopped;
                    }
                    catch (Exception ex)
                    {
                        Status = CancellableTaskStatus.Faulted;
                        Exception = ex;
                    }
                });
                Task = new Task(cancellableAction, cancellationToken);
            }
        }

        public void Start()
        {
            lock (taskLock)
                switch (Status)
                {
                    case CancellableTaskStatus.Created:
                    case CancellableTaskStatus.Faulted:
                    case CancellableTaskStatus.Stopped:
                        Initialize();
                        Task.Start();
                        Status = CancellableTaskStatus.Started;
                        break;
                    case CancellableTaskStatus.Started:
                        Log.Write($"Task '{TaskOwner}' already running!");
                        break;
                    case CancellableTaskStatus.Terminated:
                        Log.Write(new NotImplementedException($"Terminated task '{TaskOwner}' cannot be restarted!"), string.Empty);
                        break;
                }
        }

        public void Restart()
        {
            lock (taskLock)
            {
                Log.Write($"Task '{TaskOwner}' restarting");
                Stop();
                Task.Wait();
                Start();
            }
        }

        public void Wait()
        {
            if (Task != null && Status != CancellableTaskStatus.Created)
                Task.Wait();
        }

        public void Stop()
        {
            lock(taskLock)
                if ((Status == CancellableTaskStatus.Started || Status == CancellableTaskStatus.Faulted)
                    && cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
                    cancellationTokenSource?.Cancel();
        }

        private bool mustTerminate = false;
        public void Terminate()
        {
            lock(taskLock)
                if (Status != CancellableTaskStatus.Terminated)
                {
                    mustTerminate = true;
                    Stop();
                    action = null;
                }
        }

        public static void WaitTermination(params CancellableTask[] _cancellableTasks)
        {
            var cancellableTasks = _cancellableTasks.Where(x => x.Status != CancellableTaskStatus.Created);
            Task.WaitAll(cancellableTasks.Select(y => y.Task).ToArray());
            Log.Write($"Cancellable tasks ({cancellableTasks.Count()}) properly terminated", LogEntry.SeverityType.Medium);
        }
    }
}