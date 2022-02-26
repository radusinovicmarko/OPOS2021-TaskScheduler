using System.Diagnostics;

namespace TaskScheduler
{
    public class TaskScheduler
    {
        private readonly int _maxNumberOfCores;
        private readonly int _maxNumberOfConcurrentTasks;
        private bool _started = false;

        private PriorityQueue<MyTask, MyTask.TaskPriority> _scheduledTasks = new ();

        private List<Thread> _threads;

        private object _lock = new object();

        public int MaxNumberOfCores => _maxNumberOfCores;
        public int MaxNumberOfConcurrentTasks => _maxNumberOfConcurrentTasks;

        public TaskScheduler(int maxNumberOfCores, int maxNumberOfConcurrentTasks)
        {
            _maxNumberOfCores = maxNumberOfCores;
            _maxNumberOfConcurrentTasks = maxNumberOfConcurrentTasks;
            _threads = new List<Thread>(maxNumberOfCores);
        }

        public void AddTask(MyTask myTask)
        {
            int maxNumberOfTasks = Math.Min(_maxNumberOfConcurrentTasks, _maxNumberOfCores);
            lock (_lock)
            {
                if (_scheduledTasks.Count < maxNumberOfTasks)
                    myTask.State = MyTask.TaskState.Ready;
                _scheduledTasks.Enqueue(myTask, myTask.Priority);
                Monitor.PulseAll(_lock);
            }
        }

        public void Start()
        {
            if (_started)
                return;
            _started = true;
            for (int i = 0; i < _maxNumberOfCores; i++)
            {
                int iCopy = i;
                new Thread(() =>
                {
                    while (true)
                    {
                        if (_scheduledTasks.Count > 0)
                        {
                            MyTask nextTask;
                            lock (_lock)
                            {
                                if (_scheduledTasks.Count <= 0)
                                    continue;
                                nextTask = _scheduledTasks.Peek();
                                if (nextTask.State == MyTask.TaskState.Ready)
                                    _scheduledTasks.Dequeue();
                            }
                            Thread cancelTaskThread = new Thread(() => CancelTask(nextTask));
                            cancelTaskThread.Start();
                            nextTask.Execute();
                            nextTask.Terminated = true;
                            lock (_lock)
                            {
                                if (_scheduledTasks.Count > 0)
                                    _scheduledTasks.Peek().State = MyTask.TaskState.Ready;
                                Monitor.PulseAll(_lock);
                            }
                        }
                        else
                            lock (_lock)
                                Monitor.Wait(_lock);
                    }
                }).Start();
            }
        }

        private static void CancelTask(MyTask task)
        {
            if (task.ControlToken == null)
                return;
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!task.Terminated)
            {
                if (stopwatch.Elapsed.TotalSeconds >= task.MaxExecTime || DateTime.Now >= task.Deadline)
                {
                    task.ControlToken.Terminate();
                    break;
                }
            }
        }

        private void ThreadCoreExecution()
        {
            while (true)
            {
                if (_scheduledTasks.Count > 0)
                {
                    MyTask nextTask;
                    lock (_lock)
                    {
                        if (_scheduledTasks.Count <= 0)
                            continue;
                        nextTask = _scheduledTasks.Peek();
                        if (nextTask.State == MyTask.TaskState.Ready)
                            _scheduledTasks.Dequeue();
                    }
                    Thread cancelTaskThread = new Thread(() => CancelTask(nextTask));
                    cancelTaskThread.Start();
                    nextTask.Execute();
                    nextTask.Terminated = true;
                    lock (_lock)
                    {
                        if (_scheduledTasks.Count > 0)
                            _scheduledTasks.Peek().State = MyTask.TaskState.Ready;
                        Monitor.PulseAll(_lock);
                    }
                }
                else
                    lock (_lock)
                        Monitor.Wait(_lock);
            }
        }
    }
}