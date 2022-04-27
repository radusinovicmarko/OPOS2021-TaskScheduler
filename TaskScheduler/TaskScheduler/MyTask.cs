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
using System.Xml.Schema;
using System.Xml;

namespace TaskScheduler
{
    [Serializable]
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
        [NonSerialized]
        protected Action? _progressBarUpdateAction;
        [NonSerialized]
        protected Action? _finishedTaskCallback;

        protected readonly string _id;
        protected readonly DateTime _deadline;
        [NonSerialized]
        protected DateTime dateTimeFinished;
        protected readonly double _maxExecTime;
        protected int _maxDegreeOfParalellism;
        protected bool _terminated = false;
        protected readonly ControlToken? _controlToken;
        protected readonly ControlToken? _userControlToken;
        protected TaskPriority _priority;

        protected double _progress = 0.0;

        //ReadWriteLock
        protected List<Resource>? _resources = null;

        protected List<bool>? _resourcesProcessed = null;

        public int MaxDegreeOfParalellism { get { return _maxDegreeOfParalellism; } set { _maxDegreeOfParalellism = value; } }

        public TaskState State { get { return _state; } set { _state = value; } }

        public bool Terminated { get { return _terminated; } set { _terminated = value; } }

        public ControlToken? ControlToken => _controlToken;

        public ControlToken? UserControlToken => _userControlToken;

        public TaskPriority Priority { get { return _priority; } set { _priority = value; } }

        public string Id => _id;
        public DateTime Deadline => _deadline;

        public double MaxExecTime => _maxExecTime;

        public DateTime DateTimeFinished { get { return dateTimeFinished; } set { dateTimeFinished = value; } }

        [XmlIgnore]
        [JsonIgnore]
        public Action Action { get { return _action; } protected set { _action = value; } }

        [XmlIgnore]
        [JsonIgnore]
        public Action? ProgressBarUpdateAction { get { return _progressBarUpdateAction; } set { _progressBarUpdateAction = value; } }

        [XmlIgnore]
        [JsonIgnore]
        public Action? FinishedTaskCallback { get { return _finishedTaskCallback; } set { _finishedTaskCallback = value; } }

        public double Progress { get { return _progress; } protected set { _progress = value; } }

        public List<Resource>? Resources => _resources;

        public List<bool>? ResourcesProcessed => _resourcesProcessed;

        public MyTask() 
        {
            //_resources = new List<Resource>();
            //_resourcesProcessed = new List<bool>();
        }

        public MyTask(Action action, string id, DateTime deadline, double maxExecTime, int maxDegreeOfParalellism = 1, ControlToken? token = null, ControlToken? userToken = null, TaskPriority priority = TaskPriority.Normal)
        {
            _id = id;
            _state = TaskState.Ready;
            _action = action;
            _deadline = deadline;
            _maxExecTime = maxExecTime;
            _maxDegreeOfParalellism = maxDegreeOfParalellism;
            _priority = priority;
            _controlToken = token;
            _userControlToken = userToken;
        }

        public MyTask(Action action, string id, DateTime deadline, double maxExecTime, int maxDegreeOfParalellism = 1, TaskPriority priority = TaskPriority.Normal)
        {
            _id = id;
            _state = TaskState.Ready;
            _action = action;
            _deadline = deadline;
            _maxExecTime = maxExecTime;
            _maxDegreeOfParalellism = maxDegreeOfParalellism;
            _priority = priority;
            _controlToken = null;
            _userControlToken = null;
        }

        public MyTask(Action action, string id, DateTime deadline, double maxExecTime, int maxDegreeOfParalellism, ControlToken? token, ControlToken? userToken, TaskPriority priority, params Resource[] resources)
        {
            _id = id;
            _state = TaskState.Ready;
            _action = action;
            _deadline = deadline;
            _maxExecTime = maxExecTime;
            _maxDegreeOfParalellism = maxDegreeOfParalellism;
            _priority = priority;
            _controlToken = token;
            _userControlToken = userToken;
            _resources = new List<Resource>();
            _resources.AddRange(resources.ToArray());
            _resourcesProcessed = new List<bool>();
            foreach (Resource resource in _resources)
                _resourcesProcessed.Add(false);
        }

        public virtual void Execute()
        {
            if (_resources != null)
                foreach (Resource r in _resources)
                    r.Lock(this);
            Action();
            dateTimeFinished = DateTime.Now;
            if (_resources != null)
                foreach (Resource r in _resources)
                    r.Unlock();
        }

        public virtual void Serialize(string folderPath)
        {
            string fileName = folderPath + Path.DirectorySeparatorChar + this.GetType().Name + "_" + DateTime.Now.Ticks + ".bin";

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

        /*public override string ToString()
        {
            return "[" +((FileResource) Resources[0]).Path +  "]";
        }*/
    }
}
