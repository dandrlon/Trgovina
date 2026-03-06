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
    public partial class GrupeArtikalaControl : UserControl
    {
        private List<GrupaArtikala> _grupe = new List<GrupaArtikala>();
        private GrupaArtikala _odabrana = null;

        private Guna2DataGridView dgvGrupe;
        private Guna2TextBox txtPretraga;
        private Guna2Button btnDodaj, btnUredi, btnObrisi, btnOsvjezi;
        private Label lblBroj, lblStatus;

        public GrupeArtikalaControl()
        {
            InitializeComponent();
            InitializeUI();
            UcitajGrupe();
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
            Guna2Panel pnl = new Guna2Panel { Dock = DockStyle.Top, Height = 52, FillColor = AppColors.Background };
            lblBroj = new Label { Text = "", Font = AppFonts.Regular, ForeColor = AppColors.TextSecondary, AutoSize = true, Location = new Point(0, 30) };
            pnl.Controls.Add(new Label { Text = "🗂️  Grupe artikala", Font = AppFonts.TitleLarge, ForeColor = AppColors.TextPrimary, Location = new Point(0, 2), AutoSize = true });
            pnl.Controls.Add(lblBroj);
            this.Controls.Add(pnl);
        }

        private void KreirajToolbar()
        {
            Guna2Panel pnl = new Guna2Panel { Dock = DockStyle.Top, Height = 56, FillColor = AppColors.CardBackground, BorderRadius = 10, Margin = new Padding(0, 0, 0, 8) };
            pnl.ShadowDecoration.Enabled = true; pnl.ShadowDecoration.Depth = 5;

            int y = 10;
            txtPretraga = new Guna2TextBox { PlaceholderText = "🔍  Pretraži grupe...", Size = new Size(220, 36), Location = new Point(10, y), FillColor = AppColors.Background, BorderColor = AppColors.BorderLight, Font = AppFonts.Regular, ForeColor = AppColors.TextPrimary, BorderRadius = 8 };
            txtPretraga.Enter += (s, e) => txtPretraga.BorderColor = AppColors.Primary;
            txtPretraga.Leave += (s, e) => txtPretraga.BorderColor = AppColors.BorderLight;
            txtPretraga.TextChanged += (s, e) => UcitajGrupe();
            pnl.Controls.Add(txtPretraga);

            btnOsvjezi = KreirajGumb("🔄  Osvježi", AppColors.Secondary); btnOsvjezi.Click += (s, e) => UcitajGrupe(); pnl.Controls.Add(btnOsvjezi);
            btnDodaj = KreirajGumb("➕  Dodaj", AppColors.Success); btnDodaj.Click += BtnDodaj_Click; pnl.Controls.Add(btnDodaj);
            btnUredi = KreirajGumb("✏  Uredi", AppColors.Primary); btnUredi.Click += BtnUredi_Click; btnUredi.Enabled = false; pnl.Controls.Add(btnUredi);
            btnObrisi = KreirajGumb("🗑  Obriši", AppColors.Danger); btnObrisi.Click += BtnObrisi_Click; btnObrisi.Enabled = false; pnl.Controls.Add(btnObrisi);

            pnl.Layout += (s, e) => PozicionirajGumbe(pnl, y);
            pnl.SizeChanged += (s, e) => PozicionirajGumbe(pnl, y);
            this.Controls.Add(pnl);
        }

        private void KreirajGrid()
        {
            Guna2Panel pnlGrid = new Guna2Panel { Dock = DockStyle.Fill, FillColor = AppColors.CardBackground, BorderRadius = 12, Padding = new Padding(1) };
            pnlGrid.ShadowDecoration.Enabled = true; pnlGrid.ShadowDecoration.Depth = 8;

            lblStatus = new Label { Dock = DockStyle.Bottom, Height = 26, Font = AppFonts.Regular, ForeColor = AppColors.TextSecondary, BackColor = Color.FromArgb(245, 246, 250), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) };

            dgvGrupe = new Guna2DataGridView { Dock = DockStyle.Fill, BackgroundColor = AppColors.CardBackground, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, GridColor = Color.FromArgb(230, 232, 240), EnableHeadersVisualStyles = false, RowTemplate = { Height = 36 }, ColumnHeadersHeight = 40, AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dgvGrupe.DefaultCellStyle.BackColor = AppColors.CardBackground;
            dgvGrupe.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            dgvGrupe.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            dgvGrupe.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvGrupe.DefaultCellStyle.Font = AppFonts.Regular;
            dgvGrupe.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dgvGrupe.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);
            dgvGrupe.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            dgvGrupe.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvGrupe.ColumnHeadersDefaultCellStyle.Font = AppFonts.RegularMedium;

            // Kolone
            foreach (var k in new[] {
                (Name: "colId",    Header: "ID",    Min: 50,  Weight: 50f,  Align: DataGridViewContentAlignment.MiddleCenter),
                (Name: "colNaziv", Header: "Naziv", Min: 200, Weight: 250f, Align: DataGridViewContentAlignment.MiddleLeft),
                (Name: "colOpis",  Header: "Opis",  Min: 200, Weight: 350f, Align: DataGridViewContentAlignment.MiddleLeft),
            })
            {
                var col = new DataGridViewTextBoxColumn { Name = k.Name, HeaderText = k.Header, MinimumWidth = k.Min, FillWeight = k.Weight, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
                col.DefaultCellStyle.Alignment = k.Align;
                col.HeaderCell.Style.Alignment = k.Align;
                dgvGrupe.Columns.Add(col);
            }

            dgvGrupe.SelectionChanged += (s, e) => {
                _odabrana = dgvGrupe.SelectedRows.Count > 0 && dgvGrupe.SelectedRows[0].Index < _grupe.Count ? _grupe[dgvGrupe.SelectedRows[0].Index] : null;
                btnUredi.Enabled = _odabrana != null; btnObrisi.Enabled = _odabrana != null;
            };
            dgvGrupe.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnUredi_Click(s, e); };

            pnlGrid.Controls.Add(dgvGrupe);
            pnlGrid.Controls.Add(lblStatus);
            this.Controls.Add(pnlGrid);
        }

        private void UcitajGrupe()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                _grupe = GrupeArtikalaRepository.GetSveGrupe(txtPretraga?.Text ?? "");
                dgvGrupe.Rows.Clear();
                foreach (var g in _grupe)
                    dgvGrupe.Rows.Add(g.Id, g.Naziv, g.Opis);
                _odabrana = null;
                btnUredi.Enabled = false; btnObrisi.Enabled = false;
                lblBroj.Text = $"Ukupno {_grupe.Count} grupa";
                lblStatus.Text = $"  Prikazano: {_grupe.Count} rezultata";
            }
            catch (Exception ex) { MessageBox.Show("Greška:\n" + ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { this.Cursor = Cursors.Default; }
        }

        private void BtnDodaj_Click(object sender, EventArgs e)
        { if (new frmGrupaArtikala().ShowDialog() == DialogResult.OK) UcitajGrupe(); }

        private void BtnUredi_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            var g = GrupeArtikalaRepository.GetGrupaById(_odabrana.Id);
            if (g != null && new frmGrupaArtikala(g).ShowDialog() == DialogResult.OK) UcitajGrupe();
        }

        private void BtnObrisi_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            if (MessageBox.Show($"Sigurno želiš obrisati grupu:\n\n{_odabrana.Naziv}?", "Potvrda", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try { GrupeArtikalaRepository.Obrisi(_odabrana.Id); UcitajGrupe(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void PozicionirajGumbe(Control parent, int y)
        {
            int w = 100, r = 6, x = parent.ClientSize.Width - 10;
            btnObrisi.Size = btnUredi.Size = btnDodaj.Size = btnOsvjezi.Size = new Size(w, 36);
            btnObrisi.Location = new Point(x - w, y + 7);
            btnUredi.Location = new Point(x - w * 2 - r, y + 7);
            btnDodaj.Location = new Point(x - w * 3 - r * 2, y + 7);
            btnOsvjezi.Location = new Point(x - w * 4 - r * 3, y + 7);
        }

        private Guna2Button KreirajGumb(string text, Color boja)
        {
            var btn = new Guna2Button { Text = text, FillColor = boja, Font = AppFonts.Regular, ForeColor = AppColors.TextWhite, BorderRadius = 8, Cursor = Cursors.Hand };
            btn.HoverState.FillColor = ControlPaint.Light(boja, 0.15f);
            btn.PressedColor = ControlPaint.Dark(boja, 0.1f);
            return btn;
        }
    }
}