using System;
using System.Collections.Generic;
using System.IO;
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

namespace GUIApp
{
    /// <summary>
    /// Interaction logic for StartupWindow.xaml
    /// </summary>
    public partial class StartupWindow : Window
    {
        public StartupWindow()
        {
            InitializeComponent();
            ResizeMode = ResizeMode.CanMinimize;
            coresCB.ItemsSource = Enumerable.Range(1, Environment.ProcessorCount);
            CheckForLoad();
        }

        private void CheckForLoad()
        {
            string folder = MainWindow.folderPath;
            DirectoryInfo directoryInfo = new DirectoryInfo(folder);
            foreach (var fileInfo in directoryInfo.GetFiles())
                if (fileInfo.Name.StartsWith("TaskScheduler"))
                {
                    if (MessageBox.Show("A save file has been detected. Do you want to load the data from it?", "Load", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                    {
                        MainWindow win = new();
                        this.Hide();
                        win.Show();
                    }
                }
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (coresCB.SelectedItem != null && maxNoConcurrentTasksTB.Text != ""
                && (preemptiveRB.IsChecked == true || nonPeemptiveRB.IsChecked == true) && (priorityRB.IsChecked == true || nonPriorityRB.IsChecked == true))
            {
                try
                {
                    int cores = Int32.Parse(coresCB.SelectedItem.ToString());
                    int tasks = Int32.Parse(maxNoConcurrentTasksTB.Text.ToString());
                    bool priority = (bool)priorityRB.IsChecked;
                    bool preemptive = (bool)preemptiveRB.IsChecked;
                    MainWindow win = new MainWindow(cores, tasks, priority, preemptive);
                    this.Hide();
                    win.Show();
                    
                } catch
                {
                    MessageBox.Show("Invalid parameters.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
                MessageBox.Show("Invalid parameters.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
