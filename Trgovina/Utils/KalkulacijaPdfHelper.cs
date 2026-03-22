using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Data.Models;
using static System.Windows.Forms.LinkLabel;

namespace Trgovina.Utils
{
    /// <summary>
    /// Generira PDF kalkulaciju (primku) u HORIZONTALNOM A4 formatu.
    /// Sukladno čl. 7. Zakona o računovodstvu (NN 78/15) i Pravilniku o PDV-u.
    /// 
    /// Obvezni elementi kalkulacije / primke:
    ///   1. Naziv, adresa i OIB primatelja (vaša tvrtka)
    ///   2. Naziv, adresa i OIB dobavljača
    ///   3. Broj i datum kalkulacije
    ///   4. Broj dobavljačevog računa (URA broj)
    ///   5. Za svaki artikl: redni broj, šifra, naziv, JM, količina,
    ///      nabavna cijena BEZ PDV-a, PDV stopa, iznos PDV-a, cijena S PDV-om
    ///   6. Rekapitulacija PDV-a po stopama (osnovica, stopa, iznos)
    ///   7. Ukupni iznos bez PDV, ukupni PDV, ukupno s PDV
    ///   8. Potpis primatelja / odgovorne osobe
    /// </summary>
    public static class KalkulacijaPdfHelper
    {
        // ── Postavke primatelja - default ─────────────────────────────────
        private static string _tvrtka = "Moja Trgovina d.o.o.";
        private static string _adresa = "Ulica 123, 10000 Zagreb";
        private static string _oib = "12345678901";
        private static string _pdvId = "HR12345678901";
        private static string _telefon = "+385 1 234 5678";
        private static string _email = "info@mojatvrtka.hr";

        // ── Fontovi ───────────────────────────────────────────────────────────
        private static XFont FN(double s) => new XFont("Arial", s, XFontStyleEx.Regular);
        private static XFont FB(double s) => new XFont("Arial", s, XFontStyleEx.Bold);
        private static XFont FI(double s) => new XFont("Arial", s, XFontStyleEx.Italic);

        // ── Crno-bijela paleta ────────────────────────────────────────────────
        private static readonly XColor cBlack = XColors.Black;
        private static readonly XColor cWhite = XColors.White;
        private static readonly XColor cDark = XColor.FromArgb(30, 30, 30);
        private static readonly XColor cMid = XColor.FromArgb(90, 90, 90);
        private static readonly XColor cLight = XColor.FromArgb(180, 180, 180);
        private static readonly XColor cBg = XColor.FromArgb(245, 245, 245);
        private static readonly XColor cHdrBg = XColor.FromArgb(30, 30, 30);

        // ════════════════════════════════════════════════════════════════════
        //  JAVNE METODE
        // ════════════════════════════════════════════════════════════════════

        public static void SpremiPdf(Kalkulacija k)
        {
            if (k == null) return;
            UcitajPostavkeTvrtke();

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Spremi kalkulaciju kao PDF";
                dlg.Filter = "PDF datoteke (*.pdf)|*.pdf";
                dlg.FileName = $"Kalkulacija_{Sanitize(k.BrojKalkulacije)}_{k.DatumKalkulacije:yyyyMMdd}.pdf";
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    GenerirajPdf(k, dlg.FileName);
                    if (MessageBox.Show("PDF je uspješno spremljen.\n\nZelite li ga otvoriti?",
                            "Uspjeh", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Greska pri generiranju PDF-a:\n" + ex.Message,
                        "Greska", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static void IspisiKalkulaciju(Kalkulacija k)
        {
            if (k == null) return;
            UcitajPostavkeTvrtke();
            try
            {
                string temp = Path.Combine(Path.GetTempPath(), $"kalk_print_{Guid.NewGuid():N}.pdf");
                GenerirajPdf(k, temp);
                using (var dlg = new PrintDialog())
                {
                    dlg.Document = new System.Drawing.Printing.PrintDocument
                    { DocumentName = $"Kalkulacija {k.BrojKalkulacije}" };
                    if (dlg.ShowDialog() == DialogResult.OK)
                        Process.Start(new ProcessStartInfo(temp)
                        { Verb = "print", UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greska pri ispisu:\n" + ex.Message,
                    "Greska", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static string GenerirajPdfTemp(Kalkulacija k)
        {
            UcitajPostavkeTvrtke();
            string temp = Path.Combine(Path.GetTempPath(), $"kalk_preview_{Guid.NewGuid():N}.pdf");
            GenerirajPdf(k, temp);
            return temp;
        }

        // ════════════════════════════════════════════════════════════════════
        //  GENERIRANJE — HORIZONTALNI A4
        // ════════════════════════════════════════════════════════════════════

        private static void GenerirajPdf(Kalkulacija k, string putanja)
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            var doc = new PdfDocument();
            doc.Info.Title = $"Kalkulacija {k.BrojKalkulacije}";
            doc.Info.Author = _tvrtka;
            doc.Info.Subject = $"Kalkulacija dobavljac {k.NazivDobavljaca}";

            var page = doc.AddPage();
            // ── HORIZONTALNI format ──────────────────────────────────────────
            page.Size = PdfSharp.PageSize.A4;
            page.Orientation = PdfSharp.PageOrientation.Landscape;

            var g = XGraphics.FromPdfPage(page);

            // A4 landscape: 842 x 595 pt
            double pw = page.Width.Point;   // 842
            double ph = page.Height.Point;  // 595
            double ml = 36, mr = 36;
            double cw = pw - ml - mr;       // ~770 pt
            double y = 32;

            y = CrtajNaslov(g, ml, y, cw, k); y += 12;
            y = CrtajStranke(g, ml, y, cw, k); y += 10;
            y = CrtajPodaciDokumenta(g, ml, y, cw, k); y += 10;
            y = CrtajTablicuStavki(g, ml, y, cw, k); y += 8;
            y = CrtajPdvRekapitulacija(g, ml, y, cw, k); y += 8;
            y = CrtajTotaleIPotpis(g, ml, y, cw, k);

            if (!string.IsNullOrWhiteSpace(k.Napomena))
                CrtajNapomenu(g, ml, y + 6, cw, k.Napomena);

            CrtajFooter(g, ml, ph - 26, cw, k);

            g.Dispose();
            doc.Save(putanja);
        }

        // ════════════════════════════════════════════════════════════════════
        //  1. NASLOV
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajNaslov(XGraphics g, double x, double y, double w, Kalkulacija k)
        {
            g.DrawLine(Pen(cDark, 2.5), x, y, x + w, y);
            y += 7;

            // Lijevo: "KALKULACIJA"
            g.DrawString("KALKULACIJA", FB(20), Brush(cDark), Rect(x, y, 280, 26), FmtL());

            // Sredina: naziv primatelja
            g.DrawString(_tvrtka, FB(11), Brush(cDark),
                Rect(x + 280, y + 4, w - 560, 18), XStringFormats.TopCenter);

            // Desno: broj + status
            g.DrawString(k.BrojKalkulacije, FN(11), Brush(cMid),
                Rect(x, y + 4, w, 18), FmtR());

            y += 28;

            g.DrawLine(Pen(cLight, 0.6), x, y + 13, x + w, y + 13);
            return y + 15;
        }

        // ════════════════════════════════════════════════════════════════════
        //  2. STRANKE — PRIMATELJ | DOBAVLJAC (3 stupca: primatelj | sredina | dobavljac)
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajStranke(XGraphics g, double x, double y, double w, Kalkulacija k)
        {
            double col = (w - 20) / 2;
            double kx = x + col + 20;
            double lh = 11;   // line height
            double gap = 11;

            // ── Naslovi ──────────────────────────────────────────────────────
            g.DrawString("Primatelj:", FB(7.5), Brush(cMid), Rect(x, y, col, lh), FmtL());
            g.DrawString("Dobavljac:", FB(7.5), Brush(cMid), Rect(kx, y, col, lh), FmtL());
            y += 13;

            // ── Primatelj ────────────────────────────────────────────────────
            double ly = y;
            g.DrawString(_tvrtka, FB(8.5), Brush(cDark), Rect(x, ly, col, lh), FmtL()); ly += gap;
            g.DrawString(_adresa, FN(8), Brush(cDark), Rect(x, ly, col, lh), FmtL()); ly += gap;
            g.DrawString("OIB: " + _oib, FN(8), Brush(cDark), Rect(x, ly, col, lh), FmtL()); ly += gap;
            g.DrawString("PDV ID: " + _pdvId, FN(8), Brush(cDark), Rect(x, ly, col, lh), FmtL()); ly += gap;
            g.DrawString(_telefon + "  |  " + _email, FN(7.5), Brush(cMid), Rect(x, ly, col, lh), FmtL());
            double maxLy = ly + lh;

            // ── Dobavljac ─────────────────────────────────────────────────────
            double dy = y;
            g.DrawString(k.NazivDobavljaca ?? "—", FB(8.5), Brush(cDark), Rect(kx, dy, col, lh), FmtL()); dy += gap;
            g.DrawString(k.AdresaDobavljaca ?? "—", FN(8), Brush(cDark), Rect(kx, dy, col, lh), FmtL()); dy += gap;
            g.DrawString("OIB: " + (k.OibDobavljaca ?? "—"), FN(8), Brush(cDark), Rect(kx, dy, col, lh), FmtL()); dy += gap;

            // Vertikalna linija
            double lineX = x + col + 10;
            g.DrawLine(Pen(cLight, 0.5), lineX, y - 13, lineX, maxLy);

            g.DrawLine(Pen(cLight, 0.6), x, maxLy, x + w, maxLy);
            return maxLy;
        }

        // ════════════════════════════════════════════════════════════════════
        //  3. PODACI DOKUMENTA — u jednom retku
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajPodaciDokumenta(XGraphics g, double x, double y, double w, Kalkulacija k)
        {
            double col = w / 4.0;
            string[] naslovi = { "Broj kalkulacije:", "Datum kalkulacije:", "Br. dob. racuna:", "Datum knjizenja:" };
            string[] vrijednosti =
            {
                k.BrojKalkulacije,
                k.DatumKalkulacije.ToString("dd. MM. yyyy."),
                string.IsNullOrEmpty(k.BrojDobavljacevogRacuna) ? "—" : k.BrojDobavljacevogRacuna,
                k.DatumKnjizenja?.ToString("dd. MM. yyyy. HH:mm") ?? "—"
            };

            for (int i = 0; i < 4; i++)
            {
                double cx = x + i * col;
                g.DrawString(naslovi[i], FB(7), Brush(cMid), Rect(cx, y, col - 4, 10), FmtL());
                g.DrawString(vrijednosti[i], FN(8.5), Brush(cDark), Rect(cx, y + 11, col - 4, 12), FmtL());
            }

            double hy = y + 25;
            g.DrawLine(Pen(cLight, 0.6), x, hy, x + w, hy);
            return hy;
        }

        // ════════════════════════════════════════════════════════════════════
        //  4. TABLICA STAVKI — horizontalno, puno stupaca
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajTablicuStavki(XGraphics g, double x, double y, double w, Kalkulacija k)
        {
            // Stupci (suma mora biti == w ~ 770 pt)
            // Rbr | Sifra | Naziv | JM | Kolicina | Nab.cij.bez PDV | PDV% | Iznos PDV | Nab.cij.s PDV | Ukupno bez PDV | Ukupno s PDV
            double[] cw = { 20, 48, 0, 28, 46, 68, 36, 62, 68, 72, 72 };
            //                                  Naziv = ostatak
            double fixed_ = cw.Sum() - cw[2];
            cw[2] = w - fixed_;  // naziv dobiva ostatak (~220 pt)

            string[] hdrs = {
                "Rbr", "Sifra", "Naziv artikla / usluge", "JM",
                "Kolicina", "Cij.bez PDV", "PDV%", "Iznos PDV",
                "Cij.s PDV", "Uk.bez PDV", "Uk.s PDV"
            };
            bool[] right = { false, false, false, true, true, true, true, true, true, true, true };

            double hdrH = 17;
            double rowH = 15;

            // Header
            g.DrawRectangle(Brush(cHdrBg), x, y, w, hdrH);
            double cx = x;
            for (int i = 0; i < hdrs.Length; i++)
            {
                g.DrawString(hdrs[i], FB(6.5), Brush(cWhite),
                    Rect(cx + 2, y + 2, cw[i] - 4, hdrH - 4),
                    right[i] ? FmtR() : FmtL());
                cx += cw[i];
            }
            y += hdrH;

            bool alt = false;
            int rbr = 1;
            int ukRed = 0;

            if (k.Stavke != null)
            {
                foreach (var s in k.Stavke)
                {
                    if (alt) g.DrawRectangle(Brush(cBg), x, y, w, rowH);

                    // Jedinicna cijena BEZ PDV-a
                    decimal jedBezPdv = s.PdvStopa > 0
                        ? s.NabavnaCijena / (1 + s.PdvStopa / 100m)
                        : s.NabavnaCijena;

                    // Iznos PDV-a za jedan kom
                    decimal jedPdvIzn = s.NabavnaCijena - jedBezPdv;

                    string[] vals =
                    {
                        rbr++.ToString(),
                        s.SifraArtikla ?? "",
                        TruncStr(s.NazivArtikla, 45),
                        s.NazivJM ?? "",
                        s.Kolicina.ToString("N3"),
                        jedBezPdv.ToString("N4"),
                        s.PdvStopa.ToString("0") + "%",
                        jedPdvIzn.ToString("N4"),
                        s.NabavnaCijena.ToString("N4"),
                        s.IznosBezPdv.ToString("N2"),
                        s.IznosSaPdv.ToString("N2")
                    };

                    cx = x;
                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawString(vals[i], FN(7), Brush(cDark),
                            Rect(cx + 2, y + 2, cw[i] - 4, rowH - 4),
                            right[i] ? FmtR() : FmtL());
                        cx += cw[i];
                    }

                    g.DrawLine(Pen(cLight, 0.3), x, y + rowH, x + w, y + rowH);
                    y += rowH;
                    alt = !alt;
                    ukRed++;
                }
            }

            // Okvir tablice
            g.DrawRectangle(Pen(cLight, 0.7), x, y - ukRed * rowH - hdrH, w, ukRed * rowH + hdrH);

            return y;
        }

        // ════════════════════════════════════════════════════════════════════
        //  5. PDV REKAPITULACIJA (zakonska obveza)
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajPdvRekapitulacija(XGraphics g, double x, double y, double w, Kalkulacija k)
        {
            if (k.Stavke == null || k.Stavke.Count == 0) return y;

            var grupe = k.Stavke
                .GroupBy(s => s.PdvStopa)
                .OrderBy(gr => gr.Key)
                .Select(gr => new
                {
                    Stopa = gr.Key,
                    BezPdv = gr.Sum(s => s.IznosBezPdv),
                    PdvIzn = gr.Sum(s => s.IznosPdv),
                    SaPdv = gr.Sum(s => s.IznosSaPdv)
                })
                .ToList();

            g.DrawString("Rekapitulacija PDV-a:", FB(7.5), Brush(cMid), Rect(x, y, w, 11), FmtL());
            y += 11;

            double[] cw = { 55, 80, 80, 80 };
            double tw = cw.Sum();
            double tx = x + w - tw;
            double rh = 13;

            // Header
            string[] mh = { "PDV stopa", "Osnovica (EUR)", "Iznos PDV (EUR)", "Ukupno (EUR)" };
            g.DrawRectangle(Brush(cHdrBg), tx, y, tw, rh);
            double cx = tx;
            for (int i = 0; i < mh.Length; i++)
            {
                g.DrawString(mh[i], FB(6.5), Brush(cWhite),
                    Rect(cx + 2, y + 1, cw[i] - 4, rh - 2), FmtR());
                cx += cw[i];
            }
            y += rh;

            bool alt = false;
            int ukRed = 0;
            foreach (var gr in grupe)
            {
                if (alt) g.DrawRectangle(Brush(cBg), tx, y, tw, rh);
                string[] vals = { gr.Stopa.ToString("0") + " %", gr.BezPdv.ToString("N2"), gr.PdvIzn.ToString("N2"), gr.SaPdv.ToString("N2") };
                cx = tx;
                for (int i = 0; i < vals.Length; i++)
                {
                    g.DrawString(vals[i], FN(7), Brush(cDark),
                        Rect(cx + 2, y + 1, cw[i] - 4, rh - 2), FmtR());
                    cx += cw[i];
                }
                g.DrawLine(Pen(cLight, 0.3), tx, y + rh, tx + tw, y + rh);
                y += rh;
                alt = !alt;
                ukRed++;
            }

            g.DrawRectangle(Pen(cLight, 0.7), tx, y - ukRed * rh - rh, tw, ukRed * rh + rh);
            return y;
        }

        // ════════════════════════════════════════════════════════════════════
        //  6. TOTALI + POTPIS (lijevo ukupni iznosi, desno polje za potpis)
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajTotaleIPotpis(XGraphics g, double x, double y, double w, Kalkulacija k)
        {
            double tw = 200;
            double tx = x + w - tw;
            double rh = 14;

            g.DrawLine(Pen(cLight, 0.6), tx, y, tx + tw, y);
            y += 4;

            TotalRed(g, tx, y, tw, rh,
                "Ukupno bez PDV:", $"{k.UkupnoBezPdv:N2} EUR", FN(8), cDark);
            y += rh;
            TotalRed(g, tx, y, tw, rh,
                "Ukupno PDV:", $"{k.UkupnoPdv:N2} EUR", FN(8), cDark);
            y += rh + 2;

            // Crni ukupno red
            g.DrawRectangle(Brush(cDark), tx, y, tw, rh + 6);
            TotalRed(g, tx, y + 3, tw, rh,
                "SVEUKUPNO:", $"{k.UkupnoSaPdv:N2} EUR", FB(9), cWhite);
            y += rh + 10;

            // ── Polja za potpis — lijevo ──────────────────────────────────────
            double potpisX = x;
            double potpisY = y - rh * 2 - 20;

            // Roba primljena od:
            g.DrawString("Primio / odgovorna osoba:", FN(7.5), Brush(cMid),
                Rect(potpisX, potpisY, 180, 11), FmtL());
            potpisY += 13;
            g.DrawLine(Pen(cDark, 0.5), potpisX, potpisY + 12, potpisX + 175, potpisY + 12);
            g.DrawString("(ime, prezime i potpis)", FI(6.5), Brush(cLight),
                Rect(potpisX, potpisY + 14, 175, 10), FmtL());

            // Primio:
            double p2x = potpisX + 200;
            g.DrawString("Datum primitka:", FN(7.5), Brush(cMid),
                Rect(p2x, potpisY - 13, 200, 11), FmtL());
            g.DrawLine(Pen(cDark, 0.5), p2x, potpisY + 12, p2x + 175, potpisY + 12);

            return y;
        }

        // ════════════════════════════════════════════════════════════════════
        //  7. NAPOMENA
        // ════════════════════════════════════════════════════════════════════

        private static void CrtajNapomenu(XGraphics g, double x, double y, double w, string napomena)
        {
            g.DrawString("Napomena:", FB(7.5), Brush(cMid), Rect(x, y, 60, 11), FmtL());
            g.DrawString(napomena, FI(7.5), Brush(cDark), Rect(x + 63, y, w - 63, 11), FmtL());
        }

        // ════════════════════════════════════════════════════════════════════
        //  8. FOOTER
        // ════════════════════════════════════════════════════════════════════

        private static void CrtajFooter(XGraphics g, double x, double y, double w, Kalkulacija k)
        {
            g.DrawLine(Pen(cLight, 0.6), x, y, x + w, y);
            y += 4;
            string l1 = $"{_tvrtka}  |  OIB: {_oib}  |  PDV ID: {_pdvId}";
            string l2 = $"Kalkulacija br. {k.BrojKalkulacije}  |  Generirana: {DateTime.Now:dd. MM. yyyy. HH:mm}  |  Sukladno clanku 7. Zakona o racunovodstvu (NN 78/15)";
            g.DrawString(l1, FN(6), Brush(cMid), Rect(x, y, w, 9), XStringFormats.TopCenter);
            g.DrawString(l2, FI(5.5), Brush(cLight), Rect(x, y + 9, w, 9), XStringFormats.TopCenter);
        }

        // ════════════════════════════════════════════════════════════════════
        //  HELPERI
        // ════════════════════════════════════════════════════════════════════

        private static void TotalRed(XGraphics g, double x, double y, double w, double h,
            string label, string value, XFont font, XColor color)
        {
            g.DrawString(label, font, Brush(color), Rect(x + 5, y, w - 75, h), FmtL());
            g.DrawString(value, font, Brush(color), Rect(x, y, w - 5, h), FmtR());
        }

        private static XStringFormat FmtL() => new XStringFormat
        { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Near };

        private static XStringFormat FmtR() => new XStringFormat
        { Alignment = XStringAlignment.Far, LineAlignment = XLineAlignment.Near };

        private static XSolidBrush Brush(XColor c) => new XSolidBrush(c);
        private static XPen Pen(XColor c, double w) => new XPen(c, w);
        private static XRect Rect(double x, double y, double w, double h) => new XRect(x, y, w, h);

        private static string TruncStr(string s, int max)
        {
            if (s == null) return "";
            return s.Length > max ? s.Substring(0, max - 1) + "..." : s;
        }

        private static string Sanitize(string s)
            => s?.Replace("/", "-").Replace("\\", "-").Replace(":", "-") ?? "kalkulacija";

        private static void UcitajPostavkeTvrtke()
        {
            try
            {
                var p = DatabaseHelper.GetSvePostavke();
                if (p.ContainsKey("NazivTvrtke")) _tvrtka = p["NazivTvrtke"];
                if (p.ContainsKey("OIB")) _oib = p["OIB"];
                if (p.ContainsKey("PdvId")) _pdvId = p["PdvId"];
                if (p.ContainsKey("Telefon")) _telefon = p["Telefon"];
                if (p.ContainsKey("Email")) _email = p["Email"];

                string adr = p.ContainsKey("Adresa") ? p["Adresa"] : "";
                string grad = p.ContainsKey("Grad") ? p["Grad"] : "";
                string pbr = p.ContainsKey("PostanskiBroj") ? p["PostanskiBroj"] : "";
                _adresa = $"{adr}, {pbr} {grad}".Trim(' ', ',');
            }
            catch { }
        }
    }
}