using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Data.Models;
using Trgovina.Forms;
using Trgovina.Utils;

namespace Trgovina.UserControls
{
    public partial class PonudeControl : UserControl
    {
        private List<Ponuda> _ponude = new List<Ponuda>();
        private Ponuda _odabrana = null;

        // ── Toolbar ───────────────────────────────────────────────────────
        private Guna2DataGridView dgvPonude;
        private Guna2TextBox txtPretraga;
        private Guna2ComboBox cmbStatusFilter;
        private DateTimePicker dtpOd, dtpDo;
        private Guna2ToggleSwitch tglFilterDatum;

        private Guna2Button btnDodaj, btnUredi, btnObrisi, btnOsvjezi;

        // ── Akcijski toolbar ──────────────────────────────────────────────
        private Guna2Button btnPreview, btnSpremiPdf;
        private Guna2Button btnPoslano, btnPrihvaceno, btnOdbijeno;
        private Guna2Button btnPretvoriOtpremnicu, btnPretvoriRacun;

        // ── Info ──────────────────────────────────────────────────────────
        private Label lblBrojPonuda, lblStatus, lblUkupnoIznos;

        public PonudeControl()
        {
            InitializeComponent();
            InitializeUI();
            UcitajPonude();
        }

        // ══════════════════════════════════════════════════════════════════
        //  INIT
        // ══════════════════════════════════════════════════════════════════

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
            var pnlHeader = new Guna2Panel { Dock = DockStyle.Top, Height = 52, FillColor = AppColors.Background };
            pnlHeader.Controls.Add(new Label
            {
                Text = "📋  Ponude",
                Font = AppFonts.TitleLarge,
                ForeColor = AppColors.TextPrimary,
                Location = new Point(0, 2),
                AutoSize = true
            });
            lblBrojPonuda = new Label { Font = AppFonts.Regular, ForeColor = AppColors.TextSecondary, AutoSize = true, Location = new Point(0, 30) };
            pnlHeader.Controls.Add(lblBrojPonuda);
            this.Controls.Add(pnlHeader);
        }

        private void KreirajToolbar()
        {
            // ── Gornji toolbar ────────────────────────────────────────────
            var pnlToolbar = new Guna2Panel { Dock = DockStyle.Top, Height = 56, FillColor = AppColors.CardBackground, BorderRadius = 10, Margin = new Padding(0, 0, 0, 4) };
            pnlToolbar.ShadowDecoration.Enabled = true; pnlToolbar.ShadowDecoration.Depth = 5;
            pnlToolbar.ShadowDecoration.Color = Color.FromArgb(20, 0, 0, 0);

            int y = 11;

            txtPretraga = new Guna2TextBox
            {
                PlaceholderText = "🔍  Pretraži po broju ili kupcu...",
                Size = new Size(220, 30),
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

            DodajMiniLabel(pnlToolbar, "Status:", 240, y + 7);
            cmbStatusFilter = new Guna2ComboBox
            {
                Size = new Size(130, 34),
                Location = new Point(286, y + 3),
                FillColor = AppColors.Background,
                BorderColor = AppColors.BorderLight,
                FocusedColor = AppColors.Primary,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextPrimary,
                BorderRadius = 8,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatusFilter.Items.AddRange(new object[] { "— Svi —", "KREIRANA", "POSLANA", "PRIHVAĆENA", "ODBIJENA", "PRETVORENA" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(cmbStatusFilter);

            DodajMiniLabel(pnlToolbar, "Od:", 426, y + 7);
            dtpOd = new DateTimePicker { Size = new Size(105, 26), Location = new Point(453, y + 5), Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Now.Year, 1, 1), Enabled = false };
            dtpOd.ValueChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(dtpOd);

            DodajMiniLabel(pnlToolbar, "Do:", 568, y + 7);
            dtpDo = new DateTimePicker { Size = new Size(105, 26), Location = new Point(595, y + 5), Format = DateTimePickerFormat.Short, Value = DateTime.Today, Enabled = false };
            dtpDo.ValueChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(dtpDo);

            DodajMiniLabel(pnlToolbar, "Datum:", 710, y + 7);
            tglFilterDatum = new Guna2ToggleSwitch { Size = new Size(46, 24), Location = new Point(761, y + 3), Checked = false };
            tglFilterDatum.CheckedState.FillColor = AppColors.Primary;
            tglFilterDatum.UncheckedState.FillColor = AppColors.BorderLight;
            tglFilterDatum.CheckedChanged += (s, e) =>
            {
                dtpOd.Enabled = tglFilterDatum.Checked;
                dtpDo.Enabled = tglFilterDatum.Checked;
                PrimijeniFilter();
            };
            pnlToolbar.Controls.Add(tglFilterDatum);

            btnOsvjezi = KreirajGumb("🔄  Osvježi", AppColors.Secondary); btnOsvjezi.Click += (s, e) => UcitajPonude();
            btnDodaj = KreirajGumb("➕  Nova", AppColors.Success); btnDodaj.Click += BtnDodaj_Click;
            btnUredi = KreirajGumb("✏  Uredi", AppColors.Primary); btnUredi.Enabled = false; btnUredi.Click += BtnUredi_Click;
            btnObrisi = KreirajGumb("🗑  Obriši", AppColors.Danger); btnObrisi.Enabled = false; btnObrisi.Click += BtnObrisi_Click;

            pnlToolbar.Controls.Add(btnOsvjezi); pnlToolbar.Controls.Add(btnDodaj);
            pnlToolbar.Controls.Add(btnUredi); pnlToolbar.Controls.Add(btnObrisi);
            pnlToolbar.Layout += (s, e) => PozicionirajGumbeToolbar(pnlToolbar, y);
            pnlToolbar.SizeChanged += (s, e) => PozicionirajGumbeToolbar(pnlToolbar, y);
            this.Controls.Add(pnlToolbar);

            // ── Akcijski toolbar ──────────────────────────────────────────
            var pnlAkcije = new Guna2Panel { Dock = DockStyle.Top, Height = 50, FillColor = AppColors.CardBackground, BorderRadius = 10, Margin = new Padding(0, 0, 0, 6) };
            pnlAkcije.ShadowDecoration.Enabled = true; pnlAkcije.ShadowDecoration.Depth = 4;
            pnlAkcije.ShadowDecoration.Color = Color.FromArgb(15, 0, 0, 0);

            int ax = 10, ah = 34, ay = 8;

            void AkcijskiGumb(ref Guna2Button field, string tekst, Color boja, int sirina, EventHandler handler)
            {
                field = KreirajGumb(tekst, boja);
                field.Size = new Size(sirina, ah);
                field.Location = new Point(ax, ay);
                field.Enabled = false;
                field.Click += handler;
                pnlAkcije.Controls.Add(field);
                ax += sirina + 8;
            }

            AkcijskiGumb(ref btnPreview, "👁  Preview", Color.FromArgb(52, 73, 94), 100, BtnPreview_Click);
            AkcijskiGumb(ref btnSpremiPdf, "💾  Spremi PDF", Color.FromArgb(142, 68, 173), 128, BtnSpremiPdf_Click);

            // Separator
            pnlAkcije.Controls.Add(new Panel { Size = new Size(1, 30), Location = new Point(ax, ay + 2), BackColor = AppColors.BorderLight }); ax += 10;

            AkcijskiGumb(ref btnPoslano, "📤  Označi poslano", Color.FromArgb(41, 128, 185), 150, BtnPoslano_Click);
            AkcijskiGumb(ref btnPrihvaceno, "✅  Prihvaćena", Color.FromArgb(39, 174, 96), 120, BtnPrihvaceno_Click);
            AkcijskiGumb(ref btnOdbijeno, "❌  Odbijena", Color.FromArgb(192, 57, 43), 110, BtnOdbijeno_Click);

            // Separator
            pnlAkcije.Controls.Add(new Panel { Size = new Size(1, 30), Location = new Point(ax, ay + 2), BackColor = AppColors.BorderLight }); ax += 10;

            AkcijskiGumb(ref btnPretvoriOtpremnicu, "📦  → Otpremnica", Color.FromArgb(230, 126, 34), 145, BtnPretvoriOtpremnicu_Click);
            AkcijskiGumb(ref btnPretvoriRacun, "🧾  → Račun", Color.FromArgb(41, 128, 185), 115, BtnPretvoriRacun_Click);

            // Ukupno — desno
            lblUkupnoIznos = new Label { Font = AppFonts.RegularMedium, ForeColor = AppColors.TextPrimary, AutoSize = true };
            pnlAkcije.Controls.Add(lblUkupnoIznos);
            pnlAkcije.SizeChanged += (s, e) =>
                lblUkupnoIznos.Location = new Point(pnlAkcije.Width - lblUkupnoIznos.Width - 15, 16);

            this.Controls.Add(pnlAkcije);
        }

        private void PozicionirajGumbeToolbar(Control parent, int y)
        {
            int sirina = 108, razmak = 6, desnaIv = parent.ClientSize.Width - 10;
            btnObrisi.Size = new Size(sirina, 36); btnUredi.Size = new Size(sirina, 36);
            btnDodaj.Size = new Size(120, 36); btnOsvjezi.Size = new Size(sirina, 36);
            btnObrisi.Location = new Point(desnaIv - sirina, y + 7);
            btnUredi.Location = new Point(desnaIv - sirina * 2 - razmak, y + 7);
            btnDodaj.Location = new Point(desnaIv - sirina * 2 - 120 - razmak * 2, y + 7);
            btnOsvjezi.Location = new Point(desnaIv - sirina * 3 - 120 - razmak * 3, y + 7);
        }

        private void KreirajGrid()
        {
            var pnlGrid = new Guna2Panel { Dock = DockStyle.Fill, FillColor = AppColors.CardBackground, BorderRadius = 12, Padding = new Padding(1) };
            pnlGrid.ShadowDecoration.Enabled = true; pnlGrid.ShadowDecoration.Depth = 8;
            pnlGrid.ShadowDecoration.Color = Color.FromArgb(20, 0, 0, 0);

            lblStatus = new Label { Dock = DockStyle.Bottom, Height = 26, Font = AppFonts.Regular, ForeColor = AppColors.TextSecondary, BackColor = Color.FromArgb(245, 246, 250), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) };

            dgvPonude = new Guna2DataGridView
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
            dgvPonude.DefaultCellStyle.BackColor = AppColors.CardBackground;
            dgvPonude.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            dgvPonude.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            dgvPonude.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvPonude.DefaultCellStyle.Font = AppFonts.Regular;
            dgvPonude.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dgvPonude.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);
            dgvPonude.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            dgvPonude.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPonude.ColumnHeadersDefaultCellStyle.Font = AppFonts.RegularMedium;
            dgvPonude.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

            KonfigurisiKolone();
            dgvPonude.SelectionChanged += DgvPonude_SelectionChanged;
            dgvPonude.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnPreview_Click(s, e); };

            pnlGrid.Controls.Add(dgvPonude);
            pnlGrid.Controls.Add(lblStatus);
            this.Controls.Add(pnlGrid);
        }

        private void KonfigurisiKolone()
        {
            dgvPonude.Columns.Clear();
            var kolone = new[]
            {
                new { Name="colId",       Header="ID",          Min=42,  Weight=42f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colBroj",     Header="Broj ponude", Min=120, Weight=120f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colDatum",    Header="Datum",       Min=88,  Weight=88f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colVazenje",  Header="Vrijedi do",  Min=90,  Weight=90f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colKupac",    Header="Kupac",       Min=160, Weight=200f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colBezPdv",   Header="Bez PDV (€)", Min=95,  Weight=90f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colSaPdv",    Header="Ukupno (€)",  Min=95,  Weight=90f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colVezano",   Header="Vezani dok.", Min=110, Weight=110f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colStatus",   Header="Status",      Min=100, Weight=95f,  Align=DataGridViewContentAlignment.MiddleCenter },
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
                dgvPonude.Columns.Add(col);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  DATA
        // ══════════════════════════════════════════════════════════════════

        private void UcitajPonude()
        {
            try
            {
                SetLoading(true);
                string pretraga = txtPretraga?.Text ?? "";
                string status = cmbStatusFilter?.SelectedIndex > 0 ? cmbStatusFilter.SelectedItem.ToString() : "";
                DateTime? od = tglFilterDatum?.Checked == true ? dtpOd.Value.Date : (DateTime?)null;
                DateTime? dok = tglFilterDatum?.Checked == true ? dtpDo.Value.Date : (DateTime?)null;

                _ponude = PonudaRepository.GetSvePonude(pretraga, status, od, dok);
                PopuniGrid();
            }
            catch (Exception ex) { PrikaziGresku("Greška pri učitavanju ponuda:\n" + ex.Message); }
            finally { SetLoading(false); }
        }

        private void PopuniGrid()
        {
            dgvPonude.Rows.Clear();
            decimal ukupno = 0;

            foreach (var p in _ponude)
            {
                string vezano = p.BrojOtpremnice != null ? $"OTP: {p.BrojOtpremnice}"
                              : p.BrojRacuna != null ? $"R: {p.BrojRacuna}"
                              : "—";

                int idx = dgvPonude.Rows.Add(
                    p.Id, p.BrojPonude,
                    p.DatumPonude.ToString("dd.MM.yyyy"),
                    p.DatumVazenja?.ToString("dd.MM.yyyy") ?? "—",
                    p.NazivKupca,
                    p.UkupnoBezPdv.ToString("N2"),
                    p.UkupnoSaPdv.ToString("N2"),
                    vezano,
                    p.Status
                );

                var row = dgvPonude.Rows[idx];

                // Boja statusa
                switch (p.Status)
                {
                    case "KREIRANA":
                        row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(230, 126, 34); break;
                    case "POSLANA":
                        row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(41, 128, 185);
                        row.Cells["colStatus"].Style.Font = AppFonts.RegularMedium; break;
                    case "PRIHVAĆENA":
                        row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(39, 174, 96);
                        row.Cells["colStatus"].Style.Font = AppFonts.RegularMedium; break;
                    case "ODBIJENA":
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(150, 150, 160); break;
                    case "PRETVORENA":
                        row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(142, 68, 173);
                        row.Cells["colStatus"].Style.Font = AppFonts.RegularMedium;
                        row.Cells["colVezano"].Style.ForeColor = Color.FromArgb(41, 128, 185); break;
                }

                // Upozori na isteklu ponudu
                if (p.JeIstekla)
                    row.Cells["colVazenje"].Style.ForeColor = AppColors.Danger;

                ukupno += p.UkupnoSaPdv;
            }

            _odabrana = null;
            AzurirajGumbe();
            lblBrojPonuda.Text = $"Ukupno {_ponude.Count} ponuda";
            lblStatus.Text = $"  Prikazano: {_ponude.Count} rezultata";
            lblUkupnoIznos.Text = $"Ukupno (s PDV):  {ukupno:N2} €";
        }

        private void PrimijeniFilter() => UcitajPonude();

        // ══════════════════════════════════════════════════════════════════
        //  EVENTI — SELEKCIJA
        // ══════════════════════════════════════════════════════════════════

        private void DgvPonude_SelectionChanged(object sender, EventArgs e)
        {
            _odabrana = null;
            if (dgvPonude.SelectedRows.Count > 0)
            {
                int ri = dgvPonude.SelectedRows[0].Index;
                if (ri >= 0 && ri < _ponude.Count)
                    _odabrana = _ponude[ri];
            }
            AzurirajGumbe();
        }

        // ══════════════════════════════════════════════════════════════════
        //  EVENTI — CRUD
        // ══════════════════════════════════════════════════════════════════

        private void BtnDodaj_Click(object sender, EventArgs e)
        {
            using (var form = new frmPonuda())
                if (form.ShowDialog() == DialogResult.OK)
                { UcitajPonude(); PrikaziPoruku("Ponuda je uspješno kreirana!", "Uspjeh"); }
        }

        private void BtnUredi_Click(object sender, EventArgs e)
        {
            if (_odabrana == null || _odabrana.JePretvorena) return;
            var ponuda = PonudaRepository.GetPonudaById(_odabrana.Id);
            using (var form = new frmPonuda(ponuda))
                if (form.ShowDialog() == DialogResult.OK)
                { UcitajPonude(); PrikaziPoruku("Ponuda je uspješno ažurirana!", "Uspjeh"); }
        }

        private void BtnObrisi_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            if (MessageBox.Show(
                $"Obriši ponudu \"{_odabrana.BrojPonude}\" — {_odabrana.NazivKupca}?\n\nOva akcija je trajna!",
                "Potvrda brisanja", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try { PonudaRepository.ObrisiPonudu(_odabrana.Id); UcitajPonude(); }
                catch (Exception ex) { PrikaziGresku(ex.Message); }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  EVENTI — AKCIJE
        // ══════════════════════════════════════════════════════════════════

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            var ponuda = PonudaRepository.GetPonudaById(_odabrana.Id);
            try
            {
                string pdfPath = PonudaPdfHelper.GenerirajPdfTemp(ponuda);
                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            }
            catch (Exception ex) { PrikaziGresku(ex.Message); }
        }

        private void BtnSpremiPdf_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            var ponuda = PonudaRepository.GetPonudaById(_odabrana.Id);
            PonudaPdfHelper.SpremiPdf(ponuda);
        }

        private void BtnPoslano_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            if (MessageBox.Show($"Označi ponudu \"{_odabrana.BrojPonude}\" kao poslanu kupcu?",
                "Potvrda", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try { PonudaRepository.OznaciPoslano(_odabrana.Id); UcitajPonude(); PrikaziPoruku("Označeno kao poslano.", "Uspjeh"); }
                catch (Exception ex) { PrikaziGresku(ex.Message); }
            }
        }

        private void BtnPrihvaceno_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            if (MessageBox.Show($"Označi ponudu \"{_odabrana.BrojPonude}\" kao prihvaćenu?",
                "Potvrda", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try { PonudaRepository.OznaciPrihvaceno(_odabrana.Id); UcitajPonude(); PrikaziPoruku("Ponuda je prihvaćena.", "Uspjeh"); }
                catch (Exception ex) { PrikaziGresku(ex.Message); }
            }
        }

        private void BtnOdbijeno_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            if (MessageBox.Show($"Označi ponudu \"{_odabrana.BrojPonude}\" kao odbijenu?",
                "Potvrda", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try { PonudaRepository.OznaciOdbijeno(_odabrana.Id); UcitajPonude(); PrikaziPoruku("Ponuda je odbijena.", "Uspjeh"); }
                catch (Exception ex) { PrikaziGresku(ex.Message); }
            }
        }

        private void BtnPretvoriOtpremnicu_Click(object sender, EventArgs e)
        {
            if (_odabrana == null || _odabrana.JePretvorena) return;
            if (MessageBox.Show(
                $"Pretvori ponudu \"{_odabrana.BrojPonude}\" u otpremnicu?\n\nBit će kreirana nova otpremnica s istim stavkama.",
                "Potvrda pretvorbe", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    int otpId = PonudaRepository.PretvoriUOtpremnicu(_odabrana.Id);
                    UcitajPonude();
                    PrikaziPoruku($"Kreirana otpremnica ID: {otpId}\n\nMožete je pronaći u modulu Otpremnice.", "Uspjeh");
                }
                catch (Exception ex) { PrikaziGresku(ex.Message); }
            }
        }

        private void BtnPretvoriRacun_Click(object sender, EventArgs e)
        {
            if (_odabrana == null || _odabrana.JePretvorena) return;

            DateTime datumValute = DateTime.Today.AddDays(30);
            using (var dlg = new frmOdabirDatuma("Datum valute (rok plaćanja):", datumValute))
            {
                if (dlg.ShowDialog() == DialogResult.OK) datumValute = dlg.OdabraniDatum;
                else return;
            }

            if (MessageBox.Show(
                $"Pretvori ponudu \"{_odabrana.BrojPonude}\" direktno u račun?\n\nBit će kreiran novi račun bez otpremnice.",
                "Potvrda pretvorbe", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    int racunId = PonudaRepository.PretvoriURacun(_odabrana.Id, datumValute);
                    UcitajPonude();
                    PrikaziPoruku($"Kreiran račun ID: {racunId}\n\nMožete ga pronaći u modulu Računi.", "Uspjeh");
                }
                catch (Exception ex) { PrikaziGresku(ex.Message); }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  GUMBI — LOGIKA
        // ══════════════════════════════════════════════════════════════════

        private void AzurirajGumbe()
        {
            bool sel = _odabrana != null;
            bool pretvorena = sel && _odabrana.JePretvorena;
            bool aktivna = sel && !pretvorena && _odabrana.Status != "ODBIJENA";

            btnPreview.Enabled = sel;
            btnSpremiPdf.Enabled = sel;

            btnUredi.Enabled = sel && !pretvorena;
            btnObrisi.Enabled = sel && !pretvorena;

            // Statusne akcije
            btnPoslano.Enabled = sel && _odabrana.Status == "KREIRANA";
            btnPrihvaceno.Enabled = sel && (_odabrana.Status == "KREIRANA" || _odabrana.Status == "POSLANA");
            btnOdbijeno.Enabled = sel && (_odabrana.Status == "KREIRANA" || _odabrana.Status == "POSLANA");

            // Pretvorba — samo aktivna, prihvaćena ili poslana ponuda
            bool mozePretvoriti = sel && !pretvorena
                && (_odabrana.Status == "PRIHVAĆENA" || _odabrana.Status == "POSLANA" || _odabrana.Status == "KREIRANA");
            btnPretvoriOtpremnicu.Enabled = mozePretvoriti;
            btnPretvoriRacun.Enabled = mozePretvoriti;
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════

        private Guna2Button KreirajGumb(string text, Color boja) =>
            new Guna2Button
            {
                Text = text,
                FillColor = boja,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextWhite,
                BorderRadius = 8,
                Cursor = Cursors.Hand,
                HoverState = { FillColor = ControlPaint.Light(boja, 0.15f) },
                PressedColor = ControlPaint.Dark(boja, 0.1f)
            };

        private void DodajMiniLabel(Control parent, string text, int x, int y) =>
            parent.Controls.Add(new Label { Text = text, Font = AppFonts.Regular, ForeColor = AppColors.TextSecondary, Location = new Point(x, y), AutoSize = true });

        private void SetLoading(bool loading)
        {
            this.Cursor = loading ? Cursors.WaitCursor : Cursors.Default;
            if (btnOsvjezi != null) btnOsvjezi.Enabled = !loading;
            if (loading)
            {
                if (lblBrojPonuda != null) lblBrojPonuda.Text = "Učitavanje...";
                if (lblStatus != null) lblStatus.Text = "  Učitavanje...";
            }
        }

        private void PrikaziPoruku(string p, string n) =>
            MessageBox.Show(p, n, MessageBoxButtons.OK, MessageBoxIcon.Information);
        private void PrikaziGresku(string p) =>
            MessageBox.Show(p, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}