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
    public partial class OtpremnicaControl : UserControl
    {
        private List<Otpremnica> _otpremnice = new List<Otpremnica>();
        private Otpremnica _odabrana = null;

        // ── Toolbar ───────────────────────────────────────────────────────
        private Guna2DataGridView dgvOtpremnice;
        private Guna2TextBox txtPretraga;
        private Guna2ComboBox cmbStatusFilter;
        private DateTimePicker dtpOd, dtpDo;
        private Guna2ToggleSwitch tglFilterDatum;

        private Guna2Button btnDodaj, btnUredi, btnObrisi, btnOsvjezi;

        // ── Akcijski toolbar ──────────────────────────────────────────────
        private Guna2Button btnPreview, btnSpremiPdf;
        private Guna2Button btnIsporuci, btnFakturiraj;

        // ── Info labele ───────────────────────────────────────────────────
        private Label lblBrojOtpremnica, lblStatus, lblUkupnoIznos;

        public OtpremnicaControl()
        {
            InitializeComponent();
            InitializeUI();
            UcitajOtpremnice();
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
            var pnlHeader = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                FillColor = AppColors.Background
            };

            var lblNaslov = new Label
            {
                Text = "📦  Otpremnice",
                Font = AppFonts.TitleLarge,
                ForeColor = AppColors.TextPrimary,
                Location = new Point(0, 2),
                AutoSize = true
            };

            lblBrojOtpremnica = new Label
            {
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Location = new Point(0, 30)
            };

            pnlHeader.Controls.Add(lblNaslov);
            pnlHeader.Controls.Add(lblBrojOtpremnica);
            this.Controls.Add(pnlHeader);
        }

        private void KreirajToolbar()
        {
            // ── Gornji toolbar: pretraga + filteri + CRUD ─────────────────
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
                Size = new Size(120, 34),
                Location = new Point(286, y + 3),
                FillColor = AppColors.Background,
                BorderColor = AppColors.BorderLight,
                FocusedColor = AppColors.Primary,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextPrimary,
                BorderRadius = 8,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatusFilter.Items.AddRange(new object[] { "— Svi —", "KREIRANA", "ISPORUČENA", "FAKTURIRANA", "STORNIRANA" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(cmbStatusFilter);

            DodajMiniLabel(pnlToolbar, "Od:", 416, y + 7);
            dtpOd = new DateTimePicker
            {
                Size = new Size(105, 26),
                Location = new Point(443, y + 5),
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                Enabled = false
            };
            dtpOd.ValueChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(dtpOd);

            DodajMiniLabel(pnlToolbar, "Do:", 558, y + 7);
            dtpDo = new DateTimePicker
            {
                Size = new Size(105, 26),
                Location = new Point(585, y + 5),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Enabled = false
            };
            dtpDo.ValueChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(dtpDo);

            DodajMiniLabel(pnlToolbar, "Datum:", 700, y + 7);
            tglFilterDatum = new Guna2ToggleSwitch
            {
                Size = new Size(46, 24),
                Location = new Point(751, y + 3),
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

            btnOsvjezi = KreirajGumb("🔄  Osvježi", AppColors.Secondary);
            btnOsvjezi.Click += (s, e) => UcitajOtpremnice();
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

            // ── Akcijski toolbar ──────────────────────────────────────────
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

            // 👁 Preview
            btnPreview = KreirajGumb("👁  Preview", Color.FromArgb(52, 73, 94));
            btnPreview.Size = new Size(100, ah); btnPreview.Location = new Point(ax, ay);
            btnPreview.Enabled = false;
            btnPreview.Click += BtnPreview_Click;
            pnlAkcije.Controls.Add(btnPreview); ax += 108;

            // 💾 Spremi PDF
            btnSpremiPdf = KreirajGumb("💾  Spremi PDF", Color.FromArgb(142, 68, 173));
            btnSpremiPdf.Size = new Size(128, ah); btnSpremiPdf.Location = new Point(ax, ay);
            btnSpremiPdf.Enabled = false;
            btnSpremiPdf.Click += BtnSpremiPdf_Click;
            pnlAkcije.Controls.Add(btnSpremiPdf); ax += 136;

            // Separator
            pnlAkcije.Controls.Add(new Panel { Size = new Size(1, 30), Location = new Point(ax, ay + 2), BackColor = AppColors.BorderLight }); ax += 10;

            // 🚚 Isporuči
            btnIsporuci = KreirajGumb("🚚  Isporuči", Color.FromArgb(39, 174, 96));
            btnIsporuci.Size = new Size(118, ah); btnIsporuci.Location = new Point(ax, ay);
            btnIsporuci.Enabled = false;
            btnIsporuci.Click += BtnIsporuci_Click;
            pnlAkcije.Controls.Add(btnIsporuci); ax += 126;

            // 🧾 Fakturiraj
            btnFakturiraj = KreirajGumb("🧾  Fakturiraj", Color.FromArgb(41, 128, 185));
            btnFakturiraj.Size = new Size(128, ah); btnFakturiraj.Location = new Point(ax, ay);
            btnFakturiraj.Enabled = false;
            btnFakturiraj.Click += BtnFakturiraj_Click;
            pnlAkcije.Controls.Add(btnFakturiraj);

            // Ukupno — desno
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

            dgvOtpremnice = new Guna2DataGridView
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

            dgvOtpremnice.DefaultCellStyle.BackColor = AppColors.CardBackground;
            dgvOtpremnice.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            dgvOtpremnice.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            dgvOtpremnice.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvOtpremnice.DefaultCellStyle.Font = AppFonts.Regular;
            dgvOtpremnice.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dgvOtpremnice.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);
            dgvOtpremnice.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            dgvOtpremnice.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvOtpremnice.ColumnHeadersDefaultCellStyle.Font = AppFonts.RegularMedium;
            dgvOtpremnice.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

            KonfigurisiKolone();
            dgvOtpremnice.SelectionChanged += DgvOtpremnice_SelectionChanged;
            dgvOtpremnice.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnPreview_Click(s, e); };

            pnlGrid.Controls.Add(dgvOtpremnice);
            pnlGrid.Controls.Add(lblStatus);
            this.Controls.Add(pnlGrid);
        }

        private void KonfigurisiKolone()
        {
            dgvOtpremnice.Columns.Clear();
            var kolone = new[]
            {
                new { Name="colId",       Header="ID",             Min=42,  Weight=42f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colBroj",     Header="Broj otpr.",     Min=120, Weight=120f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colDatum",    Header="Datum",          Min=88,  Weight=88f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colIsporuka", Header="Datum isporuke", Min=110, Weight=105f, Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colKupac",    Header="Kupac",          Min=160, Weight=200f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colUkupno",   Header="Ukupno bez PDV", Min=110, Weight=105f, Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colRacun",    Header="Br. računa",     Min=110, Weight=105f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colStatus",   Header="Status",         Min=105, Weight=100f, Align=DataGridViewContentAlignment.MiddleCenter },
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
                dgvOtpremnice.Columns.Add(col);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  DATA
        // ══════════════════════════════════════════════════════════════════

        private void UcitajOtpremnice()
        {
            try
            {
                SetLoading(true);
                string pretraga = txtPretraga?.Text ?? "";
                string status = cmbStatusFilter?.SelectedIndex > 0 ? cmbStatusFilter.SelectedItem.ToString() : "";
                DateTime? od = tglFilterDatum?.Checked == true ? dtpOd.Value.Date : (DateTime?)null;
                DateTime? dok = tglFilterDatum?.Checked == true ? dtpDo.Value.Date : (DateTime?)null;

                _otpremnice = OtpremnicaRepository.GetSveOtpremnice(pretraga, status, od, dok);
                PopuniGrid();
            }
            catch (Exception ex) { PrikaziGresku("Greška pri učitavanju otpremnica:\n" + ex.Message); }
            finally { SetLoading(false); }
        }

        private void PopuniGrid()
        {
            dgvOtpremnice.Rows.Clear();
            decimal ukupno = 0;

            foreach (var o in _otpremnice)
            {
                int idx = dgvOtpremnice.Rows.Add(
                    o.Id,
                    o.BrojOtpremnice,
                    o.DatumOtpremnice.ToString("dd.MM.yyyy"),
                    o.DatumIsporuke?.ToString("dd.MM.yyyy") ?? "—",
                    o.NazivKupca,
                    o.UkupnoVrijednost.ToString("N2"),
                    o.BrojRacuna ?? "—",
                    o.Status
                );

                var row = dgvOtpremnice.Rows[idx];

                // Boja statusa
                switch (o.Status)
                {
                    case "KREIRANA":
                        row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(230, 126, 34);
                        break;
                    case "ISPORUČENA":
                        row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(39, 174, 96);
                        row.Cells["colStatus"].Style.Font = AppFonts.RegularMedium;
                        break;
                    case "FAKTURIRANA":
                        row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(41, 128, 185);
                        row.Cells["colStatus"].Style.Font = AppFonts.RegularMedium;
                        // Vezani račun — naglasi
                        row.Cells["colRacun"].Style.ForeColor = Color.FromArgb(41, 128, 185);
                        break;
                    case "STORNIRANA":
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(150, 150, 160);
                        break;
                }

                // Upozori na prekoračen planirani datum isporuke (samo kreirana)
                if (o.DatumIsporuke.HasValue && o.DatumIsporuke.Value < DateTime.Today
                    && o.Status == "KREIRANA")
                    row.Cells["colIsporuka"].Style.ForeColor = AppColors.Danger;

                ukupno += o.UkupnoVrijednost;
            }

            _odabrana = null;
            AzurirajGumbe();
            lblBrojOtpremnica.Text = $"Ukupno {_otpremnice.Count} otpremnica";
            lblStatus.Text = $"  Prikazano: {_otpremnice.Count} rezultata";
            lblUkupnoIznos.Text = $"Ukupno bez PDV:  {ukupno:N2} €";
        }

        private void PrimijeniFilter() => UcitajOtpremnice();

        // ══════════════════════════════════════════════════════════════════
        //  EVENTI — SELEKCIJA
        // ══════════════════════════════════════════════════════════════════

        private void DgvOtpremnice_SelectionChanged(object sender, EventArgs e)
        {
            _odabrana = null;
            if (dgvOtpremnice.SelectedRows.Count > 0)
            {
                int ri = dgvOtpremnice.SelectedRows[0].Index;
                if (ri >= 0 && ri < _otpremnice.Count)
                    _odabrana = _otpremnice[ri];
            }
            AzurirajGumbe();
        }

        // ══════════════════════════════════════════════════════════════════
        //  EVENTI — CRUD
        // ══════════════════════════════════════════════════════════════════

        private void BtnDodaj_Click(object sender, EventArgs e)
        {
            using (var form = new frmOtpremnica())
                if (form.ShowDialog() == DialogResult.OK)
                { UcitajOtpremnice(); PrikaziPoruku("Otpremnica je uspješno kreirana!", "Uspjeh"); }
        }

        private void BtnUredi_Click(object sender, EventArgs e)
        {
            if (_odabrana == null || _odabrana.Isporuceno) return;
            var otp = OtpremnicaRepository.GetOtpremnicaById(_odabrana.Id);
            using (var form = new frmOtpremnica(otp))
                if (form.ShowDialog() == DialogResult.OK)
                { UcitajOtpremnice(); PrikaziPoruku("Otpremnica je uspješno ažurirana!", "Uspjeh"); }
        }

        private void BtnObrisi_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            if (MessageBox.Show(
                $"Obriši otpremnicu \"{_odabrana.BrojOtpremnice}\" — {_odabrana.NazivKupca}?\n\nOva akcija je trajna!",
                "Potvrda brisanja", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    OtpremnicaRepository.ObrisiOtpremnicu(_odabrana.Id);
                    UcitajOtpremnice();
                }
                catch (Exception ex) { PrikaziGresku(ex.Message); }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  EVENTI — AKCIJE
        // ══════════════════════════════════════════════════════════════════

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            var otp = OtpremnicaRepository.GetOtpremnicaById(_odabrana.Id);
            try
            {
                string pdfPath = OtpremnicaPdfHelper.GenerirajPdfTemp(otp);
                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            }
            catch (Exception ex) { PrikaziGresku(ex.Message); }
        }

        private void BtnSpremiPdf_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            var otp = OtpremnicaRepository.GetOtpremnicaById(_odabrana.Id);
            OtpremnicaPdfHelper.SpremiPdf(otp);
        }

        private void BtnIsporuci_Click(object sender, EventArgs e)
        {
            if (_odabrana == null || _odabrana.Isporuceno) return;

            if (MessageBox.Show(
                $"Isporuči otpremnicu \"{_odabrana.BrojOtpremnice}\"?\n\n" +
                "Artikli će biti skinuti sa zalihe — ne može se poništiti!",
                "Potvrda isporuke", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    OtpremnicaRepository.IsporuciOtpremnicu(_odabrana.Id);
                    UcitajOtpremnice();
                    PrikaziPoruku("Otpremnica je isporučena. Zalihe su ažurirane.", "Uspjeh");
                }
                catch (Exception ex) { PrikaziGresku(ex.Message); }
            }
        }

        private void BtnFakturiraj_Click(object sender, EventArgs e)
        {
            if (_odabrana == null || !_odabrana.Isporuceno || _odabrana.Fakturirano) return;

            // Pitaj za datum valute
            DateTime datumValute = DateTime.Today.AddDays(30);
            using (var dlg = new frmOdabirDatuma("Datum valute (rok plaćanja):", datumValute))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    datumValute = dlg.OdabraniDatum;
                else
                    return;
            }

            if (MessageBox.Show(
                $"Fakturiraj otpremnicu \"{_odabrana.BrojOtpremnice}\"?\n\n" +
                "Kreirat će se novi račun iz ove otpremnice.",
                "Potvrda fakturiranja", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    int racunId = OtpremnicaRepository.FakturirajOtpremnicu(_odabrana.Id, datumValute);
                    UcitajOtpremnice();
                    PrikaziPoruku(
                        $"Otpremnica je fakturirana.\nKreirani račun ID: {racunId}",
                        "Uspjeh");
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

            btnPreview.Enabled = sel;
            btnSpremiPdf.Enabled = sel;

            // Uredi/Briši — samo kreirana (nije još isporučena)
            btnUredi.Enabled = sel && !_odabrana.Isporuceno;
            btnObrisi.Enabled = sel && !_odabrana.Isporuceno && !_odabrana.Fakturirano;

            // Isporuči — samo kreirana
            btnIsporuci.Enabled = sel && !_odabrana.Isporuceno && _odabrana.Status != "STORNIRANA";

            // Fakturiraj — samo isporučena, još nije fakturirana
            btnFakturiraj.Enabled = sel && _odabrana.Isporuceno && !_odabrana.Fakturirano;
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════

        private Guna2Button KreirajGumb(string text, Color boja)
        {
            return new Guna2Button
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
                if (lblBrojOtpremnica != null) lblBrojOtpremnica.Text = "Učitavanje...";
                if (lblStatus != null) lblStatus.Text = "  Učitavanje...";
            }
        }

        private void PrikaziPoruku(string p, string n) =>
            MessageBox.Show(p, n, MessageBoxButtons.OK, MessageBoxIcon.Information);
        private void PrikaziGresku(string p) =>
            MessageBox.Show(p, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}