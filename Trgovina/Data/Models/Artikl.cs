using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trgovina.Data.Models
{
    public class Artikl
    {
        public int Id { get; set; }
        public string Sifra { get; set; }
        public string Naziv { get; set; }
        public string Opis { get; set; }

        public decimal CijenaNabave { get; set; }
        public decimal CijenaProdaje { get; set; }
        public decimal Kolicina { get; set; }

        public int IdGrupe { get; set; }
        public string NazivGrupe { get; set; }

        public int IdJediniceMjere { get; set; }
        public string NazivJediniceMjere { get; set; }

        public int IdPdv { get; set; }
        public decimal PdvStopa { get; set; }

        public bool Aktivan { get; set; } = true;

        public decimal CijenaSPdv => CijenaProdaje * (1 + PdvStopa / 100);
        public decimal MarzaPostotak => CijenaNabave > 0
            ? Math.Round((CijenaProdaje - CijenaNabave) / CijenaNabave * 100, 2)
            : 0;
    }

    public class GrupaArtikala
    {
        public int Id { get; set; }
        public string Naziv { get; set; }
        public override string ToString() => Naziv;
    }

    public class JedinicaMjere
    {
        public int Id { get; set; }
        public string Naziv { get; set; }
        public string Skracenica { get; set; }
        public override string ToString() => $"{Naziv} ({Skracenica})";
    }

    public class Pdv
    {
        public int Id { get; set; }
        public string Naziv { get; set; }
        public decimal Stopa { get; set; }
        public override string ToString() => $"{Naziv} ({Stopa}%)";
    }
}