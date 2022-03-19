using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace TaskScheduler
{
    public class MyTask
    {

        public enum TaskState
        {
            Created,
            Ready,
            Running,
            Paused
        }

        public enum TaskPriority
        {
            Low = 5,
            BelowNormal = 4,
            Normal = 3,
            AboveNormal = 2,
            High = 1
        }

        protected TaskState _state;
        protected Action _action;
        protected readonly DateTime _deadline;
        protected readonly double _maxExecTime;
        protected int _maxDegreeOfParalellism;
        protected bool _terminated = false;
        protected readonly ControlToken? _controlToken;
        protected readonly TaskPriority _priority;
        //ReadWriteLock
        protected List<Resource>? _resources = null;

        public int MaxDegreeOfParalellism { get { return _maxDegreeOfParalellism; } set { _maxDegreeOfParalellism = value; } }

        public TaskState State { get { return _state; } set { _state = value; } }

        public bool Terminated { get { return _terminated; } set { _terminated = value; } }

        public ControlToken? ControlToken => _controlToken;

        public TaskPriority Priority => _priority;

        public DateTime Deadline => _deadline;

        public double MaxExecTime => _maxExecTime;

        public Action Action { get { return _action; } protected set { _action = value; } }

        public Action Action2 { get; set; }
        public double Progress { get; set; }

        public MyTask(Action action, DateTime deadline, double maxExecTime, int maxDegreeOfParalellism = 1, ControlToken? token = null, TaskPriority priority = TaskPriority.Normal)
        {
            _state = TaskState.Ready;
            _action = action;
            _deadline = deadline;
            _maxExecTime = maxExecTime;
            _maxDegreeOfParalellism = maxDegreeOfParalellism;
            _priority = priority;
            _controlToken = token;
        }

        public MyTask(Action action, DateTime deadline, double maxExecTime, int maxDegreeOfParalellism = 1, TaskPriority priority = TaskPriority.Normal)
        {
            _state = TaskState.Ready;
            _action = action;
            _deadline = deadline;
            _maxExecTime = maxExecTime;
            _maxDegreeOfParalellism = maxDegreeOfParalellism;
            _priority = priority;
            _controlToken = null;
        }

        public MyTask(Action action, DateTime deadline, double maxExecTime, int maxDegreeOfParalellism, ControlToken? token, TaskPriority priority, params Resource[] resources)
        {
            _state = TaskState.Ready;
            _action = action;
            _deadline = deadline;
            _maxExecTime = maxExecTime;
            _maxDegreeOfParalellism = maxDegreeOfParalellism;
            _priority = priority;
            _controlToken = token;
            _resources = new List<Resource>();
            _resources.AddRange(resources.ToArray());
        }

        public void Execute()
        {
            Action();
        }

        public void Serialize(string fileName)
        {
            string jsonString = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fileName, jsonString);
        }

        public static MyTask? Deserialize(string fileName)
        {
            string jsonString = File.ReadAllText(fileName);
            return JsonSerializer.Deserialize<MyTask>(jsonString);
        }
    }
}
