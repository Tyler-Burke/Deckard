using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinForms = System.Windows.Forms;
using Cain;
using System.Windows.Media;
using System.Windows.Data;

namespace Deckard
{
    public partial class MainWindow : Window
    {
        private readonly FieldInfo _menuDropAlignmentField;
        private Dictionary<int, TabItem> treeViewOpenedNodes;
        private List<string> acceptedFileExtensions;
        private MetricFile metricFile;
        private const string DATE_FORMAT = "ddMMMyyyy HH:mm";

        public MainWindow()
        {
            Initialize();

            _menuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
            System.Diagnostics.Debug.Assert(_menuDropAlignmentField != null);
        }
        public MainWindow(string path)
        {
            Initialize();

            _menuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
            System.Diagnostics.Debug.Assert(_menuDropAlignmentField != null);

            OpenCaseFolder(path);
        }

        /// <summary>
        /// Initialize the MainWindow and global properties
        /// </summary>
        private void Initialize()
        {
            InitializeComponent();

            //Forces menu bar to open tabs inside application window
            EnsureStandardPopupAlignment();
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;

            //Instantiate Accepted File Extensions and fill
            acceptedFileExtensions = new List<string>();
            foreach (var extension in ConfigurationManager.AppSettings["acceptedFileExtensions"].Split(';'))
            {
                acceptedFileExtensions.Add(extension);
            }

            //Load Metric File
            string applicationFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            metricFile = new MetricFile(applicationFolderPath + @"Metrics.json");
        }
        /// <summary>
        /// Removes the preset pop-up alignment from left aligned and changes to default right aligned
        /// </summary>
        private void EnsureStandardPopupAlignment()
        {
            if (SystemParameters.MenuDropAlignment && _menuDropAlignmentField != null)
            {
                _menuDropAlignmentField.SetValue(null, false);
            }
        }
        /// <summary>
        /// Open and display the selected folder and contents
        /// </summary>
        /// <param name="path"></param>
        private void OpenCaseFolder(string path)
        {
            DirectoryInfo caseFolder = new DirectoryInfo(path);
            string caseName = caseFolder.Name;
            mainWindow.Title += " - " + caseName;
            lblIncidentNumber.Text = caseName;

            //Instantiate Tree View Opened Nodes List
            treeViewOpenedNodes = new Dictionary<int, TabItem>();

            //Create Tree View
            ListDirectory(treeViewCaseFolder, path);

            //Open Case Notes Tab and Populate Controls
            string docxPath = caseFolder.GetFiles().Where(a => a.Extension == ".docx").FirstOrDefault().FullName;
            if (docxPath != null)
            {
                var tabItem = treeViewCaseFolder.FindItemByPath(docxPath);
                tabItem.IsSelected = true;
                OpenTabItem(tabItem);
            }
        }
        /// <summary>
        /// Populate an IndexedTreeView with the contents of a system directory
        /// </summary>
        /// <param name="treeView">The IndexedTreeView object to be populated</param>
        /// <param name="path">The path to the system directory to populate the tree with</param>
        private void ListDirectory(IndexedTreeView treeView, string path)
        {
            treeView.Items.Clear();
            DirectoryInfo rootDirectoryInfo = new DirectoryInfo(path);
            IndexedTreeViewItem rootDirectoryNode = CreateDirectoryNode(rootDirectoryInfo);
            rootDirectoryNode.IsExpanded = true;
            treeView.Items.Add(rootDirectoryNode);
        }
        /// <summary>
        /// Recursively goes into system directory tree and populates an IndexedTreeViewItem object with the directory's contents
        /// </summary>
        /// <param name="directory">Directory that will be used to populate IndexedTreeViewItem</param>
        /// <returns></returns>
        private IndexedTreeViewItem CreateDirectoryNode(DirectoryInfo directory)
        {
            IndexedTreeViewItem directoryNode = new IndexedTreeViewItem { Header = directory.Name, Path = directory.FullName };
            directoryNode.Style = treeViewCaseFolder.Resources["Folder"] as Style;
            directoryNode.MouseDoubleClick += TreeViewItem_MouseDoubleClick;

            //Register with tree (adds index)
            treeViewCaseFolder.RegisterChildNode(directoryNode);

            foreach (var subDirectory in directory.GetDirectories())
            {
                IndexedTreeViewItem newNode = CreateDirectoryNode(subDirectory);
                treeViewCaseFolder.AddToCollection(newNode);
                directoryNode.Items.Add(newNode);
            }

            foreach (var file in directory.GetFiles())
            {
                if (acceptedFileExtensions.Contains(file.Extension))
                {
                    IndexedTreeViewItem fileNode = new IndexedTreeViewItem { Header = file.Name, Path = file.FullName };
                    fileNode.Style = treeViewCaseFolder.Resources["File"] as Style;
                    fileNode.MouseDoubleClick += TreeViewItem_MouseDoubleClick;
                    treeViewCaseFolder.RegisterChildNode(fileNode);
                    treeViewCaseFolder.AddToCollection(fileNode);
                    directoryNode.Items.Add(fileNode);
                }
            }

            return directoryNode;
        }
        /// <summary>
        /// Populates a TabItem with an embedded web browser to view the specified file
        /// </summary>
        /// <param name="tabItem">The TabItem object to be populated</param>
        /// <param name="filePath">The path to the file to be opened</param>
        private void OpenTabItem(IndexedTreeViewItem item)
        {
            if (item.IsSelected && item.Index != 0 && !treeViewOpenedNodes.Keys.Contains(item.Index))
            {
                string[] splitFileName = item.Header.ToString().Split('.');
                string extension = ".";

                if (splitFileName.Length > 1)
                {
                    extension += splitFileName[1];
                }

                //Create tab item if the tab is an accepted file extension
                if (acceptedFileExtensions != null && acceptedFileExtensions.Contains(extension))
                {
                    switch (extension)
                    {
                        case ".docx":
                            CaseNotesTabItem caseNotesTabItem = new CaseNotesTabItem() { Header = item.Header };
                            caseNotesTabItem.MouseRightButtonUp += TabItemMainContent_MouseRightButtonUp;
                            caseNotesTabItem = PopulateCaseNotesTab(caseNotesTabItem, item.Path);
                            tabControlMainContent.Items.Add(caseNotesTabItem);
                            caseNotesTabItem.Focus();
                            treeViewOpenedNodes.Add(item.Index, caseNotesTabItem);
                            break;
                        default:
                            TabItem tabItem = new TabItem() { Header = item.Header };
                            tabItem.MouseRightButtonUp += TabItemMainContent_MouseRightButtonUp;
                            tabItem = PopulateFileTab(tabItem, item.Path);
                            tabControlMainContent.Items.Add(tabItem);
                            tabItem.Focus();
                            treeViewOpenedNodes.Add(item.Index, tabItem);
                            break;
                    }                      
                }
            }
        }

        private CaseNotesTabItem PopulateCaseNotesTab(CaseNotesTabItem tabItem, string path)
        {
            tabItem.ENTable = CainLibrary.ConvertDocxToENTable(path);
            txtFirstEntryTime.Text = tabItem.ENTable.GetStartTime().ToString(DATE_FORMAT).ToUpper();
            txtLastEntryTime.Text = tabItem.ENTable.GetEndTime().ToString(DATE_FORMAT).ToUpper();

            //Build Grid
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = GridLength.Auto
            });
            grid.RowDefinitions.Add(new RowDefinition()
            {
                Height = GridLength.
            });

            //Build Tab Item's content
            DataGrid dataGrid = new DataGrid()
            {
                ItemsSource = tabItem.ENTable.Rows,
                AutoGenerateColumns = false,
                IsReadOnly = true,
                SelectionUnit = DataGridSelectionUnit.Cell,
                RowBackground = (Brush)new BrushConverter().ConvertFrom("#FF807878"),
                AlternationCount = 2,
                AlternatingRowBackground = (Brush)new BrushConverter().ConvertFrom("#FF6C6C6C"),
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                Background = (Brush)new BrushConverter().ConvertFrom("#FF6C6C6C")
            };
            ScrollViewer.SetCanContentScroll(dataGrid, true);
            Grid.SetColumn(dataGrid, 0);
            Grid.SetRow(dataGrid, 0);

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Entry Number",
                Width = 150,
                Binding = new Binding("EntryNumber"),
                ElementStyle = tabControlMainContent.FindResource("ColumnElementStyle") as Style,
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Entry Number",
                Width = 200,
                Binding = new Binding("EntryDateTime"),
                ElementStyle = tabControlMainContent.FindResource("ColumnElementStyle") as Style
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Entry Number",
                Width = DataGridLength.Auto,
                Binding = new Binding("EntryContent"),
                ElementStyle = tabControlMainContent.FindResource("ColumnElementStyle") as Style
            });

            //grid.Children.Add(dataGrid);
            TestTab.Children.Add(dataGrid);
            //tabItem.Content = grid;

            return tabItem;
        }
        private TabItem PopulateFileTab(TabItem tabItem, string path)
        {
            WebBrowser webBrowser = new WebBrowser();
            webBrowser.Navigate(path);
            tabItem.Content = webBrowser;
            return tabItem;
        }
        private TabItem PopulateGalleryTab(TabItem tabItem, string path) { throw new NotImplementedException(); }
        private ScrollViewer GetScrollViewer(UIElement element)
        {
            if (element == null) return null;

            ScrollViewer retour = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element) && retour == null; i++)
            {
                if (VisualTreeHelper.GetChild(element, i) is ScrollViewer)
                {
                    retour = (ScrollViewer)(VisualTreeHelper.GetChild(element, i));
                }
                else
                {
                    retour = GetScrollViewer(VisualTreeHelper.GetChild(element, i) as UIElement);
                }
            }
            return retour;
        }

        private void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            EnsureStandardPopupAlignment();
        }
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            using (WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog())
            {
                var result = folderDialog.ShowDialog();
                if (result == WinForms.DialogResult.OK)
                {
                    OpenCaseFolder(folderDialog.SelectedPath);
                }
            }
        }
        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (sender is IndexedTreeViewItem item)
                {
                    OpenTabItem(item);
                }
            }
        }
        private void TabItemMainContent_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            TabItem tabItem = sender as TabItem;
            tabControlMainContent.Items.Remove(tabItem);
            treeViewOpenedNodes.Remove(treeViewOpenedNodes.SingleOrDefault(a => a.Value == tabItem).Key);
        }
        private void AddMetricButton_Click(object sender, RoutedEventArgs e)
        {
            //Open 'Create Metric' window
        }
        private void RemoveMetricButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}