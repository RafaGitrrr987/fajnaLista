using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace WinFormsWywal3 // ← dopasuj do swojego namespace z Form1.Designer.cs
{
    public class Form1 : Form
    {
        private SmoothListView listView;
        private int sortColumn = -1;
        private bool sortAsc = true;
        private int hoverIndex = -1;

        // --- FLEX dla kolumny „Nazwa” ---
        private ColumnHeader? colNazwa;   // referencja do kolumny wypełniającej
        private bool _fitting = false;    // strażnik przed rekurencją

        // Kolory stylu (jasny motyw)
        private readonly Color RowEven = Color.White;
        private readonly Color RowOdd = Color.FromArgb(248, 248, 252);
        private readonly Color RowHover = Color.FromArgb(235, 243, 255);
        private readonly Color RowGrid = Color.FromArgb(220, 225, 232);
        private readonly Color ColGrid = Color.FromArgb(225, 230, 238);

        // NAGŁÓWKI: ciemne tło + jasny tekst
        private readonly Color HeadFlat = Color.FromArgb(0x1F, 0x29, 0x37);  // #1F2937
        private readonly Color HeadLine = Color.FromArgb(0x37, 0x41, 0x51);  // #374151
        private readonly Color HeadText = Color.FromArgb(0xE5, 0xE7, 0xEB);  // #E5E7EB

        // Material-like (Google) zaznaczenie
        private readonly Color SelBackFocused = Color.FromArgb(0xE8, 0xF0, 0xFE); // #E8F0FE
        private readonly Color SelBackUnfocused = Color.FromArgb(0xF1, 0xF3, 0xF4); // #F1F3F4
        private readonly Color SelBorder = Color.FromArgb(0x1A, 0x73, 0xE8); // #1A73E8
        private readonly Color SelText = Color.FromArgb(0x12, 0x1A, 0x22); // ciemny

        private Font? headerFont;

        public Form1()
        {
            // GLOBALNIE dla całego formularza (przed InitializeComponent)
            var uiFont = CreateModernFont(10f); // Segoe UI Variable / Segoe UI / Inter / Roboto
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Font = uiFont;

            //InitializeComponent();
            InitializeListView();

            this.Shown += (_, __) =>
            {
                WarmUpCheckBoxRenderer();
                FitFlexColumn();           // dopasuj szerokość „Nazwa” po pierwszym pokazaniu
                listView.Invalidate(true);
            };
        }

        private void InitializeListView()
        {
            Text = "ListView – nowoczesny wygląd (checkboxy, sort, zebra, hover)";
            Size = new Size(900, 540);
            StartPosition = FormStartPosition.CenterScreen;

            // Nagłówki: semibold/medium gdy dostępne, inaczej Bold. Rozmiar = this.Font.Size
            headerFont = CreateHeaderFont(this.Font);

            listView = new SmoothListView
            {
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = false,             // własne checkboxy w subitemach
                OwnerDraw = true,
                Dock = DockStyle.Fill,
                HideSelection = false,
                UseCompatibleStateImageBehavior = false,
                AllowColumnReorder = true
                // Font dziedziczony z this.Font
            };

            // Definicje kolumn
            listView.Columns.Add("ID", 140, HorizontalAlignment.Left);        // 0
            listView.Columns.Add("Aktywny", 120, HorizontalAlignment.Left);   // 1
            listView.Columns.Add("Nazwa", 360, HorizontalAlignment.Left);     // 2 (FLEX)
            listView.Columns.Add("Zaznaczony", 140, HorizontalAlignment.Left);// 3
            colNazwa = listView.Columns[2]; // zapamiętaj kolumnę „Nazwa” jako flex

            // Dane przykładowe
            //listView.Items.Add(CreateRow("A-001", "Śrubka", true, false));
            //listView.Items.Add(CreateRow("A-002", "Podkładka", false, true));
            //listView.Items.Add(CreateRow("B-100", "Nakrętka", true, true));
            //listView.Items.Add(CreateRow("C-777", "Zestaw montażowy", false, false));

            foreach (var row in SampleData.GetRows())
            {
                listView.Items.Add(CreateRow(row.Id, row.Nazwa, row.Aktywny, row.Zaznaczony));
            }

            // Rysowanie
            listView.DrawColumnHeader += DrawColumnHeaderWithSortGlyph; // nagłówek: ciemne tło + jasny tekst + strzałka
            listView.DrawSubItem += DrawSubItemStyled;                  // zebra + hover + checkboxy + siatka + "pigułka"

            // Interakcje
            listView.MouseClick += HandleClick;
            listView.ColumnClick += SortByColumn;
            listView.MouseMove += ListView_MouseMove;
            listView.MouseLeave += (_, __) =>
            {
                if (hoverIndex != -1) { int i = hoverIndex; hoverIndex = -1; InvalidateRow(i); }
            };

            // --- AUTODOPASOWANIE FLEX KOLUMNY ---
            listView.Resize += (_, __) => FitFlexColumn();
            listView.ColumnWidthChanged += (_, __) => FitFlexColumn();        // gdy użytkownik zmienia inne kolumny
            listView.ColumnReordered += (_, __) => BeginInvoke((MethodInvoker)(() => FitFlexColumn())); // po przeciągnięciu kolumn

            Controls.Add(listView);

            // Domyślne sortowanie i strzałka od razu
            SetInitialSort(0, true);

            // Pierwsze dopasowanie
            FitFlexColumn();
        }

        // Dopasowanie szerokości kolumny „Nazwa” do pozostałej przestrzeni
        private void FitFlexColumn(int minWidth = 160, int padding = 2)
        {
            if (_fitting) return;
            if (listView == null || colNazwa == null) return;

            try
            {
                _fitting = true;

                int client = listView.ClientSize.Width;
                if (client <= 0) return;

                int other = 0;
                foreach (ColumnHeader c in listView.Columns)
                {
                    if (!object.ReferenceEquals(c, colNazwa))
                        other += c.Width;
                }

                // dostępna przestrzeń dla kolumny „Nazwa”
                int target = client - other - padding;
                if (target < minWidth) target = minWidth;

                // Uwaga: ustawienie Width wywoła ColumnWidthChanged -> chroni nas _fitting
                colNazwa.Width = target;
            }
            finally
            {
                _fitting = false;
            }
        }

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

        private void SetInitialSort(int column, bool ascending)
        {
            sortColumn = column;
            sortAsc = ascending;
            listView.ListViewItemSorter = new ListViewComparer(sortColumn, sortAsc);
            listView.Sort();
            listView.Refresh();
        }

        private ListViewItem CreateRow(string id, string nazwa, bool aktywny, bool zaznaczony)
        {
            var item = new ListViewItem(id); // kol 0
            item.SubItems.Add("");           // kol 1 – checkbox
            item.SubItems.Add(nazwa);        // kol 2 – tekst (FLEX)
            item.SubItems.Add("");           // kol 3 – checkbox

            // stabilny stan w Tag (nie gubi się przy sortowaniu)
            item.SubItems[1].Tag = aktywny;
            item.SubItems[3].Tag = zaznaczony;

            return item;
        }

        // ===== Nagłówek: ciemne tło + jasny tekst + strzałka ▲/▼ =====
        private void DrawColumnHeaderWithSortGlyph(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (var bHead = new SolidBrush(HeadFlat))
                e.Graphics.FillRectangle(bHead, e.Bounds);

            // cienka dolna linia
            using (var pen = new Pen(HeadLine))
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            // rezerwa na strzałkę
            Rectangle textRect = e.Bounds;
            const int glyphWidth = 10;
            const int glyphPad = 8;
            bool isSortedCol = (e.ColumnIndex == sortColumn);
            if (isSortedCol)
                textRect.Width -= (glyphWidth + glyphPad);

            // tekst nagłówka – jasny kolor
            TextRenderer.DrawText(
                e.Graphics,
                e.Header.Text ?? "",
                headerFont ?? this.Font,
                new Rectangle(textRect.X + 8, textRect.Y, textRect.Width - 8, textRect.Height),
                HeadText,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis
            );

            // strzałka ▲/▼ – jasna, żeby była widoczna na ciemnym tle
            if (isSortedCol)
            {
                int centerX = e.Bounds.Right - glyphPad - glyphWidth / 2;
                int centerY = e.Bounds.Top + e.Bounds.Height / 2;
                int half = 4;

                Point[] tri = sortAsc
                    ? new[] {
                        new Point(centerX - half, centerY + 2),
                        new Point(centerX + half, centerY + 2),
                        new Point(centerX,       centerY - 3)
                      }
                    : new[] {
                        new Point(centerX - half, centerY - 2),
                        new Point(centerX + half, centerY - 2),
                        new Point(centerX,       centerY + 3)
                      };

                using var b = new SolidBrush(Color.FromArgb(220, HeadText));
                e.Graphics.FillPolygon(b, tri);
            }

            // pionowy separator kolumn
            using (var penV = new Pen(HeadLine))
                e.Graphics.DrawLine(penV, e.Bounds.Right - 1, e.Bounds.Top + 2, e.Bounds.Right - 1, e.Bounds.Bottom - 2);
        }

        // ===== Komórki: zebra/hover + checkboxy + „pigułka” zaznaczenia =====
        private void DrawSubItemStyled(object? sender, DrawListViewSubItemEventArgs e)
        {
            bool isSelected = e.Item.Selected;

            // TŁO wiersza (zebra/hover) tylko gdy NIE zaznaczony
            if (!isSelected)
            {
                Color back = (e.ItemIndex == hoverIndex) ? RowHover
                            : (e.ItemIndex % 2 == 0 ? RowEven : RowOdd);
                using var b = new SolidBrush(back);
                e.Graphics.FillRectangle(b, e.Bounds);
            }
            else
            {
                // „pigułka” przez CAŁY wiersz – rysujemy raz (w kolumnie 0)
                if (e.ColumnIndex == 0)
                {
                    var rowRect = e.Item.GetBounds(ItemBoundsPortion.Entire);
                    var pill = Rectangle.Inflate(rowRect, -4, -2); // padding
                    var fill = listView.Focused ? SelBackFocused : SelBackUnfocused;
                    FillRoundedRect(e.Graphics, pill, radius: 6, fill: fill, border: SelBorder);
                }
            }

            // ZAWARTOŚĆ
            int col = e.ColumnIndex;
            Color fore = isSelected ? SelText : this.ForeColor;

            if (col == 1 || col == 3)
            {
                bool state = e.SubItem.Tag is bool b2 && b2;
                var pt = new Point(e.Bounds.X + 8, e.Bounds.Y + (e.Bounds.Height - 13) / 2);
                CheckBoxRenderer.DrawCheckBox(
                    e.Graphics,
                    pt,
                    state
                        ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
                        : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal
                );
            }
            else
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    e.SubItem.Text ?? "",
                    this.Font,
                    new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height),
                    fore,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.Left
                );
            }

            // SIATKA – nie rysujemy na zaznaczonym wierszu, żeby nie "przecinać" pigułki
            if (!isSelected && col < listView.Columns.Count - 1)
            {
                using var penV = new Pen(ColGrid);
                e.Graphics.DrawLine(penV, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
            }
            if (!isSelected)
            {
                using var penH = new Pen(RowGrid);
                e.Graphics.DrawLine(penH, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
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

        private void SortByColumn(object? sender, ColumnClickEventArgs e)
        {
            if (e.Column == sortColumn) sortAsc = !sortAsc;
            else { sortColumn = e.Column; sortAsc = true; }

            listView.ListViewItemSorter = new ListViewComparer(sortColumn, sortAsc);
            listView.Sort();
            listView.Refresh();

            // po sortowaniu dopasuj flex (na wypadek przewin. paska poziomego)
            FitFlexColumn();
        }

        private class ListViewComparer : IComparer
        {
            private readonly int col;
            private readonly bool asc;
            public ListViewComparer(int column, bool ascending) { col = column; asc = ascending; }

            public int Compare(object? x, object? y)
            {
                var i1 = (ListViewItem)x!;
                var i2 = (ListViewItem)y!;

                if (col == 1 || col == 3) // kolumny checkboxów: sortowanie po bool z Tag
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

        // ListView z podwójnym buforowaniem (bez migotania)
        private class SmoothListView : ListView
        {
            public SmoothListView()
            {
                DoubleBuffered = true; // protected; dostępne w klasie pochodnej
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }
        }

        // „Rozgrzewka” renderera – eliminuje jednorazowe znikanie na starcie
        private static void WarmUpCheckBoxRenderer()
        {
            using var bmp = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bmp);
            CheckBoxRenderer.DrawCheckBox(
                g,
                new Point(0, 0),
                System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal
            );
        }

        // ===== Helper: wypełnienie zaokrąglonego prostokąta ("pigułka") =====
        private static void FillRoundedRect(Graphics g, Rectangle rect, int radius, Color fill, Color? border = null)
        {
            using var path = new GraphicsPath();
            int d = radius * 2;
            var r = new Rectangle(rect.X, rect.Y, d, d);

            path.AddArc(r, 180, 90);                 // lewy-górny
            r.X = rect.Right - d; path.AddArc(r, 270, 90); // prawy-górny
            r.Y = rect.Bottom - d; path.AddArc(r, 0, 90);  // prawy-dolny
            r.X = rect.X; path.AddArc(r, 90, 90);          // lewy-dolny
            path.CloseFigure();

            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(fill)) g.FillPath(b, path);
            if (border.HasValue) using (var p = new Pen(border.Value)) g.DrawPath(p, path);
            g.SmoothingMode = old;
        }

        // ======= Font helpers: nowoczesna czcionka + semibold header =======
        private static Font CreateModernFont(float size, FontStyle style = FontStyle.Regular)
        {
            // Priorytet: Windows 11 → Segoe UI Variable (Text/Display), potem Segoe UI, Inter, Roboto
            string[] candidates =
            {
                "Segoe UI Variable Text",
                "Segoe UI Variable Display",
                "Segoe UI",
                "Inter",
                "Roboto"
            };

            foreach (var family in candidates)
                if (IsFontInstalled(family))
                    return new Font(family, size, style, GraphicsUnit.Point);

            // systemowy fallback
            return SystemFonts.MessageBoxFont; // zwykle Segoe UI 9–10pt
        }

        private static Font CreateHeaderFont(Font baseFont)
        {
            // wariant semibold/medium, jeśli dostępny
            string[] semiCandidates =
            {
                "Segoe UI Semibold",
                "Inter Semi Bold",
                "Inter SemiBold",
                "Roboto Medium"
            };

            foreach (var fam in semiCandidates)
                if (IsFontInstalled(fam))
                    return new Font(fam, baseFont.Size, FontStyle.Regular, GraphicsUnit.Point);

            // fallback: Bold bazowej
            return new Font(baseFont, FontStyle.Bold);
        }

        private static bool IsFontInstalled(string familyName)
        {
            using var fonts = new InstalledFontCollection();
            foreach (var f in fonts.Families)
                if (string.Equals(f.Name, familyName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
