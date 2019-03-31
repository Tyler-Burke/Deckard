using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Deckard
{
    public partial class PropertyList : Window
    {
        private List<Entry> entries;
        private MainWindow mainWindow;

        public PropertyList()
        {
            InitializeComponent();
        }

        private void WindowPropertyList_Initialized(object sender, System.EventArgs e)
        {
            //Freeze Main Window
            mainWindow = ((MainWindow)Application.Current.MainWindow);
            mainWindow.IsEnabled = false;

            entries = mainWindow.GetEntries();

            List<string> propertiesTest = new List<string>();
            foreach (var b in entries)
                foreach (var c in b.PropertyList)
                    if (!propertiesTest.Contains(c))
                        propertiesTest.Add(c);

            listViewProperties.ItemsSource = propertiesTest;
        }

        private List<Entry> FilterEntries(List<string> propertyListToFilterBy)
        {
            //Return Entries that have content
            return entries.Where(a => a.PropertyList.Intersect(propertyListToFilterBy).Any()).ToList();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> properties = new List<string>();
            foreach (var item in listViewProperties.SelectedItems)
                properties.Add((string)item);
            mainWindow.entriesDataGrid.ItemsSource = FilterEntries(properties);
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
