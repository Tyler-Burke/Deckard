using Cain;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Deckard
{
    /// <summary>
    /// Interaction logic for MetricsWindow.xaml
    /// </summary>
    public partial class MetricsWindow : Window
    {
        private MetricFile _metricFile;

        public MetricsWindow()
        {
            InitializeComponent();

            LoadMetrics();
        }

        private void LoadMetrics()
        {
            //Load Metric File
            string applicationFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            _metricFile = new MetricFile(applicationFolderPath + @"Metrics.json");
            dataGridMetrics.ItemsSource = _metricFile.GetMetrics();
        }

        public void Add_Click(object sender, RoutedEventArgs e)
        {
            MetricsCreateWindow window = new MetricsCreateWindow();
            window.CreateMetric += Window_CreateMetric;
            window.Show();
        }

        public void Delete_Click(object sender, RoutedEventArgs e)
        {
            Metric metric = ((FrameworkElement)sender).DataContext as Metric;
            _metricFile.DeleteMetric(metric);
            dataGridMetrics.ItemsSource = _metricFile.GetMetrics();
        }
        
        public void Edit_Click(object sender, RoutedEventArgs e)
        {
            Metric metric = ((FrameworkElement)sender).DataContext as Metric;
            MetricsEditWindow window = new MetricsEditWindow(metric);
            window.EditMetric += Window_EditMetric;
            window.Show();
        }

        private void Window_CreateMetric(Metric metric)
        {
            _metricFile.CreateMetric(metric);
            dataGridMetrics.ItemsSource = _metricFile.GetMetrics();
        }

        private void Window_EditMetric(Metric metric)
        {
            _metricFile.UpdateMetric(metric);
            dataGridMetrics.ItemsSource = _metricFile.GetMetrics();
        }
    }
}
