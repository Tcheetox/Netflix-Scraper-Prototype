using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Logger;

namespace Utilities
{
    // TODO: add static metadata storage

    /// <summary>
    /// Support class to create tasks that can be canceled or restarted
    /// </summary>
    public class CancellableTask 
    {
        private string taskOwner;
        public string TaskOwner
        {
            get
            {
                if (string.IsNullOrEmpty(taskOwner))
                    return new StackTrace().GetFrame(2).GetMethod().ReflectedType.Name;
                else return taskOwner;
            }
            set
            {
                taskOwner = value;
            }
        }

        public event EventHandler ExceptionRaised;

        private CancellationTokenSource CancellationTokenSource { get; set; }
        private Task task;
        private readonly object taskLock = new object();
        private object action;
        private const int sleepInterval = 30;

        protected virtual void OnExceptionRaised(EventArgs e)
        {
            ExceptionRaised?.Invoke(this, e);
        }

        public void SleepOrExit(int ms)
        {
            if (ms <= 0) 
                return;

            if (CancellationTokenSource == null)
                Thread.Sleep(ms);
            else
            {
                int timeSlept = 0;
                while (timeSlept < ms && CancellationTokenSource != null && !CancellationTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(sleepInterval);
                    timeSlept += sleepInterval;
                }
            }
        }

        public bool IsCancelled
        {
            get
            {
                if (CancellationTokenSource != null)
                    return CancellationTokenSource.IsCancellationRequested;
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
                Status = CancellableTaskStatus.Faulted;
                Log.Write($"Task '{TaskOwner}' faulted");
                OnExceptionRaised(EventArgs.Empty);
            }
        }

        public CancellableTask(Action _action)
        {
            action = _action;
        }

        public CancellableTask(Action<object> _action)
        {
            action = _action;
        }

        public CancellableTaskStatus Status { get; private set; }
        public enum CancellableTaskStatus
        {
            Created,
            Running,
            Stopped,
            Faulted,
            Terminated
        }

        private void Initialize()
        {
            task = null;
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = CancellationTokenSource.Token;

            Action<object> cancellableAction = new Action<object>((cancelled) =>
            {
                try
                {
                    if (action is Action<object> _cancellableAction)
                        _cancellableAction.Invoke(cancelled);
                    else
                        ((Action)action).Invoke();
                }
                catch (Exception ex)
                {
                    Status = CancellableTaskStatus.Faulted;
                    Exception = ex;
                }
            });

            task = new Task(cancellableAction, cancellationToken);
            Status = CancellableTaskStatus.Created;
        }

        public void Start()
        {
            lock (taskLock)
            {
                try
                {
                    if (Status != CancellableTaskStatus.Running)
                    {
                        Initialize();
                        task.Start();
                        if (!IsRestarting)
                            Log.Write($"Task '{TaskOwner}' started");
                        IsRestarting = false;
                    }
                    else
                        Log.Write($"Task '{TaskOwner}' already running!");
                }
                catch (Exception ex)
                {
                    Status = CancellableTaskStatus.Faulted;
                    Exception = ex;
                }
                Status = CancellableTaskStatus.Running;
            }
        }

        private bool IsRestarting;
        public void Restart()
        {
            lock (taskLock)
                if (Status != CancellableTaskStatus.Terminated)
                {
                    IsRestarting = true;
                    Log.Write($"Task '{TaskOwner}' restarting");
                    Stop(true);
                    Initialize();
                    Start();
                }
                else
                    throw new NotImplementedException("A terminated cancellable task can never be restarted!");
        }

        public void Wait()
        {
            while(IsRestarting || task != null)
            {
                task?.Wait();
                Thread.Sleep(sleepInterval);
            }
        }

        public void Stop(bool waitForTermination = false)
        {
            lock (taskLock)
                if (Status == CancellableTaskStatus.Running || Status == CancellableTaskStatus.Faulted)
                {
                    CancellationTokenSource?.Cancel();
                    if (task != null)
                    {
                        if (waitForTermination)
                            task.Wait();
                        if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
                            task.Dispose();
                        Status = CancellableTaskStatus.Stopped;
                    }
                    if (!IsRestarting)
                        Log.Write($"Task '{TaskOwner}' stopped");
                }
        }

        public void Terminate()
        {
            lock (taskLock)
                if (Status != CancellableTaskStatus.Terminated)
                {
                    Stop(true);
                    action = null;
                    task = null;
                    CancellationTokenSource = null;
                    Status = CancellableTaskStatus.Terminated;
                    Log.Write($"Task '{TaskOwner}' terminated");
                }
        }
    }
}