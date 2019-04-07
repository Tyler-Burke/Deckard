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
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

namespace Deckard
{
    public partial class MainWindow : Window
    {
        private readonly FieldInfo _menuDropAlignmentField;
        private Dictionary<int, TabItem> treeViewOpenedNodes;
        private List<string> acceptedFileExtensions;
        private MetricFile metricFile;
        private const string DATE_FORMAT = "ddMMMyyyy HH:mm";
        private const string DEFAULT_CASE_PATH = @"E:";
        private Case rootCase;

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
            //Instantiate Tree View Opened Nodes List
            treeViewOpenedNodes = new Dictionary<int, TabItem>();

            Case caseFolder = CainLibrary.ConvertPathToCase(path);

            mainWindow.Title = "Deckard Cain";
            mainWindow.Title += " - " + caseFolder.CaseNumber;
            headerStackPanel.Visibility = Visibility.Visible;
            lblIncidentNumber.Text = caseFolder.CaseNumber;

            CreateCaseFolderTreeView(caseFolder);            
        }

        private void CreateCaseFolderTreeView(Case caseFolder)
        {
            treeViewCaseFolder.Items.Clear();
            IndexedTreeViewItem rootDirectoryNode = CreateCaseFolderTreeViewItem(caseFolder.RootCaseDirectory);
            rootDirectoryNode.IsExpanded = true;
            treeViewCaseFolder.Items.Add(rootDirectoryNode);
        }
        private IndexedTreeViewItem CreateCaseFolderTreeViewItem(CaseDirectory caseDirectory)
        {
            IndexedTreeViewItem directoryNode = new IndexedTreeViewItem(caseDirectory)
            {
                Header = caseDirectory.DirectoryName,
                Style = treeViewCaseFolder.Resources["Folder"] as Style
            };
            directoryNode.MouseRightButtonUp += DirectoryNode_MouseRightButtonUp;
            ContextMenu contextMenu = new ContextMenu() { PlacementTarget = directoryNode };
            MenuItem menuItem = new MenuItem
            {
                Header = "Open as Gallery"
            };
            menuItem.Click += MenuItem_Click;
            contextMenu.Items.Add(menuItem);
            directoryNode.ContextMenu = contextMenu;

            //Register with tree (adds index)
            treeViewCaseFolder.RegisterChildNode(directoryNode);

            foreach (var subDirectory in caseDirectory.CaseDirectories)
            {
                IndexedTreeViewItem newNode = CreateCaseFolderTreeViewItem(subDirectory);
                treeViewCaseFolder.AddToCollection(newNode);
                directoryNode.Items.Add(newNode);
            }

            foreach (var file in caseDirectory.CaseFiles)
            {
                if (acceptedFileExtensions.Contains(file.Extension))
                {
                    IndexedTreeViewItem fileNode = new IndexedTreeViewItem(file)
                    {
                        Header = file.FileName,
                        Style = treeViewCaseFolder.Resources["File"] as Style
                    };
                    fileNode.MouseDoubleClick += FileNode_MouseDoubleClick;
                    fileNode.MouseRightButtonUp += FileNode_MouseRightButtonUp;
                    treeViewCaseFolder.RegisterChildNode(fileNode);
                    treeViewCaseFolder.AddToCollection(fileNode);
                    directoryNode.Items.Add(fileNode);
                }
            }

            return directoryNode;
        }

        /// <summary>
        /// Populate an IndexedTreeView with the contents of a system directory
        /// </summary>
        /// <param name="treeView">The IndexedTreeView object to be populated</param>
        /// <param name="path">The path to the system directory to populate the tree with</param>
        /*private void ListDirectory(IndexedTreeView treeView, string path)
        {
            treeView.Items.Clear();
            DirectoryInfo rootDirectoryInfo = new DirectoryInfo(path);
            IndexedTreeViewItem rootDirectoryNode = CreateDirectoryNode(rootDirectoryInfo);
            rootDirectoryNode.IsExpanded = true;
            treeView.Items.Add(rootDirectoryNode);
        }*/
        /// <summary>
        /// Recursively goes into system directory tree and populates an IndexedTreeViewItem object with the directory's contents
        /// </summary>
        /// <param name="directory">Directory that will be used to populate IndexedTreeViewItem</param>
        /// <returns></returns>
        /*private IndexedTreeViewItem CreateDirectoryNode(DirectoryInfo directory)
        {
            IndexedTreeViewItem directoryNode = new IndexedTreeViewItem { Header = directory.Name, Path =  directory.FullName, FileType = FileType.Directory };
            directoryNode.Style = treeViewCaseFolder.Resources["Folder"] as Style;
            directoryNode.MouseRightButtonUp += DirectoryNode_MouseRightButtonUp;
            ContextMenu contextMenu = new ContextMenu() { PlacementTarget = directoryNode };
            MenuItem menuItem = new MenuItem
            {
                Header = "Open as Gallery"
            };
            menuItem.Click += MenuItem_Click;
            contextMenu.Items.Add(menuItem);
            directoryNode.ContextMenu = contextMenu;

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
                    IndexedTreeViewItem fileNode = new IndexedTreeViewItem { Header = file.Name, Path = file.FullName, FileType = FileType.File };
                    fileNode.Style = treeViewCaseFolder.Resources["File"] as Style;
                    fileNode.MouseDoubleClick += FileNode_MouseDoubleClick;
                    fileNode.MouseRightButtonUp += FileNode_MouseRightButtonUp;
                    treeViewCaseFolder.RegisterChildNode(fileNode);
                    treeViewCaseFolder.AddToCollection(fileNode);
                    directoryNode.Items.Add(fileNode);
                }
            }

            return directoryNode;
        }

        */

        /// <summary>
        /// Creates a new TabItem and populates the tab basesd on the contents of the parameter IndexedTreeViewItem
        /// </summary>
        /// <param name="item">The IndexedTreeViewItem to be used for context</param>
        /// 
        private void OpenFileTabItem(IndexedTreeViewItem item)
        {
            if (item.FileType == FileType.File)
            {
                if (item.IsSelected && item.Index != 0 && !treeViewOpenedNodes.Keys.Contains(item.Index))
                {
                    //Create tab item if the tab is an accepted file extension
                    if (acceptedFileExtensions != null && acceptedFileExtensions.Contains(item.CaseFile.Extension))
                    {
                        switch (item.CaseFile.Extension)
                        {
                            case ".docx":
                                if (item.CaseFile != null && item.CaseFile.CaseNotes != null)
                                {
                                    CaseNotesTabItem caseNotesTabItem = CreateCaseNotesTab(item.CaseFile);                                    
                                    tabControlMainContent.Items.Add(caseNotesTabItem);
                                    caseNotesTabItem.Focus();
                                    treeViewOpenedNodes.Add(item.Index, caseNotesTabItem);
                                }                                
                                break;
                            default:
                                TabItem tabItem = PopulateFileTab(item.CaseFile);
                                tabControlMainContent.Items.Add(tabItem);
                                tabItem.Focus();
                                treeViewOpenedNodes.Add(item.Index, tabItem);
                                break;
                        }
                    }
                }
            }

        }
        private void OpenDirectoryTabItem(IndexedTreeViewItem item)
        {
            if (item.FileType == FileType.Directory && item.CaseDirectory != null)
            {
                TabItem tabItem = PopulateGalleryTab(item.CaseDirectory);
                tabControlMainContent.Items.Add(tabItem);
                tabItem.Focus();
                treeViewOpenedNodes.Add(item.Index, tabItem);
            }
        }

        private CaseNotesTabItem CreateCaseNotesTab(CaseFile caseFile)
        {
            CaseNotesTabItem tabItem = new CaseNotesTabItem
            {
                Header = caseFile.FileName
            };
            tabItem.MouseRightButtonUp += TabItemMainContent_MouseRightButtonUp;
            tabItem.CaseNotes = caseFile.CaseNotes;
            txtFirstEntryTime.Text = caseFile.CaseNotes.GetStartTime().ToString(DATE_FORMAT).ToUpper();
            txtLastEntryTime.Text = caseFile.CaseNotes.GetEndTime().ToString(DATE_FORMAT).ToUpper();

            //Build Grid
            Grid grid = new Grid();
            
            //Columns
            grid.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(10)
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = GridLength.Auto
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(10)
            });

            //Rows
            grid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(10)
            });
            grid.RowDefinitions.Add(new RowDefinition()
            {
                Height = GridLength.Auto
            });
            grid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(1, GridUnitType.Star)
            });
            grid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(10)
            });

            //Build Tab Item's content
            DataGrid dataGrid = new DataGrid()
            {
                ItemsSource = caseFile.CaseNotes.Rows,
                AutoGenerateColumns = false,
                IsReadOnly = true,
                SelectionUnit = DataGridSelectionUnit.Cell,
                RowBackground = (Brush)new BrushConverter().ConvertFrom("#FF807878"),
                AlternationCount = 2,
                AlternatingRowBackground = (Brush)new BrushConverter().ConvertFrom("#FF6C6C6C"),
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                Background = (Brush)new BrushConverter().ConvertFrom("#FF6C6C6C")
            };
            ScrollViewer.SetCanContentScroll(dataGrid, false);
            Grid.SetColumn(dataGrid, 1);
            Grid.SetColumnSpan(dataGrid, 2);
            Grid.SetRow(dataGrid, 1);
            Grid.SetRowSpan(dataGrid, 3);

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
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                Binding = new Binding("EntryContent"),
                ElementStyle = tabControlMainContent.FindResource("ColumnElementStyle") as Style
            });

            grid.Children.Add(dataGrid);
            tabItem.Content = grid;
            TextBlock.SetTextAlignment(tabItem, TextAlignment.Left);

            return tabItem;
        }
        private TabItem PopulateFileTab(CaseFile caseFile)
        {
            TabItem tabItem = new TabItem
            {
                Header = caseFile.FileName
            };
            tabItem.MouseRightButtonUp += TabItemMainContent_MouseRightButtonUp;

            WebBrowser webBrowser = new WebBrowser();
            webBrowser.Navigate(caseFile.RootPath);
            tabItem.Content = webBrowser;
            return tabItem;
        }
        private TabItem PopulateGalleryTab(CaseDirectory directory)
        {
            TabItem tabItem = new TabItem()
            {
                Header = directory.DirectoryName
            };
            tabItem.MouseRightButtonUp += TabItemMainContent_MouseRightButtonUp;

            ListBox listBox = new ListBox();
            ScrollViewer.SetHorizontalScrollBarVisibility(listBox, ScrollBarVisibility.Disabled);

            var d = new FrameworkElementFactory(typeof(UniformGrid));
            d.SetValue(UniformGrid.ColumnsProperty, 3);

            listBox.ItemsPanel = new ItemsPanelTemplate
            {
                VisualTree = d
            };

            listBox.ItemsSource = directory.Images;

            tabItem.Content = listBox;

            return tabItem;
        }

        //Events
        private void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            EnsureStandardPopupAlignment();
        }
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            using (WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog())
            {
                folderDialog.RootFolder = Environment.SpecialFolder.MyComputer;
                folderDialog.SelectedPath = DEFAULT_CASE_PATH;
                var result = folderDialog.ShowDialog();
                if (result == WinForms.DialogResult.OK)
                {
                    OpenCaseFolder(folderDialog.SelectedPath);
                }
            }
        }
        private void TabItemMainContent_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is TabItem item && sender is TabItem)
            {
                tabControlMainContent.Items.Remove(item);
                treeViewOpenedNodes.Remove(treeViewOpenedNodes.SingleOrDefault(a => a.Value == item).Key);
            }
        }
        private void DirectoryNode_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is IndexedTreeViewItem item)
            {
                if (item.FileType == FileType.Directory)
                {
                    item.ContextMenu.IsOpen = true;
                    e.Handled = true;
                }
            }
        }
        private void FileNode_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is IndexedTreeViewItem item)
            {
                if (item.FileType == FileType.File)
                {
                    e.Handled = true;
                }
            }
        }
        private void FileNode_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (sender is IndexedTreeViewItem item)
                {
                    OpenFileTabItem(item);
                }
            }
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                if (item.Parent is ContextMenu contextMenu)
                {
                    var indexItem = (IndexedTreeViewItem)contextMenu.PlacementTarget;
                    OpenDirectoryTabItem(indexItem);
                }
            }
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