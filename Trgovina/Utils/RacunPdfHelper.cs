using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Data.Models;

namespace Trgovina.Utils
{
    /// <summary>
    /// Generira PDF račun koristeći PdfSharp 6.x.
    /// PdfSharp 6.x koristi XFontStyleEx umjesto XFontStyle.
    /// </summary>
    public static class RacunPdfHelper
    {
        // ── Postavke tvrtke ───────────────────────────────────────────────────
        private static string _tvrtka = "Moja Trgovina d.o.o.";
        private static string _adresa = "Ulica 123, 10000 Zagreb";
        private static string _oib = "12345678901";
        private static string _telefon = "+385 1 234 5678";
        private static string _email = "info@mojatvrtka.hr";

        // ── Fontovi (PdfSharp 6.x — XFontStyleEx) ────────────────────────────
        private static XFont FN(double size) => new XFont("Arial", size, XFontStyleEx.Regular);
        private static XFont FB(double size) => new XFont("Arial", size, XFontStyleEx.Bold);
        private static XFont FI(double size) => new XFont("Arial", size, XFontStyleEx.Italic);

        // ── Boje ──────────────────────────────────────────────────────────────
        private static readonly XColor cPrimary = XColor.FromArgb(67, 97, 238);
        private static readonly XColor cHeader = XColor.FromArgb(44, 62, 80);
        private static readonly XColor cAlt = XColor.FromArgb(248, 249, 252);
        private static readonly XColor cBorder = XColor.FromArgb(220, 222, 235);
        private static readonly XColor cSuccess = XColor.FromArgb(39, 174, 96);
        private static readonly XColor cWarning = XColor.FromArgb(230, 126, 34);
        private static readonly XColor cInfo = XColor.FromArgb(41, 128, 185);
        private static readonly XColor cLightBg = XColor.FromArgb(235, 240, 255);
        private static readonly XColor cGray = XColor.FromArgb(110, 110, 120);

        // ══════════════════════════════════════════════════════════════════════
        //  JAVNE METODE
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Otvori SaveFileDialog i spremi PDF na disk.
        /// </summary>
        public static void SpremiPdf(Racun racun)
        {
            if (racun == null) return;
            UcitajPostavkeTvrtke();

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Spremi račun kao PDF";
                dlg.Filter = "PDF datoteke (*.pdf)|*.pdf";
                dlg.FileName = $"Racun_{racun.BrojRacuna.Replace("/", "-").Replace("\\", "-")}_{racun.DatumRacuna:yyyyMMdd}.pdf";
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    GenerirajPdf(racun, dlg.FileName);

                    if (MessageBox.Show(
                            "PDF je uspješno spremljen.\n\nŽelite li ga otvoriti?",
                            "Uspjeh", MessageBoxButtons.YesNo, MessageBoxIcon.Information)
                        == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Greška pri generiranju PDF-a:\n" + ex.Message,
                        "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Generira PDF u temp, otvori print dialog i pošalji na pisač.
        /// </summary>
        public static void IspisiRacun(Racun racun)
        {
            if (racun == null) return;
            UcitajPostavkeTvrtke();

            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(),
                    $"racun_print_{Guid.NewGuid():N}.pdf");

                GenerirajPdf(racun, tempPath);

                using (var dlg = new PrintDialog())
                {
                    var doc = new System.Drawing.Printing.PrintDocument();
                    doc.DocumentName = $"Račun {racun.BrojRacuna}";
                    dlg.Document = doc;

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        Process.Start(new ProcessStartInfo(tempPath)
                        {
                            Verb = "print",
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri ispisu:\n" + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  GENERIRANJE PDF-a
        // ══════════════════════════════════════════════════════════════════════

        private static void GenerirajPdf(Racun racun, string putanja)
        {
            // Enable Windows fonts (important for PdfSharp 6.x)
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            var doc = new PdfDocument();
            doc.Info.Title = $"Račun {racun.BrojRacuna}";
            doc.Info.Author = _tvrtka;
            doc.Info.Subject = $"Račun za kupca {racun.NazivKupca}";

            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var g = XGraphics.FromPdfPage(page);

            double pw = page.Width.Point;
            double ml = 40, mr = 40;
            double cw = pw - ml - mr;
            double y = ml;

            y = CrtajHeader(g, ml, y, cw, racun);
            y = CrtajInfoBoxove(g, ml, y + 10, cw, racun);
            y = CrtajStavke(g, ml, y + 10, cw, racun);
            y = CrtajTotale(g, ml, y + 8, cw, racun);

            if (!string.IsNullOrWhiteSpace(racun.Napomena))
                CrtajNapomenu(g, ml, y + 8, cw, racun.Napomena);

            CrtajFooter(g, ml, page.Height.Point - 36, cw, racun);

            g.Dispose();
            doc.Save(putanja);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  SEKCIJE
        // ══════════════════════════════════════════════════════════════════════

        private static double CrtajHeader(XGraphics g, double x, double y, double w, Racun racun)
        {
            double h = 64;
            g.DrawRectangle(Brush(cPrimary), x, y, w, h);

            // "RAČUN" naslov
            g.DrawString("RAČUN", FB(22), Brush(XColors.White),
                Rect(x + 12, y + 8, 160, 30), XStringFormats.TopLeft);

            // Broj računa ispod
            g.DrawString(racun.BrojRacuna, FN(10), Brush(XColor.FromArgb(200, 230, 255)),
                Rect(x + 12, y + 38, 220, 18), XStringFormats.TopLeft);

            // Tvrtka desno
            string[] linije = { _tvrtka, $"OIB: {_oib}", _adresa, $"{_telefon}  |  {_email}" };
            double ty = y + 6;
            var sfDesno = new XStringFormat
            {
                Alignment = XStringAlignment.Far,
                LineAlignment = XLineAlignment.Near
            };
            foreach (var l in linije)
            {
                g.DrawString(l, FN(8), Brush(XColors.White), Rect(x, ty, w - 10, 14), sfDesno);
                ty += 13;
            }

            // Status badge
            double badgeH = 18, badgeW = 105;
            double badgeY = y + h + 2;
            g.DrawRectangle(Brush(StatusBoja(racun.Status)), x + w - badgeW, badgeY, badgeW, badgeH);
            g.DrawString(racun.Status ?? "KREIRAN", FB(8), Brush(XColors.White),
                Rect(x + w - badgeW, badgeY, badgeW, badgeH), XStringFormats.Center);

            return y + h + badgeH + 4;
        }

        private static double CrtajInfoBoxove(XGraphics g, double x, double y, double w, Racun racun)
        {
            double boxW = (w - 10) / 2;
            double boxH = 90;

            CrtajInfoBox(g, x, y, boxW, boxH, "PODACI O RAČUNU", new[]
            {
                ("Datum računa:", racun.DatumRacuna.ToString("dd.MM.yyyy")),
                ("Datum valute:", racun.DatumValute?.ToString("dd.MM.yyyy") ?? "—"),
                ("Prodavač:",     racun.NazivProdavaca ?? "—"),
                ("Plaćeno:",      racun.Placeno ? "DA ✓" : "NE"),
            });

            CrtajInfoBox(g, x + boxW + 10, y, boxW, boxH, "KUPAC", new[]
            {
                ("Naziv:",    racun.NazivKupca),
                ("",          ""),
                ("Napomena:", string.IsNullOrWhiteSpace(racun.Napomena) ? "—" : racun.Napomena),
            });

            return y + boxH;
        }

        private static void CrtajInfoBox(XGraphics g, double x, double y, double w, double h,
            string naslov, (string label, string value)[] redci)
        {
            g.DrawRectangle(Pen(cBorder, 0.6), x, y, w, h);
            g.DrawRectangle(Brush(XColor.FromArgb(245, 246, 252)), x + 0.3, y + 0.3, w - 0.6, 18);
            g.DrawString(naslov, FB(8), Brush(cPrimary),
                Rect(x + 6, y + 3, w - 12, 14), XStringFormats.TopLeft);

            double ry = y + 22;
            foreach (var (label, value) in redci)
            {
                if (string.IsNullOrEmpty(label) && string.IsNullOrEmpty(value)) { ry += 4; continue; }
                if (!string.IsNullOrEmpty(label))
                    g.DrawString(label, FN(8), Brush(cGray),
                        Rect(x + 6, ry, 82, 13), XStringFormats.TopLeft);
                if (!string.IsNullOrEmpty(value))
                    g.DrawString(value, FB(8.5), Brush(XColors.Black),
                        Rect(x + 90, ry, w - 96, 13), XStringFormats.TopLeft);
                ry += 14;
                if (ry > y + h - 4) break;
            }
        }

        private static double CrtajStavke(XGraphics g, double x, double y, double w, Racun racun)
        {
            double[] colW = { 24, 52, w - 24 - 52 - 36 - 52 - 52 - 36 - 36 - 60, 36, 52, 52, 36, 36, 60 };
            string[] hdrs = { "Rbr", "Šifra", "Naziv", "J/M", "Kol.", "Cij. €", "Pop%", "PDV%", "S PDV €" };
            bool[] rAlign = { false, false, false, true, true, true, true, true, true };

            double rowH = 18;

            // Header red
            g.DrawRectangle(Brush(cHeader), x, y, w, rowH);
            double cx = x;
            for (int i = 0; i < hdrs.Length; i++)
            {
                var sf = new XStringFormat
                {
                    Alignment = rAlign[i] ? XStringAlignment.Far : XStringAlignment.Near,
                    LineAlignment = XLineAlignment.Center
                };
                g.DrawString(hdrs[i], FB(7.5), Brush(XColors.White),
                    Rect(cx + 2, y, colW[i] - 4, rowH), sf);
                cx += colW[i];
            }
            y += rowH;

            // Stavke
            bool alt = false;
            if (racun.Stavke != null)
            {
                foreach (var s in racun.Stavke)
                {
                    if (alt)
                        g.DrawRectangle(Brush(cAlt), x, y, w, rowH);

                    string naziv = s.NazivArtikla?.Length > 36
                        ? s.NazivArtikla.Substring(0, 34) + "…"
                        : s.NazivArtikla ?? "";

                    string[] vals = {
                        s.Rbr.ToString(),
                        s.SifraArtikla ?? "",
                        naziv,
                        s.NazivJediniceMjere ?? "",
                        s.Kolicina.ToString("N2"),
                        s.ProdajnaCijena.ToString("N2"),
                        s.Popust > 0 ? s.Popust.ToString("N1") + "%" : "—",
                        s.PdvStopa.ToString("0") + "%",
                        s.IznosSaPdv.ToString("N2")
                    };

                    cx = x;
                    for (int i = 0; i < vals.Length; i++)
                    {
                        var sf = new XStringFormat
                        {
                            Alignment = rAlign[i] ? XStringAlignment.Far : XStringAlignment.Near,
                            LineAlignment = XLineAlignment.Center
                        };
                        g.DrawString(vals[i], FN(7.5), Brush(XColors.Black),
                            Rect(cx + 2, y, colW[i] - 4, rowH), sf);
                        cx += colW[i];
                    }

                    g.DrawLine(Pen(cBorder, 0.4), x, y + rowH, x + w, y + rowH);
                    y += rowH;
                    alt = !alt;
                }
            }

            return y;
        }

        private static double CrtajTotale(XGraphics g, double x, double y, double w, Racun racun)
        {
            double totalW = 220;
            double tx = x + w - totalW;
            double rh = 17;

            TotalRed(g, tx, y, totalW, rh, "Ukupno bez PDV:", $"{racun.UkupnoBezPdv:N2} €", FN(8.5), XColors.Black);
            TotalRed(g, tx, y + rh, totalW, rh, "PDV:", $"{racun.UkupnoPdv:N2} €", FN(8.5), XColors.Black);

            double uy = y + rh * 2 + 2;
            g.DrawRectangle(Brush(cLightBg), tx, uy, totalW, rh + 4);
            g.DrawRectangle(Pen(cPrimary, 0.8), tx, uy, totalW, rh + 4);
            TotalRed(g, tx, uy + 2, totalW, rh, "UKUPNO S PDV:", $"{racun.UkupnoSaPdv:N2} €", FB(10), cPrimary);

            return uy + rh + 6;
        }

        private static void TotalRed(XGraphics g, double x, double y, double w, double h,
            string label, string value, XFont font, XColor color)
        {
            var sfL = new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center };
            var sfR = new XStringFormat { Alignment = XStringAlignment.Far, LineAlignment = XLineAlignment.Center };
            g.DrawString(label, font, Brush(color), Rect(x + 6, y, 120, h), sfL);
            g.DrawString(value, font, Brush(color), Rect(x, y, w - 6, h), sfR);
        }

        private static void CrtajNapomenu(XGraphics g, double x, double y, double w, string napomena)
        {
            g.DrawString("Napomena:", FB(8), Brush(cGray),
                Rect(x, y, 70, 14), XStringFormats.TopLeft);
            g.DrawString(napomena, FI(8), Brush(cGray),
                Rect(x + 72, y, w - 72, 14), XStringFormats.TopLeft);
        }

        private static void CrtajFooter(XGraphics g, double x, double y, double w, Racun racun)
        {
            g.DrawLine(Pen(cBorder, 0.5), x, y, x + w, y);
            string footer = $"Račun {racun.BrojRacuna}  |  Generiran: {DateTime.Now:dd.MM.yyyy HH:mm}  |  {_tvrtka}  |  OIB: {_oib}";
            g.DrawString(footer, FN(7), Brush(cGray),
                Rect(x, y + 5, w, 14), XStringFormats.TopCenter);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  MINI HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private static XSolidBrush Brush(XColor c) => new XSolidBrush(c);
        private static XPen Pen(XColor c, double w) => new XPen(c, w);
        private static XRect Rect(double x, double y, double w, double h) => new XRect(x, y, w, h);

        private static XColor StatusBoja(string status)
        {
            switch (status)
            {
                case "PLAĆENO": return cSuccess;
                case "PROKNJIZENO": return cInfo;
                default: return cWarning;
            }
        }

        private static void UcitajPostavkeTvrtke()
        {
            try
            {
                var p = DatabaseHelper.GetSvePostavke();
                if (p.ContainsKey("NazivTvrtke")) _tvrtka = p["NazivTvrtke"];
                if (p.ContainsKey("OIB")) _oib = p["OIB"];
                if (p.ContainsKey("Telefon")) _telefon = p["Telefon"];
                if (p.ContainsKey("Email")) _email = p["Email"];

                string adr = p.ContainsKey("Adresa") ? p["Adresa"] : "";
                string grad = p.ContainsKey("Grad") ? p["Grad"] : "";
                string pbr = p.ContainsKey("PostanskiBroj") ? p["PostanskiBroj"] : "";
                _adresa = $"{adr}, {pbr} {grad}".Trim(' ', ',');
            }
            catch { }
        }

        public static string GenerirajPdfTemp(Racun racun)
        {
            UcitajPostavkeTvrtke();

            string temp = Path.Combine(Path.GetTempPath(),
                $"racun_preview_{Guid.NewGuid():N}.pdf");

            GenerirajPdf(racun, temp);

            return temp;
        }
    }
}