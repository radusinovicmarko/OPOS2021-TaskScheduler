using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace TaskScheduler
{
    [Serializable]
    public class ControlToken
    {
        private bool _paused = false;
        private bool _terminated = false;
        private readonly object _lock = new();

        public bool Paused { get { return _paused; } private set { _paused = value; } }

        public bool Terminated { get { return _terminated; } private set { _terminated = value; } }

        public object Lock => _lock;

        public void Terminate() { _terminated = true; }

        public void Pause()
        {
            _paused = true;
            Console.WriteLine("paused");
        }

        public void Resume()
        {
            _paused = false;
            Console.WriteLine("resumed");
            lock (_lock)
                Monitor.PulseAll(_lock);
        }
    }
}
