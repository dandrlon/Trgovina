using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Data.Models;
using Trgovina.Forms;
using Trgovina.Utils;

namespace Trgovina.UserControls
{
    public partial class PartneriControl : UserControl
    {
        // ─── State ────────────────────────────────────────────────────────────────
        private List<Partner> _partneri = new List<Partner>();
        private Partner _odabrani = null;

        // ─── UI komponente ────────────────────────────────────────────────────────
        private Guna2DataGridView dgvPartneri;
        private Guna2TextBox txtPretraga;
        private Guna2ToggleSwitch tglSamoAktivni;

        private Guna2Button btnDodaj;
        private Guna2Button btnUredi;
        private Guna2Button btnObrisi;
        private Guna2Button btnOsvjezi;

        private Label lblBrojPartnera;
        private Label lblStatus;

        public PartneriControl()
        {
            InitializeComponent();
            InitializeUI();
            UcitajPartnere();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  UI INICIJALIZACIJA
        // ══════════════════════════════════════════════════════════════════════════

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Background;
            this.Padding = new Padding(15, 10, 10, 15);

            KreirajGrid();    // Fill - mora prvi
            KreirajToolbar(); // Top - drugi
            KreirajHeader();  // Top - treći
        }

        private void KreirajHeader()
        {
            Guna2Panel pnlHeader = new Guna2Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 52;
            pnlHeader.FillColor = AppColors.Background;

            Label lblNaslov = new Label();
            lblNaslov.Text = "🤝  Partneri";
            lblNaslov.Font = AppFonts.TitleLarge;
            lblNaslov.ForeColor = AppColors.TextPrimary;
            lblNaslov.Location = new Point(0, 2);
            lblNaslov.AutoSize = true;

            lblBrojPartnera = new Label();
            lblBrojPartnera.Text = "";
            lblBrojPartnera.Font = AppFonts.Regular;
            lblBrojPartnera.ForeColor = AppColors.TextSecondary;
            lblBrojPartnera.AutoSize = true;
            lblBrojPartnera.Location = new Point(0, 30);

            pnlHeader.Controls.Add(lblNaslov);
            pnlHeader.Controls.Add(lblBrojPartnera);
            this.Controls.Add(pnlHeader);
        }

        private void KreirajToolbar()
        {
            Guna2Panel pnlToolbar = new Guna2Panel();
            pnlToolbar.Dock = DockStyle.Top;
            pnlToolbar.Height = 56;
            pnlToolbar.FillColor = AppColors.CardBackground;
            pnlToolbar.BorderRadius = 10;
            pnlToolbar.ShadowDecoration.Enabled = true;
            pnlToolbar.ShadowDecoration.Depth = 5;
            pnlToolbar.ShadowDecoration.Color = Color.FromArgb(20, 0, 0, 0);
            pnlToolbar.Margin = new Padding(0, 0, 0, 8);

            int y = 10;

            // Pretraga
            txtPretraga = new Guna2TextBox();
            txtPretraga.PlaceholderText = "🔍  Pretraži po nazivu, OIB-u ili gradu...";
            txtPretraga.Size = new Size(240, 36);
            txtPretraga.Location = new Point(10, y);
            txtPretraga.FillColor = AppColors.Background;
            txtPretraga.BorderColor = AppColors.BorderLight;
            txtPretraga.Font = AppFonts.Regular;
            txtPretraga.ForeColor = AppColors.TextPrimary;
            txtPretraga.BorderRadius = 8;
            txtPretraga.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            txtPretraga.Enter += (s, e) => txtPretraga.BorderColor = AppColors.Primary;
            txtPretraga.Leave += (s, e) => txtPretraga.BorderColor = AppColors.BorderLight;
            txtPretraga.TextChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(txtPretraga);

            // Samo aktivni toggle
            Label lblAkt = new Label();
            lblAkt.Text = "Samo aktivni:";
            lblAkt.Font = AppFonts.Regular;
            lblAkt.ForeColor = AppColors.TextSecondary;
            lblAkt.Location = new Point(265, y + 9);
            lblAkt.AutoSize = true;
            lblAkt.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            pnlToolbar.Controls.Add(lblAkt);

            tglSamoAktivni = new Guna2ToggleSwitch();
            tglSamoAktivni.Size = new Size(46, 24);
            tglSamoAktivni.Location = new Point(365, y + 6);
            tglSamoAktivni.Checked = true;
            tglSamoAktivni.CheckedState.FillColor = AppColors.Primary;
            tglSamoAktivni.UncheckedState.FillColor = AppColors.BorderLight;
            tglSamoAktivni.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            tglSamoAktivni.CheckedChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(tglSamoAktivni);

            // Gumbi - desna strana
            btnOsvjezi = KreirajGumb("🔄  Osvježi", AppColors.Secondary);
            btnOsvjezi.Click += (s, e) => UcitajPartnere();
            pnlToolbar.Controls.Add(btnOsvjezi);

            btnDodaj = KreirajGumb("➕  Dodaj", AppColors.Success);
            btnDodaj.Click += BtnDodaj_Click;
            pnlToolbar.Controls.Add(btnDodaj);

            btnUredi = KreirajGumb("✏  Uredi", AppColors.Primary);
            btnUredi.Enabled = false;
            btnUredi.Click += BtnUredi_Click;
            pnlToolbar.Controls.Add(btnUredi);

            btnObrisi = KreirajGumb("🗑  Obriši", AppColors.Danger);
            btnObrisi.Enabled = false;
            btnObrisi.Click += BtnObrisi_Click;
            pnlToolbar.Controls.Add(btnObrisi);

            pnlToolbar.Layout += (s, e) => PozicionirajGumbe(pnlToolbar, y);
            pnlToolbar.SizeChanged += (s, e) => PozicionirajGumbe(pnlToolbar, y);

            this.Controls.Add(pnlToolbar);
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
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 26;
            lblStatus.Font = AppFonts.Regular;
            lblStatus.ForeColor = AppColors.TextSecondary;
            lblStatus.BackColor = Color.FromArgb(245, 246, 250);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Padding = new Padding(10, 0, 0, 0);

            dgvPartneri = new Guna2DataGridView();
            dgvPartneri.Dock = DockStyle.Fill;
            dgvPartneri.BackgroundColor = AppColors.CardBackground;
            dgvPartneri.BorderStyle = BorderStyle.None;
            dgvPartneri.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvPartneri.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvPartneri.GridColor = Color.FromArgb(230, 232, 240);
            dgvPartneri.EnableHeadersVisualStyles = false;
            dgvPartneri.RowTemplate.Height = 36;
            dgvPartneri.ColumnHeadersHeight = 40;
            dgvPartneri.AllowUserToAddRows = false;
            dgvPartneri.AllowUserToDeleteRows = false;
            dgvPartneri.ReadOnly = true;
            dgvPartneri.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPartneri.MultiSelect = false;
            dgvPartneri.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPartneri.ScrollBars = ScrollBars.Both;

            dgvPartneri.DefaultCellStyle.BackColor = AppColors.CardBackground;
            dgvPartneri.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            dgvPartneri.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            dgvPartneri.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvPartneri.DefaultCellStyle.Font = AppFonts.Regular;
            dgvPartneri.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dgvPartneri.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);

            dgvPartneri.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            dgvPartneri.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPartneri.ColumnHeadersDefaultCellStyle.Font = AppFonts.RegularMedium;
            dgvPartneri.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

            KonfigurisiKolone();

            dgvPartneri.SelectionChanged += DgvPartneri_SelectionChanged;
            dgvPartneri.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnUredi_Click(s, e); };

            pnlGrid.Controls.Add(dgvPartneri);
            pnlGrid.Controls.Add(lblStatus);
            this.Controls.Add(pnlGrid);
        }

        private void KonfigurisiKolone()
        {
            dgvPartneri.Columns.Clear();

            var kolone = new[]
            {
                new { Name="colId",           Header="ID",             Min=45,  Weight=45f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colNaziv",         Header="Naziv",          Min=160, Weight=200f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colOIB",           Header="OIB",            Min=95,  Weight=95f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colGrad",          Header="Grad",           Min=100, Weight=110f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colAdresa",        Header="Adresa",         Min=140, Weight=160f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colTelefon",       Header="Telefon",        Min=100, Weight=105f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colEmail",         Header="E-mail",         Min=130, Weight=150f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colKontaktOsoba",  Header="Kontakt osoba",  Min=120, Weight=120f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colAktivan",       Header="Aktivan",        Min=60,  Weight=60f,  Align=DataGridViewContentAlignment.MiddleCenter },
            };

            foreach (var k in kolone)
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = k.Name;
                col.HeaderText = k.Header;
                col.MinimumWidth = k.Min;
                col.FillWeight = k.Weight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.DefaultCellStyle.Alignment = k.Align;
                col.HeaderCell.Style.Alignment = k.Align;
                dgvPartneri.Columns.Add(col);
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  DOHVAĆANJE PODATAKA
        // ══════════════════════════════════════════════════════════════════════════

        private void UcitajPartnere()
        {
            try
            {
                SetLoading(true);

                string pretraga = txtPretraga?.Text ?? "";
                bool? samoAkt = tglSamoAktivni?.Checked == true ? (bool?)true : null;

                _partneri = PartneriRepository.GetSviPartneri(pretraga, samoAkt);
                PopuniGrid();
            }
            catch (Exception ex)
            {
                PrikaziGresku("Greška pri učitavanju partnera:\n" + ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void PopuniGrid()
        {
            dgvPartneri.Rows.Clear();

            foreach (var p in _partneri)
            {
                int idx = dgvPartneri.Rows.Add(
                    p.Id,
                    p.Naziv,
                    p.OIB,
                    p.Grad,
                    p.Adresa,
                    p.Telefon,
                    p.Email,
                    p.KontaktOsoba,
                    p.Aktivan ? "✔" : "✖"
                );

                if (!p.Aktivan)
                    dgvPartneri.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(180, 180, 180);
            }

            _odabrani = null;
            AzurirajGumbe();
            AzurirajLabele();
        }

        private void AzurirajLabele()
        {
            lblBrojPartnera.Text = $"Ukupno {_partneri.Count} partnera";
            lblStatus.Text = $"  Prikazano: {_partneri.Count} rezultata";
        }

        private void PrimijeniFilter() => UcitajPartnere();

        // ══════════════════════════════════════════════════════════════════════════
        //  EVENTI GUMBA
        // ══════════════════════════════════════════════════════════════════════════

        private void BtnDodaj_Click(object sender, EventArgs e)
        {
            var frm = new frmPartneri();
            if (frm.ShowDialog() == DialogResult.OK)
                UcitajPartnere();
        }

        private void BtnUredi_Click(object sender, EventArgs e)
        {
            if (_odabrani == null) return;

            var partner = PartneriRepository.GetPartnerById(_odabrani.Id);
            if (partner == null) return;

            var frm = new frmPartneri(partner);
            if (frm.ShowDialog() == DialogResult.OK)
                UcitajPartnere();
        }

        private void BtnObrisi_Click(object sender, EventArgs e)
        {
            if (_odabrani == null) return;

            var potvrda = MessageBox.Show(
                $"Sigurno želiš deaktivirati partnera:\n\n{_odabrani.Naziv}?",
                "Potvrda deaktivacije",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (potvrda != DialogResult.Yes) return;

            try
            {
                PartneriRepository.ObrisiArtikl(_odabrani.Id);
                UcitajPartnere();
            }
            catch (Exception ex)
            {
                PrikaziGresku("Greška pri deaktivaciji partnera:\n" + ex.Message);
            }
        }

        private void DgvPartneri_SelectionChanged(object sender, EventArgs e)
        {
            _odabrani = null;
            if (dgvPartneri.SelectedRows.Count > 0)
            {
                int ri = dgvPartneri.SelectedRows[0].Index;
                if (ri >= 0 && ri < _partneri.Count)
                    _odabrani = _partneri[ri];
            }
            AzurirajGumbe();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════════

        private void PozicionirajGumbe(Control parent, int y)
        {
            int sirina = 100;
            int razmak = 6;
            int desnaIv = parent.ClientSize.Width - 10;

            btnOsvjezi.Size = new Size(sirina, 36);
            btnDodaj.Size = new Size(sirina, 36);
            btnUredi.Size = new Size(sirina, 36);
            btnObrisi.Size = new Size(sirina, 36);

            btnObrisi.Location = new Point(desnaIv - sirina, y + 7);
            btnUredi.Location = new Point(desnaIv - sirina * 2 - razmak, y + 7);
            btnDodaj.Location = new Point(desnaIv - sirina * 3 - razmak * 2, y + 7);
            btnOsvjezi.Location = new Point(desnaIv - sirina * 4 - razmak * 3, y + 7);
        }

        private Guna2Button KreirajGumb(string text, Color boja)
        {
            Guna2Button btn = new Guna2Button();
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

        private void AzurirajGumbe()
        {
            btnUredi.Enabled = _odabrani != null;
            btnObrisi.Enabled = _odabrani != null && _odabrani.Aktivan;
        }

        private void SetLoading(bool loading)
        {
            this.Cursor = loading ? Cursors.WaitCursor : Cursors.Default;
            btnOsvjezi.Enabled = !loading;
            if (loading)
            {
                lblBrojPartnera.Text = "Učitavanje...";
                lblStatus.Text = "  Učitavanje podataka...";
            }
        }

        private void PrikaziGresku(string poruka) =>
            MessageBox.Show(poruka, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}