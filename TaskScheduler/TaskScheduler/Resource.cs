using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class Resource
    {
        private readonly string _path;
        private bool _locked = false;
        private readonly object _lock = new();

        public Resource(string path)
        {
            _path = path;
        }

        public String Path => _path;

        public object Lock => _lock;

        public bool Locked { get { return _locked; } set { _locked = value; } }
    }
}
