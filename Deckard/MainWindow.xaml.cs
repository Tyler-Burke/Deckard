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

namespace Deckard
{
    public partial class MainWindow : Window
    {
        private readonly FieldInfo _menuDropAlignmentField;
        private Dictionary<int, TabItem> treeViewOpenedNodes;
        private List<string> acceptedFileExtensions;
        private MetricFile metricFile;
        private const string DATE_FORMAT = "ddMMMyyyy HH:mm";
        private ENTable currentENTable;
        private List<string> currentFilteredProperties;

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
            mainWindow.Title += " - Case Notes For " + caseName;

            //Instantiate Tree View Opened Nodes List
            treeViewOpenedNodes = new Dictionary<int, TabItem>();

            //Create Tree View
            ListDirectory(treeViewCaseFolder, path);

            //Open Case Notes Tab and Populate Controls
            tabControlMainContent.Visibility = Visibility.Visible;
            lblIncidentNumber.Text = caseName;
            string docxPath = caseFolder.GetFiles().Where(a => a.Extension == ".docx").First().FullName;
            currentENTable = CainLibrary.ConvertDocxToENTable(docxPath);
            currentFilteredProperties = currentENTable.GetDistinctPropertyNumber();

            entriesDataGrid.ItemsSource = currentENTable.Rows;
            txtFirstEntryTime.Text = currentENTable.GetStartTime().ToString(DATE_FORMAT).ToUpper();
            txtLastEntryTime.Text = currentENTable.GetEndTime().ToString(DATE_FORMAT).ToUpper();
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
        private void FillTabContent(TabItem tabItem, string filePath)
        {
            WebBrowser webBrowser = new WebBrowser();
            webBrowser.Navigate(filePath);
            tabItem.Content = webBrowser;
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
                IndexedTreeViewItem item = sender as IndexedTreeViewItem;

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
                        TabItem tabItem = new TabItem() { Header = item.Header };
                        FillTabContent(tabItem, item.Path);
                        tabItem.MouseRightButtonUp += TabItemMainContent_MouseRightButtonUp;
                        tabControlMainContent.Items.Add(tabItem);
                        tabItem.Focus();

                        treeViewOpenedNodes.Add(item.Index, tabItem);
                    }                
                }
            }
        }
        private void TabItemMainContent_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            TabItem tabItem = sender as TabItem;
            tabControlMainContent.Items.Remove(tabItem);
            treeViewOpenedNodes.Remove(treeViewOpenedNodes.SingleOrDefault(a => a.Value == tabItem).Key);
        }
        private void FilterProperties_Click(object sender, RoutedEventArgs e)
        {
            PropertyList pl = new PropertyList(currentENTable, currentFilteredProperties);
            pl.Show();
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