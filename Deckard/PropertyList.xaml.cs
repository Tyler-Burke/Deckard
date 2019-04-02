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

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (var property in _enTable.GetDistinctPropertyNumber())
            {
                if (_currentFilteredProperties.Contains(property))
                    items.Add(new ListViewItem() { Content = property, IsSelected = true });
                else
                    items.Add(new ListViewItem() { Content = property });
            }

            listViewProperties.ItemsSource = items;

            listViewProperties.Focus();
        }   

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedItem = "";
            if (listViewProperties.SelectedItems.Count > 0)
            {
                ListViewItem firstItem = listViewProperties.SelectedItems[0] as ListViewItem;
                selectedItem = firstItem.Content.ToString();
            }
            
            mainWindow.entriesDataGrid.ItemsSource = _enTable.FilterByProperty(selectedItem);
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
