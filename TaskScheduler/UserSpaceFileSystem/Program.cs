// See https://aka.ms/new-console-template for more information

using DokanNet;
using System.Diagnostics;
using System.Drawing;
using TaskScheduler;

Dictionary<string, bool> filesProcessed = new();

TaskScheduler.TaskScheduler scheduler = new(4, 10);
scheduler.Start();
new Thread(() =>
{
    while (true)
    {
        Thread.Sleep(500);
        string folder = "V:\\input\\";
        var files = Directory.GetFiles(folder);
        foreach (var file in files)
        {
            if (!filesProcessed.ContainsKey(file))
            {
                Thread.Sleep(100);
                scheduler.AddTask(new EdgeDetectionTask(new Random().Next().ToString(), new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, "V:\\output\\", new FileResource(file)));
                filesProcessed.Add(file, true);
            }
        }
    }
}).Start();

new UserSpaceFileSystem.FileSystem().Mount("V:\\", DokanOptions.DebugMode | DokanOptions.StderrOutput);