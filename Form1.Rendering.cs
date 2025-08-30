using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace WinFormsWywal3
{
    // Ta część klasy zawiera wyłącznie RENDERING (kolory, czcionki, rysowanie).
    public partial class Form1
    {
        // ===== Paleta kolorów (jasny motyw) =====
        private readonly Color RowEven = Color.White;
        private readonly Color RowOdd = Color.FromArgb(248, 248, 252);
        private readonly Color RowHover = Color.FromArgb(235, 243, 255);
        private readonly Color RowGrid = Color.FromArgb(220, 225, 232);
        private readonly Color ColGrid = Color.FromArgb(225, 230, 238);

        private readonly Color HeadFlat = Color.FromArgb(0x1F, 0x29, 0x37);
        private readonly Color HeadLine = Color.FromArgb(0x37, 0x41, 0x51);
        private readonly Color HeadText = Color.FromArgb(0xE5, 0xE7, 0xEB);

        private readonly Color SelBackFocused = Color.FromArgb(0xE8, 0xF0, 0xFE);
        private readonly Color SelBackUnfocused = Color.FromArgb(0xF1, 0xF3, 0xF4);
        private readonly Color SelBorder = Color.FromArgb(0x1A, 0x73, 0xE8);
        private readonly Color SelText = Color.FromArgb(0x12, 0x1A, 0x22);

        // Czcionka dla nagłówków kolumn (tworzona na podstawie fontu formularza)
        private Font? headerFont;

        // ===== RYSOWANIE NAGŁÓWKA: ciemne tło + jasny tekst + strzałka ▲/▼ =====
        private void DrawColumnHeaderWithSortGlyph(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (var bHead = new SolidBrush(HeadFlat))
                e.Graphics.FillRectangle(bHead, e.Bounds);

            // cienka dolna linia nagłówka
            using (var pen = new Pen(HeadLine))
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            // rezerwa na strzałkę sortowania
            Rectangle textRect = e.Bounds;
            const int glyphWidth = 10;
            const int glyphPad = 8;
            bool isSortedCol = (e.ColumnIndex == sortColumn);
            if (isSortedCol) textRect.Width -= (glyphWidth + glyphPad);

            // tekst nagłówka – jasny, z obcięciem na końcu (ellipsis)
            TextRenderer.DrawText(
                e.Graphics,
                e.Header.Text ?? "",
                headerFont ?? this.Font,
                new Rectangle(textRect.X + 8, textRect.Y, textRect.Width - 8, textRect.Height),
                HeadText,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis
            );

            // strzałka ▲/▼ dla kolumny sortowanej
            if (isSortedCol)
            {
                int centerX = e.Bounds.Right - glyphPad - glyphWidth / 2;
                int centerY = e.Bounds.Top + e.Bounds.Height / 2;
                int half = 4;

                Point[] tri = sortAsc
                    ? new[] { new Point(centerX - half, centerY + 2), new Point(centerX + half, centerY + 2), new Point(centerX, centerY - 3) }
                    : new[] { new Point(centerX - half, centerY - 2), new Point(centerX + half, centerY - 2), new Point(centerX, centerY + 3) };

                using var b = new SolidBrush(Color.FromArgb(220, HeadText));
                e.Graphics.FillPolygon(b, tri);
            }

            // separator pionowy między kolumnami
            using var penV = new Pen(HeadLine);
            e.Graphics.DrawLine(penV, e.Bounds.Right - 1, e.Bounds.Top + 2, e.Bounds.Right - 1, e.Bounds.Bottom - 2);
        }

        // ===== RYSOWANIE KOMÓREK: zebra/hover + checkboxy + „pigułka” zaznaczenia =====
        private void DrawSubItemStyled(object? sender, DrawListViewSubItemEventArgs e)
        {
            bool isSelected = e.Item.Selected;

            // Tło wiersza (zebra/hover) – tylko gdy wiersz NIE jest zaznaczony
            if (!isSelected)
            {
                Color back = (e.ItemIndex == hoverIndex) ? RowHover
                            : (e.ItemIndex % 2 == 0 ? RowEven : RowOdd);
                using var b = new SolidBrush(back);
                e.Graphics.FillRectangle(b, e.Bounds);
            }
            else if (e.ColumnIndex == 0) // „pigułka” przez cały wiersz – rysujemy raz (w kolumnie 0)
            {
                var rowRect = e.Item.GetBounds(ItemBoundsPortion.Entire);
                var pill = Rectangle.Inflate(rowRect, -4, -2); // padding
                var fill = listView.Focused ? SelBackFocused : SelBackUnfocused;
                FillRoundedRect(e.Graphics, pill, 6, fill, SelBorder);
            }

            // Zawartość komórki
            int col = e.ColumnIndex;
            Color fore = isSelected ? SelText : this.ForeColor;

            if (col == 1 || col == 3) // checkboxy rysowane systemowym rendererem
            {
                bool state = e.SubItem.Tag is bool b2 && b2;
                var pt = new Point(e.Bounds.X + 8, e.Bounds.Y + (e.Bounds.Height - 13) / 2);
                CheckBoxRenderer.DrawCheckBox(
                    e.Graphics, pt,
                    state
                        ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
                        : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal
                );
            }
            else // tekst
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

            // Siatka (nie rysujemy na zaznaczonym wierszu – nie przecinamy „pigułki”)
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

        // ===== HELPERY GRAFICZNE =====

        // Zaokrąglony prostokąt („pigułka”)
        private static void FillRoundedRect(Graphics g, Rectangle rect, int radius, Color fill, Color? border = null)
        {
            using var path = new GraphicsPath();
            int d = radius * 2;
            var r = new Rectangle(rect.X, rect.Y, d, d);

            path.AddArc(r, 180, 90);                 // lewy-górny
            r.X = rect.Right - d; path.AddArc(r, 270, 90); // prawy-górny
            r.Y = rect.Bottom - d; path.AddArc(r, 0, 90);  // prawy-dolny
            r.X = rect.X; path.AddArc(r, 90, 90); // lewy-dolny
            path.CloseFigure();

            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(fill)) g.FillPath(b, path);
            if (border.HasValue) using (var p = new Pen(border.Value)) g.DrawPath(p, path);
            g.SmoothingMode = old;
        }

        // Jednorazowa „rozgrzewka” renderera checkboxów (usuwa pierwsze mignięcie)
        private static void WarmUpCheckBoxRenderer()
        {
            using var bmp = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bmp);
            CheckBoxRenderer.DrawCheckBox(
                g, new Point(0, 0),
                System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal
            );
        }

        // Czcionka dla całego UI (priorytet: Segoe UI Variable → Segoe UI → Inter/Roboto)
        private static Font CreateModernFont(float size, FontStyle style = FontStyle.Regular)
        {
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

            return SystemFonts.MessageBoxFont; // fallback systemowy
        }

        // Pogrubiony nagłówek (semibold/medium jeśli dostępne)
        private static Font CreateHeaderFont(Font baseFont)
        {
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

            return new Font(baseFont, FontStyle.Bold); // fallback
        }

        // Sprawdź, czy dana rodzina czcionek jest zainstalowana w systemie
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
