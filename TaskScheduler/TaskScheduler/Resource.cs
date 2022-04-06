using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    [Serializable]
    public class Resource
    {
        protected bool _locked = false;

        protected MyTask? task;

        public MyTask Task => task;

        public Resource() { }

        public void Lock(MyTask task)
        {
            _locked = true;
            this.task = task;
        }

        public void Unlock()
        {
            Locked = false;
            this.task = null;
        }

        public bool Locked { get { return _locked; } private set { _locked = value; } }

        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
