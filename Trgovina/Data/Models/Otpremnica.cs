using System;
using System.Collections.Generic;
using System.Linq;

namespace Trgovina.Data.Models
{
    // ══════════════════════════════════════════════════════════════════════
    //  OTPREMNICA
    // ══════════════════════════════════════════════════════════════════════

    public class Otpremnica
    {
        public int Id { get; set; }
        public string BrojOtpremnice { get; set; }
        public DateTime DatumOtpremnice { get; set; } = DateTime.Today;
        public DateTime? DatumIsporuke { get; set; }           // planirani datum isporuke
        public DateTime? DatumIsporukeReal { get; set; }          // stvarni datum kad je isporučeno
        public DateTime? DatumFakturiranja { get; set; }

        // ─── Kupac ────────────────────────────────────────────────────────
        public int KupacId { get; set; }
        public string NazivKupca { get; set; }
        public string AdresaKupca { get; set; }
        public string OibKupca { get; set; }
        public string GradKupca { get; set; }

        // ─── Prodavač ─────────────────────────────────────────────────────
        public int? ProdavacId { get; set; }
        public string NazivProdavaca { get; set; }

        // ─── Veza prema računu ────────────────────────────────────────────
        public int? RacunId { get; set; }
        public string BrojRacuna { get; set; }   // denormalizirano za prikaz

        // ─── Iznosi ───────────────────────────────────────────────────────
        public decimal UkupnoVrijednost { get; set; }

        // ─── Stanje ───────────────────────────────────────────────────────
        public bool Isporuceno { get; set; }
        public bool Fakturirano { get; set; }
        public string Status { get; set; } = "KREIRANA";
        // KREIRANA | ISPORUČENA | FAKTURIRANA | STORNIRANA

        public string Napomena { get; set; }
        public DateTime DatumKreiranja { get; set; }

        // ─── Stavke ───────────────────────────────────────────────────────
        public List<OtpremnicaStavka> Stavke { get; set; } = new List<OtpremnicaStavka>();

        // ─── Computed ─────────────────────────────────────────────────────
        public decimal IzracunajUkupno()
            => Stavke?.Sum(s => s.IznosBezPdv) ?? 0m;

        public string StatusBadge()
        {
            switch (Status)
            {
                case "KREIRANA":
                    return "🟡  Kreirana";
                case "ISPORUČENA":
                    return "🟢  Isporučena";
                case "FAKTURIRANA":
                    return "🔵  Fakturirana";
                case "STORNIRANA":
                    return "🔴  Stornirana";
                default:
                    return Status;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  OTPREMNICA STAVKA
    // ══════════════════════════════════════════════════════════════════════

    public class OtpremnicaStavka
    {
        public int Id { get; set; }
        public int OtpremnicaId { get; set; }
        public int ArtiklId { get; set; }
        public int Rbr { get; set; }

        // Denormalizirano za prikaz / PDF
        public string SifraArtikla { get; set; }
        public string NazivArtikla { get; set; }
        public string NazivJediniceMjere { get; set; }

        public decimal Kolicina { get; set; }
        public decimal CijenaBezPdv { get; set; }
        public decimal IznosBezPdv { get; set; }
        public decimal PdvStopa { get; set; }   // pamtimo za fakturiranje

        public string Napomena { get; set; }

        // ─── Helper za fakturiranje ───────────────────────────────────────
        /// <summary>
        /// Vraća prodajnu cijenu s PDV-om (za kreiranje računa iz otpremnice).
        /// </summary>
        public decimal CijenaSaPdv()
            => CijenaBezPdv * (1 + PdvStopa / 100m);

        public decimal IznosSaPdv()
            => IznosBezPdv * (1 + PdvStopa / 100m);

        public decimal IznosPdv()
            => IznosSaPdv() - IznosBezPdv;
    }
}