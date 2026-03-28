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
    /// Generira PDF otpremnicu (bez PDV rekapitulacije).
    /// Isti crno-bijeli stil kao RacunPdfHelper.
    /// </summary>
    public static class OtpremnicaPdfHelper
    {
        // ── Postavke tvrtke ───────────────────────────────────────────────
        private static string _tvrtka = "Moja Trgovina d.o.o.";
        private static string _adresa = "Ulica 123, 10000 Zagreb";
        private static string _oib = "12345678901";
        private static string _pdvId = "HR12345678901";
        private static string _telefon = "+385 1 234 5678";
        private static string _email = "info@mojatvrtka.hr";

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
        private static readonly XColor cHdrBg = XColor.FromArgb(30, 30, 30);

        // ══════════════════════════════════════════════════════════════════
        //  JAVNE METODE
        // ══════════════════════════════════════════════════════════════════

        public static void SpremiPdf(Otpremnica otpremnica)
        {
            if (otpremnica == null) return;
            UcitajPostavkeTvrtke();
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Spremi otpremnicu kao PDF";
                dlg.Filter = "PDF datoteke (*.pdf)|*.pdf";
                dlg.FileName = $"Otpremnica_{Sanitize(otpremnica.BrojOtpremnice)}_{otpremnica.DatumOtpremnice:yyyyMMdd}.pdf";
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    GenerirajPdf(otpremnica, dlg.FileName);
                    if (MessageBox.Show("PDF je uspješno spremljen.\n\nŽelite li ga otvoriti?",
                            "Uspjeh", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Greška pri generiranju PDF-a:\n" + ex.Message,
                        "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static string GenerirajPdfTemp(Otpremnica otpremnica)
        {
            UcitajPostavkeTvrtke();
            string temp = Path.Combine(Path.GetTempPath(), $"otpremnica_preview_{Guid.NewGuid():N}.pdf");
            GenerirajPdf(otpremnica, temp);
            return temp;
        }

        // ══════════════════════════════════════════════════════════════════
        //  GENERIRANJE
        // ══════════════════════════════════════════════════════════════════

        private static void GenerirajPdf(Otpremnica otpremnica, string putanja)
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            var doc = new PdfDocument();
            doc.Info.Title = $"Otpremnica {otpremnica.BrojOtpremnice}";
            doc.Info.Author = _tvrtka;
            doc.Info.Subject = $"Otpremnica za {otpremnica.NazivKupca}";

            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var g = XGraphics.FromPdfPage(page);

            double pw = page.Width.Point;
            double ph = page.Height.Point;
            double ml = 45, mr = 45;
            double cw = pw - ml - mr;
            double y = 40;

            y = CrtajNaslov(g, ml, y, cw, otpremnica); y += 16;
            y = CrtajStranke(g, ml, y, cw, otpremnica); y += 14;
            y = CrtajPodaci(g, ml, y, cw, otpremnica); y += 14;
            y = CrtajTablica(g, ml, y, cw, otpremnica); y += 10;
            y = CrtajUkupno(g, ml, y, cw, otpremnica); y += 12;

            if (!string.IsNullOrWhiteSpace(otpremnica.Napomena))
            {
                y = CrtajNapomenu(g, ml, y, cw, otpremnica.Napomena);
                y += 10;
            }

            y = CrtajPotpis(g, ml, y, cw);

            CrtajFooter(g, ml, ph - 36, cw, otpremnica);

            g.Dispose();
            doc.Save(putanja);
        }

        // ── 1. NASLOV ─────────────────────────────────────────────────────

        private static double CrtajNaslov(XGraphics g, double x, double y, double w, Otpremnica o)
        {
            g.DrawLine(Pen(cDark, 2.5), x, y, x + w, y); y += 8;

            g.DrawString("Otpremnica", FB(22), Brush(cDark), Rect(x, y, 200, 30), FmtL());
            g.DrawString(o.BrojOtpremnice, FN(11), Brush(cMid), Rect(x, y + 5, w, 20), FmtR());

            // Status badge (ako je isporučena ili fakturirana)
            if (o.Isporuceno || o.Fakturirano)
            {
                string badge = o.Fakturirano ? "FAKTURIRANA" : "ISPORUČENA";
                g.DrawRectangle(Brush(o.Fakturirano ? cMid : cDark),
                    x + w - 105, y + 2, 100, 16);
                g.DrawString(badge, FB(7), Brush(cWhite),
                    Rect(x + w - 105, y + 2, 100, 16), FmtC());
            }

            y += 32;
            g.DrawLine(Pen(cLight, 0.6), x, y + 14, x + w, y + 14);
            return y + 16;
        }

        // ── 2. STRANKE ────────────────────────────────────────────────────

        private static double CrtajStranke(XGraphics g, double x, double y, double w, Otpremnica o)
        {
            double col = (w - 20) / 2;
            double kx = x + col + 20;
            double lineH = 12, gap = 12;

            g.DrawString("Isporučuje:", FB(8), Brush(cMid), Rect(x, y, col, lineH), FmtL());
            g.DrawString("Prima:", FB(8), Brush(cMid), Rect(kx, y, col, lineH), FmtL());
            y += 14;

            double ly = y;
            g.DrawString(_tvrtka, FB(9), Brush(cDark), Rect(x, ly, col, lineH), FmtL()); ly += gap;
            g.DrawString(_adresa, FN(8), Brush(cDark), Rect(x, ly, col, lineH), FmtL()); ly += gap;
            g.DrawString("OIB: " + _oib, FN(8), Brush(cDark), Rect(x, ly, col, lineH), FmtL()); ly += gap;
            g.DrawString(_telefon, FN(8), Brush(cDark), Rect(x, ly, col, lineH), FmtL()); ly += gap;
            g.DrawString(_email, FN(8), Brush(cDark), Rect(x, ly, col, lineH), FmtL());
            double maxLy = ly + lineH;

            double ky = y;
            g.DrawString(o.NazivKupca ?? "—", FB(9), Brush(cDark), Rect(kx, ky, col, lineH), FmtL()); ky += gap;
            g.DrawString(o.AdresaKupca ?? "—", FN(8), Brush(cDark), Rect(kx, ky, col, lineH), FmtL()); ky += gap;
            g.DrawString("OIB: " + (o.OibKupca ?? "—"), FN(8), Brush(cDark), Rect(kx, ky, col, lineH), FmtL());

            double lx = x + col + 10;
            g.DrawLine(Pen(cLight, 0.5), lx, y - 14, lx, maxLy);
            g.DrawLine(Pen(cLight, 0.6), x, maxLy, x + w, maxLy);
            return maxLy;
        }

        // ── 3. PODACI ─────────────────────────────────────────────────────

        private static double CrtajPodaci(XGraphics g, double x, double y, double w, Otpremnica o)
        {
            g.DrawString("Podaci o otpremnici:", FB(8), Brush(cMid), Rect(x, y, w, 12), FmtL());
            y += 13;

            double col = w / 4.0;
            string[] naslovi = { "Datum otpremnice:", "Datum isporuke:", "Prodavač:", "Vezani račun:" };
            string[] vrijednosti =
            {
                o.DatumOtpremnice.ToString("dd. MM. yyyy."),
                o.DatumIsporuke?.ToString("dd. MM. yyyy.") ?? "—",
                o.NazivProdavaca ?? "—",
                o.BrojRacuna     ?? "—"
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

        private static double CrtajTablica(XGraphics g, double x, double y, double w, Otpremnica o)
        {
            // Rbr | Šifra | Naziv | JM | Količina | Cijena bez PDV | Iznos bez PDV
            double[] cw = { 22, 55, 0, 30, 46, 70, 72 };
            double fixed_ = cw.Sum() - cw[2];
            cw[2] = w - fixed_;

            string[] hdrs = { "Rbr", "Šifra", "Naziv artikla / usluge", "JM", "Količina", "Cijena bez PDV", "Iznos bez PDV" };
            bool[] right = { false, false, false, true, true, true, true };

            double hdrH = 18, rowH = 16;

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

            bool alt = false; int rbr = 1; int ukRedaka = 0;
            if (o.Stavke != null)
            {
                foreach (var s in o.Stavke)
                {
                    if (alt) g.DrawRectangle(Brush(cBg), x, y, w, rowH);

                    string[] vals =
                    {
                        rbr++.ToString(),
                        s.SifraArtikla   ?? "",
                        TruncStr(s.NazivArtikla, 42),
                        s.NazivJediniceMjere ?? "",
                        s.Kolicina.ToString("N3"),
                        s.CijenaBezPdv.ToString("N4"),
                        s.IznosBezPdv.ToString("N2")
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
                    y += rowH; alt = !alt; ukRedaka++;
                }
            }

            g.DrawRectangle(Pen(cLight, 0.7), x, y - ukRedaka * rowH - hdrH, w, ukRedaka * rowH + hdrH);
            return y;
        }

        // ── 5. UKUPNO ─────────────────────────────────────────────────────

        private static double CrtajUkupno(XGraphics g, double x, double y, double w, Otpremnica o)
        {
            double tw = 230, rh = 18;
            double tx = x + w - tw;

            g.DrawLine(Pen(cLight, 0.6), tx, y, tx + tw, y); y += 4;

            // Crni pravokutnik — ukupno
            g.DrawRectangle(Brush(cHdrBg), tx, y, tw, rh + 4);
            g.DrawString("Ukupno bez PDV:", FB(9), Brush(cWhite), Rect(tx + 6, y + 2, tw - 80, rh), FmtL());
            g.DrawString($"{o.UkupnoVrijednost:N2} EUR", FB(9), Brush(cWhite), Rect(tx, y + 2, tw - 6, rh), FmtR());

            return y + rh + 8;
        }

        // ── 6. NAPOMENA ───────────────────────────────────────────────────

        private static double CrtajNapomenu(XGraphics g, double x, double y, double w, string napomena)
        {
            g.DrawString("Napomena:", FB(8), Brush(cMid), Rect(x, y, 62, 12), FmtL());
            g.DrawString(napomena, FI(8), Brush(cDark), Rect(x + 65, y, w - 65, 12), FmtL());
            return y + 14;
        }

        // ── 7. POTPIS ─────────────────────────────────────────────────────

        private static double CrtajPotpis(XGraphics g, double x, double y, double w)
        {
            y += 20;
            double polW = 160;

            // Isporučio
            g.DrawLine(Pen(cLight, 0.6), x, y + 24, x + polW, y + 24);
            g.DrawString("Isporučio:", FN(8), Brush(cMid), Rect(x, y + 28, polW, 12), FmtL());

            // Primio
            double px = x + w - polW;
            g.DrawLine(Pen(cLight, 0.6), px, y + 24, px + polW, y + 24);
            g.DrawString("Primio:", FN(8), Brush(cMid), Rect(px, y + 28, polW, 12), FmtL());

            return y + 42;
        }

        // ── 8. FOOTER ────────────────────────────────────────────────────

        private static void CrtajFooter(XGraphics g, double x, double y, double w, Otpremnica o)
        {
            g.DrawLine(Pen(cLight, 0.6), x, y, x + w, y); y += 5;
            string l1 = $"{_tvrtka}  |  OIB: {_oib}  |  PDV ID: {_pdvId}";
            string l2 = $"Otpremnica br. {o.BrojOtpremnice}  |  Generirana: {DateTime.Now:dd. MM. yyyy. HH:mm}";
            g.DrawString(l1, FN(6.5), Brush(cMid), Rect(x, y, w, 10), XStringFormats.TopCenter);
            g.DrawString(l2, FI(6), Brush(cLight), Rect(x, y + 11, w, 10), XStringFormats.TopCenter);
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERI
        // ══════════════════════════════════════════════════════════════════

        private static XStringFormat FmtL() => new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Near };
        private static XStringFormat FmtR() => new XStringFormat { Alignment = XStringAlignment.Far, LineAlignment = XLineAlignment.Near };
        private static XStringFormat FmtC() => new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };

        private static XSolidBrush Brush(XColor c) => new XSolidBrush(c);
        private static XPen Pen(XColor c, double w) => new XPen(c, w);
        private static XRect Rect(double x, double y, double w, double h) => new XRect(x, y, w, h);

        private static string TruncStr(string s, int max)
            => s == null ? "" : s.Length > max ? s.Substring(0, max - 1) + "..." : s;

        private static string Sanitize(string s)
            => s?.Replace("/", "-").Replace("\\", "-").Replace(":", "-") ?? "otpremnica";

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