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
        private int treeViewIndexCount = 0;
        private Dictionary<int, TabItem> treeViewOpenedNodes;
        private List<string> acceptedFileExtensions;
        private MetricFile metricFile;

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
            var enTable = CainLibrary.ConvertDocxToENTable(docxPath);

            entriesDataGrid.ItemsSource = enTable.Rows;
            //txtFirstEntryTime.Text = GetFirstEntryTime("");
            //txtLastEntryTime.Text = GetLastEntryTime("");
        }
        private void EnsureStandardPopupAlignment()
        {
            if (SystemParameters.MenuDropAlignment && _menuDropAlignmentField != null)
            {
                _menuDropAlignmentField.SetValue(null, false);
            }
        }
        private void ListDirectory(TreeView treeView, string path)
        {
            treeView.Items.Clear();
            var rootDirectoryInfo = new DirectoryInfo(path);
            treeView.Items.Add(CreateDirectoryNode(rootDirectoryInfo));
        }
        private IndexedTreeViewItem CreateDirectoryNode(DirectoryInfo directoryInfo)
        {
            if (treeViewIndexCount == 0)
            {
                var directoryNode = new IndexedTreeViewItem { Index = treeViewIndexCount, Header = directoryInfo.Name, Path = directoryInfo.FullName, IsExpanded = true };
                directoryNode.Style = treeViewCaseFolder.Resources["Folder"] as Style;
                treeViewIndexCount++;
                foreach (var directory in directoryInfo.GetDirectories())
                    directoryNode.Items.Add(CreateDirectoryNode(directory));

                directoryNode.MouseDoubleClick += TreeViewItem_MouseDoubleClick;

                return directoryNode;
            }
            else
            {
                var directoryNode = new IndexedTreeViewItem { Index = treeViewIndexCount, Header = directoryInfo.Name, Path = directoryInfo.FullName };
                directoryNode.Style = treeViewCaseFolder.Resources["Folder"] as Style;
                directoryNode.MouseDoubleClick += TreeViewItem_MouseDoubleClick;

                treeViewIndexCount++;

                foreach (var directory in directoryInfo.GetDirectories())
                {
                    directoryNode.Items.Add(CreateDirectoryNode(directory));
                }

                foreach (var file in directoryInfo.GetFiles())
                {
                    if (acceptedFileExtensions.Contains(file.Extension))
                    {
                        var fileNode = new IndexedTreeViewItem { Index = treeViewIndexCount, Header = file.Name, Path = file.FullName };
                        fileNode.Style = treeViewCaseFolder.Resources["File"] as Style;
                        fileNode.MouseDoubleClick += TreeViewItem_MouseDoubleClick;

                        directoryNode.Items.Add(fileNode);
                        treeViewIndexCount++;
                    }
                }

                return directoryNode;
            }
        }
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
            PropertyList pl = new PropertyList();
            pl.Show();
        }
        private void AddMetricButton_Click(object sender, RoutedEventArgs e)
        {
            //Open 'Create Metric' window
        }
        private void RemoveMetricButton_Click(object sender, RoutedEventArgs e)
        {

        }

        #region Testing

        public List<Entry> GetEntries()
        {
            string longCaseNote = "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" + Environment.NewLine +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" + Environment.NewLine + Environment.NewLine +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj" +
                "Testing lore ssfdsdf sdf sdf sdfm sdmf slk ndfklhjs dklfjslkd jflksdjf lksjdlkf jslkdfj";

            return new List<Entry>()
            {
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), longCaseNote),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19004305 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002313 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19004305 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19004305 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
                new Entry("002", new DateTime(2019, 3, 19, 11, 36, 00), "P19002323 Heres the evidence"),
                new Entry("003", new DateTime(2019, 3, 19, 11, 41, 00), "P19002323 Completed photgraphing the evidence"),
                new Entry("001", new DateTime(2019, 3, 19, 11, 32, 00), "P19002323 Started photographing the evidence"),
            };
        }
        public string GetFirstEntryTime(List<Entry> entries)
        {
            if (entries.Count() > 0)
            {
                var firstRow = entries.First();
                if (firstRow.EntryDate.HasValue)
                {
                    return firstRow.EntryDate.Value.ToString("ddMMMyyyy HH:mm").ToUpper();
                }
            }

            return "";
        }
        public string GetLastEntryTime(List<Entry> entries)
        {
            if (entries.Count() > 0)
            {
                var lastRow = entries.Last();
                if (lastRow.EntryDate.HasValue)
                {
                    return lastRow.EntryDate.Value.ToString("ddMMMyyyy HH:mm").ToUpper();
                }
            }

            return "";
        }

        #endregion
    }

    public class Entry
    {
        public string EntryNumber { get; set; }
        public DateTime? EntryDate { get; set; }
        public string EntryContent { get; set; }
        public List<string> PropertyList { get; set; }

        public Entry(string entryNumber, DateTime? entryDate, string entryContent)
        {
            EntryNumber = entryNumber;
            EntryDate = entryDate;
            EntryContent = entryContent;
            var s = entryContent.Split(' ').Select(a => Regex.Match(a, @"P\d{8}").Value).Distinct().ToList();
            s.Remove("");
            s.Remove(" ");
            PropertyList = s;
        }
    }
}