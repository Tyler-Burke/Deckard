using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Deckard
{
    public class IndexedTreeView : TreeView
    {
        private List<IndexedTreeViewItem> _collection;
        private int _currentIndex = 0;
        public IndexedTreeView() : base()
        {
            _collection = new List<IndexedTreeViewItem>();
        }

        public IndexedTreeViewItem RegisterChildNode(IndexedTreeViewItem indexedTreeViewItem)
        {
            indexedTreeViewItem.Index = _currentIndex++;
            return indexedTreeViewItem;
        }

        public void AddToCollection(IndexedTreeViewItem indexedTreeViewItem) => _collection.Add(indexedTreeViewItem);
        public IndexedTreeViewItem FindItemByPath(string path) => _collection.SingleOrDefault(a => a.Path == path);
    }
}
