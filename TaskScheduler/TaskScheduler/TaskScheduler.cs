using System.Diagnostics;

namespace TaskScheduler
{
    public class TaskScheduler
    {
        private readonly int _maxNumberOfCores;
        private readonly int _maxNumberOfConcurrentTasks;
        private bool _started = false;

        private PriorityQueue<MyTask, MyTask.TaskPriority> _scheduledTasks = new (new MyTaskComparer());

        private List<Thread> _runningThreads;

        private List<MyTask> _runningTasks;

        private Dictionary<MyTask, Thread> _pausedThreads = new ();

        private object _lock = new object();

        public int MaxNumberOfCores => _maxNumberOfCores;
        public int MaxNumberOfConcurrentTasks => _maxNumberOfConcurrentTasks;

        public TaskScheduler(int maxNumberOfCores, int maxNumberOfConcurrentTasks)
        {
            _maxNumberOfCores = maxNumberOfCores;
            _maxNumberOfConcurrentTasks = maxNumberOfConcurrentTasks;
            _runningThreads = new List<Thread>(maxNumberOfCores);
            _runningTasks = new List<MyTask>(maxNumberOfCores);
            for (int i = 0; i < maxNumberOfCores; i++)
            {
                _runningTasks.Add(null);
                _runningThreads.Add(null);
            }
        }

        public void AddTask(MyTask myTask)
        {
            int maxNumberOfTasks = Math.Min(_maxNumberOfConcurrentTasks, _maxNumberOfCores);
            lock (_lock)
            {
                if (_runningTasks.Count(task => task != null) >= maxNumberOfTasks)
                {
                    MyTask minPriorityTask = _runningTasks.ElementAt(0);
                    int index = 0;
                    for (int i = 1; i < _runningTasks.Count; i++)
                        if (_runningTasks.ElementAt(i).Priority > minPriorityTask.Priority)
                        {
                            minPriorityTask = _runningTasks.ElementAt(i);
                            index = i;
                        }
                    if (minPriorityTask.Priority > myTask.Priority)
                    {
                        minPriorityTask.ControlToken?.Pause();
                        minPriorityTask.State = MyTask.TaskState.Paused;
                        _scheduledTasks.Enqueue(minPriorityTask, minPriorityTask.Priority);
                        _pausedThreads.Add(minPriorityTask, _runningThreads.ElementAt(index));
                        Thread t = new Thread(() => ThreadCoreExecution(index));
                        _runningThreads[index] = t;
                        myTask.State = MyTask.TaskState.Ready;
                        _scheduledTasks.Enqueue(myTask, myTask.Priority);
                        t.Start();
                        return;
                    }
                }
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
                Thread t = new Thread(() => ThreadCoreExecution(iCopy));
                t.Start();
                _runningThreads[iCopy] = t;
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
                    task.ControlToken?.Terminate();
                    break;
                }
            }
        }

        private void ThreadCoreExecution(int i)
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
                        if (nextTask.State == MyTask.TaskState.Paused)
                        {
                            nextTask.ControlToken?.Resume();
                            _scheduledTasks.Dequeue();
                            return;
                        }
                        if (nextTask.State == MyTask.TaskState.Ready)
                            _scheduledTasks.Dequeue();
                    }
                    Thread cancelTaskThread = new Thread(() => CancelTask(nextTask));
                    cancelTaskThread.Start();
                    lock(_lock)
                        _runningTasks[i] = nextTask;
                    nextTask.Execute();
                    nextTask.Terminated = true;
                    lock (_lock)
                    {
                        if (_scheduledTasks.Count > 0 && _scheduledTasks.Peek().State != MyTask.TaskState.Paused)
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