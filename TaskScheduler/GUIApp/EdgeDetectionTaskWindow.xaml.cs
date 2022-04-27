using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
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
using System.Windows.Shapes;
using TaskScheduler;
using Xceed.Wpf.Toolkit;

namespace GUIApp
{
    /// <summary>
    /// Interaction logic for EdgeDetectionTaskWindow.xaml
    /// </summary>
    public partial class EdgeDetectionTaskWindow : Window
    {

        private DateTimePicker deadlineDTP = new DateTimePicker() { Width = 250, Margin = new Thickness(5, 5, 5, 5) };

        private EdgeDetectionTask? task;

        private bool priorityScheduling, preemptiveScheduling;

        public EdgeDetectionTask? Task { get { return task; } set { task = value;} }

        public EdgeDetectionTaskWindow(bool priorityScheduling, bool preemptiveScheduling)
        {
            InitializeComponent();
            this.priorityScheduling = priorityScheduling;
            this.preemptiveScheduling = preemptiveScheduling;
            deadlineSP.Children.Add(deadlineDTP);
            ResizeMode = ResizeMode.CanMinimize;
            priorityCB.ItemsSource = Enum.GetValues(typeof(MyTask.TaskPriority));
            if (!this.priorityScheduling)
            {
                priorityCB.SelectedItem = MyTask.TaskPriority.Normal;
                priorityCB.IsEnabled = false;
            }
        }

        private void addResourceBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == true)
                resourcesLB.Items.Add(fileDialog.FileName);
        }

        private void AddOutputFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new();
            if (dialog.ShowDialog() == true)
                outputFolderLbl.Content = dialog.SelectedPath;
        }

        private void ResourcesLB_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            resourcesLB.Items.Remove(resourcesLB.SelectedItem);
        }

        private void AddTaskBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Double.TryParse(maxExecTimeTB.Text, out double maxExecTime))
                System.Windows.MessageBox.Show("...");
            if (!Int32.TryParse(maxDegreeOfParallelismTB.Text, out int maxDegreeOfParallelism))
                System.Windows.MessageBox.Show("...");
            MyTask.TaskPriority priority;
            if (!Enum.TryParse(priorityCB.SelectedItem.ToString(), out priority))
                System.Windows.MessageBox.Show("...");
            if (outputFolderLbl.Content == null || outputFolderLbl.Content.Equals(""))
                System.Windows.MessageBox.Show("...");
            List<Resource> resources = new List<Resource>();
            foreach (string item in resourcesLB.Items)
                resources.Add(new FileResource(item));
            ControlToken? controlToken = preemptiveScheduling ? new() : null;
            string id = new Random().Next().ToString();
            task = new EdgeDetectionTask(id, (DateTime)deadlineDTP.Value, maxExecTime, maxDegreeOfParallelism, controlToken, new ControlToken(), priority, outputFolderLbl.Content.ToString(), resources.ToArray());
            this.Hide();
        }
    }
}