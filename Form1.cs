using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsWywal3
{
    // Główna część klasy – inicjalizacja, dane, interakcje (bez rysowania, FLEX-a i sortowania)
    public partial class Form1 : Form
    {
        private SmoothListView listView = null!; // przypiszemy w InitializeListView
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

            // <<< przeniesione do Form1.Columns.cs >>>
            SetupColumns(); // definiuje kolumny i ustawia colNazwa

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
            listView.ColumnClick += SortByColumn; // ← metoda w Form1.Sorting.cs
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
            SetInitialSort(0, true); // ← metoda w Form1.Sorting.cs
            FitFlexColumn();         // ← metoda w Form1.Flex.cs
        }

        // Fabryka wiersza
        private static ListViewItem CreateRow(string id, string nazwa, bool aktywny, bool zaznaczony)
        {
            var item = new ListViewItem(id);
            item.SubItems.Add("");     // Aktywny (checkbox)
            item.SubItems.Add(nazwa);  // Nazwa
            item.SubItems.Add("");     // Zaznaczony (checkbox)
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

        // Podwójnie buforowany ListView
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
