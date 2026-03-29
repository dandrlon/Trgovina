using System;
using System.Collections.Generic;
using System.Linq;

namespace Trgovina.Data.Models
{
    public class Ponuda
    {
        public int Id { get; set; }
        public string BrojPonude { get; set; }
        public DateTime DatumPonude { get; set; } = DateTime.Today;
        public DateTime? DatumVazenja { get; set; }   // rok valjanosti ponude
        public DateTime? DatumSlanja { get; set; }
        public DateTime? DatumOdgovora { get; set; }
        public DateTime DatumKreiranja { get; set; }

        // ─── Kupac ────────────────────────────────────────────────────────
        public int KupacId { get; set; }
        public string NazivKupca { get; set; }
        public string AdresaKupca { get; set; }
        public string OibKupca { get; set; }
        public string GradKupca { get; set; }

        // ─── Prodavač ─────────────────────────────────────────────────────
        public int? ProdavacId { get; set; }
        public string NazivProdavaca { get; set; }

        // ─── Veze prema dokumentima (nakon pretvorbe) ─────────────────────
        public int? OtpremnicaId { get; set; }
        public string BrojOtpremnice { get; set; }
        public int? RacunId { get; set; }
        public string BrojRacuna { get; set; }

        // ─── Iznosi ───────────────────────────────────────────────────────
        public decimal UkupnoBezPdv { get; set; }
        public decimal UkupnoPdv { get; set; }
        public decimal UkupnoSaPdv { get; set; }

        // ─── Uvjeti / napomene ────────────────────────────────────────────
        public string Status { get; set; } = "KREIRANA";
        // KREIRANA | POSLANA | PRIHVAĆENA | ODBIJENA | ISTEKLA | PRETVORENA

        public string Napomena { get; set; }
        public string UvjetiPlacanja { get; set; }
        public string RokIsporuke { get; set; }

        // ─── Stavke ───────────────────────────────────────────────────────
        public List<PonudaStavka> Stavke { get; set; } = new List<PonudaStavka>();

        // ─── Computed ─────────────────────────────────────────────────────
        public bool JePretvorena => OtpremnicaId.HasValue || RacunId.HasValue;
        public bool JeIstekla => DatumVazenja.HasValue && DatumVazenja.Value < DateTime.Today
                                     && Status != "PRIHVAĆENA" && Status != "PRETVORENA";

        public void IzracunajTotale()
        {
            UkupnoBezPdv = Stavke?.Sum(s => s.IznosBezPdv) ?? 0m;
            UkupnoPdv = Stavke?.Sum(s => s.IznosPdv) ?? 0m;
            UkupnoSaPdv = Stavke?.Sum(s => s.IznosSaPdv) ?? 0m;
        }

        public string StatusBadge()
        {
            switch (Status)
            {
                case "KREIRANA":
                    return "🟡  Kreirana";
                case "POSLANA":
                    return "🔵  Poslana";
                case "PRIHVAĆENA":
                    return "🟢  Prihvaćena";
                case "ODBIJENA":
                    return "🔴  Odbijena";
                case "ISTEKLA":
                    return "⚫  Istekla";
                case "PRETVORENA":
                    return "🟣  Pretvorena";
                default:
                    return Status;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PONUDA STAVKA
    // ══════════════════════════════════════════════════════════════════════

    public class PonudaStavka
    {
        public int Id { get; set; }
        public int PonudaId { get; set; }
        public int ArtiklId { get; set; }
        public int Rbr { get; set; }

        public string SifraArtikla { get; set; }
        public string NazivArtikla { get; set; }
        public string NazivJediniceMjere { get; set; }

        public decimal Kolicina { get; set; }
        public decimal CijenaBezPdv { get; set; }
        public decimal Popust { get; set; }
        public decimal PdvStopa { get; set; }

        public decimal IznosBezPdv { get; set; }
        public decimal IznosPdv { get; set; }
        public decimal IznosSaPdv { get; set; }

        public string Napomena { get; set; }

        // ─── Helper: pretvori u RacunStavku ──────────────────────────────
        public RacunStavka ToRacunStavka() => new RacunStavka
        {
            ArtiklId = ArtiklId,
            SifraArtikla = SifraArtikla,
            NazivArtikla = NazivArtikla,
            NazivJediniceMjere = NazivJediniceMjere,
            Rbr = Rbr,
            Kolicina = Kolicina,
            ProdajnaCijena = CijenaBezPdv * (1 + PdvStopa / 100m),
            Popust = Popust,
            PdvStopa = PdvStopa,
            IznosBezPdv = IznosBezPdv,
            IznosPdv = IznosPdv,
            IznosSaPdv = IznosSaPdv
        };

        // ─── Helper: pretvori u OtpremnicaStavku ─────────────────────────
        public OtpremnicaStavka ToOtpremnicaStavka() => new OtpremnicaStavka
        {
            ArtiklId = ArtiklId,
            SifraArtikla = SifraArtikla,
            NazivArtikla = NazivArtikla,
            NazivJediniceMjere = NazivJediniceMjere,
            Rbr = Rbr,
            Kolicina = Kolicina,
            CijenaBezPdv = CijenaBezPdv,
            IznosBezPdv = IznosBezPdv,
            PdvStopa = PdvStopa
        };
    }
}