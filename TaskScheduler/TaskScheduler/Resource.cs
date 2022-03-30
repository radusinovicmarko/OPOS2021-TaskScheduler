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
        protected readonly object _lock = new();

        public Resource() { }

        public object Lock => _lock;

        public bool Locked { get { return _locked; } set { _locked = value; } }
    }
}
