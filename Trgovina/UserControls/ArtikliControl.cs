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
    public partial class ArtikliControl : UserControl
    {
        // ─── State ────────────────────────────────────────────────────────────────
        private List<Artikl> _artikli = new List<Artikl>();
        private List<GrupaArtikala> _grupe = new List<GrupaArtikala>();
        private Artikl _odabrani = null;

        // ─── UI komponente ────────────────────────────────────────────────────────
        private Guna2DataGridView dgvArtikli;
        private Guna2TextBox txtPretraga;
        private Guna2ComboBox cmbGrupaFilter;
        private Guna2ToggleSwitch tglSamoAktivni;

        private Guna2Button btnDodaj;
        private Guna2Button btnUredi;
        private Guna2Button btnObrisi;
        private Guna2Button btnOsvjezi;

        private Label lblBrojArtikala;
        private Label lblStatus;

        public ArtikliControl()
        {
            InitializeComponent();
            InitializeUI();
            UcitajSifarnike();
            UcitajArtikle();
        }

        //  UI INICIJALIZACIJA

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Background;
            this.Padding = new Padding(15, 10, 10, 15);


            KreirajGrid();    // Fill - mora prvi
            KreirajToolbar(); // Top - drugi
            KreirajHeader();  // Top - treći (prikazuje se iznad toolbara)
        }

        private void KreirajHeader()
        {
            Guna2Panel pnlHeader = new Guna2Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 52;
            pnlHeader.FillColor = AppColors.Background;

            Label lblNaslov = new Label();
            lblNaslov.Text = "📦  Artikli";
            lblNaslov.Font = AppFonts.TitleLarge;
            lblNaslov.ForeColor = AppColors.TextPrimary;
            lblNaslov.Location = new Point(0, 2);
            lblNaslov.AutoSize = true;

            lblBrojArtikala = new Label();
            lblBrojArtikala.Text = "";
            lblBrojArtikala.Font = AppFonts.Regular;
            lblBrojArtikala.ForeColor = AppColors.TextSecondary;
            lblBrojArtikala.AutoSize = true;
            lblBrojArtikala.Location = new Point(0, 30);

            pnlHeader.Controls.Add(lblNaslov);
            pnlHeader.Controls.Add(lblBrojArtikala);
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

            int y = 10; // vertikalni offset unutar toolbara

            txtPretraga = new Guna2TextBox();
            txtPretraga.PlaceholderText = "🔍  Pretraži po nazivu ili šifri...";
            txtPretraga.Size = new Size(210, 30);
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

            Label lblGrupa = new Label();
            lblGrupa.Text = "Grupa:";
            lblGrupa.Font = AppFonts.Regular;
            lblGrupa.ForeColor = AppColors.TextSecondary;
            lblGrupa.Location = new Point(230, y + 9);
            lblGrupa.AutoSize = true;
            lblGrupa.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            pnlToolbar.Controls.Add(lblGrupa);

            cmbGrupaFilter = new Guna2ComboBox();
            cmbGrupaFilter.Size = new Size(130, 36);
            cmbGrupaFilter.Location = new Point(275, y + 5);
            cmbGrupaFilter.FillColor = AppColors.Background;
            cmbGrupaFilter.BorderColor = AppColors.BorderLight;
            cmbGrupaFilter.FocusedColor = AppColors.Primary;
            cmbGrupaFilter.Font = AppFonts.Regular;
            cmbGrupaFilter.ForeColor = AppColors.TextPrimary;
            cmbGrupaFilter.BorderRadius = 8;
            cmbGrupaFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGrupaFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            cmbGrupaFilter.SelectedIndexChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(cmbGrupaFilter);

            Label lblAkt = new Label();
            lblAkt.Text = "Samo aktivni:";
            lblAkt.Font = AppFonts.Regular;
            lblAkt.ForeColor = AppColors.TextSecondary;
            lblAkt.Location = new Point(420, y + 9);
            lblAkt.AutoSize = true;
            lblAkt.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            pnlToolbar.Controls.Add(lblAkt);

            tglSamoAktivni = new Guna2ToggleSwitch();
            tglSamoAktivni.Size = new Size(46, 24);
            tglSamoAktivni.Location = new Point(500, y + 6);
            tglSamoAktivni.Checked = true;
            tglSamoAktivni.CheckedState.FillColor = AppColors.Primary;
            tglSamoAktivni.UncheckedState.FillColor = AppColors.BorderLight;
            tglSamoAktivni.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            tglSamoAktivni.CheckedChanged += (s, e) => PrimijeniFilter();
            pnlToolbar.Controls.Add(tglSamoAktivni);


            btnOsvjezi = KreirajGumb("🔄  Osvježi", AppColors.Secondary);
            btnOsvjezi.Click += (s, e) => UcitajArtikle();
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

        /// <summary>
        /// Raspoređuje gumbe s desne strane toolbara dinamički.
        /// </summary>
        private void PozicionirajGumbe(Control parent, int y)
        {
            int sirina = 100;
            int razmak = 6;
            int desnaIv = parent.ClientSize.Width - 10;

            // Redoslijed: Osvježi | Dodaj | Uredi | Obriši  →  desno
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

            // Status bar na dnu
            lblStatus = new Label();
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 26;
            lblStatus.Font = AppFonts.Regular;
            lblStatus.ForeColor = AppColors.TextSecondary;
            lblStatus.BackColor = Color.FromArgb(245, 246, 250);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Padding = new Padding(10, 0, 0, 0);

            dgvArtikli = new Guna2DataGridView();
            dgvArtikli.Dock = DockStyle.Fill;
            dgvArtikli.BackgroundColor = AppColors.CardBackground;
            dgvArtikli.BorderStyle = BorderStyle.None;

            dgvArtikli.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvArtikli.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvArtikli.GridColor = Color.FromArgb(230, 232, 240);
            dgvArtikli.EnableHeadersVisualStyles = false;

            dgvArtikli.RowTemplate.Height = 36;
            dgvArtikli.ColumnHeadersHeight = 40;
            dgvArtikli.AllowUserToAddRows = false;
            dgvArtikli.AllowUserToDeleteRows = false;
            dgvArtikli.ReadOnly = true;
            dgvArtikli.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvArtikli.MultiSelect = false;
            dgvArtikli.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvArtikli.ScrollBars = ScrollBars.Both;

            // Stil ćelija
            dgvArtikli.DefaultCellStyle.BackColor = AppColors.CardBackground;
            dgvArtikli.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            dgvArtikli.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            dgvArtikli.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvArtikli.DefaultCellStyle.Font = AppFonts.Regular;
            dgvArtikli.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

            dgvArtikli.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);

            // Header stil
            dgvArtikli.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            dgvArtikli.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvArtikli.ColumnHeadersDefaultCellStyle.Font = AppFonts.RegularMedium;
            dgvArtikli.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

            KonfigurisiKolone();

            dgvArtikli.SelectionChanged += DgvArtikli_SelectionChanged;
            dgvArtikli.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnUredi_Click(s, e); };

            pnlGrid.Controls.Add(dgvArtikli);
            pnlGrid.Controls.Add(lblStatus);
            this.Controls.Add(pnlGrid);
        }

        private void KonfigurisiKolone()
        {
            dgvArtikli.Columns.Clear();

            var kolone = new[]
            {
                new { Name="colId",       Header="ID",            Min=42,  Weight=42f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colSifra",    Header="Šifra",         Min=75,  Weight=70f,  Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colNaziv",    Header="Naziv artikla", Min=150, Weight=200f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colGrupa",    Header="Grupa",         Min=95,  Weight=105f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colJM",       Header="J/M",           Min=42,  Weight=42f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colNabava",   Header="Nabavna cijena (€)",   Min=85,  Weight=88f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colProdaja",  Header="Prodajna cijena (€)",  Min=85,  Weight=88f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colPdv",      Header="PDV",           Min=46,  Weight=48f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colKolicina", Header="Zaliha",        Min=65,  Weight=68f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colAktivan",  Header="Aktivan",       Min=58,  Weight=58f,  Align=DataGridViewContentAlignment.MiddleCenter },
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
                dgvArtikli.Columns.Add(col);
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Dohvaćanje podataka
        // ══════════════════════════════════════════════════════════════════════════

        private void UcitajSifarnike()
        {
            try
            {
                _grupe = ArtikliRepository.GetGrupe();
                cmbGrupaFilter.Items.Clear();
                cmbGrupaFilter.Items.Add(new GrupaArtikala { Id = 0, Naziv = "— Sve grupe —" });
                foreach (var g in _grupe)
                    cmbGrupaFilter.Items.Add(g);
                cmbGrupaFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                PrikaziGresku("Greška pri učitavanju šifarnika: " + ex.Message);
            }
        }

        private void UcitajArtikle()
        {
            try
            {
                SetLoading(true);

                string pretraga = txtPretraga?.Text ?? "";
                int idGrupe = (cmbGrupaFilter?.SelectedItem as GrupaArtikala)?.Id ?? 0;
                bool? samoAkt = tglSamoAktivni?.Checked == true ? (bool?)true : null;

                _artikli = ArtikliRepository.GetSviArtikli(pretraga, idGrupe, samoAkt);
                PopuniGrid();
            }
            catch (Exception ex)
            {
                PrikaziGresku("Greška pri učitavanju artikala:\n" + ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void PopuniGrid()
        {
            dgvArtikli.Rows.Clear();

            foreach (var a in _artikli)
            {
                int idx = dgvArtikli.Rows.Add(
                    a.Id,
                    a.Sifra,
                    a.Naziv,
                    a.NazivGrupe,
                    a.NazivJediniceMjere,
                    a.CijenaNabave.ToString("N2"),
                    a.CijenaProdaje.ToString("N2"),
                    a.PdvStopa.ToString("0") + "%",
                    a.Kolicina.ToString("N2"),
                    a.Aktivan ? "✔" : "✖"
                );

                if (!a.Aktivan)
                    dgvArtikli.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(180, 180, 180);

                if (a.Kolicina < 5 && a.Aktivan)
                {
                    dgvArtikli.Rows[idx].Cells["colKolicina"].Style.ForeColor = Color.FromArgb(231, 76, 60);
                    dgvArtikli.Rows[idx].Cells["colKolicina"].Style.Font = AppFonts.RegularMedium;
                }
            }

            _odabrani = null;
            AzurirajGumbe();
            AzurirajLabele();
        }

        private void AzurirajLabele()
        {
            lblBrojArtikala.Text = $"Ukupno {_artikli.Count} artikala";
            lblStatus.Text = $"  Prikazano: {_artikli.Count} rezultata";
        }

        private void PrimijeniFilter() => UcitajArtikle();

        // ══════════════════════════════════════════════════════════════════════════
        //  EVENTI
        // ══════════════════════════════════════════════════════════════════════════

        private void DgvArtikli_SelectionChanged(object sender, EventArgs e)
        {
            _odabrani = null;
            if (dgvArtikli.SelectedRows.Count > 0)
            {
                int ri = dgvArtikli.SelectedRows[0].Index;
                if (ri >= 0 && ri < _artikli.Count)
                    _odabrani = _artikli[ri];
            }
            AzurirajGumbe();
        }

        private void BtnDodaj_Click(object sender, EventArgs e)
        {
            using (var form = new frmArtikli(_grupe))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    UcitajArtikle();
                    PrikaziPoruku("Artikal je uspješno dodan!", "Uspjeh");
                }
            }
        }

        private void BtnUredi_Click(object sender, EventArgs e)
        {
            if (_odabrani == null) return;
            using (var form = new frmArtikli(_grupe, _odabrani))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    UcitajArtikle();
                    PrikaziPoruku("Artikal je uspješno ažuriran!", "Uspjeh");
                }
            }
        }

        private void BtnObrisi_Click(object sender, EventArgs e)
        {
            if (_odabrani == null) return;

            if (MessageBox.Show(
                $"Deaktiviraj artikal:\n\n\"{_odabrani.Naziv}\"?\n\nArtikal neće biti trajno obrisan.",
                "Potvrda", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    ArtikliRepository.ObrisiArtikl(_odabrani.Id);
                    UcitajArtikle();
                }
                catch (Exception ex) { PrikaziGresku(ex.Message); }
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════════

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
                lblBrojArtikala.Text = "Učitavanje...";
                lblStatus.Text = "  Učitavanje podataka...";
            }
        }

        private void PrikaziPoruku(string poruka, string naslov) =>
            MessageBox.Show(poruka, naslov, MessageBoxButtons.OK, MessageBoxIcon.Information);

        private void PrikaziGresku(string poruka) =>
            MessageBox.Show(poruka, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}