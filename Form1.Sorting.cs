using System.Collections;
using System.Windows.Forms;

namespace WinFormsWywal3;

public partial class Form1
{
    private int sortColumn = -1;
    private bool sortAsc = true;

    private void SetInitialSort(int column, bool ascending)
    {
        sortColumn = column; sortAsc = ascending;
        listView.ListViewItemSorter = new ListViewComparer(sortColumn, sortAsc);
        listView.Sort();
        listView.Refresh();
    }

    private void SortByColumn(object? sender, ColumnClickEventArgs e)
    {
        if (e.Column == sortColumn) sortAsc = !sortAsc;
        else { sortColumn = e.Column; sortAsc = true; }

        listView.ListViewItemSorter = new ListViewComparer(sortColumn, sortAsc);
        listView.Sort();
        listView.Refresh();
        FitFlexColumn(); // po sortowaniu dopasuj FLEX
    }

    private sealed class ListViewComparer : IComparer
    {
        private readonly int col; private readonly bool asc;
        public ListViewComparer(int column, bool ascending) { col = column; asc = ascending; }

        public int Compare(object? x, object? y)
        {
            var i1 = (ListViewItem)x!;
            var i2 = (ListViewItem)y!;

            // Kolumny checkboxów: sort po bool (Tag)
            if (col is 1 or 3)
            {
                bool b1 = i1.SubItems[col].Tag is bool v1 && v1;
                bool b2 = i2.SubItems[col].Tag is bool v2 && v2;
                int cmp = b1.CompareTo(b2);
                return asc ? cmp : -cmp;
            }

            // Tekst (case-insensitive)
            string s1 = i1.SubItems[col].Text ?? "";
            string s2 = i2.SubItems[col].Text ?? "";
            int res = string.Compare(s1, s2, System.StringComparison.CurrentCultureIgnoreCase);
            return asc ? res : -res;
        }
    }
}
