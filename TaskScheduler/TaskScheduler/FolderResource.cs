using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{

    [Serializable]
    public class FolderResource : Resource
    {
        private readonly string _path;

        public FolderResource(String path)
        {
            _path = path;
        }

        public String Path => _path;

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj is not FolderResource)
                return false;
            return ((FolderResource)obj)._path == _path;
        }

        public override int GetHashCode()
        {
            return _path.GetHashCode();
        }
    }
}
