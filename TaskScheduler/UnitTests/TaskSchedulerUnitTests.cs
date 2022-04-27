using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using TaskScheduler;

namespace UnitTests
{
    [TestClass]
    public class TaskSchedulerUnitTests
    {
        public static readonly string path = ".." + Path.DirectorySeparatorChar + ".." + System.IO.Path.DirectorySeparatorChar + ".." + System.IO.Path.DirectorySeparatorChar + "files" + Path.DirectorySeparatorChar;
        public static readonly string output = path + "output";


        [TestMethod]
        public void ScheduleOneTaskTest()
        {
            DeleteFiles(output);
            TaskScheduler.TaskScheduler scheduler = new(4, 10);
            MyTask task = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, new FileResource(path + "test.jpg"));
            scheduler.Start();
            scheduler.AddTask(task);
            while (!task.Terminated) ;
            Assert.IsTrue(task.Terminated && !task.ControlToken.Terminated);
        }

        [TestMethod]
        public void MaxExecTimeTest()
        {
            DeleteFiles(output);
            TaskScheduler.TaskScheduler scheduler = new(1, 10);
            MyTask task = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 0.1, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, 
                new FileResource(path + "test.jpg"), new FileResource(path + "test2.jpg"), new FileResource(path + "test3.jpg"));
            scheduler.Start();
            scheduler.AddTask(task);
            while (!task.Terminated) ;
            Assert.IsTrue(task.ControlToken?.Terminated);
        }

        [TestMethod]
        public void DeadlineTest()
        {
            DeleteFiles(output);
            TaskScheduler.TaskScheduler scheduler = new(1, 10);
            MyTask task = new EdgeDetectionTask("", DateTime.Now, 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output,
                new FileResource(path + "test.jpg"), new FileResource(path + "test2.jpg"), new FileResource(path + "test3.jpg"));
            scheduler.Start();
            scheduler.AddTask(task);
            while (!task.Terminated) ;
            Assert.IsTrue(task.ControlToken?.Terminated);
        }

        [TestMethod]
        public void ParallelismTaskTest()
        {
            DeleteFiles(output);
            TaskScheduler.TaskScheduler scheduler = new(4, 10);
            MyTask task = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 4, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, new FileResource(path + "test.jpg"));
            scheduler.Start();
            scheduler.AddTask(task);
            while (!task.Terminated) ;
            Assert.IsTrue(task.Terminated && !task.ControlToken.Terminated);
        }

        [TestMethod]
        public void MultipleTasksScheduledTest()
        {
            DeleteFiles(output);
            TaskScheduler.TaskScheduler scheduler = new(4, 10);
            MyTask task1 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, new FileResource(path + "test.jpg"));
            MyTask task2 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, new FileResource(path + "test2.jpg"));
            MyTask task3 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, new FileResource(path + "test3.jpg"));
            scheduler.Start();
            scheduler.AddTask(task1);
            scheduler.AddTask(task2);
            scheduler.AddTask(task3);
            while (!task1.Terminated || !task2.Terminated || !task3.Terminated) ;
            Assert.IsTrue(task1.Terminated && !task1.ControlToken.Terminated && task2.Terminated && !task2.ControlToken.Terminated 
                && task3.Terminated && !task3.ControlToken.Terminated);
        }

        [TestMethod]
        public void CancelTaskTest()
        {
            DeleteFiles(output);
            TaskScheduler.TaskScheduler scheduler = new(4, 10);
            ControlToken userToken = new();
            MyTask task = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 4, new ControlToken(), userToken, MyTask.TaskPriority.Normal, output, new FileResource(path + "test.jpg"));
            scheduler.Start();
            scheduler.AddTask(task);
            Thread.Sleep(10);
            userToken.Terminate();
            while (!task.Terminated) ;
            Assert.IsTrue(task.UserControlToken?.Terminated);
        }

        [TestMethod]
        public void PauseAndResumeTaskTest()
        {
            DeleteFiles(output);
            TaskScheduler.TaskScheduler scheduler = new(4, 10);
            ControlToken userToken = new();
            MyTask task = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 4, new ControlToken(), userToken, MyTask.TaskPriority.Normal, output, new FileResource(path + "test.jpg"));
            scheduler.Start();
            scheduler.AddTask(task);
            Thread.Sleep(10);
            userToken.Pause();
            Thread.Sleep(200);
            userToken.Resume();
            while (!task.Terminated) ;
            Assert.IsTrue(task.Terminated && !task.UserControlToken.Terminated);
        }

        [TestMethod]
        public void SerializeAndDeserializeTest()
        {
            DeleteFiles(output);
            MyTask task = new EdgeDetectionTask(new Random().Next().ToString(), new DateTime(2023, 2, 22, 0, 0, 0), 20, 4, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, new FileResource(path + "test.jpg"));
            task.Serialize(output);
            MyTask task2 = EdgeDetectionTask.Deserialize(Directory.GetFiles(output)[0]);
            Assert.IsTrue(task.Id == task2.Id && task.Deadline == task2.Deadline && task.Priority == task2.Priority && task.MaxExecTime == task2.MaxExecTime);
        }

        [TestMethod]
        public void RealTimeSchedulingTest()
        {
            DeleteFiles(output);
            TaskScheduler.TaskScheduler scheduler = new(4, 10);
            MyTask task1 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, new FileResource(path + "test.jpg"));
            MyTask task2 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, new FileResource(path + "test2.jpg"));
            scheduler.Start();
            scheduler.AddTask(task1);
            Thread.Sleep(10);
            scheduler.AddTask(task2);
            while (!task1.Terminated || !task2.Terminated) ;
            Assert.IsTrue(task1.Terminated && !task1.ControlToken.Terminated && task2.Terminated && !task2.ControlToken.Terminated);
        }

        [TestMethod]
        public void PrioritySchedulingTest()
        {
            DeleteFiles(output);
            TaskScheduler.TaskScheduler scheduler = new(1, 10);
            MyTask task1 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, new FileResource(path + "test.jpg"));
            MyTask task2 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.AboveNormal, output, new FileResource(path + "test2.jpg"));
            MyTask task3 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.High, output, new FileResource(path + "test3.jpg"));
            scheduler.AddTask(task1);
            scheduler.AddTask(task2);
            scheduler.AddTask(task3);
            scheduler.Start();
            while (!task1.Terminated || !task2.Terminated || !task3.Terminated) ;
            Assert.IsTrue(task1.DateTimeFinished >= task2.DateTimeFinished && task2.DateTimeFinished >= task3.DateTimeFinished);
        }

        [TestMethod]
        public void PreemptiveSchedulingTest()
        {
            DeleteFiles(output);
            TaskScheduler.TaskScheduler scheduler = new(1, 10);
            MyTask task1 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, new FileResource(path + "test.jpg"));
            MyTask task2 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.High, output, new FileResource(path + "test2.jpg"));
            scheduler.Start();
            scheduler.AddTask(task1);
            Thread.Sleep(10);
            scheduler.AddTask(task2);
            while (!task1.Terminated || !task2.Terminated) ;
            Assert.IsTrue(task2.DateTimeFinished <= task1.DateTimeFinished);
        }

        [TestMethod]
        public void DeadlockPreventionTest()
        {
            DeleteFiles(output);
            TaskScheduler.TaskScheduler scheduler = new(4, 10);
            ControlToken token = new();
            MyTask task1 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), token, MyTask.TaskPriority.Normal, output, new FileResource(path + "test.jpg"), new FileResource(path + "test3.jpg"));
            MyTask task2 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, new FileResource(path + "test2.jpg"), new FileResource(path + "test3.jpg"));
            MyTask task3 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, new FileResource(path + "test.jpg"), new FileResource(path + "test2.jpg"));
            scheduler.Start();
            scheduler.AddTask(task1);
            Thread.Sleep(10);
            scheduler.AddTask(task2);
            token.Pause();
            Thread.Sleep(10);
            scheduler.AddTask(task3);
            Thread.Sleep(10);
            token.Resume();
            while (!task1.Terminated || !task2.Terminated || !task3.Terminated) ;
            Assert.IsTrue(task1.Terminated && !task1.ControlToken.Terminated && task2.Terminated && !task2.ControlToken.Terminated
                && task3.Terminated && !task3.ControlToken.Terminated);
        }

        [TestMethod]
        public void PIPTest()
        {
            DeleteFiles(output);
            TaskScheduler.TaskScheduler scheduler = new(1, 10);
            MyTask task1 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Low, output, new FileResource(path + "test.jpg"));
            MyTask task2 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, output, new FileResource(path + "test2.jpg"));
            MyTask task3 = new EdgeDetectionTask("", new DateTime(2023, 2, 22, 0, 0, 0), 20, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.High, output, new FileResource(path + "test.jpg"));
            scheduler.Start();
            scheduler.AddTask(task1);
            Thread.Sleep(10);
            scheduler.AddTask(task2);
            Thread.Sleep(10);
            scheduler.AddTask(task3);
            while (!task1.Terminated || !task2.Terminated || !task3.Terminated) ;
            Assert.IsTrue(task1.DateTimeFinished <= task3.DateTimeFinished && task3.DateTimeFinished <= task2.DateTimeFinished);
        }

        private static void DeleteFiles(string path)
        {
            DirectoryInfo directoryInfo = new(path);
            foreach (var fileInfo in directoryInfo.GetFiles())
                fileInfo.Delete();
        }
    }
}