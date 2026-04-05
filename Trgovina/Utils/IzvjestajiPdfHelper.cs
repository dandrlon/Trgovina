using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Trgovina.Data;

namespace Trgovina.Utils
{
    /// <summary>
    /// Generira PDF izvještaje – stilski usklađen s RacunPdfHelper.
    /// </summary>
    public static class IzvjestajiPdfHelper
    {
        // ── Fontovi (identični RacunPdfHelper) ───────────────────────────────
        private static XFont FN(double s) => new XFont("Arial", s, XFontStyleEx.Regular);
        private static XFont FB(double s) => new XFont("Arial", s, XFontStyleEx.Bold);
        private static XFont FI(double s) => new XFont("Arial", s, XFontStyleEx.Italic);

        // ── Paleta (identična RacunPdfHelper) ────────────────────────────────
        private static readonly XColor cBlack = XColors.Black;
        private static readonly XColor cWhite = XColors.White;
        private static readonly XColor cDark = XColor.FromArgb(30, 30, 30);
        private static readonly XColor cMid = XColor.FromArgb(90, 90, 90);
        private static readonly XColor cLight = XColor.FromArgb(180, 180, 180);
        private static readonly XColor cBg = XColor.FromArgb(245, 245, 245);
        private static readonly XColor cHdrBg = XColor.FromArgb(30, 30, 30);

        // ── Postavke tvrtke ──────────────────────────────────────────────────
        private static string _tvrtka = "Moja Trgovina d.o.o.";
        private static string _adresa = "Ulica 123, 10000 Zagreb";
        private static string _oib = "12345678901";

        // ════════════════════════════════════════════════════════════════════
        //  JAVNE METODE
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Otvara SaveFileDialog i generira PDF izvještaja.
        /// </summary>
        public static void SpremiPdf(
            string naslovIzvjestaja,
            string opisIzvjestaja,
            DateTime datumOd,
            DateTime datumDo,
            DataTable podaci)
        {
            if (podaci == null || podaci.Rows.Count == 0)
            {
                MessageBox.Show("Nema podataka za generiranje PDF-a.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            UcitajPostavkeTvrtke();

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Spremi izvještaj kao PDF";
                dlg.Filter = "PDF datoteke (*.pdf)|*.pdf";
                dlg.FileName = $"Izvjestaj_{Sanitize(naslovIzvjestaja)}_{DateTime.Now:yyyyMMdd}.pdf";
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    GenerirajPdf(naslovIzvjestaja, opisIzvjestaja, datumOd, datumDo, podaci, dlg.FileName);

                    if (MessageBox.Show(
                            "PDF je uspješno spremljen.\n\nŽelite li ga otvoriti?",
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

        /// <summary>
        /// Generira PDF u temp direktorij i vraća putanju (za print preview).
        /// </summary>
        public static string GenerirajPdfTemp(
            string naslovIzvjestaja,
            string opisIzvjestaja,
            DateTime datumOd,
            DateTime datumDo,
            DataTable podaci)
        {
            UcitajPostavkeTvrtke();
            string temp = Path.Combine(Path.GetTempPath(), $"izvjestaj_{Guid.NewGuid():N}.pdf");
            GenerirajPdf(naslovIzvjestaja, opisIzvjestaja, datumOd, datumDo, podaci, temp);
            return temp;
        }

        // ════════════════════════════════════════════════════════════════════
        //  GENERIRANJE – VIŠE STRANICA
        // ════════════════════════════════════════════════════════════════════

        private static void GenerirajPdf(
            string naslov,
            string opis,
            DateTime od,
            DateTime do_,
            DataTable podaci,
            string putanja)
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            var doc = new PdfDocument();
            doc.Info.Title = naslov;
            doc.Info.Author = _tvrtka;

            const double ml = 35, mr = 35;
            const double topMargin = 40, bottomMargin = 50;

            // ── Izračunaj širine stupaca ─────────────────────────────────────
            var colWidths = IzracunajSirineStupaca(podaci, 595 - ml - mr);

            // ── Paginacija ───────────────────────────────────────────────────
            int stranica = 1;
            int ukupnoStranica = IzracunajUkupnoStranica(podaci, doc, ml, topMargin, bottomMargin, colWidths, naslov);

            XGraphics g = null;
            PdfPage page = null;
            double y = 0;
            double pw = 0, cw = 0;

            Action novaStr = () =>
            {
                page = doc.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                pw = page.Width.Point;
                cw = pw - ml - mr;
                g?.Dispose();
                g = XGraphics.FromPdfPage(page);
                y = topMargin;

                // Header samo na prvoj stranici
                if (stranica == 1)
                {
                    y = CrtajGlavu(g, ml, y, cw, naslov, opis, od, do_);
                    y += 8;
                }
                else
                {
                    // Kompaktni header za nastavne stranice
                    y = CrtajMiniGlavu(g, ml, y, cw, naslov, stranica, ukupnoStranica);
                    y += 6;
                }

                // Header tablice na svakoj stranici
                y = CrtajHeaderTablice(g, ml, y, colWidths, podaci);
                stranica++;
            };

            novaStr();

            // ── Redci ────────────────────────────────────────────────────────
            bool alt = false;
            double rowH = 15;
            int ukRedaka = podaci.Rows.Count;

            for (int ri = 0; ri < ukRedaka; ri++)
            {
                DataRow row = podaci.Rows[ri];

                // Nova stranica ako nema mjesta (ostavi prostor za footer)
                if (y + rowH > page.Height.Point - bottomMargin)
                {
                    CrtajFooter(g, ml, page.Height.Point - bottomMargin + 8, cw, stranica - 1, ukupnoStranica);
                    novaStr();
                }

                if (alt) g.DrawRectangle(Brush(cBg), ml, y, cw, rowH);

                double cx = ml;
                for (int ci = 0; ci < podaci.Columns.Count; ci++)
                {
                    string val = FormatVrijednost(row[ci], podaci.Columns[ci].DataType);
                    bool isNum = IsNumericType(podaci.Columns[ci].DataType);
                    g.DrawString(val, FN(7.5), Brush(cDark),
                        Rect(cx + 2, y + 2, colWidths[ci] - 4, rowH - 4),
                        isNum ? FmtR() : FmtL());
                    cx += colWidths[ci];
                }

                g.DrawLine(Pen(cLight, 0.3), ml, y + rowH, ml + cw, y + rowH);
                y += rowH;
                alt = !alt;
            }

            // ── Sumarni red ──────────────────────────────────────────────────
            if (y + 20 > page.Height.Point - bottomMargin)
            {
                CrtajFooter(g, ml, page.Height.Point - bottomMargin + 8, cw, stranica - 1, ukupnoStranica);
                novaStr();
            }
            y = CrtajSumarniRed(g, ml, y, colWidths, podaci);

            // Footer na zadnjoj stranici
            CrtajFooter(g, ml, page.Height.Point - bottomMargin + 8, cw, stranica - 1, ukupnoStranica);

            g.Dispose();
            doc.Save(putanja);
        }

        // ════════════════════════════════════════════════════════════════════
        //  SEKCIJE STRANICE
        // ════════════════════════════════════════════════════════════════════

        private static double CrtajGlavu(XGraphics g, double x, double y, double w,
            string naslov, string opis, DateTime od, DateTime do_)
        {
            // Debela crna linija na vrhu
            g.DrawLine(Pen(cDark, 2.5), x, y, x + w, y);
            y += 8;

            // Naziv tvrtke desno (sivo)
            g.DrawString(_tvrtka, FN(8), Brush(cMid),
                Rect(x, y + 4, w, 14), FmtR());

            // Naslov izvještaja lijevo
            g.DrawString(naslov, FB(16), Brush(cDark),
                Rect(x, y, w * 0.7, 24), FmtL());
            y += 28;

            // Opis i period
            if (!string.IsNullOrWhiteSpace(opis))
            {
                g.DrawString(opis, FI(8), Brush(cMid),
                    Rect(x, y, w * 0.65, 12), FmtL());
            }
            g.DrawString($"Period: {od:dd.MM.yyyy} – {do_:dd.MM.yyyy}",
                FB(8), Brush(cDark), Rect(x, y, w, 12), FmtR());
            y += 14;

            // Meta: tvrtka, OIB, datum generiranja
            g.DrawString(
                $"OIB: {_oib}  |  Adresa: {_adresa}  |  Generirano: {DateTime.Now:dd.MM.yyyy HH:mm}",
                FN(7), Brush(cMid), Rect(x, y, w, 11), FmtL());
            y += 13;

            // Tanka razdjelna linija
            g.DrawLine(Pen(cLight, 0.6), x, y, x + w, y);
            y += 4;

            return y;
        }

        private static double CrtajMiniGlavu(XGraphics g, double x, double y, double w,
            string naslov, int stranica, int ukupno)
        {
            g.DrawLine(Pen(cDark, 1.5), x, y, x + w, y);
            y += 5;
            g.DrawString(naslov, FB(9), Brush(cDark), Rect(x, y, w * 0.7, 13), FmtL());
            g.DrawString($"Stranica {stranica} / {ukupno}", FN(7.5), Brush(cMid),
                Rect(x, y, w, 13), FmtR());
            y += 14;
            g.DrawLine(Pen(cLight, 0.4), x, y, x + w, y);
            y += 3;
            return y;
        }

        private static double CrtajHeaderTablice(XGraphics g, double x, double y,
            double[] colW, DataTable dt)
        {
            double hdrH = 18;
            g.DrawRectangle(Brush(cHdrBg), x, y, Sum(colW), hdrH);

            double cx = x;
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                bool isNum = IsNumericType(dt.Columns[i].DataType);
                g.DrawString(dt.Columns[i].ColumnName, FB(7), Brush(cWhite),
                    Rect(cx + 2, y + 2, colW[i] - 4, hdrH - 4),
                    isNum ? FmtR() : FmtL());
                cx += colW[i];
            }

            return y + hdrH;
        }

        private static double CrtajSumarniRed(XGraphics g, double x, double y,
            double[] colW, DataTable dt)
        {
            double rh = 18;
            g.DrawRectangle(Brush(cHdrBg), x, y, Sum(colW), rh);

            // Izračunaj sume za numeričke stupce
            double cx = x;
            for (int ci = 0; ci < dt.Columns.Count; ci++)
            {
                string val = "";
                if (ci == 0)
                {
                    val = $"UKUPNO ({dt.Rows.Count} zap.)";
                }
                else if (IsNumericType(dt.Columns[ci].DataType))
                {
                    decimal suma = 0;
                    bool mozeSum = true;
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row[ci] == DBNull.Value) { mozeSum = false; break; }
                        try { suma += Convert.ToDecimal(row[ci]); }
                        catch { mozeSum = false; break; }
                    }
                    val = mozeSum ? suma.ToString("N2") : "";
                }

                bool isNum = ci > 0 && IsNumericType(dt.Columns[ci].DataType);
                g.DrawString(val, FB(7.5), Brush(cWhite),
                    Rect(cx + 2, y + 2, colW[ci] - 4, rh - 4),
                    isNum ? FmtR() : FmtL());
                cx += colW[ci];
            }

            return y + rh + 4;
        }

        private static void CrtajFooter(XGraphics g, double x, double y, double w,
            int stranica, int ukupno)
        {
            g.DrawLine(Pen(cLight, 0.5), x, y, x + w, y);
            y += 4;
            g.DrawString(
                $"{_tvrtka}  |  OIB: {_oib}  |  Generirano: {DateTime.Now:dd.MM.yyyy HH:mm}",
                FN(6.5), Brush(cMid),
                Rect(x, y, w, 10), XStringFormats.TopCenter);
            g.DrawString($"Stranica {stranica} / {ukupno}",
                FN(6.5), Brush(cLight),
                Rect(x, y + 11, w, 10), FmtR());
        }

        // ════════════════════════════════════════════════════════════════════
        //  HELPER – ŠIRINE STUPACA
        // ════════════════════════════════════════════════════════════════════

        private static double[] IzracunajSirineStupaca(DataTable dt, double totalWidth)
        {
            int n = dt.Columns.Count;
            if (n == 0) return new double[0];

            var widths = new double[n];

            // Procijeni potrebnu širinu po tipu i duljini naziva stupca
            for (int i = 0; i < n; i++)
            {
                double headerW = dt.Columns[i].ColumnName.Length * 5.5;
                double dataW;

                Type t = dt.Columns[i].DataType;
                if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
                    dataW = 52;  // N2 format ~ 10 znakova
                else if (t == typeof(int) || t == typeof(long))
                    dataW = 36;
                else if (t == typeof(DateTime))
                    dataW = 60;
                else
                {
                    // Uzmi max duljinu prvih 50 redaka
                    int maxLen = dt.Columns[i].ColumnName.Length;
                    int check = Math.Min(50, dt.Rows.Count);
                    for (int r = 0; r < check; r++)
                    {
                        int len = dt.Rows[r][i]?.ToString()?.Length ?? 0;
                        if (len > maxLen) maxLen = len;
                    }
                    dataW = Math.Min(maxLen * 5.0, 180);
                }

                widths[i] = Math.Max(headerW, dataW) + 6;
            }

            // Skaliraj na ukupnu širinu
            double totalCalc = 0;
            foreach (var w in widths) totalCalc += w;

            if (totalCalc > totalWidth)
            {
                double factor = totalWidth / totalCalc;
                for (int i = 0; i < n; i++)
                    widths[i] = Math.Max(widths[i] * factor, 20);
            }
            else
            {
                // Rastegni text stupce da popune preostali prostor
                double extra = totalWidth - totalCalc;
                int textCols = 0;
                for (int i = 0; i < n; i++)
                    if (!IsNumericType(dt.Columns[i].DataType)) textCols++;

                if (textCols > 0)
                {
                    double perCol = extra / textCols;
                    for (int i = 0; i < n; i++)
                        if (!IsNumericType(dt.Columns[i].DataType))
                            widths[i] += perCol;
                }
            }

            return widths;
        }

        private static int IzracunajUkupnoStranica(DataTable dt, PdfDocument doc,
            double ml, double topMargin, double bottomMargin, double[] colW, string naslov)
        {
            // Aproksimacija: prva stranica ima veći header
            double prvaStr = 842 - topMargin - bottomMargin - 90 - 18; // ~670 pt korisno
            double ostaleStr = 842 - topMargin - bottomMargin - 35 - 18; // ~720 pt korisno
            const double rowH = 15;

            int rediciNaOstale = (int)(ostaleStr / rowH);
            int rediciNaPrvoj = (int)(prvaStr / rowH);

            if (dt.Rows.Count <= rediciNaPrvoj) return 1;

            int ostatak = dt.Rows.Count - rediciNaPrvoj;
            return 1 + (int)Math.Ceiling((double)ostatak / rediciNaOstale);
        }

        // ════════════════════════════════════════════════════════════════════
        //  UTILITY
        // ════════════════════════════════════════════════════════════════════

        private static string FormatVrijednost(object val, Type type)
        {
            if (val == null || val == DBNull.Value) return "";
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
            {
                if (decimal.TryParse(val.ToString(), out decimal d)) return d.ToString("N2");
            }
            if (type == typeof(DateTime))
            {
                if (DateTime.TryParse(val.ToString(), out DateTime dt)) return dt.ToString("dd.MM.yyyy");
            }
            return val.ToString();
        }

        private static bool IsNumericType(Type t)
            => t == typeof(decimal) || t == typeof(double) || t == typeof(float)
            || t == typeof(int) || t == typeof(long) || t == typeof(short);

        private static double Sum(double[] arr)
        {
            double s = 0;
            foreach (var v in arr) s += v;
            return s;
        }

        private static XStringFormat FmtL() => new XStringFormat
        { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Near };

        private static XStringFormat FmtR() => new XStringFormat
        { Alignment = XStringAlignment.Far, LineAlignment = XLineAlignment.Near };

        private static XSolidBrush Brush(XColor c) => new XSolidBrush(c);
        private static XPen Pen(XColor c, double w) => new XPen(c, w);
        private static XRect Rect(double x, double y, double w, double h) => new XRect(x, y, w, h);

        private static string Sanitize(string s)
            => s?.Replace("/", "-").Replace("\\", "-").Replace(":", "-")
                 .Replace(" ", "_").Replace("*", "") ?? "izvjestaj";

        private static void UcitajPostavkeTvrtke()
        {
            try
            {
                var p = DatabaseHelper.GetSvePostavke();
                if (p.ContainsKey("NazivTvrtke")) _tvrtka = p["NazivTvrtke"];
                if (p.ContainsKey("OIB")) _oib = p["OIB"];

                string adr = p.ContainsKey("Adresa") ? p["Adresa"] : "";
                string grad = p.ContainsKey("Grad") ? p["Grad"] : "";
                string pbr = p.ContainsKey("PostanskiBroj") ? p["PostanskiBroj"] : "";
                _adresa = $"{adr}, {pbr} {grad}".Trim(' ', ',');
            }
            catch { /* tiho — koristimo defaults */ }
        }
    }
}