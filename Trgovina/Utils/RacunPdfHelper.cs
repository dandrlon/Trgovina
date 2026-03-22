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

namespace Trgovina.Utils
{
    /// <summary>
    /// Generira crno-bijeli PDF račun sukladan čl. 79. Zakona o PDV-u (NN 73/13).
    /// PdfSharp 6.x (XFontStyleEx).
    /// </summary>
    public static class RacunPdfHelper
    {
        // ── Default postavke tvrtke ───────────────────────────────────────────────────
        private static string _tvrtka = "Moja Trgovina d.o.o.";
        private static string _adresa = "Ulica 123, 10000 Zagreb";
        private static string _oib = "12345678901";
        private static string _pdvId = "HR12345678901";
        private static string _telefon = "+385 1 234 5678";
        private static string _email = "info@mojatvrtka.hr";
        private static string _iban = "HR1210010051863000160";

        // ── Fontovi ───────────────────────────────────────────────────────────
        private static XFont FN(double s) => new XFont("Arial", s, XFontStyleEx.Regular);
        private static XFont FB(double s) => new XFont("Arial", s, XFontStyleEx.Bold);
        private static XFont FI(double s) => new XFont("Arial", s, XFontStyleEx.Italic);

        // ── Crno-bijela paleta ────────────────────────────────────────────────
        private static readonly XColor cBlack = XColors.Black;
        private static readonly XColor cWhite = XColors.White;
        private static readonly XColor cDark = XColor.FromArgb(30, 30, 30);  // gotovo crno
        private static readonly XColor cMid = XColor.FromArgb(90, 90, 90);  // tamnosivo
        private static readonly XColor cLight = XColor.FromArgb(180, 180, 180);  // okviri
        private static readonly XColor cBg = XColor.FromArgb(245, 245, 245);  // alt redci
        private static readonly XColor cHdrBg = XColor.FromArgb(30, 30, 30);  // header tablice
        private static readonly XColor cTotalBg = XColor.FromArgb(30, 30, 30);  // ukupno red

        // ════════════════════════════════════════════════════════════════════
        //  JAVNE METODE
        // ════════════════════════════════════════════════════════════════════

        public static void SpremiPdf(Racun racun)
        {
            if (racun == null) return;
            UcitajPostavkeTvrtke();

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Spremi račun kao PDF";
                dlg.Filter = "PDF datoteke (*.pdf)|*.pdf";
                dlg.FileName = $"Racun_{Sanitize(racun.BrojRacuna)}_{racun.DatumRacuna:yyyyMMdd}.pdf";
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    GenerirajPdf(racun, dlg.FileName);
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

        public static void IspisiRacun(Racun racun)
        {
            if (racun == null) return;
            UcitajPostavkeTvrtke();
            try
            {
                string temp = Path.Combine(Path.GetTempPath(), $"racun_print_{Guid.NewGuid():N}.pdf");
                GenerirajPdf(racun, temp);
                using (var dlg = new PrintDialog())
                {
                    dlg.Document = new System.Drawing.Printing.PrintDocument
                    { DocumentName = $"Racun {racun.BrojRacuna}" };
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

        public static string GenerirajPdfTemp(Racun racun)
        {
            UcitajPostavkeTvrtke();
            string temp = Path.Combine(Path.GetTempPath(), $"racun_preview_{Guid.NewGuid():N}.pdf");
            GenerirajPdf(racun, temp);
            return temp;
        }

        // ════════════════════════════════════════════════════════════════════
        //  GENERIRANJE
        // ════════════════════════════════════════════════════════════════════

        private static void GenerirajPdf(Racun racun, string putanja)
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            var doc = new PdfDocument();
            doc.Info.Title = $"Racun {racun.BrojRacuna}";
            doc.Info.Author = _tvrtka;
            doc.Info.Subject = $"Racun za {racun.NazivKupca}";

            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var g = XGraphics.FromPdfPage(page);

            double pw = page.Width.Point;   // 595 pt
            double ph = page.Height.Point;  // 842 pt
            double ml = 45, mr = 45;
            double cw = pw - ml - mr;       // ~505 pt
            double y = 40;

            y = CrtajNaslov(g, ml, y, cw, racun); y += 16;
            y = CrtajStranke(g, ml, y, cw, racun); y += 14;
            y = CrtajPodaciRacuna(g, ml, y, cw, racun); y += 14;
            y = CrtajTablicuStavki(g, ml, y, cw, racun); y += 10;
            y = CrtajPdvRekapitulacija(g, ml, y, cw, racun); y += 10;
            y = CrtajTotale(g, ml, y, cw, racun); y += 12;

            if (!string.IsNullOrWhiteSpace(racun.Napomena))
            {
                y = CrtajNapomenu(g, ml, y, cw, racun.Napomena);
                y += 10;
            }

            y = CrtajUplata(g, ml, y, cw, racun);

            CrtajFooter(g, ml, ph - 36, cw, racun);

            g.Dispose();
            doc.Save(putanja);
        }

        // ════════════════════════════════════════════════════════════════════
        //  1. NASLOV
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajNaslov(XGraphics g, double x, double y, double w, Racun racun)
        {
            // Debela crna linija na vrhu
            g.DrawLine(Pen(cDark, 2.5), x, y, x + w, y);
            y += 8;

            // "Racun" lijevo
            g.DrawString("Racun", FB(22), Brush(cDark), Rect(x, y, 200, 30), FmtL());

            // Broj racuna desno, sivo
            g.DrawString(racun.BrojRacuna, FN(11), Brush(cMid), Rect(x, y + 5, w, 20), FmtR());

            y += 32;

            // Tanka linija ispod
            g.DrawLine(Pen(cLight, 0.6), x, y + 14, x + w, y + 14);

            return y + 16;
        }

        // ════════════════════════════════════════════════════════════════════
        //  2. PRODAVATELJ | KUPAC
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajStranke(XGraphics g, double x, double y, double w, Racun racun)
        {
            double col = (w - 20) / 2;
            double kx = x + col + 20;
            double lineH = 12;
            double gap = 12;

            // ── Naslovi stupaca ──────────────────────────────────────────────
            g.DrawString("Prodavatelj:", FB(8), Brush(cMid), Rect(x, y, col, lineH), FmtL());
            g.DrawString("Kupac:", FB(8), Brush(cMid), Rect(kx, y, col, lineH), FmtL());
            y += 14;

            // ── Podaci prodavatelja ──────────────────────────────────────────
            double ly = y;
            g.DrawString(_tvrtka, FB(9), Brush(cDark), Rect(x, ly, col, lineH), FmtL()); ly += gap;
            g.DrawString(_adresa, FN(8), Brush(cDark), Rect(x, ly, col, lineH), FmtL()); ly += gap;
            g.DrawString("OIB: " + _oib, FN(8), Brush(cDark), Rect(x, ly, col, lineH), FmtL()); ly += gap;
            g.DrawString("PDV ID: " + _pdvId, FN(8), Brush(cDark), Rect(x, ly, col, lineH), FmtL()); ly += gap;
            g.DrawString(_telefon, FN(8), Brush(cDark), Rect(x, ly, col, lineH), FmtL()); ly += gap;
            g.DrawString(_email, FN(8), Brush(cDark), Rect(x, ly, col, lineH), FmtL());
            double maxLy = ly + lineH;

            // ── Podaci kupca ─────────────────────────────────────────────────
            double ky = y;
            g.DrawString(racun.NazivKupca ?? "—", FB(9), Brush(cDark), Rect(kx, ky, col, lineH), FmtL()); ky += gap;
            g.DrawString(racun.AdresaKupca ?? "—", FN(8), Brush(cDark), Rect(kx, ky, col, lineH), FmtL()); ky += gap;
            g.DrawString("OIB: " + (racun.OibKupca ?? "—"), FN(8), Brush(cDark), Rect(kx, ky, col, lineH), FmtL()); ky += gap;
            g.DrawString("PDV ID: " + (racun.PdvIdKupca ?? "—"), FN(8), Brush(cDark), Rect(kx, ky, col, lineH), FmtL());

            // Vertikalna razdjelnica
            double lineX = x + col + 10;
            g.DrawLine(Pen(cLight, 0.5), lineX, y - 14, lineX, maxLy);

            // Horizontalna linija ispod
            g.DrawLine(Pen(cLight, 0.6), x, maxLy, x + w, maxLy);

            return maxLy;
        }

        // ════════════════════════════════════════════════════════════════════
        //  3. PODACI O RACUNU (datumi, prodavac)
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajPodaciRacuna(XGraphics g, double x, double y, double w, Racun racun)
        {
            g.DrawString("Podaci o racunu:", FB(8), Brush(cMid), Rect(x, y, w, 12), FmtL());
            y += 13;

            double col = w / 4.0;
            string[] naslovi = { "Datum racuna:", "Datum isporuke:", "Datum valute:", "Prodavac:" };
            string[] vrijednosti =
            {
                racun.DatumRacuna.ToString("dd. MM. yyyy."),
                (racun.DatumIsporuke ?? racun.DatumRacuna).ToString("dd. MM. yyyy."),
                racun.DatumValute?.ToString("dd. MM. yyyy.") ?? "—",
                racun.NazivProdavaca ?? "—"
            };

            for (int i = 0; i < 4; i++)
            {
                double cx = x + i * col;
                g.DrawString(naslovi[i], FB(7.5), Brush(cMid), Rect(cx, y, col - 4, 11), FmtL());
                g.DrawString(vrijednosti[i], FN(8.5), Brush(cDark), Rect(cx, y + 12, col - 4, 12), FmtL());
            }

            y += 26;
            g.DrawLine(Pen(cLight, 0.6), x, y, x + w, y);
            return y;
        }

        // ════════════════════════════════════════════════════════════════════
        //  4. TABLICA STAVKI
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajTablicuStavki(XGraphics g, double x, double y, double w, Racun racun)
        {
            // Rbr | Sifra | Naziv | JM | Kol. | Cij.bez PDV | Pop% | PDV% | Bez PDV | S PDV
            double[] cw = { 20, 50, 0, 28, 38, 54, 28, 28, 54, 56 };
            double fixed_ = cw.Sum() - cw[2];
            cw[2] = w - fixed_;

            string[] hdrs = { "Rbr", "Sifra", "Naziv artikla / usluge", "JM", "Kol.", "Cij.bez PDV", "Pop%", "PDV%", "Bez PDV EUR", "S PDV EUR" };
            bool[] right = { false, false, false, true, true, true, true, true, true, true };
            double hdrH = 18;
            double rowH = 16;

            // Header
            g.DrawRectangle(Brush(cHdrBg), x, y, w, hdrH);
            double cx = x;
            for (int i = 0; i < hdrs.Length; i++)
            {
                g.DrawString(hdrs[i], FB(7), Brush(cWhite),
                    Rect(cx + 2, y + 2, cw[i] - 4, hdrH - 4),
                    right[i] ? FmtR() : FmtL());
                cx += cw[i];
            }
            y += hdrH;

            // Redci
            bool alt = false;
            int rbr = 1;
            int ukRedaka = 0;
            if (racun.Stavke != null)
            {
                foreach (var s in racun.Stavke)
                {
                    if (alt) g.DrawRectangle(Brush(cBg), x, y, w, rowH);

                    decimal jedCijBezPdv = s.PdvStopa > 0
                        ? s.ProdajnaCijena / (1 + s.PdvStopa / 100m)
                        : s.ProdajnaCijena;

                    string[] vals =
                    {
                        rbr++.ToString(),
                        s.SifraArtikla ?? "",
                        TruncStr(s.NazivArtikla, 40),
                        s.NazivJediniceMjere ?? "",
                        s.Kolicina.ToString("N3"),
                        jedCijBezPdv.ToString("N4"),
                        s.Popust > 0 ? s.Popust.ToString("N1") + "%" : "—",
                        s.PdvStopa.ToString("0") + "%",
                        s.IznosBezPdv.ToString("N2"),
                        s.IznosSaPdv.ToString("N2")
                    };

                    cx = x;
                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawString(vals[i], FN(7.5), Brush(cDark),
                            Rect(cx + 2, y + 2, cw[i] - 4, rowH - 4),
                            right[i] ? FmtR() : FmtL());
                        cx += cw[i];
                    }

                    g.DrawLine(Pen(cLight, 0.3), x, y + rowH, x + w, y + rowH);
                    y += rowH;
                    alt = !alt;
                    ukRedaka++;
                }
            }

            // Okvir cijele tablice
            g.DrawRectangle(Pen(cLight, 0.7), x, y - ukRedaka * rowH - hdrH,
                w, ukRedaka * rowH + hdrH);

            return y;
        }

        // ════════════════════════════════════════════════════════════════════
        //  5. PDV REKAPITULACIJA
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajPdvRekapitulacija(XGraphics g, double x, double y, double w, Racun racun)
        {
            if (racun.Stavke == null || racun.Stavke.Count == 0) return y;

            var grupe = racun.Stavke
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

            g.DrawString("Podaci o PDV-u:", FB(8), Brush(cMid), Rect(x, y, w, 11), FmtL());
            y += 12;

            double[] cw = { 55, 72, 72, 72 };
            double tw = cw.Sum();
            double tx = x + w - tw;
            double rh = 14;

            // Header
            string[] mh = { "PDV stopa", "Osnovica (EUR)", "Iznos PDV (EUR)", "Ukupno (EUR)" };
            g.DrawRectangle(Brush(cHdrBg), tx, y, tw, rh);
            double cx = tx;
            for (int i = 0; i < mh.Length; i++)
            {
                g.DrawString(mh[i], FB(7), Brush(cWhite),
                    Rect(cx + 2, y + 2, cw[i] - 4, rh - 4), FmtR());
                cx += cw[i];
            }
            y += rh;

            bool alt = false;
            int ukRedaka = 0;
            foreach (var gr in grupe)
            {
                if (alt) g.DrawRectangle(Brush(cBg), tx, y, tw, rh);

                string[] vals =
                {
                    gr.Stopa.ToString("0") + " %",
                    gr.BezPdv.ToString("N2"),
                    gr.PdvIzn.ToString("N2"),
                    gr.SaPdv.ToString("N2")
                };
                cx = tx;
                for (int i = 0; i < vals.Length; i++)
                {
                    g.DrawString(vals[i], FN(7.5), Brush(cDark),
                        Rect(cx + 2, y + 2, cw[i] - 4, rh - 4), FmtR());
                    cx += cw[i];
                }
                g.DrawLine(Pen(cLight, 0.3), tx, y + rh, tx + tw, y + rh);
                y += rh;
                alt = !alt;
                ukRedaka++;
            }

            g.DrawRectangle(Pen(cLight, 0.7), tx, y - ukRedaka * rh - rh, tw, ukRedaka * rh + rh);
            return y;
        }

        // ════════════════════════════════════════════════════════════════════
        //  6. TOTALI
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajTotale(XGraphics g, double x, double y, double w, Racun racun)
        {
            double tw = 230;
            double tx = x + w - tw;
            double rh = 15;

            g.DrawLine(Pen(cLight, 0.6), tx, y, tx + tw, y);
            y += 4;

            TotalRed(g, tx, y, tw, rh, "Ukupno bez PDV:",
                $"{racun.UkupnoBezPdv:N2} EUR", FN(8.5), cDark);
            y += rh;
            TotalRed(g, tx, y, tw, rh, "Ukupno PDV:",
                $"{racun.UkupnoPdv:N2} EUR", FN(8.5), cDark);
            y += rh + 3;

            // Crni pravokutnik — bijeli tekst
            g.DrawRectangle(Brush(cTotalBg), tx, y, tw, rh + 6);
            TotalRed(g, tx, y + 3, tw, rh, "Ukupan iznos EUR:",
                $"{racun.UkupnoSaPdv:N2} EUR", FB(10), cWhite);

            return y + rh + 10;
        }

        // ════════════════════════════════════════════════════════════════════
        //  7. NAPOMENA
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajNapomenu(XGraphics g, double x, double y, double w, string napomena)
        {
            g.DrawString("Napomena:", FB(8), Brush(cMid), Rect(x, y, 62, 12), FmtL());
            g.DrawString(napomena, FI(8), Brush(cDark), Rect(x + 65, y, w - 65, 12), FmtL());
            return y + 14;
        }

        // ════════════════════════════════════════════════════════════════════
        //  8. UPLATA
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajUplata(XGraphics g, double x, double y, double w, Racun racun)
        {
            if (string.IsNullOrWhiteSpace(_iban)) return y;

            g.DrawLine(Pen(cLight, 0.6), x, y, x + w, y);
            y += 6;

            g.DrawString("Podaci o placanju:", FB(8), Brush(cMid), Rect(x, y, 120, 12), FmtL());
            y += 13;

            double labelW = 85;
            g.DrawString("Nacin placanja:", FB(8), Brush(cMid), Rect(x, y, labelW, 12), FmtL());
            g.DrawString(racun.Placeno ? "Kartica / Gotovina" : "Transakcijski racun",
                FN(8.5), Brush(cDark), Rect(x + labelW + 2, y, w - labelW - 2, 12), FmtL());
            y += 12;

            g.DrawString("IBAN:", FB(8), Brush(cMid), Rect(x, y, labelW, 12), FmtL());
            g.DrawString(_iban, FN(8.5), Brush(cDark), Rect(x + labelW + 2, y, 220, 12), FmtL());
            y += 12;

            g.DrawString("Poziv na broj:", FB(8), Brush(cMid), Rect(x, y, labelW, 12), FmtL());
            g.DrawString(racun.BrojRacuna, FN(8.5), Brush(cDark), Rect(x + labelW + 2, y, 220, 12), FmtL());

            return y + 14;
        }

        // ════════════════════════════════════════════════════════════════════
        //  9. FOOTER
        // ════════════════════════════════════════════════════════════════════

        private static void CrtajFooter(XGraphics g, double x, double y, double w, Racun racun)
        {
            g.DrawLine(Pen(cLight, 0.6), x, y, x + w, y);
            y += 5;
            string l1 = $"{_tvrtka}  |  OIB: {_oib}  |  PDV ID: {_pdvId}";
            string l2 = $"Racun br. {racun.BrojRacuna}  |  Generiran: {DateTime.Now:dd. MM. yyyy. HH:mm}  |  Sukladno cl. 79. Zakona o PDV-u (NN 73/13)";
            g.DrawString(l1, FN(6.5), Brush(cMid), Rect(x, y, w, 10), XStringFormats.TopCenter);
            g.DrawString(l2, FI(6), Brush(cLight), Rect(x, y + 11, w, 10), XStringFormats.TopCenter);
        }

        // ════════════════════════════════════════════════════════════════════
        //  HELPERI
        // ════════════════════════════════════════════════════════════════════

        private static void TotalRed(XGraphics g, double x, double y, double w, double h,
            string label, string value, XFont font, XColor color)
        {
            g.DrawString(label, font, Brush(color), Rect(x + 6, y, w - 80, h), FmtL());
            g.DrawString(value, font, Brush(color), Rect(x, y, w - 6, h), FmtR());
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
            => s?.Replace("/", "-").Replace("\\", "-").Replace(":", "-") ?? "racun";

        // ════════════════════════════════════════════════════════════════════
        //  POSTAVKE TVRTKE IZ BAZE
        // ════════════════════════════════════════════════════════════════════

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
                if (p.ContainsKey("IBAN")) _iban = p["IBAN"];

                string adr = p.ContainsKey("Adresa") ? p["Adresa"] : "";
                string grad = p.ContainsKey("Grad") ? p["Grad"] : "";
                string pbr = p.ContainsKey("PostanskiBroj") ? p["PostanskiBroj"] : "";
                _adresa = $"{adr}, {pbr} {grad}".Trim(' ', ',');
            }
            catch { /* tiho — koristimo defaults */ }
        }
    }
}