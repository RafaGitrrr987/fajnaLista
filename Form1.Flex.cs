using System;
using System.Windows.Forms;

namespace WinFormsWywal3
{
    // Część odpowiedzialna za dopasowanie kolumny „Nazwa” (FLEX)
    public partial class Form1
    {
        // „FLEX” dla kolumny „Nazwa”
        private ColumnHeader? colNazwa;
        private bool _fitting = false; // strażnik przed rekurencją przy zmianie szerokości

        // Kolumna „Nazwa” dopasowuje się do pozostałej szerokości
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
    }
}
