// See https://aka.ms/new-console-template for more information

using DokanNet;
using System.Diagnostics;
using System.Drawing;
using TaskScheduler;

Dictionary<string, bool> filesProcessed = new();

TaskScheduler.TaskScheduler scheduler = new TaskScheduler.TaskScheduler(4, 10);
scheduler.Start();
//using var watcher = new FileSystemWatcher();
new Thread(() =>
{
    /*Thread.Sleep(1000);
    Bitmap originalImage = (Bitmap)Bitmap.FromFile("test.jpg");
    originalImage.Save("V:\\input\\test.jpg");
    //originalImage = (Bitmap)Bitmap.FromFile("V:\\output\\test2.jpg");
    scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, new FolderResource("V:\\output\\"), new FileResource("V:\\input\\test.jpg")));
    */
    while (true)
    {
        Thread.Sleep(500);
        string folder = "V:\\input\\";
        var files = Directory.GetFiles(folder);
        foreach (var file in files)
        {
            if (!filesProcessed.ContainsKey(file))
            {
                Thread.Sleep(500);
                File.AppendAllLines("log.txt", new string[] { file });
                scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, new FolderResource("V:\\output\\"), new FileResource(file)));
                filesProcessed.Add(file, true);
            }
        }
        /*watcher.Path = "V:\\input";
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Filter = "*.*";
        watcher.EnableRaisingEvents = true;
        watcher.Created += (s, e) =>
        {
            Thread.Sleep(500);
            File.WriteAllLines(".\\line.txt", new string[] { "asdads" });
            scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, new FolderResource("V:\\output\\"), new FileResource(e.FullPath)));
            new Process() { StartInfo = new ProcessStartInfo() { FileName = "cmd.exe", UseShellExecute = true } }.Start();
        };*/
    }
});//.Start();

new UserSpaceFileSystem.FileSystem().Mount("V:\\", DokanOptions.DebugMode | DokanOptions.StderrOutput);
