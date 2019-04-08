using System.Collections.Generic;
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
    }
}
