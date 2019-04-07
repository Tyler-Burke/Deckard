using Cain;
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
        public FileType FileType { get; set; }
        public CaseDirectory CaseDirectory { get; set; }
        public CaseFile CaseFile { get; set; }

        public IndexedTreeViewItem(CaseDirectory caseDirectory) : base()
        {
            FileType = FileType.Directory;
            CaseDirectory = caseDirectory;
        }
        public IndexedTreeViewItem(CaseFile caseFile) : base()
        {
            FileType = FileType.File;
            CaseFile = caseFile;
        }
    }
}
