using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using TaskScheduler;
using Xceed.Wpf.Toolkit;

namespace GUIApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private StackPanel tasksStackPanel;
        private readonly Dictionary<string, Action> taskTypes = new();

        private readonly Dictionary<string, Type> taskNames = new();

        private TaskScheduler.TaskScheduler scheduler;
        private bool priorityScheduling;
        private bool preemptiveScheduling;
        public static readonly string folderPath = /*"C:\\Users\\User20\\Desktop\\saves"; //*/".." + System.IO.Path.DirectorySeparatorChar + ".." + System.IO.Path.DirectorySeparatorChar + ".." + System.IO.Path.DirectorySeparatorChar + "saves";
        public static readonly string tasksPath = folderPath + System.IO.Path.DirectorySeparatorChar + "task saves";

        private static readonly int autosaveIntervalMs = 5000;

        public MainWindow()
        {
            InitializeComponent();
            Init();
            Load();
            new Thread(Autosave) { IsBackground = true }.Start();
        }

        public MainWindow(int cores, int concTasks, bool priority, bool preemptive)
        {
            InitializeComponent();
            Init();

            priorityScheduling = priority;
            preemptiveScheduling = preemptive;
            scheduler = new TaskScheduler.TaskScheduler(cores, concTasks, preemptiveScheduling, priorityScheduling);
            tasksStackPanel = tasksSP;
            scheduler.Start();
            new Thread(Autosave) { IsBackground = true }.Start();
        }

        private void Init()
        {
            if (!File.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            if (!File.Exists(tasksPath))
                Directory.CreateDirectory(tasksPath);
            tasksStackPanel = tasksSP;
            taskTypes.Add("Edge Detection Task", () =>
            {
                EdgeDetectionTaskWindow win = new EdgeDetectionTaskWindow(priorityScheduling, preemptiveScheduling);
                if (win.ShowDialog() == false)
                    AddTaskToStackPanel(win.Task, false);
            });

            taskNames.Add(typeof(EdgeDetectionTask).Name, typeof(EdgeDetectionTask));

            ResizeMode = ResizeMode.CanMinimize;
            taskTypeCB.ItemsSource = taskTypes.Keys;
        }

        private void Autosave()
        {
            while (true)
            {
                Save();
                Thread.Sleep(autosaveIntervalMs);
            }
        }

        private void Save()
        {
            DirectoryInfo directoryInfo = new(folderPath);
            foreach (var fileInfo in directoryInfo.GetFiles())
                fileInfo.Delete();
            directoryInfo = new(tasksPath);
            foreach (var fileInfo in directoryInfo.GetFiles())
                fileInfo.Delete();
            scheduler.Serialize(folderPath);
        }

        public void Load()
        {
            try
            {
                DirectoryInfo directoryInfo = new(folderPath);
                foreach (var fileInfo in directoryInfo.GetFiles())
                    if (fileInfo.Name.StartsWith("TaskScheduler"))
                        scheduler = TaskScheduler.TaskScheduler.Deserialize(fileInfo.FullName);
                preemptiveScheduling = scheduler.PreemptiveScheduling;
                priorityScheduling = scheduler.PriorityScheduling;
                scheduler.Start();
                foreach (var file in Directory.GetFiles(tasksPath))
                {
                    string name = System.IO.Path.GetFileName(file);
                    string type = name.Substring(0, name.IndexOf('_'));
                    MyTask task = (MyTask)taskNames[type].GetMethod("Deserialize").Invoke(null, new object[] { file });
                    //System.Windows.MessageBox.Show(task.State + " " + task.Priority + " " + task.ResourcesProcessed[0] + " " + task.Terminated);
                    //task.State = MyTask.TaskState.Ready;
                    AddTaskToStackPanel(task, true);
                    scheduler.AddTask(task);
                }
            } catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void AddTaskToStackPanel(MyTask task, bool load)
        {
            if (task == null)
            {
                System.Windows.MessageBox.Show("Task settings not correctly specified.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StackPanel stackPanel = new() { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(new Label() { Content = "Task  ID " + task.Id, Width = 150 });

            Button removeBtn = new() { Content = "Remove", Margin = new Thickness(15, 0, 0, 0), IsEnabled = true, Width = 50 };
            removeBtn.Click += new RoutedEventHandler((sender, e) => tasksStackPanel.Children.Remove(stackPanel));

            task.FinishedTaskCallback = () => this.Dispatcher.Invoke(() => removeBtn.IsEnabled = true);

            ProgressBar pb = new() { Orientation = Orientation.Horizontal, Width = 200, Margin = new Thickness(5, 0, 0, 0), Maximum = 1.0 };
            stackPanel.Children.Add(pb);
            task.ProgressBarUpdateAction = () =>
            {
                this.Dispatcher.Invoke(() => pb.Value = task.Progress);
            };

            Button startBtn = new() { Content = "Start", Margin = new Thickness(10, 0, 0, 0), Width = 50 };
            startBtn.Click += new RoutedEventHandler((sender, e) =>
            {
                scheduler.AddTask(task);
                startBtn.IsEnabled = false;
                removeBtn.IsEnabled = false;
            });
            stackPanel.Children.Add(startBtn);

            if (load)
            {
                startBtn.IsEnabled = false;
                removeBtn.IsEnabled = false;
            }

            Button pauseBtn = new() { Content = "Pause", Margin = new Thickness(15, 0, 0, 0), Width = 50 };
            pauseBtn.Click += new RoutedEventHandler((sender, e) => task.UserControlToken?.Pause());
            stackPanel.Children.Add(pauseBtn);

            Button resumeBtn = new() { Content = "Resume", Visibility = Visibility.Visible, Margin = new Thickness(5, 0, 0, 0), Width = 50 };
            resumeBtn.Click += new RoutedEventHandler((sender, e) => task.UserControlToken?.Resume());
            stackPanel.Children.Add(resumeBtn);

            Button cancelBtn = new() { Content = "Cancel", Margin = new Thickness(5, 0, 0, 0), Width = 50 };
            cancelBtn.Click += new RoutedEventHandler((sender, e) => task.UserControlToken?.Terminate());
            stackPanel.Children.Add(cancelBtn);
            stackPanel.Children.Add(removeBtn);
            tasksStackPanel.Children.Add(stackPanel);
        }

        private void AddTaskBtn_Click(object sender, RoutedEventArgs e)
        {
            if (taskTypeCB.SelectedItem != null)
            {
                string taskType = taskTypeCB.SelectedItem.ToString();
                if (taskTypes.TryGetValue(taskType, out Action? action))
                    action?.Invoke();
                else
                    System.Windows.MessageBox.Show("...");
            }
            else
                System.Windows.MessageBox.Show("...");
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
