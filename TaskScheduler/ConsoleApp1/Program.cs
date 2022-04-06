// See https://aka.ms/new-console-template for more information


using TaskScheduler;

//var task2 = EdgeDetectionTask.Deserialize("EdgeDetectionTask_637842439470068271.bin");
//foreach (var v in task2.ResourcesProcessed) Console.WriteLine(v);
//return;

TaskScheduler.TaskScheduler scheduler = new TaskScheduler.TaskScheduler(1, 10);
new Thread(() =>
{
    while (true)
    {
        Console.WriteLine("***********    " + scheduler._coresTaken);
        Thread.Sleep(1000);
    }
});//.Start();
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
/*scheduler.AddTask(new TaskScheduler.MyTask(() =>
 {
     for (int i = 0; i < 20; i++)
     {
         if (token2.Terminated)
             return;
         if (token2.Paused)
             lock (token2.Lock)
                 Monitor.Wait(token2.Lock);
         System.Console.WriteLine($"T2 {i}");
         Thread.Sleep(500);
     }
 }, new DateTime(2023, 2, 22, 0, 0, 0), 50, 3, token2, MyTask.TaskPriority.High));*/
scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 20000, 1, token2, userToken2, MyTask.TaskPriority.Normal, new FileResource("C:\\Users\\User20\\Desktop\\test2.jfif")/*, new Resource("C:\\Users\\User20\\Desktop\\test2.jfif"), new Resource("C:\\Users\\User20\\Desktop\\test3.png")*/));
//MyTask task = new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 20000, 1, token2, MyTask.TaskPriority.High, new FileResource("C:\\Users\\User20\\Desktop\\test2.jfif"), new FileResource("C:\\Users\\User20\\Desktop\\test2.jfif"), new FileResource("C:\\Users\\User20\\Desktop\\test3.png"));
//scheduler.AddTask(task);
scheduler.Start();
Thread.Sleep(4000);
//task.Serialize();
//token2.Pause();
ControlToken? token3 = new();
ControlToken? userToken3 = new();
/*scheduler.AddTask(new TaskScheduler.MyTask(() =>
{
    for (int i = 0; i < 10; i++)
    {
        if (token3.Terminated)
            return;
        if (token3.Paused)
            lock (token3.Lock)
                Monitor.Wait(token3.Lock);
        System.Console.WriteLine($"T3 {i}");
        Thread.Sleep(1500);
    }
}, new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, token3, MyTask.TaskPriority.High));*/
scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, token3, userToken3, MyTask.TaskPriority.High, new FileResource("C:\\Users\\User20\\Desktop\\test.jpg"), new FileResource("C:\\Users\\User20\\Desktop\\test2.jfif")/*, new Resource("C:\\Users\\User20\\Desktop\\test3.png")*/));
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
/*ControlToken? token5 = new();
scheduler.AddTask(new TaskScheduler.MyTask(() =>
{
    for (int i = 0; i < 10; i++)
    {
        if (token5.Terminated)
            return;
        if (token5.Paused)
            lock (token5.Lock)
                Monitor.Wait(token3.Lock);
        System.Console.WriteLine($"T5 {i}");
        Thread.Sleep(500);
    }
}, new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, token5, MyTask.TaskPriority.Low));*/
scheduler.Start();
Thread.Sleep(10000);
//token2.Resume();
