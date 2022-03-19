// See https://aka.ms/new-console-template for more information


using TaskScheduler;

TaskScheduler.TaskScheduler scheduler = new TaskScheduler.TaskScheduler(2, 10);
CancellationTokenSource cts = new CancellationTokenSource();
ControlToken? token1 = new();
scheduler.AddTask(new TaskScheduler.MyTask(() =>
{
    for (int i = 0; i < 20; i++)
    {
        if (token1.Terminated)
            return;
        if (token1.Paused)
            lock (token1.Lock)
                Monitor.Wait(token1.Lock);
        System.Console.WriteLine($"T1 {i}");
        Thread.Sleep(2000);
    }
}, new DateTime(2023, 2, 21, 22, 37, 0), 200, 1, token1, MyTask.TaskPriority.AboveNormal));
cts = new CancellationTokenSource();
ControlToken? token2 = new();
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
 }, new DateTime(2023, 2, 22, 0, 0, 0), 50, 1, token2, MyTask.TaskPriority.AboveNormal));*/
scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 20000, 3, token2, MyTask.TaskPriority.High, new Resource("C:\\Users\\User20\\Desktop\\test2.jfif"), new Resource("C:\\Users\\User20\\Desktop\\test2.jfif"), new Resource("C:\\Users\\User20\\Desktop\\test3.png")));
scheduler.Start();
Thread.Sleep(3000);
//token2.Pause();
cts = new CancellationTokenSource();
ControlToken? token3 = new();
/*scheduler.AddTask(new TaskScheduler.MyTask(() =>
{
    for (int i = 0; i < 10; i++)
    {
        if (token3.Terminated)
            return;
        System.Console.WriteLine($"T3 {i}");
        Thread.Sleep(1500);
    }
}, new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, token3, MyTask.TaskPriority.High));*/
scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 3, token3, MyTask.TaskPriority.High, new Resource("C:\\Users\\User20\\Desktop\\test.jpg")/*, new Resource("C:\\Users\\User20\\Desktop\\test2.jfif"), new Resource("C:\\Users\\User20\\Desktop\\test3.png")*/));
Console.WriteLine("Hello, World!");
ControlToken? token4 = new();
scheduler.AddTask(new TaskScheduler.MyTask(() =>
{
    for (int i = 0; i < 10; i++)
    {
        if (token4.Terminated)
            return;
        System.Console.WriteLine($"T4 {i}");
        Thread.Sleep(2500);
    }
}, new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, token4, MyTask.TaskPriority.BelowNormal));
Thread.Sleep(10000);
//token2.Resume();
