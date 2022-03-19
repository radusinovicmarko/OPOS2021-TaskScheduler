﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private TaskScheduler.TaskScheduler scheduler = new TaskScheduler.TaskScheduler(4, 10);

        public MainWindow()
        {
            InitializeComponent();
            Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            tasksStackPanel = tasksSP;
            taskTypes.Add("Edge Detection Task", CreateEdgeDetectionTask);
            ResizeMode = ResizeMode.CanMinimize;
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            deadlineSP.Children.Add(deadlineDTP);
            taskTypeCB.ItemsSource = taskTypes.Keys;
            priorityCB.ItemsSource = Enum.GetValues(typeof(MyTask.TaskPriority));
            scheduler.Start();

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
                resources.Add(new Resource(item));
            return new EdgeDetectionTask((DateTime)deadlineDTP.Value, maxExecTime, maxDegreeOfParallelism, new ControlToken(), priority, resources.ToArray());
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
                scheduler.AddTask(task);
                //resourcesLB.Items.Clear();
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
            Button pauseBtn = new Button() { Content = "Pause", Margin = new Thickness(5, 0, 0, 0) };
            pauseBtn.Click += new RoutedEventHandler((sender, e) => task.ControlToken?.Pause());
            stackPanel.Children.Add(pauseBtn);
            Button resumeBtn = new Button() { Content = "Resume", Visibility = Visibility.Visible, Margin = new Thickness(5, 0, 0, 0) };
            resumeBtn.Click += new RoutedEventHandler((sender, e) => task.ControlToken?.Resume());
            stackPanel.Children.Add(resumeBtn);
            Button cancelBtn = new Button() { Content = "Cancel", Margin = new Thickness(5, 0, 0, 0) };
            cancelBtn.Click += new RoutedEventHandler((sender, e) => task.ControlToken?.Terminate());
            stackPanel.Children.Add(cancelBtn);
            tasksStackPanel.Children.Add(stackPanel);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}