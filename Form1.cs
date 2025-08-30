using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsWywal3
{
    // Główna część klasy – inicjalizacja, dane, zdarzenia, logika (bez rysowania).
    public partial class Form1 : Form
    {
        // Uwaga: to jest partial – druga część (Rendering) ma metody rysujące i paletę kolorów.
        private SmoothListView listView = null!; // przypiszemy w InitializeListView
        private int sortColumn = -1;
        private bool sortAsc = true;
        private int hoverIndex = -1;

        // „FLEX” dla kolumny „Nazwa”
        private ColumnHeader? colNazwa;
        private bool _fitting = false; // strażnik przed rekurencją przy zmianie szerokości

        public Form1()
        {
            // Ustawienia ogólne formy (to robił wcześniej Designer/.resx)
            AutoScaleMode = AutoScaleMode.Font;
            Font = CreateModernFont(10f); // ← metoda jest w Form1.Rendering.cs (partial)
            Text = "ListView – nowoczesny wygląd (checkboxy, sort, zebra, hover)";
            ClientSize = new Size(900, 540);
            StartPosition = FormStartPosition.CenterScreen;

            InitializeListView();

            // Po pokazaniu okna: rozgrzej renderer, dopasuj FLEX i odśwież
            Shown += (_, __) =>
            {
                WarmUpCheckBoxRenderer();  // ← w Rendering.cs
                FitFlexColumn();
                listView.Invalidate(true);
            };
        }

        // Inicjalizacja ListView + dane + podpięcie zdarzeń
        private void InitializeListView()
        {
            listView = new SmoothListView
            {
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = false,     // checkboxy rysujemy sami w subitemach (Rendering.cs)
                OwnerDraw = true,       // własne rysowanie (Rendering.cs)
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
            colNazwa = listView.Columns[2];

            // Dane z osobnej klasy (prosty provider testowy)
            listView.BeginUpdate();
            foreach (var row in SampleData.GetRows())
                listView.Items.Add(CreateRow(row.Id, row.Nazwa, row.Aktywny, row.Zaznaczony));
            listView.EndUpdate();

            // Rysowanie – metody są w Form1.Rendering.cs
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

            // FLEX kolumny „Nazwa” na różne zdarzenia
            listView.Resize += (_, __) => FitFlexColumn();
            listView.ColumnWidthChanged += (_, __) => FitFlexColumn();
            listView.ColumnReordered += (_, __) => BeginInvoke((MethodInvoker)(() => FitFlexColumn()));

            Controls.Add(listView);

            // Domyślne sortowanie i pierwsze dopasowanie
            SetInitialSort(0, true);
            FitFlexColumn();
        }

        // Wiersz z danymi (prostutka fabryka ListViewItem)
        private static ListViewItem CreateRow(string id, string nazwa, bool aktywny, bool zaznaczony)
        {
            var item = new ListViewItem(id); // kol.0
            item.SubItems.Add("");           // kol.1 (checkbox)
            item.SubItems.Add(nazwa);        // kol.2 (tekst FLEX)
            item.SubItems.Add("");           // kol.3 (checkbox)

            // Stan checkboxów trzymamy w Tag subitemów (stabilne przy sortowaniu)
            item.SubItems[1].Tag = aktywny;
            item.SubItems[3].Tag = zaznaczony;
            return item;
        }

        // ====== LOGIKA FLEX: kolumna „Nazwa” wypełnia pozostałą szerokość ======
        private void FitFlexColumn(int minWidth = 160, int padding = 2)
        {
            if (_fitting) return;
            if (colNazwa is null) return;

            try
            {
                _fitting = true;

                int client = listView.ClientSize.Width;
                if (client <= 0) return;

                int other = 0;
                foreach (ColumnHeader c in listView.Columns)
                    if (!ReferenceEquals(c, colNazwa))
                        other += c.Width;

                int target = client - other - padding;
                if (target < minWidth) target = minWidth;

                colNazwa.Width = target; // ustawienie wyzwoli ColumnWidthChanged – chroni nas _fitting
            }
            finally
            {
                _fitting = false;
            }
        }

        // ====== INTERAKCJE: hover, klik w checkbox ======
        private void ListView_MouseMove(object? sender, MouseEventArgs e)
        {
            var hit = listView.HitTest(e.Location);
            int newIndex = hit.Item?.Index ?? -1;
            if (newIndex != hoverIndex)
            {
                int old = hoverIndex;
                hoverIndex = newIndex;
                InvalidateRow(old);
                InvalidateRow(hoverIndex);
            }
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
            if (col == 1 || col == 3) // nasze kolumny checkboxów
            {
                bool cur = hit.SubItem.Tag is bool b && b;
                hit.SubItem.Tag = !cur;                   // toggle
                listView.Invalidate(hit.SubItem.Bounds);  // odśwież tylko tę komórkę
            }
        }

        // ====== SORTOWANIE ======
        private void SetInitialSort(int column, bool ascending)
        {
            sortColumn = column;
            sortAsc = ascending;
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
            FitFlexColumn(); // po sortowaniu dopasuj FLEX (może pojawić się pasek poziomy)
        }

        private sealed class ListViewComparer : IComparer
        {
            private readonly int col;
            private readonly bool asc;
            public ListViewComparer(int column, bool ascending) { col = column; asc = ascending; }

            public int Compare(object? x, object? y)
            {
                var i1 = (ListViewItem)x!;
                var i2 = (ListViewItem)y!;

                // Dla kolumn checkboxów sortujemy po bool w Tag
                if (col == 1 || col == 3)
                {
                    bool b1 = i1.SubItems[col].Tag is bool v1 && v1;
                    bool b2 = i2.SubItems[col].Tag is bool v2 && v2;
                    int cmp = b1.CompareTo(b2);
                    return asc ? cmp : -cmp;
                }

                // Pozostałe kolumny – sortowanie tekstowe (case-insensitive)
                string s1 = i1.SubItems[col].Text ?? "";
                string s2 = i2.SubItems[col].Text ?? "";
                int res = string.Compare(s1, s2, StringComparison.CurrentCultureIgnoreCase);
                return asc ? res : -res;
            }
        }

        // Prosty ListView z włączonym podwójnym buforowaniem (mniej migotania)
        private sealed class SmoothListView : ListView
        {
            public SmoothListView()
            {
                DoubleBuffered = true; // własność chroniona – dostępna w klasie pochodnej
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }
        }
    }
}
