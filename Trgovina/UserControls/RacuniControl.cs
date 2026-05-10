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
    public partial class RacuniControl : UserControl
    {
        private List<Racun> _racuni = new List<Racun>();
        private Racun _odabrani = null;

        private Guna2DataGridView dgvRacuni;
        private Guna2TextBox txtPretraga;
        private Guna2ComboBox cmbStatusFilter;
        private DateTimePicker dtpOd, dtpDo;
        private Guna2ToggleSwitch tglFilterDatum;

        private Guna2Button btnDodaj, btnUredi, btnObrisi, btnOsvjezi;
        private Guna2Button btnPrikaz, btnPlaceno, btnKnjizi, btnIspis, btnSpremiPdf;

        private Label lblBrojRacuna, lblStatus, lblUkupnoIznos;

        public RacuniControl()
        {
            InitializeComponent();
            InitializeUI();
            UcitajRacune();
        }

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
            Guna2Panel pnlHeader = new Guna2Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 52;
            pnlHeader.FillColor = AppColors.Background;

            Label lblNaslov = new Label();
            lblNaslov.Text = "🧾  Računi";
            lblNaslov.Font = AppFonts.TitleLarge;
            lblNaslov.ForeColor = AppColors.TextPrimary;
            lblNaslov.Location = new Point(0, 2);
            lblNaslov.AutoSize = true;

            lblBrojRacuna = new Label();
            lblBrojRacuna.Font = AppFonts.Regular;
            lblBrojRacuna.ForeColor = AppColors.TextSecondary;
            lblBrojRacuna.AutoSize = true;
            lblBrojRacuna.Location = new Point(0, 30);

            pnlHeader.Controls.Add(lblNaslov);
            pnlHeader.Controls.Add(lblBrojRacuna);
            this.Controls.Add(pnlHeader);
        }

        private void KreirajToolbar()
        {
            // ── Gornji toolbar: pretraga + filteri + CRUD gumbi ───────────────
            Guna2Panel pnlToolbar = new Guna2Panel();
            pnlToolbar.Dock = DockStyle.Top;
            pnlToolbar.Height = 56;
            pnlToolbar.FillColor = AppColors.CardBackground;
            pnlToolbar.BorderRadius = 10;
            pnlToolbar.ShadowDecoration.Enabled = true;
            pnlToolbar.ShadowDecoration.Depth = 5;
            pnlToolbar.ShadowDecoration.Color = Color.FromArgb(20, 0, 0, 0);
            pnlToolbar.Margin = new Padding(0, 0, 0, 4);

            int y = 11;

            txtPretraga = new Guna2TextBox();
            txtPretraga.PlaceholderText = "🔍  Pretraži po broju računa ili kupcu...";
            txtPretraga.Size = new Size(220, 30);
            txtPretraga.Location = new Point(10, y - 1);
            txtPretraga.FillColor = AppColors.Background;
            txtPretraga.BorderColor = AppColors.BorderLight;
            txtPretraga.Font = AppFonts.Regular;
            txtPretraga.ForeColor = AppColors.TextPrimary;
            txtPretraga.BorderRadius = 8;
            txtPretraga.Enter += (s, e) => txtPretraga.BorderColor = AppColors.Primary;
            txtPretraga.Leave += (s, e) => txtPretraga.BorderColor = AppColors.BorderLight;
            txtPretraga.TextChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(txtPretraga);

            DodajMiniLabel(pnlToolbar, "Status:", 240, y + 7);
            cmbStatusFilter = new Guna2ComboBox();
            cmbStatusFilter.Size = new Size(112, 34);
            cmbStatusFilter.Location = new Point(286, y + 3);
            cmbStatusFilter.FillColor = AppColors.Background;
            cmbStatusFilter.BorderColor = AppColors.BorderLight;
            cmbStatusFilter.FocusedColor = AppColors.Primary;
            cmbStatusFilter.Font = AppFonts.Regular;
            cmbStatusFilter.ForeColor = AppColors.TextPrimary;
            cmbStatusFilter.BorderRadius = 8;
            cmbStatusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatusFilter.Items.AddRange(new object[] { "— Svi —", "KREIRAN", "PLAĆENO", "PROKNJIZENO" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(cmbStatusFilter);

            DodajMiniLabel(pnlToolbar, "Od:", 408, y + 7);
            dtpOd = new DateTimePicker();
            dtpOd.Size = new Size(105, 26); dtpOd.Location = new Point(435, y + 5);
            dtpOd.Format = DateTimePickerFormat.Short;
            dtpOd.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dtpOd.Enabled = false;
            dtpOd.ValueChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(dtpOd);

            DodajMiniLabel(pnlToolbar, "Do:", 550, y + 7);
            dtpDo = new DateTimePicker();
            dtpDo.Size = new Size(105, 26); dtpDo.Location = new Point(577, y + 5);
            dtpDo.Format = DateTimePickerFormat.Short;
            dtpDo.Value = DateTime.Today;
            dtpDo.Enabled = false;
            dtpDo.ValueChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(dtpDo);

            DodajMiniLabel(pnlToolbar, "Datum:", 685, y + 7);
            tglFilterDatum = new Guna2ToggleSwitch();
            tglFilterDatum.Size = new Size(46, 24);
            tglFilterDatum.Location = new Point(736, y + 3);
            tglFilterDatum.Checked = false;
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
            btnOsvjezi.Click += (s, e) => UcitajRacune();
            btnDodaj = KreirajGumb("➕  Novi", AppColors.Success);
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

            // ── Akcijski toolbar: Prikaži | Plaćeno | Proknjiži | Ispis | Spremi PDF ──
            Guna2Panel pnlAkcije = new Guna2Panel();
            pnlAkcije.Dock = DockStyle.Top;
            pnlAkcije.Height = 50;
            pnlAkcije.FillColor = AppColors.CardBackground;
            pnlAkcije.BorderRadius = 10;
            pnlAkcije.ShadowDecoration.Enabled = true;
            pnlAkcije.ShadowDecoration.Depth = 4;
            pnlAkcije.ShadowDecoration.Color = Color.FromArgb(15, 0, 0, 0);
            pnlAkcije.Margin = new Padding(0, 0, 0, 6);

            int ax = 10, ah = 34, ay = 8;

            // 🔍 Prikaži
            btnPrikaz = KreirajGumb("🔍  Prikaži", Color.FromArgb(52, 73, 94));
            btnPrikaz.Size = new Size(108, ah); btnPrikaz.Location = new Point(ax, ay);
            btnPrikaz.Enabled = false;
            btnPrikaz.Click += BtnPrikaz_Click;
            pnlAkcije.Controls.Add(btnPrikaz); ax += 116;

            // 💰 Označi plaćenim
            btnPlaceno = KreirajGumb("💰  Označi plaćenim", Color.FromArgb(39, 174, 96));
            btnPlaceno.Size = new Size(158, ah); btnPlaceno.Location = new Point(ax, ay);
            btnPlaceno.Enabled = false;
            btnPlaceno.Click += BtnPlaceno_Click;
            pnlAkcije.Controls.Add(btnPlaceno); ax += 166;

            // 📒 Proknjiži
            btnKnjizi = KreirajGumb("📒  Proknjiži", Color.FromArgb(41, 128, 185));
            btnKnjizi.Size = new Size(120, ah); btnKnjizi.Location = new Point(ax, ay);
            btnKnjizi.Enabled = false;
            btnKnjizi.Click += BtnKnjizi_Click;
            pnlAkcije.Controls.Add(btnKnjizi); ax += 128;

            // Separator vizualni
            Panel sep = new Panel();
            sep.Size = new Size(1, 30); sep.Location = new Point(ax, ay + 2);
            sep.BackColor = AppColors.BorderLight;
            pnlAkcije.Controls.Add(sep); ax += 10;

            // 🖨 Ispis
            btnIspis = KreirajGumb("🖨  Ispis", Color.FromArgb(100, 100, 115));
            btnIspis.Size = new Size(96, ah); btnIspis.Location = new Point(ax, ay);
            btnIspis.Enabled = false;
            btnIspis.Click += BtnIspis_Click;
            pnlAkcije.Controls.Add(btnIspis); ax += 104;

            // 💾 Spremi PDF
            btnSpremiPdf = KreirajGumb("💾  Spremi PDF", Color.FromArgb(142, 68, 173));
            btnSpremiPdf.Size = new Size(130, ah); btnSpremiPdf.Location = new Point(ax, ay);
            btnSpremiPdf.Enabled = false;
            btnSpremiPdf.Click += BtnSpremiPdf_Click;
            pnlAkcije.Controls.Add(btnSpremiPdf);

            // Ukupno iznos — desno
            lblUkupnoIznos = new Label();
            lblUkupnoIznos.Font = AppFonts.RegularMedium;
            lblUkupnoIznos.ForeColor = AppColors.TextPrimary;
            lblUkupnoIznos.AutoSize = true;
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
            Guna2Panel pnlGrid = new Guna2Panel();
            pnlGrid.Dock = DockStyle.Fill;
            pnlGrid.FillColor = AppColors.CardBackground;
            pnlGrid.BorderRadius = 12;
            pnlGrid.Padding = new Padding(1);
            pnlGrid.ShadowDecoration.Enabled = true;
            pnlGrid.ShadowDecoration.Depth = 8;
            pnlGrid.ShadowDecoration.Color = Color.FromArgb(20, 0, 0, 0);

            lblStatus = new Label();
            lblStatus.Dock = DockStyle.Bottom; lblStatus.Height = 26;
            lblStatus.Font = AppFonts.Regular; lblStatus.ForeColor = AppColors.TextSecondary;
            lblStatus.BackColor = Color.FromArgb(245, 246, 250);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Padding = new Padding(10, 0, 0, 0);

            dgvRacuni = new Guna2DataGridView();
            dgvRacuni.Dock = DockStyle.Fill;
            dgvRacuni.BackgroundColor = AppColors.CardBackground;
            dgvRacuni.BorderStyle = BorderStyle.None;
            dgvRacuni.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvRacuni.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvRacuni.GridColor = Color.FromArgb(230, 232, 240);
            dgvRacuni.EnableHeadersVisualStyles = false;
            dgvRacuni.RowTemplate.Height = 36;
            dgvRacuni.ColumnHeadersHeight = 40;
            dgvRacuni.AllowUserToAddRows = false;
            dgvRacuni.AllowUserToDeleteRows = false;
            dgvRacuni.ReadOnly = true;
            dgvRacuni.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRacuni.MultiSelect = false;
            dgvRacuni.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvRacuni.ScrollBars = ScrollBars.Both;

            dgvRacuni.DefaultCellStyle.BackColor = AppColors.CardBackground;
            dgvRacuni.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            dgvRacuni.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            dgvRacuni.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvRacuni.DefaultCellStyle.Font = AppFonts.Regular;
            dgvRacuni.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dgvRacuni.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);
            dgvRacuni.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            dgvRacuni.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvRacuni.ColumnHeadersDefaultCellStyle.Font = AppFonts.RegularMedium;
            dgvRacuni.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

            KonfigurisiKolone();
            dgvRacuni.SelectionChanged += DgvRacuni_SelectionChanged;
            dgvRacuni.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnPrikaz_Click(s, e); };

            pnlGrid.Controls.Add(dgvRacuni);
            pnlGrid.Controls.Add(lblStatus);
            this.Controls.Add(pnlGrid);
        }

        private void KonfigurisiKolone()
        {
            dgvRacuni.Columns.Clear();
            var kolone = new[]
            {
                new { Name="colId",      Header="ID",            Min=42,  Weight=42f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colBroj",    Header="Broj računa",   Min=120, Weight=120f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colDatum",   Header="Datum",         Min=90,  Weight=88f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colValuta",  Header="Val. plaćanja", Min=90,  Weight=88f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colKupac",   Header="Kupac",         Min=150, Weight=200f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colBezPdv",  Header="Bez PDV (€)",   Min=95,  Weight=90f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colPdv",     Header="PDV (€)",       Min=80,  Weight=80f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colSaPdv",   Header="Ukupno (€)",    Min=100, Weight=95f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colPlaceno", Header="Plaćeno",       Min=65,  Weight=65f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colStatus",  Header="Status",        Min=95,  Weight=90f,  Align=DataGridViewContentAlignment.MiddleCenter },
            };
            foreach (var k in kolone)
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = k.Name; col.HeaderText = k.Header;
                col.MinimumWidth = k.Min; col.FillWeight = k.Weight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.DefaultCellStyle.Alignment = k.Align;
                col.HeaderCell.Style.Alignment = k.Align;
                dgvRacuni.Columns.Add(col);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  DATA
        // ══════════════════════════════════════════════════════════════════════

        private void UcitajRacune()
        {
            try
            {
                SetLoading(true);
                string pretraga = txtPretraga?.Text ?? "";
                string status = cmbStatusFilter?.SelectedIndex > 0 ? cmbStatusFilter.SelectedItem.ToString() : "";
                DateTime? od = tglFilterDatum?.Checked == true ? dtpOd.Value.Date : (DateTime?)null;
                DateTime? dok = tglFilterDatum?.Checked == true ? dtpDo.Value.Date : (DateTime?)null;

                _racuni = RacuniRepository.GetSviRacuni(pretraga, status, od, dok);
                PopuniGrid();
            }
            catch (Exception ex) { PrikaziGresku("Greška pri učitavanju računa:\n" + ex.Message); }
            finally { SetLoading(false); }
        }

        private void PopuniGrid()
        {
            dgvRacuni.Rows.Clear();
            decimal ukupno = 0;

            foreach (var r in _racuni)
            {
                int idx = dgvRacuni.Rows.Add(
                    r.Id, r.BrojRacuna,
                    r.DatumRacuna.ToString("dd.MM.yyyy"),
                    r.DatumValute?.ToString("dd.MM.yyyy") ?? "—",
                    r.NazivKupca,
                    r.UkupnoBezPdv.ToString("N2"),
                    r.UkupnoPdv.ToString("N2"),
                    r.UkupnoSaPdv.ToString("N2"),
                    r.Placeno ? "✔  Da" : "✖  Ne",
                    r.Status
                );

                var row = dgvRacuni.Rows[idx];
                if (r.Proknjizeno) row.DefaultCellStyle.ForeColor = Color.FromArgb(140, 140, 150);
                row.Cells["colPlaceno"].Style.ForeColor = r.Placeno ? Color.FromArgb(39, 174, 96) : AppColors.Danger;

                if (r.DatumValute.HasValue && r.DatumValute.Value < DateTime.Today && !r.Placeno)
                    row.Cells["colValuta"].Style.ForeColor = AppColors.Danger;

                switch (r.Status)
                {
                    case "PLAĆENO":
                        row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(39, 174, 96);
                        row.Cells["colStatus"].Style.Font = AppFonts.RegularMedium; break;
                    case "PROKNJIZENO":
                        row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(41, 128, 185);
                        row.Cells["colStatus"].Style.Font = AppFonts.RegularMedium; break;
                    default:
                        row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(230, 126, 34); break;
                }
                ukupno += r.UkupnoSaPdv;
            }

            _odabrani = null;
            AzurirajGumbe();
            lblBrojRacuna.Text = $"Ukupno {_racuni.Count} računa";
            lblStatus.Text = $"  Prikazano: {_racuni.Count} rezultata";
            lblUkupnoIznos.Text = $"Ukupno (s PDV):  {ukupno:N2} €";
        }

        private void PrimijeniFilter() => UcitajRacune();

        // ══════════════════════════════════════════════════════════════════════
        //  EVENTI
        // ══════════════════════════════════════════════════════════════════════

        private void DgvRacuni_SelectionChanged(object sender, EventArgs e)
        {
            _odabrani = null;
            if (dgvRacuni.SelectedRows.Count > 0)
            {
                int ri = dgvRacuni.SelectedRows[0].Index;
                if (ri >= 0 && ri < _racuni.Count)
                    _odabrani = _racuni[ri];
            }
            AzurirajGumbe();
        }

        private void BtnDodaj_Click(object sender, EventArgs e)
        {
            using (var form = new frmRacun())
                if (form.ShowDialog() == DialogResult.OK)
                { UcitajRacune(); PrikaziPoruku("Račun je uspješno kreiran!", "Uspjeh"); }
        }

        private void BtnUredi_Click(object sender, EventArgs e)
        {
            if (_odabrani == null || _odabrani.Proknjizeno) return;
            var racun = RacuniRepository.GetRacunById(_odabrani.Id);
            using (var form = new frmRacun(racun))
                if (form.ShowDialog() == DialogResult.OK)
                { UcitajRacune(); PrikaziPoruku("Račun je uspješno ažuriran!", "Uspjeh"); }
        }

        private void BtnObrisi_Click(object sender, EventArgs e)
        {
            if (_odabrani == null || _odabrani.Proknjizeno) return;
            if (MessageBox.Show(
                $"Obriši račun \"{_odabrani.BrojRacuna}\" — {_odabrani.NazivKupca}?\n\nOva akcija je trajna!",
                "Potvrda brisanja", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try { RacuniRepository.ObrisiRacun(_odabrani.Id); UcitajRacune(); }
                catch (Exception ex) { PrikaziGresku(ex.Message); }
            }
        }

        private void BtnPrikaz_Click(object sender, EventArgs e)
        {
            if (_odabrani == null) return;
            var racun = RacuniRepository.GetRacunById(_odabrani.Id);
            string pdfPath = RacunPdfHelper.GenerirajPdfTemp(racun);
            Process.Start(new ProcessStartInfo(pdfPath)
            {
                UseShellExecute = true
            });
        }

        private void BtnPlaceno_Click(object sender, EventArgs e)
        {
            if (_odabrani == null || _odabrani.Placeno) return;
            if (MessageBox.Show($"Označi račun \"{_odabrani.BrojRacuna}\" kao plaćen?",
                "Potvrda", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try { RacuniRepository.OznaciPlacenim(_odabrani.Id); UcitajRacune(); PrikaziPoruku("Označeno plaćenim.", "Uspjeh"); }
                catch (Exception ex) { PrikaziGresku(ex.Message); }
            }
        }

        private async void BtnKnjizi_Click(object sender, EventArgs e)
        {
            if (_odabrani == null || _odabrani.Proknjizeno) return;

            if (MessageBox.Show(
                $"Proknjiži račun \"{_odabrani.BrojRacuna}\"?\n\nSkida artikle sa zalihe i šalje na fiskalizaciju — ne može se poništiti!",
                "Potvrda knjiženja", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                btnKnjizi.Enabled = false;
                btnKnjizi.Text = "⏳  Knjižim...";

                // 1. Proknjiži — skida zalihe
                RacuniRepository.ProkniziRacun(_odabrani.Id);

                // 2. Učitaj puni račun za fiskalizaciju
                var racun = RacuniRepository.GetRacunById(_odabrani.Id);

                // 3. Fiskalizacija
                await IzvrsiFiskalizaciju(racun);

                UcitajRacune();
                PrikaziPoruku("Proknjiženo. Zalihe ažurirane.", "Uspjeh");
            }
            catch (Exception ex)
            {
                PrikaziGresku(ex.Message);
            }
            finally
            {
                btnKnjizi.Enabled = true;
                btnKnjizi.Text = "📒  Proknjiži";
            }
        }

        private async System.Threading.Tasks.Task IzvrsiFiskalizaciju(Racun r)
        {
            if (r.VrstaProdaje == "B2C")
            {
                btnKnjizi.Text = "📡  Šaljem na CIS...";

                var rezultat = await FiskalizacijaService.FiskalizirajAsync(r);

                if (rezultat.Uspjeh)
                {
                    RacuniRepository.SpremiJirZki(r.Id, rezultat.JIR, rezultat.ZKI, "FISKALIZIRAN", null);
                    r.JIR = rezultat.JIR;
                    r.ZKI = rezultat.ZKI;

                    var pitanje = MessageBox.Show(
                        $"✅ Račun uspješno fiskaliziran!\n\nJIR: {rezultat.JIR}\nZKI: {rezultat.ZKI}\n\nŽelite li ispisati/spremiti PDF?",
                        "Fiskalizacija", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (pitanje == DialogResult.Yes)
                        RacunPdfHelper.SpremiPdf(r);
                }
                else
                {
                    RacuniRepository.SpremiJirZki(r.Id, null, rezultat.ZKI, "GREŠKA", rezultat.Poruka);

                    MessageBox.Show(
                        $"⚠️ Proknjiženo, ali fiskalizacija nije uspjela:\n\n{rezultat.Poruka}\n\n" +
                        $"ZKI je izračunat. Fiskalizaciju ponoviti u roku 48 sati!",
                        "Greška fiskalizacije", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else // B2B
            {
                btnKnjizi.Text = "📧  Šaljem eRačun...";

                var rezultat = await FiskalizacijaService.PosaljiEracunAsync(r);

                if (rezultat.Uspjeh)
                {
                    RacuniRepository.SpremiEracunStatus(r.Id, "POSLANO", rezultat.EracunReferenca, null);
                    MessageBox.Show(
                        $"✅ eRačun uspješno poslan!\n\nReferenca: {rezultat.EracunReferenca}",
                        "eRačun", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    RacuniRepository.SpremiEracunStatus(r.Id, "GREŠKA", null, rezultat.Poruka);
                    MessageBox.Show(
                        $"⚠️ eRačun nije poslan:\n\n{rezultat.Poruka}",
                        "eRačun — Greška", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnIspis_Click(object sender, EventArgs e)
        {
            if (_odabrani == null) return;
            var racun = RacuniRepository.GetRacunById(_odabrani.Id);
            RacunPdfHelper.IspisiRacun(racun);
        }

        private void BtnSpremiPdf_Click(object sender, EventArgs e)
        {
            if (_odabrani == null) return;
            var racun = RacuniRepository.GetRacunById(_odabrani.Id);
            RacunPdfHelper.SpremiPdf(racun);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  GUMBI LOGIKA
        // ══════════════════════════════════════════════════════════════════════

        private void AzurirajGumbe()
        {
            bool sel = _odabrani != null;

            btnPrikaz.Enabled = sel;
            btnIspis.Enabled = sel;
            btnSpremiPdf.Enabled = sel;

            btnUredi.Enabled = sel && !_odabrani.Proknjizeno;
            btnObrisi.Enabled = sel && !_odabrani.Proknjizeno;

            btnPlaceno.Enabled = sel && !_odabrani.Placeno && _odabrani.Proknjizeno;
            btnKnjizi.Enabled = sel && !_odabrani.Proknjizeno;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private Guna2Button KreirajGumb(string text, Color boja)
        {
            var btn = new Guna2Button();
            btn.Text = text;
            btn.FillColor = boja;
            btn.HoverState.FillColor = ControlPaint.Light(boja, 0.15f);
            btn.PressedColor = ControlPaint.Dark(boja, 0.1f);
            btn.Font = AppFonts.Regular;
            btn.ForeColor = AppColors.TextWhite;
            btn.BorderRadius = 8;
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private void DodajMiniLabel(Control parent, string text, int x, int y)
        {
            var lbl = new Label(); lbl.Text = text;
            lbl.Font = AppFonts.Regular; lbl.ForeColor = AppColors.TextSecondary;
            lbl.Location = new Point(x, y); lbl.AutoSize = true;
            parent.Controls.Add(lbl);
        }

        private void SetLoading(bool loading)
        {
            this.Cursor = loading ? Cursors.WaitCursor : Cursors.Default;
            if (btnOsvjezi != null) btnOsvjezi.Enabled = !loading;
            if (loading) { lblBrojRacuna.Text = "Učitavanje..."; lblStatus.Text = "  Učitavanje..."; }
        }

        private void PrikaziPoruku(string p, string n) =>
            MessageBox.Show(p, n, MessageBoxButtons.OK, MessageBoxIcon.Information);
        private void PrikaziGresku(string p) =>
            MessageBox.Show(p, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}