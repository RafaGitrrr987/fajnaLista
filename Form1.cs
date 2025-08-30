using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsWywal3
{
    // Główna część klasy – inicjalizacja, dane, zdarzenia, logika (bez rysowania i bez FLEX-a)
    public partial class Form1 : Form
    {
        private SmoothListView listView = null!; // przypiszemy w InitializeListView
        private int sortColumn = -1;
        private bool sortAsc = true;
        private int hoverIndex = -1;

        public Form1()
        {
            AutoScaleMode = AutoScaleMode.Font;
            Font = CreateModernFont(10f); // ← w Form1.Rendering.cs
            Text = "ListView – nowoczesny wygląd (checkboxy, sort, zebra, hover)";
            ClientSize = new Size(900, 540);
            StartPosition = FormStartPosition.CenterScreen;

            InitializeListView();

            Shown += (_, __) =>
            {
                WarmUpCheckBoxRenderer();  // ← w Form1.Rendering.cs
                FitFlexColumn();           // ← w Form1.Flex.cs
                listView.Invalidate(true);
            };
        }

        private void InitializeListView()
        {
            listView = new SmoothListView
            {
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = false,
                OwnerDraw = true,
                Dock = DockStyle.Fill,
                HideSelection = false,
                UseCompatibleStateImageBehavior = false,
                AllowColumnReorder = true
            };

            // Kolumny
            listView.Columns.Add("ID", 140, HorizontalAlignment.Left); // 0
            listView.Columns.Add("Aktywny", 120, HorizontalAlignment.Left); // 1 (checkbox)
            listView.Columns.Add("Nazwa", 360, HorizontalAlignment.Left); // 2 (FLEX)
            listView.Columns.Add("Zaznaczony", 140, HorizontalAlignment.Left); // 3 (checkbox)

            // Ten symbol jest zdefiniowany w Form1.Flex.cs – ale to ta sama klasa (partial),
            // więc możemy go ustawić tutaj bez problemu:
            colNazwa = listView.Columns[2];

            // Dane testowe
            listView.BeginUpdate();
            foreach (var row in SampleData.GetRows())
                listView.Items.Add(CreateRow(row.Id, row.Nazwa, row.Aktywny, row.Zaznaczony));
            listView.EndUpdate();

            // Rysowanie – metody w Form1.Rendering.cs
            listView.DrawColumnHeader += DrawColumnHeaderWithSortGlyph;
            listView.DrawSubItem += DrawSubItemStyled;

            // Interakcje
            listView.MouseClick += HandleClick;
            listView.ColumnClick += SortByColumn;
            listView.MouseMove += ListView_MouseMove;
            listView.MouseLeave += (_, __) =>
            {
                if (hoverIndex != -1) { int i = hoverIndex; hoverIndex = -1; InvalidateRow(i); }
            };

            // FLEX kolumny „Nazwa”
            listView.Resize += (_, __) => FitFlexColumn();
            listView.ColumnWidthChanged += (_, __) => FitFlexColumn();
            listView.ColumnReordered += (_, __) => BeginInvoke((MethodInvoker)(() => FitFlexColumn()));

            Controls.Add(listView);

            // Domyślne sortowanie + pierwsze dopasowanie
            SetInitialSort(0, true);
            FitFlexColumn();
        }

        // Fabryka wiersza
        private static ListViewItem CreateRow(string id, string nazwa, bool aktywny, bool zaznaczony)
        {
            var item = new ListViewItem(id);
            item.SubItems.Add("");      // Aktywny (checkbox)
            item.SubItems.Add(nazwa);   // Nazwa
            item.SubItems.Add("");      // Zaznaczony (checkbox)
            item.SubItems[1].Tag = aktywny;
            item.SubItems[3].Tag = zaznaczony;
            return item;
        }

        // Hover
        private void ListView_MouseMove(object? sender, MouseEventArgs e)
        {
            var hit = listView.HitTest(e.Location);
            int newIndex = hit.Item?.Index ?? -1;
            if (newIndex == hoverIndex) return;
            int old = hoverIndex;
            hoverIndex = newIndex;
            InvalidateRow(old);
            InvalidateRow(hoverIndex);
        }

        private void InvalidateRow(int index)
        {
            if (index < 0 || index >= listView.Items.Count) return;
            var rect = listView.Items[index].GetBounds(ItemBoundsPortion.Entire);
            listView.Invalidate(rect);
        }

        private void HandleClick(object? sender, MouseEventArgs e)
        {
            var hit = listView.HitTest(e.Location);
            if (hit.Item == null || hit.SubItem == null) return;

            int col = hit.Item.SubItems.IndexOf(hit.SubItem);
            if (col == 1 || col == 3)
            {
                bool cur = hit.SubItem.Tag is bool b && b;
                hit.SubItem.Tag = !cur;
                listView.Invalidate(hit.SubItem.Bounds);
            }
        }

        // Sortowanie
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
            FitFlexColumn();
        }

        private sealed class ListViewComparer : IComparer
        {
            private readonly int col; private readonly bool asc;
            public ListViewComparer(int column, bool ascending) { col = column; asc = ascending; }

            public int Compare(object? x, object? y)
            {
                var i1 = (ListViewItem)x!;
                var i2 = (ListViewItem)y!;

                if (col == 1 || col == 3)
                {
                    bool b1 = i1.SubItems[col].Tag is bool v1 && v1;
                    bool b2 = i2.SubItems[col].Tag is bool v2 && v2;
                    int cmp = b1.CompareTo(b2);
                    return asc ? cmp : -cmp;
                }

                string s1 = i1.SubItems[col].Text ?? "";
                string s2 = i2.SubItems[col].Text ?? "";
                int res = string.Compare(s1, s2, StringComparison.CurrentCultureIgnoreCase);
                return asc ? res : -res;
            }
        }

        // ListView z włączonym podwójnym buforowaniem
        private sealed class SmoothListView : ListView
        {
            public SmoothListView()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }
        }
    }
}
