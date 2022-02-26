using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class ControlToken
    {
        private bool _paused = false;
        private bool _terminated = false;
        private readonly object _lock = new object();

        public bool Paused { get { return _paused; } private set { _paused = value; } }

        public bool Terminated { get { return _terminated; } private set { _terminated = value; } }

        public object Lock => _lock;

        public void Terminate() { _terminated = true; }

        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
            lock (_lock)
                Monitor.PulseAll(_lock);
        }

    }
}
