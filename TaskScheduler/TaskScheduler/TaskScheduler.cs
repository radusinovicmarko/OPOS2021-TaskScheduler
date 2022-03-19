using System.Diagnostics;

namespace TaskScheduler
{
    public class TaskScheduler
    {
        private readonly int _maxNumberOfCores;
        private readonly int _maxNumberOfConcurrentTasks;
        private bool _started = false;

        //private int _coresTaken = 0;

        private PriorityQueue<MyTask, MyTask.TaskPriority> _scheduledTasks = new (new MyTaskComparer());

        private List<MyTask> _runningTasks;

        private object _lock = new object();

        public int MaxNumberOfCores => _maxNumberOfCores;
        public int MaxNumberOfConcurrentTasks => _maxNumberOfConcurrentTasks;

        public TaskScheduler(int maxNumberOfCores, int maxNumberOfConcurrentTasks)
        {
            _maxNumberOfCores = maxNumberOfCores;
            _maxNumberOfConcurrentTasks = maxNumberOfConcurrentTasks;
            _runningTasks = new List<MyTask>(maxNumberOfCores);
            for (int i = 0; i < maxNumberOfCores; i++)
                _runningTasks.Add(null);
            //Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)0xF;
        }

        //Adding tasks when maxNoOfConc. > maxNoOfCores
        //State = Ready when adding task of higher priority then one in the queue

        public void AddTask(MyTask myTask)
        {
            int maxNumberOfTasks = Math.Min(_maxNumberOfConcurrentTasks, _maxNumberOfCores);
            lock (_lock)
            {
                if (_runningTasks.Count(task => task != null) >= maxNumberOfTasks)
                {
                    MyTask? minPriorityTask = _runningTasks.Find(task => task != null);
                    int index = 0;
                    for (int i = 1; i < _runningTasks.Count; i++)
                        if (_runningTasks.ElementAt(i) != null && _runningTasks.ElementAt(i).Priority > minPriorityTask.Priority)
                        {
                            minPriorityTask = _runningTasks.ElementAt(i);
                            index = i;
                        }
                    if (minPriorityTask?.Priority > myTask.Priority)
                    {
                        minPriorityTask.ControlToken?.Pause();
                        minPriorityTask.State = MyTask.TaskState.Paused;
                        _scheduledTasks.Enqueue(minPriorityTask, minPriorityTask.Priority);
                        Thread t = new Thread(() => ThreadCoreExecution(index));
                        myTask.State = MyTask.TaskState.Ready;
                        _scheduledTasks.Enqueue(myTask, myTask.Priority);
                        t.Start();
                        return;
                    }
                }
                //if (_scheduledTasks.Count < maxNumberOfTasks)
                  //  myTask.State = MyTask.TaskState.Ready;
                /*int counter = 0;
                foreach (var task in _scheduledTasks.UnorderedItems)
                {
                    if (counter < maxNumberOfTasks)
                        task.Element.State = MyTask.TaskState.Ready;
                    else
                        task.Element.State = MyTask.TaskState.Created;
                    counter++;
                }*/
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
                t.IsBackground = true;
                t.Start();
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
                        else if (nextTask.State == MyTask.TaskState.Ready)
                            _scheduledTasks.Dequeue();
                        else
                            continue;
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