using System.Diagnostics;

namespace TaskScheduler
{

    //TODO
    //Prevencija -> promjena coresTaken, vodjenje racuna o broju zauzetih jezgara za svaki zadatak

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
        //State = Ready when adding task of higher priority then one in the queue

        public void AddTask(MyTask myTask)
        {
            int maxNumberOfTasks = Math.Min(_maxNumberOfConcurrentTasks, _maxNumberOfCores);
            lock (_lock)
            {
                //if (_runningTasks.Count(task => task != null) >= maxNumberOfTasks)
                /*if (_coresTaken == MaxNumberOfCores)
                {
                    int index = 0;
                    MyTask? minPriorityTask = null;
                    for (; index < _runningTasks.Count; index++)
                        if (_runningTasks[index] != null)
                        {
                            minPriorityTask = _runningTasks[index];
                            break;
                        }
                    for (int i = index + 1; i < _runningTasks.Count; i++)
                        if (_runningTasks.ElementAt(i) != null && _runningTasks.ElementAt(i).Priority > minPriorityTask?.Priority)
                        {
                            minPriorityTask = _runningTasks.ElementAt(i);
                            index = i;
                        }
                    if (minPriorityTask?.Priority > myTask.Priority)
                    {
                        minPriorityTask.ControlToken?.Pause();
                        minPriorityTask.State = MyTask.TaskState.Paused;
                        _coresTaken -= _coresTakenByRunningTasks[index];
                        _scheduledTasks.Enqueue(minPriorityTask, minPriorityTask.Priority);
                        Thread t = new Thread(() => ThreadCoreExecution(index));
                        myTask.State = MyTask.TaskState.Ready;
                        _scheduledTasks.Enqueue(myTask, myTask.Priority);
                        t.Start();
                        Monitor.PulseAll(_lock);
                        return;
                    }
                }*/
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
            /*for (int i = 0; i < _maxNumberOfCores; i++)
            {
                int iCopy = i;
                Thread t = new Thread(() => ThreadCoreExecution(iCopy));
                t.IsBackground = true;
                t.Start();
            }*/
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
                            if (lowestPriorityTask != null && lowestPriorityTask.Priority > nextTask.Priority)
                            {
                                //_scheduledTasks.Dequeue();
                                lowestPriorityTask.ControlToken?.Pause();
                                lowestPriorityTask.State = MyTask.TaskState.Paused;
                                _stopwatches.TryGetValue(lowestPriorityTask, out Stopwatch? watch);
                                watch?.Stop();
                                _runningTasks2.TryGetValue(lowestPriorityTask, out int cores);
                                _coresTaken -= cores;
                                numberOfFreeCores = MaxNumberOfCores - _coresTaken;
                                _runningTasks2.Remove(lowestPriorityTask);
                                _scheduledTasks.Enqueue(lowestPriorityTask, lowestPriorityTask.Priority);
                                /*if (nextTask.MaxDegreeOfParalellism > numberOfFreeCores)
                                    coresToBeTaken = numberOfFreeCores;
                                else
                                    coresToBeTaken = nextTask.MaxDegreeOfParalellism;
                                _coresTaken += coresToBeTaken;
                                //MyTask taskCopy = nextTask;
                                new Thread(() =>
                                {
                                    nextTask.Execute();
                                    nextTask.Terminated = true;
                                    lock (_lock)
                                    {
                                        _coresTaken -= coresToBeTaken;
                                        if (_scheduledTasks.Count > 0 && _scheduledTasks.Peek().State != MyTask.TaskState.Paused)
                                            _scheduledTasks.Peek().State = MyTask.TaskState.Ready;
                                        Monitor.PulseAll(_lock);
                                    }
                                })
                                { IsBackground = true }.Start();
                                _runningTasks2.Add(nextTask, coresToBeTaken);
                                continue;*/                            
                            }
                            else
                            {
                                Monitor.Wait(_lock);
                                continue;
                            }
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
                            //MyTask taskCopy = nextTask;
                            new Thread(() =>
                            {
                                nextTask.Execute();
                                nextTask.Terminated = true;
                                lock (_lock)
                                {
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
                        //Thread cancelTaskThread = new Thread(() => CancelTask(nextTask));
                        //cancelTaskThread.Start();
                        /*lock (_lock)
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
                        }*/
                    }
                }
            })
            { IsBackground = true }.Start();
            new Thread(CancelTasks) { IsBackground = true }.Start();
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