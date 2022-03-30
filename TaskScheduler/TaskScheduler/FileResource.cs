using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class FileResource : Resource
    {
        private readonly string _path;

        public FileResource(String path)
        {
            _path = path;
        }

        public String Path => _path;
    }
}
