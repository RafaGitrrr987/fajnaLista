//klasa statyczna, która ma jedną metodę GetRows zwracającą listę przykładowych elementów.

using System.Collections.Generic;

namespace WinFormsWywal3
{
    public static class SampleData
    {
        public static List<(string Id, string Nazwa, bool Aktywny, bool Zaznaczony)> GetRows()
        {
            return new List<(string, string, bool, bool)>
            {
                ("A-001", "Śrubka", true, false),
                ("A-002", "Podkładka", false, true),
                ("B-100", "Nakrętka", true, true),
                ("C-777", "Zestaw montażowy", false, false),
                ("e-753", "Dupa maryna", false, true)
            };
        }
    }
}
