using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trgovina.Data.Models
{
    public class Partner
    {
        public int Id { get; set; }
        public string Naziv { get; set; }
        public string Adresa { get; set; }
        public string Grad { get; set; }
        public string PostanskiBroj { get; set; }
        public string Drzava { get; set; }
        public string OIB { get; set; }
        public string Telefon { get; set; }
        public string Email { get; set; }
        public string KontaktOsoba { get; set; }
        public bool Aktivan { get; set; } = true;

        public string Napomena { get; set; }

        public override string ToString() => Naziv;

    }

}
