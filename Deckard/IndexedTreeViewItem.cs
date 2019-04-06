using System.Windows.Controls;

namespace Deckard
{
    public enum FileType
    {
        Directory = 1,
        File = 2
    }

    public class IndexedTreeViewItem : TreeViewItem
    {
        public int Index { get; set; }
        public string Path { get; set; }
        public FileType FileType { get; set; }

        public IndexedTreeViewItem() : base() { }
    }
}
