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
        private Dictionary<string, Action> taskTypes = new();
        private TaskScheduler.TaskScheduler scheduler;
        private bool priorityScheduling, preemptiveScheduling;


        public static readonly string folderPath = ".." + System.IO.Path.DirectorySeparatorChar + ".." + System.IO.Path.DirectorySeparatorChar + ".." + System.IO.Path.DirectorySeparatorChar + "saves";

        private static readonly int autosaveIntervalMs = 5000;

        public MainWindow(int cores, int concTasks, bool priority, bool preemptive)
        {
            InitializeComponent();

            if (!File.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            TaskScheduler.TaskScheduler scheduler1 = TaskScheduler.TaskScheduler.Deserialize(folderPath + System.IO.Path.DirectorySeparatorChar + "TaskScheduler_637849402613931537.bin");
            System.Windows.MessageBox.Show(scheduler1.ToString());

            priorityScheduling = priority;
            preemptiveScheduling = preemptive;
            scheduler = new TaskScheduler.TaskScheduler(cores, concTasks, preemptiveScheduling);
            //Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            tasksStackPanel = tasksSP;
            taskTypes.Add("Edge Detection Task", () =>
            {
                EdgeDetectionTaskWindow win = new EdgeDetectionTaskWindow(priorityScheduling, preemptiveScheduling);
                if (win.ShowDialog() == false)
                    AddTaskToStackPanel(win.Task);
            });
            ResizeMode = ResizeMode.CanMinimize;
            taskTypeCB.ItemsSource = taskTypes.Keys;
            scheduler.Start();
            new Thread(() =>
            {
                while (true)
                {
                    Save();
                    Thread.Sleep(autosaveIntervalMs);
                }
            })
            { IsBackground = true };//.Start();
        }

        private void Save()
        {
            DirectoryInfo directoryInfo = new(folderPath);
            foreach (var fileInfo in directoryInfo.GetFiles())
                fileInfo.Delete();
            scheduler.Serialize(folderPath);
        }

       /* private EdgeDetectionTask CreateEdgeDetectionTask()
        {
            if (!Int32.TryParse(maxExecTimeTB.Text, out int maxExecTime))
                System.Windows.MessageBox.Show("...");
            if (!Int32.TryParse(maxDegreeOfParallelismTB.Text, out int maxDegreeOfParallelism))
                System.Windows.MessageBox.Show("...");
            MyTask.TaskPriority priority;
            if (!Enum.TryParse(priorityCB.SelectedItem.ToString(), out priority))
                System.Windows.MessageBox.Show("...");
            List<Resource> resources = new List<Resource>();
            foreach (string item in resourcesLB.Items)
                resources.Add(new FileResource(item));
            ControlToken? controlToken = preemptiveScheduling ? new() : null;
            return new EdgeDetectionTask((DateTime)deadlineDTP.Value, maxExecTime, maxDegreeOfParallelism, controlToken, new ControlToken(), priority, resources.ToArray());
        }*/

        private void AddTaskToStackPanel(MyTask task)
        {
            if (task == null)
            {
                System.Windows.MessageBox.Show("Task settings not correctly specified.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(new Label() { Content = task.ToString() });

            Button removeBtn = new Button() { Content = "Remove", Margin = new Thickness(15, 0, 0, 0), IsEnabled = true, Width = 50 };
            removeBtn.Click += new RoutedEventHandler((sender, e) => tasksStackPanel.Children.Remove(stackPanel));

            task.FinishedTaskCallback = () => this.Dispatcher.Invoke(() => removeBtn.IsEnabled = true);

            ProgressBar pb = new ProgressBar() { Orientation = Orientation.Horizontal, Width = 175, Margin = new Thickness(5, 0, 0, 0), Maximum = 1.0 };
            stackPanel.Children.Add(pb);
            task.ProgressBarUpdateAction = () =>
            {
                this.Dispatcher.Invoke(() => pb.Value = task.Progress);
            };

            Button startBtn = new Button() { Content = "Start", Margin = new Thickness(10, 0, 0, 0), Width = 50 };
            startBtn.Click += new RoutedEventHandler((sender, e) =>
            { 
                scheduler.AddTask(task);
                startBtn.IsEnabled = false;
                removeBtn.IsEnabled = false;
            });
            stackPanel.Children.Add(startBtn);

            Button pauseBtn = new Button() { Content = "Pause", Margin = new Thickness(15, 0, 0, 0), Width = 50 };
            pauseBtn.Click += new RoutedEventHandler((sender, e) => task.UserControlToken?.Pause());
            stackPanel.Children.Add(pauseBtn);

            Button resumeBtn = new Button() { Content = "Resume", Visibility = Visibility.Visible, Margin = new Thickness(5, 0, 0, 0), Width = 50 };
            resumeBtn.Click += new RoutedEventHandler((sender, e) => task.UserControlToken?.Resume());
            stackPanel.Children.Add(resumeBtn);

            Button cancelBtn = new Button() { Content = "Cancel", Margin = new Thickness(5, 0, 0, 0), Width = 50 };
            cancelBtn.Click += new RoutedEventHandler((sender, e) => task.UserControlToken?.Terminate());
            stackPanel.Children.Add(cancelBtn);
            stackPanel.Children.Add(removeBtn);
            tasksStackPanel.Children.Add(stackPanel);
        }

        private void addTaskBtn_Click(object sender, RoutedEventArgs e)
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
