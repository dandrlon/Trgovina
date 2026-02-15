using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Trgovina.Data;

namespace Trgovina
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Testiraj konekciju prije pokretanja
            if (!DatabaseHelper.TestConnection())
            {
                MessageBox.Show("Greška pri povezivanju s bazom podataka!\n\n" +
                               "Provjerite:\n" +
                               "1. Je li SQL Server pokrenut\n" +
                               "2. Postoji li baza 'Trgovina'\n" +
                               "3. Connection string u App.config",
                               "Greška s bazom",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
                return;
            }

            Application.Run(new frmMain());
        }
    }
}
