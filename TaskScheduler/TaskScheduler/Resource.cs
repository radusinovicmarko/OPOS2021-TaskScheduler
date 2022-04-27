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
    public abstract class Resource
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

        public abstract Stream GetData();

        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /*public XmlSchema? GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            
        }

        public void WriteXml(XmlWriter writer)
        {
            task?.WriteXml(writer);
        }*/
    }
}
