using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
        private Dictionary<string, Func<MyTask>> taskTypes = new();
        private DateTimePicker deadlineDTP = new DateTimePicker() { Width = 150 };
        private List<MyTask> tasks = new List<MyTask>();
        private TaskScheduler.TaskScheduler scheduler;
        private bool priorityScheduling, preemptiveScheduling;

        private static readonly int autosaveIntervalMs = 5000;

        public MainWindow(int cores, int concTasks, bool priority, bool preemptive)
        {
            InitializeComponent();
            scheduler = new TaskScheduler.TaskScheduler(cores, concTasks);
            priorityScheduling = priority;
            preemptiveScheduling = preemptive; 
            Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            tasksStackPanel = tasksSP;
            taskTypes.Add("Edge Detection Task", CreateEdgeDetectionTask);
            ResizeMode = ResizeMode.CanMinimize;
            deadlineSP.Children.Add(deadlineDTP);
            taskTypeCB.ItemsSource = taskTypes.Keys;
            priorityCB.ItemsSource = Enum.GetValues(typeof(MyTask.TaskPriority));
            if (!priorityScheduling)
            {
                priorityCB.SelectedItem = MyTask.TaskPriority.Normal;
                priorityCB.IsEnabled = false;
            }
            scheduler.Start();
            /*new Thread(() =>
            {
                while (true)
                {
                    Save();
                    Thread.Sleep(autosaveIntervalMs);
                }
            })
            { IsBackground = true }.Start();*/
        }

        private void Save()
        {
            throw new NotImplementedException();
        }

        private EdgeDetectionTask CreateEdgeDetectionTask()
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
        }

        private void taskTypeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!taskTypes.TryGetValue(taskTypeCB.SelectedItem.ToString(), out Func<MyTask>? action))
                action?.Invoke();
        }

        private void addResourceBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == true)
                resourcesLB.Items.Add(fileDialog.FileName);
        }

        private void addTaskBtn_Click(object sender, RoutedEventArgs e)
        {
            MyTask task = null;
            if (taskTypes.TryGetValue(taskTypeCB.SelectedItem.ToString(), out Func<MyTask>? func))
                task = func.Invoke();
            if (task != null)
            {
                tasks.Add(task);
                //scheduler.AddTask(task);
                AddTaskToStackPanel(task);
                //scheduler.AddTask(task);
                resourcesLB.Items.Clear();
                //deadlineDTP.Value = null;
                //maxExecTimeTB.Clear();
                //maxDegreeOfParallelismTB.Clear();
            }
        }

        private void AddTaskToStackPanel(MyTask task)
        {
            StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(new Label() { Content = task.ToString() });
            ProgressBar pb = new ProgressBar() { Orientation = Orientation.Horizontal, Width = 150, Margin = new Thickness(5, 0, 0, 0), Maximum = 1.0 };
            stackPanel.Children.Add(pb);
            task.Action2 = () =>
            {
                this.Dispatcher.Invoke(() => pb.Value = task.Progress);
            };
            Button removeBtn = new Button() { Content = "Remove", Margin = new Thickness(15, 0, 0, 0), IsEnabled = false };
            removeBtn.Click += new RoutedEventHandler((sender, e) => task.UserControlToken?.Pause());
            Button startBtn = new Button() { Content = "Start", Margin = new Thickness(5, 0, 0, 0) };
            startBtn.Click += new RoutedEventHandler((sender, e) =>
            { 
                scheduler.AddTask(task);
                startBtn.IsEnabled = false;
                removeBtn.IsEnabled = false;
            });
            stackPanel.Children.Add(startBtn);
            Button pauseBtn = new Button() { Content = "Pause", Margin = new Thickness(15, 0, 0, 0) };
            pauseBtn.Click += new RoutedEventHandler((sender, e) => task.UserControlToken?.Pause());
            stackPanel.Children.Add(pauseBtn);
            Button resumeBtn = new Button() { Content = "Resume", Visibility = Visibility.Visible, Margin = new Thickness(5, 0, 0, 0) };
            resumeBtn.Click += new RoutedEventHandler((sender, e) => task.UserControlToken?.Resume());
            stackPanel.Children.Add(resumeBtn);
            Button cancelBtn = new Button() { Content = "Cancel", Margin = new Thickness(5, 0, 0, 0) };
            cancelBtn.Click += new RoutedEventHandler((sender, e) => task.UserControlToken?.Terminate());
            //cancelBtn.Click += new RoutedEventHandler((sender, e) => task.Serialize("task.txt"));
            stackPanel.Children.Add(cancelBtn);
            stackPanel.Children.Add(removeBtn);
            tasksStackPanel.Children.Add(stackPanel);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
