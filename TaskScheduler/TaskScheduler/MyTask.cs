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

        private TaskState _state;
        private readonly Action _action;
        private readonly DateTime _deadline;
        private readonly double _maxExecTime;
        private int _maxDegreeOfParalellism;
        private bool _terminated = false;
        private readonly ControlToken? _controlToken;
        private readonly TaskPriority _priority;

        public string name;

        public int MaxDegreeOfParalellism => _maxDegreeOfParalellism;

        public TaskState State { get { return _state; } set { _state = value; } }

        public bool Terminated { get { return _terminated; } set { _terminated = value; } }

        public ControlToken? ControlToken => _controlToken;

        public TaskPriority Priority => _priority;

        public DateTime Deadline => _deadline;

        public double MaxExecTime => _maxExecTime;

        public Action Action => _action;

        public MyTask(string name, Action action, DateTime deadline, double maxExecTime, int maxDegreeOfParalellism = 1, ControlToken? token = null, TaskPriority priority = TaskPriority.Normal)
        {
            this.name = name;
            _state = TaskState.Ready;
            _action = action;
            _deadline = deadline;
            _maxExecTime = maxExecTime;
            _maxDegreeOfParalellism = maxDegreeOfParalellism;
            _priority = priority;
            _controlToken = token;
        }

        public MyTask(string name, Action action, DateTime deadline, double maxExecTime, int maxDegreeOfParalellism = 1, TaskPriority priority = TaskPriority.Normal)
        {
            this.name = name;
            _state = TaskState.Ready;
            _action = action;
            _deadline = deadline;
            _maxExecTime = maxExecTime;
            _maxDegreeOfParalellism = maxDegreeOfParalellism;
            _priority = priority;
            _controlToken = null;
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
