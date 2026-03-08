using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trgovina.Data.Models
{
    public partial class Prodavac
    {
        public int Id { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }

        // Used in ComboBox display
        public override string ToString() => $"{Ime} {Prezime}".Trim();
    }
}
