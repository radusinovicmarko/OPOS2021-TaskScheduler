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

        private readonly DateTimePicker deadlineDTP = new() { Width = 250, Margin = new Thickness(5, 5, 5, 5) };

        private EdgeDetectionTask? task;
        private readonly bool priorityScheduling;
        private readonly bool preemptiveScheduling;

        public EdgeDetectionTask? Task { get { return task; } set { task = value;} }

        public EdgeDetectionTaskWindow(bool priorityScheduling, bool preemptiveScheduling)
        {
            InitializeComponent();
            this.priorityScheduling = priorityScheduling;
            this.preemptiveScheduling = preemptiveScheduling;
            deadlineSP.Children.Add(deadlineDTP);
            ResizeMode = ResizeMode.CanMinimize;
            priorityCB.ItemsSource = Enum.GetValues(typeof(MyTask.TaskPriority));
            priorityCB.SelectedItem = MyTask.TaskPriority.Normal;
            if (!this.priorityScheduling)
                priorityCB.IsEnabled = false;
        }

        private void AddResourceBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new();
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
            bool valid = true;
            if (deadlineDTP.Value == null)
            {
                System.Windows.MessageBox.Show("Invalid parameter: Deadline.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                valid = false;
            }
            if (!Double.TryParse(maxExecTimeTB.Text, out double maxExecTime))
            {
                System.Windows.MessageBox.Show("Invalid parameter: Maximum Execution Time.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                valid = false;
            }
            if (!Int32.TryParse(maxDegreeOfParallelismTB.Text, out int maxDegreeOfParallelism))
            {
                System.Windows.MessageBox.Show("Invalid parameter Maximum Degree Of Parallelism.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                valid = false;
            }
            if (!Enum.TryParse(priorityCB.SelectedItem.ToString(), out MyTask.TaskPriority priority))
            {
                System.Windows.MessageBox.Show("Invalid parameter: Priority.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                valid = false;
            }
            if (outputFolderLbl.Content == null || outputFolderLbl.Content.Equals(""))
            {
                System.Windows.MessageBox.Show("Invalid parameter: Output Folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                valid = false;
            }
            if (!valid)
                return;
            List<Resource> resources = new();
            foreach (string item in resourcesLB.Items)
                resources.Add(new FileResource(item));
            ControlToken? controlToken = preemptiveScheduling ? new() : null;
            string id = new Random().Next().ToString();
            try
            {
                task = new EdgeDetectionTask(id, (DateTime)deadlineDTP.Value, maxExecTime, maxDegreeOfParallelism, controlToken, new ControlToken(), priority, outputFolderLbl.Content.ToString(), resources.ToArray());
            } catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                task = null;
                return;
            }
            this.Hide();
        }
    }
}