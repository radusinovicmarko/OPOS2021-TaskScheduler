using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TaskScheduler
{
    [Serializable]
    public class TaskScheduler
    {
        private readonly int _maxNumberOfCores;
        private readonly int _maxNumberOfConcurrentTasks;
        [NonSerialized]
        private bool _started = false;
        private readonly bool _preemptiveScheduling = true;
        private readonly bool _priorityScheduling = true;
        private int _coresTaken = 0;

        [NonSerialized]
        private PriorityQueue<MyTask, MyTask.TaskPriority> _scheduledTasks = new (new MyTaskComparer());

        [NonSerialized]
        private Dictionary<MyTask, int> _runningTasks = new();

        [NonSerialized]
        private Dictionary<MyTask, Stopwatch> _stopwatches = new();

        private readonly Dictionary<string, double> _elapsedTime = new();

        private readonly Dictionary<MyTask, List<Resource>> _resourcesTaken = new();

        private readonly HashSet<Resource> _resourcesTaken2 = new();

        private readonly object _lock = new();

        public int MaxNumberOfCores => _maxNumberOfCores;
        public int MaxNumberOfConcurrentTasks => _maxNumberOfConcurrentTasks;

        public bool PriorityScheduling => _priorityScheduling;

        public bool PreemptiveScheduling => _preemptiveScheduling;

        public TaskScheduler(int maxNumberOfCores, int maxNumberOfConcurrentTasks)
        {
            if (maxNumberOfConcurrentTasks < maxNumberOfCores)
                throw new ArgumentException("Maximum number of concurrent tasks must not be less than maximum number of cores.");
            _maxNumberOfCores = maxNumberOfCores;
            _maxNumberOfConcurrentTasks = maxNumberOfConcurrentTasks;
            Process.GetCurrentProcess().ProcessorAffinity = GetProcessorAffinity(maxNumberOfCores);
        }

        public TaskScheduler(int maxNumberOfCores, int maxNumberOfConcurrentTasks, bool preemptiveScheduling, bool priorityScheduling) 
            : this(maxNumberOfCores, maxNumberOfConcurrentTasks)
        {
            _preemptiveScheduling = preemptiveScheduling;
            _priorityScheduling = priorityScheduling;
        }

        private static IntPtr GetProcessorAffinity(int maxNumberOfCores)
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

        public void AddTask(MyTask myTask)
        {
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
                            foreach (MyTask task in _runningTasks.Keys)
                            {
                                if (task.Priority > lowestPriority)
                                {
                                    lowestPriority = task.Priority;
                                    lowestPriorityTask = task;
                                }
                            }
                            var pair = ResourcesFree(nextTask);
                            if (_preemptiveScheduling && lowestPriorityTask != null && lowestPriorityTask.Priority > nextTask.Priority && pair.Item1)// || !pair.Item1 && pair.Item2 == nextTask))
                            {
                                lowestPriorityTask.ControlToken?.Pause();
                                lowestPriorityTask.State = MyTask.TaskState.Paused;
                                _stopwatches.TryGetValue(lowestPriorityTask, out Stopwatch? watch);
                                watch?.Stop();
                                _runningTasks.TryGetValue(lowestPriorityTask, out int cores);
                                _coresTaken -= cores;
                                numberOfFreeCores = MaxNumberOfCores - _coresTaken;
                                _runningTasks.Remove(lowestPriorityTask);
                                _scheduledTasks.Enqueue(lowestPriorityTask, lowestPriorityTask.Priority);                            
                            }
                            // PIP
                            else if (_preemptiveScheduling && lowestPriorityTask != null && lowestPriorityTask.Priority > nextTask.Priority && !pair.Item1 && pair.Item2?.State == MyTask.TaskState.Paused) 
                            {
                                pair.Item2.Priority = nextTask.Priority;
                                var collection = _scheduledTasks.UnorderedItems;
                                PriorityQueue<MyTask, MyTask.TaskPriority> newQueue = new();
                                newQueue.Enqueue(pair.Item2, pair.Item2.Priority);
                                foreach (var (Element, Priority) in collection)
                                    if (Element != pair.Item2)
                                        newQueue.Enqueue(Element, Element.Priority);
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
                            {
                                _resourcesTaken.Add(nextTask, nextTask.Resources);
                                for (int i = 0; i < nextTask.Resources.Count; i++)
                                {
                                    if (_resourcesTaken2.Contains(nextTask.Resources[i]))
                                    {
                                        _resourcesTaken2.TryGetValue(nextTask.Resources[i], out Resource? res);
                                        nextTask.Resources[i] = res;
                                    }
                                    else
                                        _resourcesTaken2.Add(nextTask.Resources[i]);
                                }
                            }
                            new Thread(() =>
                            {
                                nextTask.Execute();
                                nextTask.Terminated = true;
                                lock (_lock)
                                {
                                    _resourcesTaken.Remove(nextTask);
                                    _runningTasks.Remove(nextTask);
                                    _coresTaken -= coresToBeTaken;
                                    if (_scheduledTasks.Count > 0 && _scheduledTasks.Peek().State != MyTask.TaskState.Paused)
                                        _scheduledTasks.Peek().State = MyTask.TaskState.Ready;
                                    Monitor.PulseAll(_lock);
                                }
                            })
                            { IsBackground = true }.Start();
                        }
                        else
                            continue;
                        _runningTasks.Add(nextTask, coresToBeTaken);
                    }
                }
            })
            { IsBackground = true }.Start();
            new Thread(CancelTasks) { IsBackground = true }.Start();
        }

        private (bool, MyTask?) ResourcesFree(MyTask nextTask)
        {
            if (nextTask.Resources != null)
                foreach (var resource in nextTask.Resources)
                    if (_resourcesTaken2.Contains(resource))
                    {
                        _resourcesTaken2.TryGetValue(resource, out Resource r);
                        if (r.Locked && r.Task != nextTask)
                            return (false, r.Task);
                    }        
            return (true, null);
        }

        public void Serialize(string folderPath)
        {
            string fileName = folderPath + Path.DirectorySeparatorChar + this.GetType().Name + "_" + DateTime.Now.Ticks + ".bin";

            foreach (var task in _runningTasks.Keys)
                task.Serialize(folderPath + Path.DirectorySeparatorChar + "task saves");

            foreach (var (Element, _) in _scheduledTasks.UnorderedItems)
                Element.Serialize(folderPath + Path.DirectorySeparatorChar + "task saves");

            _elapsedTime.Clear();
            foreach (var task in _stopwatches.Keys)
                _elapsedTime.Add(task.Id, (long)_stopwatches[task].Elapsed.TotalSeconds);

            IFormatter formatter = new BinaryFormatter();
            using Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, this);
        }

        public static TaskScheduler Deserialize(string fileName)
        {
            IFormatter formatter = new BinaryFormatter();
            using Stream stream = new FileStream(fileName, FileMode.Open);
            TaskScheduler scheduler = (TaskScheduler)formatter.Deserialize(stream);

            Process.GetCurrentProcess().ProcessorAffinity = GetProcessorAffinity(scheduler._maxNumberOfCores);

            scheduler._started = false;
            scheduler._coresTaken = 0;
            scheduler._runningTasks = new();
            scheduler._stopwatches = new();
            scheduler._resourcesTaken2.Clear();
            scheduler._scheduledTasks = new(new MyTaskComparer());

            return scheduler;
        }

        private void CancelTasks()
        {
            while (true)
            {
                foreach (MyTask task in _runningTasks.Keys)
                {
                    if (task.ControlToken != null)
                    {
                        double previousElapsedTime = 0;
                        if (_elapsedTime.ContainsKey(task.Id))
                            previousElapsedTime = _elapsedTime[task.Id];
                        _stopwatches.TryGetValue(task, out Stopwatch? stopwatch);
                        if (stopwatch?.Elapsed.TotalSeconds + previousElapsedTime >= task.MaxExecTime || DateTime.Now >= task.Deadline)
                            task.ControlToken?.Terminate();
                    }
                }
                Thread.Sleep(100);
            }
        }
    }
}