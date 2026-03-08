using System;
using System.Collections.Generic;

namespace Trgovina.Data.Models
{
    public class Racun
    {
        public int Id { get; set; }
        public string BrojRacuna { get; set; }
        public DateTime DatumRacuna { get; set; }
        public DateTime? DatumValute { get; set; }

        // Kupac
        public int KupacId { get; set; }
        public string NazivKupca { get; set; }

        // Prodavač
        public int? ProdavacId { get; set; }
        public string NazivProdavaca { get; set; }

        // Iznosi
        public decimal UkupnoBezPdv { get; set; }
        public decimal UkupnoPdv { get; set; }
        public decimal UkupnoSaPdv { get; set; }

        // Status
        public bool Placeno { get; set; }
        public DateTime? DatumPlacanja { get; set; }
        public bool Proknjizeno { get; set; }
        public DateTime? DatumKnjizenja { get; set; }
        public string Status { get; set; }

        public string Napomena { get; set; }
        public DateTime DatumKreiranja { get; set; }

        // Stavke (za formu)
        public List<RacunStavka> Stavke { get; set; } = new List<RacunStavka>();
    }

    public class RacunStavka
    {
        public int Id { get; set; }
        public int RacunId { get; set; }

        public int ArtiklId { get; set; }
        public string SifraArtikla { get; set; }
        public string NazivArtikla { get; set; }
        public string NazivJediniceMjere { get; set; }

        public int Rbr { get; set; }
        public decimal Kolicina { get; set; }
        public decimal ProdajnaCijena { get; set; }
        public decimal Popust { get; set; }      // %
        public decimal PdvStopa { get; set; }    // %

        public decimal IznosBezPdv { get; set; }
        public decimal IznosPdv { get; set; }
        public decimal IznosSaPdv { get; set; }

        //helper
        public decimal CijenaPojednine => ProdajnaCijena * (1 - Popust / 100);
    }
}