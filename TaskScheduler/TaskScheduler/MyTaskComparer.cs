using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class MyTaskComparer : IComparer<MyTask.TaskPriority>
    {
        public int Compare(MyTask.TaskPriority x, MyTask.TaskPriority y)
        {
            return x - y;
        }
    }
}
