using System.Windows.Controls;

namespace Deckard
{
    public class IndexedTreeViewItem : TreeViewItem
    {
        public int Index { get; set; }
        public string Path { get; set; }
        public IndexedTreeViewItem() : base() { }
    }
}
