using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Data.Models;
using Trgovina.Utils;

namespace Trgovina.Forms
{
    /// <summary>
    /// Popup forma za odabir artikla s pretragom i gridom.
    /// Koristi se umjesto ComboBoxa kada ima puno artikala.
    /// </summary>
    public partial class frmOdabirArtikla : Form
    {
        // ─── Rezultat odabira ─────────────────────────────────────────────────
        public Artikl OdabraniArtikl { get; private set; }

        // ─── State ────────────────────────────────────────────────────────────
        private List<Artikl> _sviArtikli = new List<Artikl>();
        private List<Artikl> _filtrirani = new List<Artikl>();

        // ─── UI ───────────────────────────────────────────────────────────────
        private Guna2TextBox txtPretraga;
        private Guna2DataGridView dgvArtikli;
        private Guna2Button btnOdaberi, btnOdustani;
        private Label lblBroj;

        public frmOdabirArtikla()
        {
            InitializeForm();
            UcitajArtikle();
        }

        private void InitializeForm()
        {
            this.Text = "Odabir artikla";
            this.Size = new Size(720, 520);
            this.MinimumSize = new Size(620, 440);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;

            // ── Naslov ────────────────────────────────────────────────────────
            Label lblNaslov = new Label();
            lblNaslov.Text = "🔍  Odabir artikla";
            lblNaslov.Font = AppFonts.TitleSmall;
            lblNaslov.ForeColor = AppColors.TextPrimary;
            lblNaslov.Location = new Point(15, 12);
            lblNaslov.AutoSize = true;
            this.Controls.Add(lblNaslov);

            Panel sep = new Panel();
            sep.Height = 1; sep.Top = 40; sep.Left = 15;
            sep.BackColor = AppColors.BorderLight;
            sep.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(sep);
            this.SizeChanged += (s, e) => sep.Width = this.ClientSize.Width - 30;
            sep.Width = 690;

            // ── Dno: gumbi ────────────────────────────────────────────────────
            Panel pnlDno = new Panel();
            pnlDno.Dock = DockStyle.Bottom;
            pnlDno.Height = 52;
            pnlDno.BackColor = AppColors.Background;
            this.Controls.Add(pnlDno);

            Panel sepDno = new Panel();
            sepDno.Dock = DockStyle.Top;
            sepDno.Height = 1;
            sepDno.BackColor = AppColors.BorderLight;
            pnlDno.Controls.Add(sepDno);

            btnOdaberi = new Guna2Button();
            btnOdaberi.Text = "✔  Odaberi";
            btnOdaberi.Size = new Size(130, 36);
            btnOdaberi.Location = new Point(15, 8);
            btnOdaberi.FillColor = AppColors.Primary;
            btnOdaberi.HoverState.FillColor = AppColors.PrimaryLight;
            btnOdaberi.Font = AppFonts.RegularMedium;
            btnOdaberi.ForeColor = Color.White;
            btnOdaberi.BorderRadius = 8;
            btnOdaberi.Cursor = Cursors.Hand;
            btnOdaberi.Enabled = false;
            btnOdaberi.Click += BtnOdaberi_Click;
            pnlDno.Controls.Add(btnOdaberi);

            btnOdustani = new Guna2Button();
            btnOdustani.Text = "✖  Odustani";
            btnOdustani.Size = new Size(110, 36);
            btnOdustani.Location = new Point(153, 8);
            btnOdustani.FillColor = Color.FromArgb(210, 210, 215);
            btnOdustani.HoverState.FillColor = Color.FromArgb(190, 190, 195);
            btnOdustani.Font = AppFonts.Regular;
            btnOdustani.ForeColor = AppColors.TextPrimary;
            btnOdustani.BorderRadius = 8;
            btnOdustani.Cursor = Cursors.Hand;
            btnOdustani.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlDno.Controls.Add(btnOdustani);

            lblBroj = new Label();
            lblBroj.Font = AppFonts.Regular;
            lblBroj.ForeColor = AppColors.TextSecondary;
            lblBroj.AutoSize = true;
            lblBroj.Location = new Point(280, 16);
            pnlDno.Controls.Add(lblBroj);

            // ── Pretraga ──────────────────────────────────────────────────────
            txtPretraga = new Guna2TextBox();
            txtPretraga.PlaceholderText = "🔍  Pretraži po šifri ili nazivu...";
            txtPretraga.Location = new Point(15, 50);
            txtPretraga.Size = new Size(690, 34);
            txtPretraga.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtPretraga.FillColor = AppColors.CardBackground;
            txtPretraga.BorderColor = AppColors.BorderLight;
            txtPretraga.Font = AppFonts.Regular;
            txtPretraga.ForeColor = AppColors.TextPrimary;
            txtPretraga.BorderRadius = 8;
            txtPretraga.Enter += (s, e) => txtPretraga.BorderColor = AppColors.Primary;
            txtPretraga.Leave += (s, e) => txtPretraga.BorderColor = AppColors.BorderLight;
            txtPretraga.TextChanged += (s, e) => PrimijeniFilter();
            this.Controls.Add(txtPretraga);
            this.SizeChanged += (s, e) => txtPretraga.Width = this.ClientSize.Width - 30;

            // ── Grid ──────────────────────────────────────────────────────────
            Guna2Panel pnlGrid = new Guna2Panel();
            pnlGrid.Location = new Point(15, 94);
            pnlGrid.FillColor = AppColors.CardBackground;
            pnlGrid.BorderRadius = 10;
            pnlGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(pnlGrid);
            this.SizeChanged += (s, e) =>
            {
                pnlGrid.Width = this.ClientSize.Width - 30;
                pnlGrid.Height = this.ClientSize.Height - pnlDno.Height - 94 - 10;
            };
            pnlGrid.Width = 690;
            pnlGrid.Height = 330;

            dgvArtikli = new Guna2DataGridView();
            dgvArtikli.Dock = DockStyle.Fill;
            dgvArtikli.BackgroundColor = AppColors.CardBackground;
            dgvArtikli.BorderStyle = BorderStyle.None;
            dgvArtikli.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvArtikli.GridColor = Color.FromArgb(230, 232, 240);
            dgvArtikli.EnableHeadersVisualStyles = false;
            dgvArtikli.RowTemplate.Height = 32;
            dgvArtikli.ColumnHeadersHeight = 36;
            dgvArtikli.AllowUserToAddRows = false;
            dgvArtikli.ReadOnly = true;
            dgvArtikli.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvArtikli.MultiSelect = false;
            dgvArtikli.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvArtikli.DefaultCellStyle.BackColor = AppColors.CardBackground;
            dgvArtikli.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            dgvArtikli.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            dgvArtikli.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvArtikli.DefaultCellStyle.Font = AppFonts.Regular;
            dgvArtikli.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);

            dgvArtikli.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            dgvArtikli.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvArtikli.ColumnHeadersDefaultCellStyle.Font = AppFonts.RegularMedium;

            // Kolone
            var kolone = new[]
            {
                new { Name="colSifra",   Header="Šifra",         Weight=80f,  Align=DataGridViewContentAlignment.MiddleLeft  },
                new { Name="colNaziv",   Header="Naziv",         Weight=220f, Align=DataGridViewContentAlignment.MiddleLeft  },
                new { Name="colJM",      Header="J/M",           Weight=45f,  Align=DataGridViewContentAlignment.MiddleCenter},
                new { Name="colCijena",  Header="Cijena (€)",    Weight=85f,  Align=DataGridViewContentAlignment.MiddleRight },
                new { Name="colPdv",     Header="PDV %",         Weight=55f,  Align=DataGridViewContentAlignment.MiddleCenter},
                new { Name="colZaliha",  Header="Zaliha",        Weight=70f,  Align=DataGridViewContentAlignment.MiddleRight },
            };
            foreach (var k in kolone)
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = k.Name; col.HeaderText = k.Header;
                col.FillWeight = k.Weight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.DefaultCellStyle.Alignment = k.Align;
                col.HeaderCell.Style.Alignment = k.Align;
                dgvArtikli.Columns.Add(col);
            }

            dgvArtikli.SelectionChanged += (s, e) =>
                btnOdaberi.Enabled = dgvArtikli.SelectedRows.Count > 0;
            dgvArtikli.CellDoubleClick += (s, e) =>
            { if (e.RowIndex >= 0) BtnOdaberi_Click(s, e); };

            pnlGrid.Controls.Add(dgvArtikli);
        }

        // ══════════════════════════════════════════════════════════════════════

        private void UcitajArtikle()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                _sviArtikli = RacuniRepository.GetArtikliZaOdabir();
                PrimijeniFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri učitavanju artikala:\n" + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { this.Cursor = Cursors.Default; }
        }

        private void PrimijeniFilter()
        {
            string q = txtPretraga.Text.Trim().ToLower();

            _filtrirani = string.IsNullOrEmpty(q)
                ? new List<Artikl>(_sviArtikli)
                : _sviArtikli.FindAll(a =>
                    a.Sifra.ToLower().Contains(q) ||
                    a.Naziv.ToLower().Contains(q));

            PopuniGrid();
        }

        private void PopuniGrid()
        {
            dgvArtikli.Rows.Clear();
            foreach (var a in _filtrirani)
            {
                int idx = dgvArtikli.Rows.Add(
                    a.Sifra,
                    a.Naziv,
                    a.NazivJediniceMjere,
                    a.CijenaProdaje.ToString("N2"),
                    a.PdvStopa.ToString("0") + "%",
                    a.Kolicina.ToString("N2")
                );
                // Crvena zaliha
                if (a.Kolicina <= 0)
                    dgvArtikli.Rows[idx].Cells["colZaliha"].Style.ForeColor = AppColors.Danger;
            }

            lblBroj.Text = $"{_filtrirani.Count} / {_sviArtikli.Count} artikala";
            btnOdaberi.Enabled = false;
        }

        private void BtnOdaberi_Click(object sender, EventArgs e)
        {
            if (dgvArtikli.SelectedRows.Count == 0) return;
            int idx = dgvArtikli.SelectedRows[0].Index;
            if (idx >= 0 && idx < _filtrirani.Count)
            {
                OdabraniArtikl = _filtrirani[idx];
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        // Tipkovnica: Enter potvrdi, Escape zatvori
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter && btnOdaberi.Enabled)
            { BtnOdaberi_Click(null, null); return true; }
            if (keyData == Keys.Escape)
            { DialogResult = DialogResult.Cancel; Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}