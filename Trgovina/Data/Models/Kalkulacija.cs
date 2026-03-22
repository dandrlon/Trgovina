using System;
using System.Collections.Generic;

namespace Trgovina.Data.Models
{
    public class Kalkulacija
    {
        public int Id { get; set; }
        public string BrojKalkulacije { get; set; }
        public DateTime DatumKalkulacije { get; set; }
        public int? DobavljacId { get; set; }
        public string NazivDobavljaca { get; set; }   // denormalizirano za prikaz
        public string AdresaDobavljaca { get; set; }
        public string OibDobavljaca { get; set; }
        public string BrojDobavljacevogRacuna { get; set; }
        public decimal UkupnoBezPdv { get; set; }
        public decimal UkupnoPdv { get; set; }
        public decimal UkupnoSaPdv { get; set; }
        public bool Proknjizeno { get; set; }
        public DateTime? DatumKnjizenja { get; set; }
        public string Napomena { get; set; }
        public DateTime DatumKreiranja { get; set; } = DateTime.Now;

        public List<KalkulacijaStavka> Stavke { get; set; } = new List<KalkulacijaStavka>();
    }

    public class KalkulacijaStavka
    {
        public int Id { get; set; }
        public int KalkulacijaId { get; set; }
        public int ArtiklId { get; set; }
        public string SifraArtikla { get; set; }
        public string NazivArtikla { get; set; }
        public string NazivJM { get; set; }
        public int Rbr { get; set; }
        public decimal Kolicina { get; set; }
        public decimal NabavnaCijena { get; set; }   // cijena s PDV-om
        public decimal PdvStopa { get; set; }
        public decimal IznosBezPdv { get; set; }
        public decimal IznosPdv { get; set; }
        public decimal IznosSaPdv { get; set; }
    }
}