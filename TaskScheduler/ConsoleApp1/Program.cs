// See https://aka.ms/new-console-template for more information


using TaskScheduler;

//var task2 = EdgeDetectionTask.Deserialize("EdgeDetectionTask_637842439470068271.bin");
//foreach (var v in task2.ResourcesProcessed) Console.WriteLine(v);


/*MyTask t = new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 20000, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, new FileResource("C:\\Users\\User20\\Desktop\\test2.jfif"));

t.Serialize();
Console.WriteLine(Type.GetType(t.GetType().AssemblyQualifiedName).GetMethod("Deserialize"));
Type.GetType(t.GetType().AssemblyQualifiedName).GetMethod("Deserialize").Invoke(null, new object[]{""});

return;*/

TaskScheduler.TaskScheduler scheduler = new TaskScheduler.TaskScheduler(1, 10);

ControlToken? token1 = new();
ControlToken? userToken1 = new();
scheduler.AddTask(new TaskScheduler.MyTask(() =>
{
    for (int i = 0; i < 20; i++)
    {
        if (token1.Terminated)
            return;
        if (token1.Paused)
            lock (token1.Lock)
            {
                global::System.Console.WriteLine("AAAA");
                Monitor.Wait(token1.Lock);
            }
        System.Console.WriteLine($"T1 {i}");
        Thread.Sleep(2000);
    }
}, new DateTime(2023, 3, 22, 23, 48, 40), 200, 1, token1, userToken1, MyTask.TaskPriority.Low));

ControlToken? token2 = new();
ControlToken? userToken2 = new();
scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 20000, 1, token2, userToken2, MyTask.TaskPriority.Normal, new FolderResource("."), new FileResource("C:\\Users\\User20\\Desktop\\test2.jfif")));

//MyTask task = new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 20000, 1, token2, userToken2, MyTask.TaskPriority.High, new FolderResource("C:\\Users\\User20\\Desktop\\"), new FileResource("C:\\Users\\User20\\Desktop\\test.jpg"), new FileResource("C:\\Users\\User20\\Desktop\\test2.jfif"), new FileResource("C:\\Users\\User20\\Desktop\\test3.png"), new FileResource("C:\\Users\\User20\\Desktop\\test4.jpg"), new FileResource("C:\\Users\\User20\\Desktop\\test5.jpg"));
MyTask task = (MyTask) Type.GetType("TaskScheduler.EdgeDetectionTask, TaskScheduler, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null").GetMethod("Deserialize").Invoke(null, new object[] { "TaskScheduler.EdgeDetectionTask, TaskScheduler, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null_637863263498237069.bin" });
scheduler.AddTask(task);

//foreach (var r in task.ResourcesProcessed)
    //Console.WriteLine(r);

//return;

scheduler.Start();

Thread.Sleep(15000);

Console.WriteLine("Serialize");
//task.Serialize();

//return;
//token2.Pause();

ControlToken? token3 = new();
ControlToken? userToken3 = new();
scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, token3, userToken3, MyTask.TaskPriority.High, new FolderResource("."), new FileResource("C:\\Users\\User20\\Desktop\\test.jpg"), new FileResource("C:\\Users\\User20\\Desktop\\test2.jfif")));

Console.WriteLine("Hello, World!");

ControlToken? token4 = new();
ControlToken? userToken4 = new();
scheduler.AddTask(new TaskScheduler.MyTask(() =>
{
    for (int i = 0; i < 10; i++)
    {
        if (token4.Terminated)
            return;
        if (token4.Paused)
            lock (token4.Lock)
                Monitor.Wait(token3.Lock);
        System.Console.WriteLine($"T4 {i}");
        Thread.Sleep(2500);
    }
}, new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, token4, userToken4, MyTask.TaskPriority.Low));

Thread.Sleep(Timeout.Infinite);