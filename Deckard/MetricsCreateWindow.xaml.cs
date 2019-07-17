using Cain;
using System;
using System.Windows;

namespace Deckard
{
    /// <summary>
    /// Interaction logic for MetricsCreateWindow.xaml
    /// </summary>
    public partial class MetricsCreateWindow : Window
    {
        public event Action<Metric> CreateMetric;

        public MetricsCreateWindow()
        {
            InitializeComponent();

            LoadMetricFileTypes();
        }

        private void LoadMetricFileTypes()
        {
            comboBoxMetricType.ItemsSource = Enum.GetNames(typeof(MetricType));
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            CreateMetric?.Invoke(new Metric()
            {
                Name = txtName.Text,
                StartingValue = txtStartingValue.Text,
                EndingValue = txtEndingValue.Text,
                Type = (MetricType)comboBoxMetricType.SelectedIndex
            });

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
