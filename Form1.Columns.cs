using System.Windows.Forms;

namespace WinFormsWywal3;

public partial class Form1
{
    // Definicje kolumn + ustawienie referencji do FLEX kolumny
    private void SetupColumns()
    {
        listView.Columns.Add("ID", 140, HorizontalAlignment.Left); // 0
        listView.Columns.Add("Aktywny", 120, HorizontalAlignment.Left); // 1 (checkbox)
        listView.Columns.Add("Nazwa", 360, HorizontalAlignment.Left); // 2 (FLEX)
        listView.Columns.Add("Zaznaczony", 140, HorizontalAlignment.Left); // 3 (checkbox)
        colNazwa = listView.Columns[2]; // pole zadeklarowane w Form1.Flex.cs
    }
}
