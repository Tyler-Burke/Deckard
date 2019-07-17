using Cain;
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

namespace Deckard
{
    public partial class MetricsEditWindow : Window
    {
        private Metric _metric;
        public event Action<Metric> EditMetric;

        public MetricsEditWindow(Metric metric)
        {
            InitializeComponent();

            _metric = metric;

            LoadMetricFileTypes();
            LoadMetric();
        }

        private void LoadMetric()
        {
            if (_metric != null)
            {
                txtName.Text = _metric.Name;
                txtStartingValue.Text = _metric.StartingValue;
                txtEndingValue.Text = _metric.EndingValue;
                comboBoxMetricType.SelectedValue = _metric.Type.ToString();
            }
        }

        private void LoadMetricFileTypes()
        {
            comboBoxMetricType.ItemsSource = Enum.GetNames(typeof(MetricType));
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_metric != null)
            {
                _metric.Name = txtName.Text;
                _metric.StartingValue = txtStartingValue.Text;
                _metric.EndingValue = txtEndingValue.Text;
                _metric.Type = (MetricType)comboBoxMetricType.SelectedIndex;

                EditMetric?.Invoke(_metric);
            }

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
