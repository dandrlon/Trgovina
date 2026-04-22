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

        public DateTime? DatumIsporuke { get; set; }

        // ── Fiskalizacija 1.0 (B2C → CIS) ─────────────────────────────────────

        /// <summary>Zaštitni kod izdavatelja (MD5 RSA potpisa). 32 hex znaka.</summary>
        public string ZKI { get; set; }

        /// <summary>Jedinstveni identifikator računa — UUID vraćen od CIS-a Porezne uprave.</summary>
        public string JIR { get; set; }

        /// <summary>Status fiskalizacije: NIJE_POSLANO | FISKALIZIRAN | GREŠKA | N/A</summary>
        public string FiskalizacijaStatus { get; set; } = "NIJE_POSLANO";

        /// <summary>Datum i vrijeme uspješne fiskalizacije.</summary>
        public string FiskalizacijaVrijeme { get; set; }

        /// <summary>Poruka greške od CIS-a ako fiskalizacija nije uspjela.</summary>
        public string FiskalizacijaGreska { get; set; }

        // ── Vrsta transakcije i način plaćanja ─────────────────────────────────

        /// <summary>B2C = Fiskalizacija 1.0 (CIS), B2B = eRačun Fiskalizacija 2.0</summary>
        public string VrstaProdaje { get; set; } = "B2C";

        /// <summary>G=Gotovina, K=Kartica, T=Transakcijski račun, O=Ostalo</summary>
        public string NacinPlacanja { get; set; } = "T";

        // ── eRačun / Fiskalizacija 2.0 (B2B) ──────────────────────────────────

        /// <summary>Status slanja eRačuna: NIJE_POSLANO | POSLANO | PRIHVAĆEN | ODBIJEN | GREŠKA</summary>
        public string EracunStatus { get; set; } = "NIJE_POSLANO";

        /// <summary>Datum i vrijeme slanja eRačuna.</summary>
        public string EracunVrijeme { get; set; }

        /// <summary>Referenca/ID eRačuna od middleware posrednika.</summary>
        public string EracunReferenca { get; set; }

        /// <summary>Poruka greške pri slanju eRačuna.</summary>
        public string EracunGreska { get; set; }

        // ── Podaci kupca za XML/eRačun ─────────────────────────────────────────

        /// <summary>OIB kupca — obavezno za B2B eRačune.</summary>
        public string OibKupca { get; set; }

        /// <summary>Adresa kupca za PDF i eRačun XML.</summary>
        public string AdresaKupca { get; set; }

        /// <summary>PDV ID kupca (HR + OIB ili EU VAT ID).</summary>
        public string PdvIdKupca { get; set; }

        // ── Computed properties ────────────────────────────────────────────────

        /// <summary>True ako je račun uspješno fiskaliziran (ima JIR).</summary>
        public bool JeFiskaliziran => !string.IsNullOrWhiteSpace(JIR);

        /// <summary>True ako je eRačun uspješno poslan.</summary>
        public bool JeEracunPoslan =>
            EracunStatus == "POSLANO" || EracunStatus == "PRIHVAĆEN";

        /// <summary>Prikazni tekst statusa fiskalizacije za grid.</summary>
        public string FiskalStatusPrikaz
        {
            get
            {
                if (VrstaProdaje == "B2B")
                {
                    switch (EracunStatus)
                    {
                        case "POSLANO":
                            return "📧 eRačun poslan";
                        case "PRIHVAĆEN":
                            return "✅ eRačun prihvaćen";
                        case "ODBIJEN":
                            return "❌ eRačun odbijen";
                        case "GREŠKA":
                            return "⚠️ Greška eRačun";
                        default:
                            return "⏳ Nije poslano";
                    }
                }
                else
                {
                    switch (FiskalizacijaStatus)
                    {
                        case "FISKALIZIRAN":
                            return "✅ Fiskaliziran";
                        case "GREŠKA":
                            return "⚠️ Greška CIS";
                        case "N/A":
                            return "—";
                        default:
                            return "⏳ Nije poslano";
                    }
                }
            }
        }



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