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
    /// Generira PDF ponudu.
    /// Layout: naslov → stranke → podaci ponude → tablica stavki → PDV rekapitulacija
    ///         → totali → uvjeti plaćanja + rok isporuke → footer
    /// </summary>
    public static class PonudaPdfHelper
    {
        // ── Postavke tvrtke ───────────────────────────────────────────────
        private static string _tvrtka = "Moja Trgovina d.o.o.";
        private static string _adresa = "Ulica 123, 10000 Zagreb";
        private static string _oib = "12345678901";
        private static string _pdvId = "HR12345678901";
        private static string _telefon = "+385 1 234 5678";
        private static string _email = "info@mojatvrtka.hr";
        private static string _iban = "HR1210010051863000160";

        // ── Fontovi ───────────────────────────────────────────────────────
        private static XFont FN(double s) => new XFont("Arial", s, XFontStyleEx.Regular);
        private static XFont FB(double s) => new XFont("Arial", s, XFontStyleEx.Bold);
        private static XFont FI(double s) => new XFont("Arial", s, XFontStyleEx.Italic);

        // ── Paleta ────────────────────────────────────────────────────────
        private static readonly XColor cBlack = XColors.Black;
        private static readonly XColor cWhite = XColors.White;
        private static readonly XColor cDark = XColor.FromArgb(30, 30, 30);
        private static readonly XColor cMid = XColor.FromArgb(90, 90, 90);
        private static readonly XColor cLight = XColor.FromArgb(180, 180, 180);
        private static readonly XColor cBg = XColor.FromArgb(245, 245, 245);
        private static readonly XColor cHdr = XColor.FromArgb(30, 30, 30);

        // ══════════════════════════════════════════════════════════════════
        //  JAVNE METODE
        // ══════════════════════════════════════════════════════════════════

        public static void SpremiPdf(Ponuda ponuda)
        {
            if (ponuda == null) return;
            UcitajPostavkeTvrtke();
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Spremi ponudu kao PDF";
                dlg.Filter = "PDF datoteke (*.pdf)|*.pdf";
                dlg.FileName = $"Ponuda_{Sanitize(ponuda.BrojPonude)}_{ponuda.DatumPonude:yyyyMMdd}.pdf";
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    GenerirajPdf(ponuda, dlg.FileName);
                    if (MessageBox.Show("PDF je uspješno spremljen.\n\nŽelite li ga otvoriti?",
                            "Uspjeh", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Greška:\n" + ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static string GenerirajPdfTemp(Ponuda ponuda)
        {
            UcitajPostavkeTvrtke();
            string temp = Path.Combine(Path.GetTempPath(), $"ponuda_preview_{Guid.NewGuid():N}.pdf");
            GenerirajPdf(ponuda, temp);
            return temp;
        }

        // ══════════════════════════════════════════════════════════════════
        //  GENERIRANJE
        // ══════════════════════════════════════════════════════════════════

        private static void GenerirajPdf(Ponuda ponuda, string putanja)
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            var doc = new PdfDocument();
            doc.Info.Title = $"Ponuda {ponuda.BrojPonude}";
            doc.Info.Author = _tvrtka;
            doc.Info.Subject = $"Ponuda za {ponuda.NazivKupca}";

            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var g = XGraphics.FromPdfPage(page);

            double pw = page.Width.Point;
            double ph = page.Height.Point;
            double ml = 45, mr = 45;
            double cw = pw - ml - mr;
            double y = 40;

            y = CrtajNaslov(g, ml, y, cw, ponuda); y += 16;
            y = CrtajStranke(g, ml, y, cw, ponuda); y += 14;
            y = CrtajPodaci(g, ml, y, cw, ponuda); y += 14;
            y = CrtajTablica(g, ml, y, cw, ponuda); y += 10;
            y = CrtajPdvRekapitulacija(g, ml, y, cw, ponuda); y += 10;
            y = CrtajTotale(g, ml, y, cw, ponuda); y += 14;
            y = CrtajUvjeti(g, ml, y, cw, ponuda); y += 10;

            if (!string.IsNullOrWhiteSpace(ponuda.Napomena))
            {
                y = CrtajNapomenu(g, ml, y, cw, ponuda.Napomena);
                y += 8;
            }

            CrtajFooter(g, ml, ph - 36, cw, ponuda);
            g.Dispose();
            doc.Save(putanja);
        }

        // ── 1. NASLOV ─────────────────────────────────────────────────────

        private static double CrtajNaslov(XGraphics g, double x, double y, double w, Ponuda p)
        {
            g.DrawLine(Pen(cDark, 2.5), x, y, x + w, y); y += 8;
            g.DrawString("Ponuda / Predračun", FB(20), Brush(cDark), Rect(x, y, 250, 30), FmtL());
            g.DrawString(p.BrojPonude, FN(11), Brush(cMid), Rect(x, y + 5, w, 20), FmtR());

            y += 32;
            g.DrawLine(Pen(cLight, 0.6), x, y + 14, x + w, y + 14);
            return y + 16;
        }

        // ── 2. STRANKE ────────────────────────────────────────────────────

        private static double CrtajStranke(XGraphics g, double x, double y, double w, Ponuda p)
        {
            double col = (w - 20) / 2, kx = x + col + 20;
            double lineH = 12, gap = 12;

            g.DrawString("Ponuditelj:", FB(8), Brush(cMid), Rect(x, y, col, lineH), FmtL());
            g.DrawString("Naručitelj:", FB(8), Brush(cMid), Rect(kx, y, col, lineH), FmtL());
            y += 14;

            double ly = y;
            g.DrawString(_tvrtka, FB(9), Brush(cDark), Rect(x, ly, col, lineH), FmtL()); ly += gap;
            g.DrawString(_adresa, FN(8), Brush(cDark), Rect(x, ly, col, lineH), FmtL()); ly += gap;
            g.DrawString("OIB: " + _oib, FN(8), Brush(cDark), Rect(x, ly, col, lineH), FmtL()); ly += gap;
            g.DrawString(_telefon, FN(8), Brush(cDark), Rect(x, ly, col, lineH), FmtL()); ly += gap;
            g.DrawString(_email, FN(8), Brush(cDark), Rect(x, ly, col, lineH), FmtL());
            double maxLy = ly + lineH;

            double ky = y;
            g.DrawString(p.NazivKupca ?? "—", FB(9), Brush(cDark), Rect(kx, ky, col, lineH), FmtL()); ky += gap;
            g.DrawString(p.AdresaKupca ?? "—", FN(8), Brush(cDark), Rect(kx, ky, col, lineH), FmtL()); ky += gap;
            g.DrawString("OIB: " + (p.OibKupca ?? "—"), FN(8), Brush(cDark), Rect(kx, ky, col, lineH), FmtL());

            g.DrawLine(Pen(cLight, 0.5), x + col + 10, y - 14, x + col + 10, maxLy);
            g.DrawLine(Pen(cLight, 0.6), x, maxLy, x + w, maxLy);
            return maxLy;
        }

        // ── 3. PODACI PONUDE ──────────────────────────────────────────────

        private static double CrtajPodaci(XGraphics g, double x, double y, double w, Ponuda p)
        {
            g.DrawString("Podaci o ponudi:", FB(8), Brush(cMid), Rect(x, y, w, 12), FmtL());
            y += 13;

            double col = w / 4.0;
            string[] naslovi = { "Datum ponude:", "Vrijedi do:", "Prodavač:", "Vezani dokument:" };
            string[] vrijednosti =
            {
                p.DatumPonude.ToString("dd. MM. yyyy."),
                p.DatumVazenja?.ToString("dd. MM. yyyy.") ?? "—",
                p.NazivProdavaca ?? "—",
                p.BrojOtpremnice ?? p.BrojRacuna ?? "—"
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

        // ── 4. TABLICA STAVKI ─────────────────────────────────────────────

        private static double CrtajTablica(XGraphics g, double x, double y, double w, Ponuda p)
        {
            double[] cw = { 20, 50, 0, 28, 38, 50, 28, 28, 52, 54 };
            double fixed_ = cw.Sum() - cw[2];
            cw[2] = w - fixed_;

            string[] hdrs = { "Rbr", "Šifra", "Naziv artikla", "JM", "Kol.", "Cij.bez PDV", "Pop%", "PDV%", "Bez PDV", "S PDV" };
            bool[] right = { false, false, false, true, true, true, true, true, true, true };
            double hdrH = 18, rowH = 16;

            g.DrawRectangle(Brush(cHdr), x, y, w, hdrH);
            double cx = x;
            for (int i = 0; i < hdrs.Length; i++)
            {
                g.DrawString(hdrs[i], FB(7), Brush(cWhite), Rect(cx + 2, y + 2, cw[i] - 4, hdrH - 4),
                    right[i] ? FmtR() : FmtL());
                cx += cw[i];
            }
            y += hdrH;

            bool alt = false; int rbr = 1; int ukR = 0;
            if (p.Stavke != null)
            {
                foreach (var s in p.Stavke)
                {
                    if (alt) g.DrawRectangle(Brush(cBg), x, y, w, rowH);
                    string[] vals =
                    {
                        rbr++.ToString(), s.SifraArtikla ?? "", TruncStr(s.NazivArtikla, 38),
                        s.NazivJediniceMjere ?? "",
                        s.Kolicina.ToString("N3"),
                        s.CijenaBezPdv.ToString("N2"),
                        s.Popust > 0 ? s.Popust.ToString("N1") + "%" : "—",
                        s.PdvStopa.ToString("0") + "%",
                        s.IznosBezPdv.ToString("N2"),
                        s.IznosSaPdv.ToString("N2")
                    };
                    cx = x;
                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawString(vals[i], FN(7.5), Brush(cDark),
                            Rect(cx + 2, y + 2, cw[i] - 4, rowH - 4), right[i] ? FmtR() : FmtL());
                        cx += cw[i];
                    }
                    g.DrawLine(Pen(cLight, 0.3), x, y + rowH, x + w, y + rowH);
                    y += rowH; alt = !alt; ukR++;
                }
            }
            g.DrawRectangle(Pen(cLight, 0.7), x, y - ukR * rowH - hdrH, w, ukR * rowH + hdrH);
            return y;
        }

        // ── 5. PDV REKAPITULACIJA ─────────────────────────────────────────

        private static double CrtajPdvRekapitulacija(XGraphics g, double x, double y, double w, Ponuda p)
        {
            if (p.Stavke == null || p.Stavke.Count == 0) return y;

            var grupe = p.Stavke
                .GroupBy(s => s.PdvStopa).OrderBy(gr => gr.Key)
                .Select(gr => new { Stopa = gr.Key, BezPdv = gr.Sum(s => s.IznosBezPdv), PdvIzn = gr.Sum(s => s.IznosPdv), SaPdv = gr.Sum(s => s.IznosSaPdv) })
                .ToList();

            g.DrawString("Pregled PDV-a:", FB(8), Brush(cMid), Rect(x, y, w, 11), FmtL()); y += 12;

            double[] cw = { 55, 72, 72, 72 };
            double tw = cw.Sum(), tx = x + w - tw;
            double rh = 14;

            string[] mh = { "PDV stopa", "Osnovica (€)", "Iznos PDV (€)", "Ukupno (€)" };
            g.DrawRectangle(Brush(cHdr), tx, y, tw, rh);
            double cx = tx;
            for (int i = 0; i < mh.Length; i++)
            {
                g.DrawString(mh[i], FB(7), Brush(cWhite), Rect(cx + 2, y + 2, cw[i] - 4, rh - 4), FmtR());
                cx += cw[i];
            }
            y += rh;

            bool alt = false; int ukR = 0;
            foreach (var gr in grupe)
            {
                if (alt) g.DrawRectangle(Brush(cBg), tx, y, tw, rh);
                string[] vals = { gr.Stopa.ToString("0") + " %", gr.BezPdv.ToString("N2"), gr.PdvIzn.ToString("N2"), gr.SaPdv.ToString("N2") };
                cx = tx;
                for (int i = 0; i < vals.Length; i++)
                {
                    g.DrawString(vals[i], FN(7.5), Brush(cDark), Rect(cx + 2, y + 2, cw[i] - 4, rh - 4), FmtR());
                    cx += cw[i];
                }
                g.DrawLine(Pen(cLight, 0.3), tx, y + rh, tx + tw, y + rh);
                y += rh; alt = !alt; ukR++;
            }
            g.DrawRectangle(Pen(cLight, 0.7), tx, y - ukR * rh - rh, tw, ukR * rh + rh);
            return y;
        }

        // ── 6. TOTALI ─────────────────────────────────────────────────────

        private static double CrtajTotale(XGraphics g, double x, double y, double w, Ponuda p)
        {
            double tw = 230, tx = x + w - tw, rh = 15;
            g.DrawLine(Pen(cLight, 0.6), tx, y, tx + tw, y); y += 4;
            TotalRed(g, tx, y, tw, rh, "Ukupno bez PDV:", $"{p.UkupnoBezPdv:N2} EUR", FN(8.5), cDark); y += rh;
            TotalRed(g, tx, y, tw, rh, "Ukupno PDV:", $"{p.UkupnoPdv:N2} EUR", FN(8.5), cDark); y += rh + 3;
            g.DrawRectangle(Brush(cHdr), tx, y, tw, rh + 6);
            TotalRed(g, tx, y + 3, tw, rh, "UKUPAN IZNOS:", $"{p.UkupnoSaPdv:N2} EUR", FB(10), cWhite);
            return y + rh + 10;
        }

        // ── 7. UVJETI PLAĆANJA + ROK ISPORUKE ────────────────────────────

        private static double CrtajUvjeti(XGraphics g, double x, double y, double w, Ponuda p)
        {
            bool imaUvjete = !string.IsNullOrWhiteSpace(p.UvjetiPlacanja);
            bool imaRok = !string.IsNullOrWhiteSpace(p.RokIsporuke);
            if (!imaUvjete && !imaRok) return y;

            g.DrawLine(Pen(cLight, 0.6), x, y, x + w, y); y += 6;
            g.DrawString("Uvjeti ponude:", FB(8), Brush(cMid), Rect(x, y, w, 12), FmtL()); y += 13;

            double col = (w - 20) / 2, rx = x + col + 20;

            if (imaUvjete)
            {
                g.DrawString("Uvjeti plaćanja:", FB(7.5), Brush(cMid), Rect(x, y, col, 11), FmtL());
                g.DrawString(p.UvjetiPlacanja, FN(8.5), Brush(cDark), Rect(x, y + 12, col, 14), FmtL());
            }
            if (imaRok)
            {
                g.DrawString("Rok isporuke:", FB(7.5), Brush(cMid), Rect(rx, y, col, 11), FmtL());
                g.DrawString(p.RokIsporuke, FN(8.5), Brush(cDark), Rect(rx, y + 12, col, 14), FmtL());
            }

            y += 30;

            // Valjanost ponude
            if (p.DatumVazenja.HasValue)
            {
                g.DrawString($"Ponuda vrijedi do: {p.DatumVazenja.Value:dd. MM. yyyy.}",
                    FI(8), Brush(cMid), Rect(x, y, w, 12), FmtL());
                y += 14;
            }

            return y;
        }

        // ── 8. NAPOMENA ───────────────────────────────────────────────────

        private static double CrtajNapomenu(XGraphics g, double x, double y, double w, string napomena)
        {
            g.DrawString("Napomena:", FB(8), Brush(cMid), Rect(x, y, 62, 12), FmtL());
            g.DrawString(napomena, FI(8), Brush(cDark), Rect(x + 65, y, w - 65, 12), FmtL());
            return y + 14;
        }

        // ── 9. FOOTER ────────────────────────────────────────────────────

        private static void CrtajFooter(XGraphics g, double x, double y, double w, Ponuda p)
        {
            g.DrawLine(Pen(cLight, 0.6), x, y, x + w, y); y += 5;
            string l1 = $"{_tvrtka}  |  OIB: {_oib}  |  PDV ID: {_pdvId}";
            string l2 = $"Ponuda br. {p.BrojPonude}  |  Generirana: {DateTime.Now:dd. MM. yyyy. HH:mm}";
            g.DrawString(l1, FN(6.5), Brush(cMid), Rect(x, y, w, 10), XStringFormats.TopCenter);
            g.DrawString(l2, FI(6), Brush(cLight), Rect(x, y + 11, w, 10), XStringFormats.TopCenter);
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERI
        // ══════════════════════════════════════════════════════════════════

        private static void TotalRed(XGraphics g, double x, double y, double w, double h,
            string label, string value, XFont font, XColor color)
        {
            g.DrawString(label, font, Brush(color), Rect(x + 6, y, w - 80, h), FmtL());
            g.DrawString(value, font, Brush(color), Rect(x, y, w - 6, h), FmtR());
        }

        private static XStringFormat FmtL() => new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Near };
        private static XStringFormat FmtR() => new XStringFormat { Alignment = XStringAlignment.Far, LineAlignment = XLineAlignment.Near };

        private static XSolidBrush Brush(XColor c) => new XSolidBrush(c);
        private static XPen Pen(XColor c, double w) => new XPen(c, w);
        private static XRect Rect(double x, double y, double w, double h) => new XRect(x, y, w, h);

        private static string TruncStr(string s, int max)
            => s == null ? "" : s.Length > max ? s.Substring(0, max - 1) + "..." : s;
        private static string Sanitize(string s)
            => s?.Replace("/", "-").Replace("\\", "-").Replace(":", "-") ?? "ponuda";

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
            catch { }
        }
    }
}