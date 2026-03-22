using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Data.Models;
using Trgovina.Forms;
using Trgovina.Utils;

namespace Trgovina.UserControls
{
    public partial class KalkulacijeControl : UserControl
    {
        // ── State ─────────────────────────────────────────────────────────────
        private List<Kalkulacija> _kalkulacije = new List<Kalkulacija>();
        private Kalkulacija _odabrana = null;

        // ── UI ────────────────────────────────────────────────────────────────
        private Guna2DataGridView dgvKalkulacije;
        private Guna2TextBox txtPretraga;
        private DateTimePicker dtpOd, dtpDo;
        private Guna2ToggleSwitch tglFilterDatum, tglSamoProknjizene;

        private Guna2Button btnDodaj, btnUredi, btnObrisi, btnOsvjezi;
        private Guna2Button btnPrikaz, btnProknjiziSel, btnIspis, btnSpremiPdf;

        private Label lblBrojKalkulacija, lblStatus, lblUkupnoIznos;

        // ═════════════════════════════════════════════════════════════════════

        public KalkulacijeControl()
        {
            InitializeComponent();
            InitializeUI();
            UcitajKalkulacije();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  INIT
        // ═════════════════════════════════════════════════════════════════════

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Background;
            this.Padding = new Padding(15, 10, 10, 15);

            KreirajGrid();
            KreirajToolbar();
            KreirajHeader();
        }

        private void KreirajHeader()
        {
            var pnlHeader = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                FillColor = AppColors.Background
            };

            var lblNaslov = new Label
            {
                Text = "📋  Kalkulacije",
                Font = AppFonts.TitleLarge,
                ForeColor = AppColors.TextPrimary,
                Location = new Point(0, 2),
                AutoSize = true
            };

            lblBrojKalkulacija = new Label
            {
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Location = new Point(0, 30)
            };

            pnlHeader.Controls.Add(lblNaslov);
            pnlHeader.Controls.Add(lblBrojKalkulacija);
            this.Controls.Add(pnlHeader);
        }

        private void KreirajToolbar()
        {
            // ── Gornji toolbar: pretraga + filteri + CRUD gumbi ───────────────
            var pnlToolbar = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                FillColor = AppColors.CardBackground,
                BorderRadius = 10,
                Margin = new Padding(0, 0, 0, 4)
            };
            pnlToolbar.ShadowDecoration.Enabled = true;
            pnlToolbar.ShadowDecoration.Depth = 5;
            pnlToolbar.ShadowDecoration.Color = Color.FromArgb(20, 0, 0, 0);

            int y = 11;

            txtPretraga = new Guna2TextBox
            {
                PlaceholderText = "🔍  Pretraži po broju, dobavljaču...",
                Size = new Size(230, 30),
                Location = new Point(10, y - 1),
                FillColor = AppColors.Background,
                BorderColor = AppColors.BorderLight,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextPrimary,
                BorderRadius = 8
            };
            txtPretraga.Enter += (s, e) => txtPretraga.BorderColor = AppColors.Primary;
            txtPretraga.Leave += (s, e) => txtPretraga.BorderColor = AppColors.BorderLight;
            txtPretraga.TextChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(txtPretraga);

            // Datum filter
            DodajMiniLabel(pnlToolbar, "Od:", 250, y + 7);
            dtpOd = new DateTimePicker
            {
                Size = new Size(105, 26),
                Location = new Point(280, y + 5),
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                Enabled = false
            };
            dtpOd.ValueChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(dtpOd);

            DodajMiniLabel(pnlToolbar, "Do:", 390, y + 7);
            dtpDo = new DateTimePicker
            {
                Size = new Size(105, 26),
                Location = new Point(420, y + 5),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Enabled = false
            };
            dtpDo.ValueChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(dtpDo);

            DodajMiniLabel(pnlToolbar, "Datum:", 530, y + 7);
            tglFilterDatum = new Guna2ToggleSwitch
            {
                Size = new Size(46, 24),
                Location = new Point(581, y + 3),
                Checked = false
            };
            tglFilterDatum.CheckedState.FillColor = AppColors.Primary;
            tglFilterDatum.UncheckedState.FillColor = AppColors.BorderLight;
            tglFilterDatum.CheckedChanged += (s, e) =>
            {
                dtpOd.Enabled = tglFilterDatum.Checked;
                dtpDo.Enabled = tglFilterDatum.Checked;
                PrimijeniFilter();
            };
            pnlToolbar.Controls.Add(tglFilterDatum);

            DodajMiniLabel(pnlToolbar, "Samo neknjižene:", 638, y + 7);
            tglSamoProknjizene = new Guna2ToggleSwitch
            {
                Size = new Size(46, 24),
                Location = new Point(760, y + 3),
                Checked = false
            };
            tglSamoProknjizene.CheckedState.FillColor = AppColors.Danger;
            tglSamoProknjizene.UncheckedState.FillColor = AppColors.BorderLight;
            tglSamoProknjizene.CheckedChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(tglSamoProknjizene);

            // CRUD gumbi (desno, repositioned dinamički)
            btnOsvjezi = KreirajGumb("🔄  Osvježi", AppColors.Secondary);
            btnOsvjezi.Click += (s, e) => UcitajKalkulacije();

            btnDodaj = KreirajGumb("➕  Nova", AppColors.Success);
            btnDodaj.Click += BtnDodaj_Click;

            btnUredi = KreirajGumb("✏  Uredi", AppColors.Primary);
            btnUredi.Enabled = false;
            btnUredi.Click += BtnUredi_Click;

            btnObrisi = KreirajGumb("🗑  Obriši", AppColors.Danger);
            btnObrisi.Enabled = false;
            btnObrisi.Click += BtnObrisi_Click;

            pnlToolbar.Controls.Add(btnOsvjezi);
            pnlToolbar.Controls.Add(btnDodaj);
            pnlToolbar.Controls.Add(btnUredi);
            pnlToolbar.Controls.Add(btnObrisi);

            pnlToolbar.Layout += (s, e) => PozicionirajGumbeToolbar(pnlToolbar, y);
            pnlToolbar.SizeChanged += (s, e) => PozicionirajGumbeToolbar(pnlToolbar, y);
            this.Controls.Add(pnlToolbar);

            // ── Akcijski toolbar: Prikaži | Proknjizi | Ispis | Spremi PDF ────
            var pnlAkcije = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FillColor = AppColors.CardBackground,
                BorderRadius = 10,
                Margin = new Padding(0, 0, 0, 6)
            };
            pnlAkcije.ShadowDecoration.Enabled = true;
            pnlAkcije.ShadowDecoration.Depth = 4;
            pnlAkcije.ShadowDecoration.Color = Color.FromArgb(15, 0, 0, 0);

            int ax = 10, ah = 34, ay = 8;

            btnPrikaz = KreirajGumb("🔍  Prikaži PDF", Color.FromArgb(52, 73, 94));
            btnPrikaz.Size = new Size(120, ah);
            btnPrikaz.Location = new Point(ax, ay);
            btnPrikaz.Enabled = false;
            btnPrikaz.Click += BtnPrikaz_Click;
            pnlAkcije.Controls.Add(btnPrikaz); ax += 128;

            btnProknjiziSel = KreirajGumb("📦  Proknjizi zalihe", Color.FromArgb(39, 174, 96));
            btnProknjiziSel.Size = new Size(160, ah);
            btnProknjiziSel.Location = new Point(ax, ay);
            btnProknjiziSel.Enabled = false;
            btnProknjiziSel.Click += BtnProknjiziSel_Click;
            pnlAkcije.Controls.Add(btnProknjiziSel); ax += 168;

            // Separator
            var sep = new Panel { Size = new Size(1, 30), Location = new Point(ax, ay + 2), BackColor = AppColors.BorderLight };
            pnlAkcije.Controls.Add(sep); ax += 10;

            btnIspis = KreirajGumb("🖨  Ispis", Color.FromArgb(100, 100, 115));
            btnIspis.Size = new Size(96, ah);
            btnIspis.Location = new Point(ax, ay);
            btnIspis.Enabled = false;
            btnIspis.Click += BtnIspis_Click;
            pnlAkcije.Controls.Add(btnIspis); ax += 104;

            btnSpremiPdf = KreirajGumb("💾  Spremi PDF", Color.FromArgb(142, 68, 173));
            btnSpremiPdf.Size = new Size(130, ah);
            btnSpremiPdf.Location = new Point(ax, ay);
            btnSpremiPdf.Enabled = false;
            btnSpremiPdf.Click += BtnSpremiPdf_Click;
            pnlAkcije.Controls.Add(btnSpremiPdf);

            // Ukupno iznos — desno
            lblUkupnoIznos = new Label
            {
                Font = AppFonts.RegularMedium,
                ForeColor = AppColors.TextPrimary,
                AutoSize = true
            };
            pnlAkcije.Controls.Add(lblUkupnoIznos);
            pnlAkcije.SizeChanged += (s, e) =>
                lblUkupnoIznos.Location = new Point(pnlAkcije.Width - lblUkupnoIznos.Width - 15, 16);

            this.Controls.Add(pnlAkcije);
        }

        private void PozicionirajGumbeToolbar(Control parent, int y)
        {
            int sirina = 108, razmak = 6;
            int desnaIv = parent.ClientSize.Width - 10;

            btnObrisi.Size = new Size(sirina, 36);
            btnUredi.Size = new Size(sirina, 36);
            btnDodaj.Size = new Size(120, 36);
            btnOsvjezi.Size = new Size(sirina, 36);

            btnObrisi.Location = new Point(desnaIv - sirina, y + 7);
            btnUredi.Location = new Point(desnaIv - sirina * 2 - razmak, y + 7);
            btnDodaj.Location = new Point(desnaIv - sirina * 2 - 120 - razmak * 2, y + 7);
            btnOsvjezi.Location = new Point(desnaIv - sirina * 3 - 120 - razmak * 3, y + 7);
        }

        private void KreirajGrid()
        {
            var pnlGrid = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = AppColors.CardBackground,
                BorderRadius = 12,
                Padding = new Padding(1)
            };
            pnlGrid.ShadowDecoration.Enabled = true;
            pnlGrid.ShadowDecoration.Depth = 8;
            pnlGrid.ShadowDecoration.Color = Color.FromArgb(20, 0, 0, 0);

            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextSecondary,
                BackColor = Color.FromArgb(245, 246, 250),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            dgvKalkulacije = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppColors.CardBackground,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                GridColor = Color.FromArgb(230, 232, 240),
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 36 },
                ColumnHeadersHeight = 40,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.Both
            };

            dgvKalkulacije.DefaultCellStyle.BackColor = AppColors.CardBackground;
            dgvKalkulacije.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            dgvKalkulacije.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            dgvKalkulacije.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvKalkulacije.DefaultCellStyle.Font = AppFonts.Regular;
            dgvKalkulacije.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dgvKalkulacije.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);
            dgvKalkulacije.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            dgvKalkulacije.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvKalkulacije.ColumnHeadersDefaultCellStyle.Font = AppFonts.RegularMedium;
            dgvKalkulacije.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

            KonfigurisiKolone();
            dgvKalkulacije.SelectionChanged += DgvKalkulacije_SelectionChanged;
            dgvKalkulacije.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnPrikaz_Click(s, e); };

            pnlGrid.Controls.Add(dgvKalkulacije);
            pnlGrid.Controls.Add(lblStatus);
            this.Controls.Add(pnlGrid);
        }

        private void KonfigurisiKolone()
        {
            dgvKalkulacije.Columns.Clear();
            var kolone = new[]
            {
                new { Name="colId",       Header="ID",                  Min=40,  Weight=40f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colBroj",     Header="Broj kalkulacije",    Min=130, Weight=130f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colDatum",    Header="Datum",               Min=90,  Weight=88f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colDob",      Header="Dobavljač",           Min=160, Weight=200f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colBrDobRac", Header="Br. dob. računa",     Min=120, Weight=110f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colBezPdv",   Header="Bez PDV (€)",         Min=95,  Weight=90f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colPdv",      Header="PDV (€)",             Min=80,  Weight=80f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colSaPdv",    Header="Ukupno (€)",          Min=100, Weight=95f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colStatus",   Header="Status",              Min=120, Weight=110f, Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colDatKnj",   Header="Datum knjiženja",     Min=115, Weight=105f, Align=DataGridViewContentAlignment.MiddleCenter },
            };

            foreach (var k in kolone)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    Name = k.Name,
                    HeaderText = k.Header,
                    MinimumWidth = k.Min,
                    FillWeight = k.Weight,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                };
                col.DefaultCellStyle.Alignment = k.Align;
                col.HeaderCell.Style.Alignment = k.Align;
                dgvKalkulacije.Columns.Add(col);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  DATA
        // ═════════════════════════════════════════════════════════════════════

        private void UcitajKalkulacije()
        {
            try
            {
                SetLoading(true);
                string pretraga = txtPretraga?.Text ?? "";
                DateTime? od = tglFilterDatum?.Checked == true ? dtpOd.Value.Date : (DateTime?)null;
                DateTime? dok = tglFilterDatum?.Checked == true ? dtpDo.Value.Date : (DateTime?)null;

                _kalkulacije = KalkulacijeRepository.GetSveKalkulacije(pretraga, od, dok);

                // Filter samo neknjizene
                if (tglSamoProknjizene?.Checked == true)
                    _kalkulacije = _kalkulacije.Where(k => !k.Proknjizeno).ToList();

                PopuniGrid();
            }
            catch (Exception ex) { PrikaziGresku("Greška pri učitavanju kalkulacija:\n" + ex.Message); }
            finally { SetLoading(false); }
        }

        private void PopuniGrid()
        {
            dgvKalkulacije.Rows.Clear();
            decimal ukupno = 0;

            foreach (var k in _kalkulacije)
            {
                int idx = dgvKalkulacije.Rows.Add(
                    k.Id,
                    k.BrojKalkulacije,
                    k.DatumKalkulacije.ToString("dd.MM.yyyy"),
                    k.NazivDobavljaca,
                    string.IsNullOrEmpty(k.BrojDobavljacevogRacuna) ? "—" : k.BrojDobavljacevogRacuna,
                    k.UkupnoBezPdv.ToString("N2"),
                    k.UkupnoPdv.ToString("N2"),
                    k.UkupnoSaPdv.ToString("N2"),
                    k.Proknjizeno ? "✅  Proknjiženo" : "⏳  Nije proknjiženo",
                    k.DatumKnjizenja?.ToString("dd.MM.yyyy HH:mm") ?? "—"
                );

                var row = dgvKalkulacije.Rows[idx];

                if (k.Proknjizeno)
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(140, 140, 150);
                    row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(39, 174, 96);
                    row.Cells["colStatus"].Style.Font = AppFonts.RegularMedium;
                }
                else
                {
                    row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(230, 126, 34);
                }

                ukupno += k.UkupnoSaPdv;
            }

            _odabrana = null;
            AzurirajGumbe();
            lblBrojKalkulacija.Text = $"Ukupno {_kalkulacije.Count} kalkulacija";
            lblStatus.Text = $"  Prikazano: {_kalkulacije.Count} rezultata";
            lblUkupnoIznos.Text = $"Ukupna nabavna vrijednost:  {ukupno:N2} €";
        }

        private void PrimijeniFilter() => UcitajKalkulacije();

        // ═════════════════════════════════════════════════════════════════════
        //  EVENTI
        // ═════════════════════════════════════════════════════════════════════

        private void DgvKalkulacije_SelectionChanged(object sender, EventArgs e)
        {
            _odabrana = null;
            if (dgvKalkulacije.SelectedRows.Count > 0)
            {
                int ri = dgvKalkulacije.SelectedRows[0].Index;
                if (ri >= 0 && ri < _kalkulacije.Count)
                    _odabrana = _kalkulacije[ri];
            }
            AzurirajGumbe();
        }

        private void BtnDodaj_Click(object sender, EventArgs e)
        {
            using (var frm = new frmKalkulacija())
                if (frm.ShowDialog() == DialogResult.OK)
                { UcitajKalkulacije(); PrikaziPoruku("Kalkulacija je uspješno kreirana!", "Uspjeh"); }
        }

        private void BtnUredi_Click(object sender, EventArgs e)
        {
            if (_odabrana == null || _odabrana.Proknjizeno) return;
            var puna = KalkulacijeRepository.GetKalkulacijaById(_odabrana.Id);
            if (puna == null) return;
            using (var frm = new frmKalkulacija(puna))
                if (frm.ShowDialog() == DialogResult.OK)
                { UcitajKalkulacije(); PrikaziPoruku("Kalkulacija je uspješno ažurirana!", "Uspjeh"); }
        }

        private void BtnObrisi_Click(object sender, EventArgs e)
        {
            if (_odabrana == null || _odabrana.Proknjizeno) return;
            if (MessageBox.Show(
                $"Obriši kalkulaciju \"{_odabrana.BrojKalkulacije}\"?\n\nOva akcija je trajna!",
                "Potvrda brisanja", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try { KalkulacijeRepository.ObrisiKalkulaciju(_odabrana.Id); UcitajKalkulacije(); }
                catch (Exception ex) { PrikaziGresku(ex.Message); }
            }
        }

        private void BtnPrikaz_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            var puna = KalkulacijeRepository.GetKalkulacijaById(_odabrana.Id);
            string pdfPath = KalkulacijaPdfHelper.GenerirajPdfTemp(puna);
            Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
        }

        private void BtnProknjiziSel_Click(object sender, EventArgs e)
        {
            if (_odabrana == null || _odabrana.Proknjizeno) return;
            if (MessageBox.Show(
                $"Proknjižiti kalkulaciju \"{_odabrana.BrojKalkulacije}\"?\n\nZalihe artikala bit će povećane — ne može se poništiti!",
                "Potvrda knjiženja", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    KalkulacijeRepository.ProknjiziKalkulaciju(_odabrana.Id);
                    UcitajKalkulacije();
                    PrikaziPoruku("Proknjiženo. Zalihe su ažurirane.", "Uspjeh");
                }
                catch (Exception ex) { PrikaziGresku(ex.Message); }
            }
        }

        private void BtnIspis_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            var puna = KalkulacijeRepository.GetKalkulacijaById(_odabrana.Id);
            KalkulacijaPdfHelper.IspisiKalkulaciju(puna);
        }

        private void BtnSpremiPdf_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            var puna = KalkulacijeRepository.GetKalkulacijaById(_odabrana.Id);
            KalkulacijaPdfHelper.SpremiPdf(puna);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  GUMBI LOGIKA
        // ═════════════════════════════════════════════════════════════════════

        private void AzurirajGumbe()
        {
            bool sel = _odabrana != null;

            btnPrikaz.Enabled = sel;
            btnIspis.Enabled = sel;
            btnSpremiPdf.Enabled = sel;

            btnUredi.Enabled = sel && !_odabrana.Proknjizeno;
            btnObrisi.Enabled = sel && !_odabrana.Proknjizeno;
            btnProknjiziSel.Enabled = sel && !_odabrana.Proknjizeno;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ═════════════════════════════════════════════════════════════════════

        private Guna2Button KreirajGumb(string text, Color boja)
        {
            var btn = new Guna2Button
            {
                Text = text,
                FillColor = boja,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextWhite,
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };
            btn.HoverState.FillColor = ControlPaint.Light(boja, 0.15f);
            btn.PressedColor = ControlPaint.Dark(boja, 0.1f);
            return btn;
        }

        private void DodajMiniLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextSecondary,
                Location = new Point(x, y),
                AutoSize = true
            });
        }

        private void SetLoading(bool loading)
        {
            this.Cursor = loading ? Cursors.WaitCursor : Cursors.Default;
            if (btnOsvjezi != null) btnOsvjezi.Enabled = !loading;
            if (loading)
            {
                if (lblBrojKalkulacija != null) lblBrojKalkulacija.Text = "Učitavanje...";
                if (lblStatus != null) lblStatus.Text = "  Učitavanje...";
            }
        }

        private void PrikaziPoruku(string p, string n) =>
            MessageBox.Show(p, n, MessageBoxButtons.OK, MessageBoxIcon.Information);
        private void PrikaziGresku(string p) =>
            MessageBox.Show(p, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}