using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Xml.Serialization;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TaskScheduler
{
    [Serializable]
    [XmlInclude(typeof(EdgeDetectionTask))]
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
        [NonSerialized]
        protected Action _action;
        protected readonly DateTime _deadline;
        protected readonly double _maxExecTime;
        protected int _maxDegreeOfParalellism;
        protected bool _terminated = false;
        protected readonly ControlToken? _controlToken;
        protected readonly TaskPriority _priority;
        //ReadWriteLock
        protected List<Resource>? _resources = null;

        protected List<bool>? _resourcesProcessed = null;

        public int MaxDegreeOfParalellism { get { return _maxDegreeOfParalellism; } set { _maxDegreeOfParalellism = value; } }

        public TaskState State { get { return _state; } set { _state = value; } }

        public bool Terminated { get { return _terminated; } set { _terminated = value; } }

        public ControlToken? ControlToken => _controlToken;

        public TaskPriority Priority => _priority;

        public DateTime Deadline => _deadline;

        public double MaxExecTime => _maxExecTime;

        [XmlIgnore]
        public Action Action { get { return _action; } protected set { _action = value; } }

        [XmlIgnore]
        public Action Action2 { get; set; }
        public double Progress { get; set; }

        public List<Resource>? Resources => _resources;

        public List<bool>? ResourcesProcessed => _resourcesProcessed;

        public MyTask() 
        {
            //_resources = new List<Resource>();
            //_resourcesProcessed = new List<bool>();
        }

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
            _resourcesProcessed = new List<bool>();
            foreach (Resource resource in _resources)
                _resourcesProcessed.Add(false);
        }

        public virtual void Execute()
        {
            Action();
        }

        public virtual void Serialize()
        {
            string fileName = "MyTask_" + DateTime.Now.Ticks + ".bin";
            //XmlSerializer serializer = new XmlSerializer(typeof(MyTask));
            //using StreamWriter writer = new StreamWriter(fileName);
            //serializer.Serialize(writer, this);
            //string jsonString = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            //File.WriteAllText(fileName, jsonString);
            IFormatter formatter = new BinaryFormatter();
            using Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, this);
        }

        public static MyTask? Deserialize(string fileName)
        {
            //XmlSerializer serializer = new XmlSerializer(typeof(MyTask));
            //using FileStream stream = new FileStream(fileName, FileMode.Open);
            //return (MyTask)serializer.Deserialize(stream);
            //string jsonString = File.ReadAllText(fileName);
            //return JsonSerializer.Deserialize<MyTask>(jsonString);
            IFormatter formatter = new BinaryFormatter();
            using Stream stream = new FileStream(fileName, FileMode.Open);
            return (MyTask)formatter.Deserialize(stream);
        }
    }
}
