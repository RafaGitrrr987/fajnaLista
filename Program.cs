// Punkt startowy aplikacji WinForms (.NET 6+)
using System;
using System.Windows.Forms;

namespace WinFormsWywal3
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Ustawienia domyœlne WinForms (DPI, wizualne style itp.)
            ApplicationConfiguration.Initialize();

            // Uruchamiamy nasz formularz (tworzony w 100% w kodzie)
            Application.Run(new Form1());
        }
    }
}
