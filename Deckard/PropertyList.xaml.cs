using Cain;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Deckard
{
    public partial class PropertyList : Window
    {
        private ENTable _enTable;
        private MainWindow mainWindow;
        private List<string> _currentFilteredProperties;

        public PropertyList(ENTable enTable, List<string> currentFilteredProperties)
        {
            _enTable = enTable;
            _currentFilteredProperties = currentFilteredProperties;

            InitializeComponent();
        }

        private void WindowPropertyList_Initialized(object sender, System.EventArgs e)
        {
            //Freeze Main Window
            mainWindow = ((MainWindow)Application.Current.MainWindow);
            mainWindow.IsEnabled = false;

            listViewProperties.SelectedItems.Clear();
            var disctinctPropertyNumbers = _enTable.GetDistinctPropertyNumber();
            listViewProperties.ItemsSource = disctinctPropertyNumbers;

            foreach (var item in _currentFilteredProperties)
            {
                listViewProperties.SelectedItems.Add(item);
            }            

            listViewProperties.Focus();
        }   

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (listViewProperties.SelectedItems.Count > 0)
            {
                List<string> selectedItems = new List<string>();
                foreach (var item in listViewProperties.SelectedItems)
                {
                    selectedItems.Add(item.ToString());
                }
                mainWindow.currentFilteredProperties = selectedItems;
            }

            mainWindow.entriesDataGrid.ItemsSource = _enTable.FilterByProperty(_currentFilteredProperties);
            CloseWindow();
        }

        private void WindowPropertyList_Closed(object sender, System.EventArgs e)
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            mainWindow.IsEnabled = true;
            Close();
        }
    }
}
