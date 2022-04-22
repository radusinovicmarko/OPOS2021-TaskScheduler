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
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            int cores = 0, tasks = 0;
            if (coresCB.SelectedItem != null && maxNoConcurrentTasksTB.Text != ""
                && (preemptiveRB.IsChecked == true || nonPeemptiveRB.IsChecked == true) && (priorityRB.IsChecked == true || nonPriorityRB.IsChecked == true))
            {
                try
                {
                    cores = Int32.Parse(coresCB.SelectedItem.ToString());
                    tasks = Int32.Parse(maxNoConcurrentTasksTB.Text.ToString());
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
