using System.Diagnostics;

namespace TaskScheduler
{
    public class TaskScheduler
    {
        private readonly int _maxNumberOfCores;
        private readonly int _maxNumberOfConcurrentTasks;
        private bool _started = false;

        public int _coresTaken = 0;

        private PriorityQueue<MyTask, MyTask.TaskPriority> _scheduledTasks = new (new MyTaskComparer());

        private List<MyTask?> _runningTasks;

        private Dictionary<MyTask, int> _runningTasks2 = new Dictionary<MyTask, int>();

        private Dictionary<MyTask, Stopwatch> _stopwatches = new Dictionary<MyTask, Stopwatch>();

        private Dictionary<MyTask, List<Resource>> _resourcesTaken = new Dictionary<MyTask, List<Resource>>();

        private List<int> _coresTakenByRunningTasks = new List<int>();

        private object _lock = new object();

        public int MaxNumberOfCores => _maxNumberOfCores;
        public int MaxNumberOfConcurrentTasks => _maxNumberOfConcurrentTasks;

        public TaskScheduler(int maxNumberOfCores, int maxNumberOfConcurrentTasks)
        {
            _maxNumberOfCores = maxNumberOfCores;
            _maxNumberOfConcurrentTasks = maxNumberOfConcurrentTasks;
            _runningTasks = new List<MyTask?>(maxNumberOfCores);
            for (int i = 0; i < maxNumberOfCores; i++)
            {
                _runningTasks.Add(null);
                _coresTakenByRunningTasks.Add(0);
            }
            Process.GetCurrentProcess().ProcessorAffinity = getProcessorAffinity(maxNumberOfCores);
        }

        private IntPtr getProcessorAffinity(int maxNumberOfCores)
        {
            int affinity = 0, i = 1;
            while (maxNumberOfCores > 0)
            {
                affinity |= i;
                i *= 2;
                maxNumberOfCores--;
            }
            return (IntPtr)affinity;
        }

        //Adding tasks when maxNoOfConc. > maxNoOfCores
        //State = Ready when adding task of higher priority than one in the queue

        public void AddTask(MyTask myTask)
        {
            int maxNumberOfTasks = Math.Min(_maxNumberOfConcurrentTasks, _maxNumberOfCores);
            lock (_lock)
            {
                _scheduledTasks.Enqueue(myTask, myTask.Priority);
                Monitor.PulseAll(_lock);
            }
        }

        public void Start()
        {
            if (_started)
                return;
            _started = true;
            new Thread(() =>
            {
                while (true)
                {
                    lock (_lock)
                    {
                        if (_scheduledTasks.Count == 0)
                        {
                            Monitor.Wait(_lock);
                            continue;
                        }
                        int coresToBeTaken = 0;
                        MyTask nextTask = _scheduledTasks.Peek();
                        int numberOfFreeCores = MaxNumberOfCores - _coresTaken;
                        if (numberOfFreeCores == 0)
                        {
                            MyTask.TaskPriority lowestPriority = MyTask.TaskPriority.High;
                            MyTask lowestPriorityTask = null;
                            foreach (MyTask task in _runningTasks2.Keys)
                            {
                                if (task.Priority > lowestPriority)
                                {
                                    lowestPriority = task.Priority;
                                    lowestPriorityTask = task;
                                }
                            }
                            var pair = ResourcesFree(nextTask);
                            if (lowestPriorityTask != null && lowestPriorityTask.Priority > nextTask.Priority && pair.Item1)
                            {
                                lowestPriorityTask.ControlToken?.Pause();
                                lowestPriorityTask.State = MyTask.TaskState.Paused;
                                _stopwatches.TryGetValue(lowestPriorityTask, out Stopwatch? watch);
                                watch?.Stop();
                                _runningTasks2.TryGetValue(lowestPriorityTask, out int cores);
                                _coresTaken -= cores;
                                numberOfFreeCores = MaxNumberOfCores - _coresTaken;
                                _runningTasks2.Remove(lowestPriorityTask);
                                _scheduledTasks.Enqueue(lowestPriorityTask, lowestPriorityTask.Priority);                            
                            }
                            // PIP
                            else if (lowestPriorityTask != null && lowestPriorityTask.Priority > nextTask.Priority && !pair.Item1 && pair.Item2?.State == MyTask.TaskState.Paused) 
                            {

                                //_scheduledTasks.Dequeue();
                                pair.Item2.Priority = nextTask.Priority;
                                var collection = _scheduledTasks.UnorderedItems;
                                PriorityQueue<MyTask, MyTask.TaskPriority> newQueue = new();
                                newQueue.Enqueue(pair.Item2, pair.Item2.Priority);
                                foreach (var item in collection)
                                    if (item.Element != pair.Item2)
                                        newQueue.Enqueue(item.Element, item.Element.Priority);
                                /*while (_scheduledTasks.Count > 0)
                                {
                                    var item = _scheduledTasks.Dequeue();
                                    if (item != pair.Item2)
                                        _scheduledTasks.Enqueue(item, item.Priority);
                                }*/
                                //_scheduledTasks.Enqueue(nextTask, nextTask.Priority);
                                _scheduledTasks = newQueue;
                                continue;
                            }
                            else
                            {
                                Monitor.Wait(_lock);
                                continue;
                            }
                        }
                        if (!ResourcesFree(nextTask).Item1)
                        {
                            Monitor.Wait(_lock);
                            continue;
                        }
                        if (nextTask.MaxDegreeOfParalellism > numberOfFreeCores)
                            coresToBeTaken = numberOfFreeCores;
                        else
                            coresToBeTaken = nextTask.MaxDegreeOfParalellism;
                        if (nextTask.State == MyTask.TaskState.Paused)
                        {
                            _coresTaken += coresToBeTaken;
                            nextTask.ControlToken?.Resume();
                            _stopwatches.TryGetValue(nextTask, out Stopwatch? watch);
                            watch?.Start();
                            _scheduledTasks.Dequeue();
                            continue;
                        }
                        else if (nextTask.State == MyTask.TaskState.Ready)
                        {
                            _coresTaken += coresToBeTaken;
                            _scheduledTasks.Dequeue();
                            _stopwatches.Add(nextTask, Stopwatch.StartNew());
                            if (nextTask.Resources != null)
                                _resourcesTaken.Add(nextTask, nextTask.Resources);
                            // ThreadPool ? 
                            new Thread(() =>
                            {
                                nextTask.Execute();
                                nextTask.Terminated = true;
                                lock (_lock)
                                {
                                    _resourcesTaken.Remove(nextTask);
                                    _runningTasks2.Remove(nextTask);
                                    _coresTaken -= coresToBeTaken;
                                    if (_scheduledTasks.Count > 0 && _scheduledTasks.Peek().State != MyTask.TaskState.Paused)
                                        _scheduledTasks.Peek().State = MyTask.TaskState.Ready;
                                    Monitor.PulseAll(_lock);
                                }
                            })
                            { IsBackground = true }.Start();
                        }
                        //Potrebno?
                        else
                            continue;
                        _runningTasks2.Add(nextTask, coresToBeTaken);
                    }
                }
            })
            { IsBackground = true }.Start();
            new Thread(CancelTasks) { IsBackground = true }.Start();
        }

        private (bool, MyTask?) ResourcesFree(MyTask nextTask)
        {
            if (nextTask.Resources != null)
                foreach (Resource nextTaskResource in nextTask.Resources)
                {
                    foreach (MyTask task in _resourcesTaken.Keys)
                    {
                        if (task != nextTask)
                            foreach (Resource r in task.Resources)
                                if (nextTaskResource.Equals(r))
                                    return (false, task);
                    }
                }
            return (true, null);
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

        private void CancelTasks()
        {
            while (true)
            {
                foreach (MyTask task in _runningTasks2.Keys)
                {
                    if (task.ControlToken != null)
                    {
                        _stopwatches.TryGetValue(task, out Stopwatch? stopwatch);
                        if (stopwatch?.Elapsed.TotalSeconds >= task.MaxExecTime || DateTime.Now >= task.Deadline)
                            task.ControlToken?.Terminate();
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private void ThreadCoreExecution(int i)
        {
            while (true)
            {
                if (_scheduledTasks.Count > 0)
                {
                    MyTask nextTask;
                    int coresToBeTaken = 0;
                    lock (_lock)
                    {
                        if (_scheduledTasks.Count <= 0)
                            continue;
                        nextTask = _scheduledTasks.Peek();
                        int numberOfFreeCores = MaxNumberOfCores - _coresTaken;
                        if (numberOfFreeCores == 0)
                            Monitor.Wait(_lock);
                        if (nextTask.MaxDegreeOfParalellism > numberOfFreeCores)
                            coresToBeTaken = numberOfFreeCores;
                        else
                            coresToBeTaken = nextTask.MaxDegreeOfParalellism;
                        if (nextTask.State == MyTask.TaskState.Paused)
                        {
                            _coresTaken += coresToBeTaken;
                            nextTask.ControlToken?.Resume();
                            _scheduledTasks.Dequeue();
                            return;
                        }
                        else if (nextTask.State == MyTask.TaskState.Ready)
                        {
                            _coresTaken += coresToBeTaken;
                            _scheduledTasks.Dequeue();
                        }
                        else
                            continue;
                    }
                    Thread cancelTaskThread = new Thread(() => CancelTask(nextTask));
                    cancelTaskThread.Start();
                    lock (_lock)
                    {
                        _runningTasks[i] = nextTask;
                        _coresTakenByRunningTasks[i] = coresToBeTaken;
                    }
                    nextTask.Execute();
                    nextTask.Terminated = true;
                    lock (_lock)
                    {
                        _coresTaken -= coresToBeTaken;
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