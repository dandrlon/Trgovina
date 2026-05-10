using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trgovina.Data.Models
{
    public class DashboardStats
    {
        public decimal UkupnaProdajaMjesec { get; set; }
        public int BrojRacunaDanas { get; set; }
        public decimal VrijednostRobe { get; set; }
        public int AktivniPartneri { get; set; }
        public decimal PromijenaProdaje { get; set; } // % vs prošli mjesec
    }

    public class NedavniRacunRow
    {
        public string BrojRacuna { get; set; }
        public string Partner { get; set; }
        public decimal Iznos { get; set; }
        public string Status { get; set; }
    }
}
