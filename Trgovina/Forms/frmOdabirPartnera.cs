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
    /// Popup forma za odabir partnera (kupca) s pretragom i gridom.
    /// </summary>
    public partial class frmOdabirPartnera : Form
    {
        public Partner OdabraniPartner { get; private set; }

        private List<Partner> _sviPartneri = new List<Partner>();
        private List<Partner> _filtrirani = new List<Partner>();

        private Guna2TextBox txtPretraga;
        private Guna2DataGridView dgvPartneri;
        private Guna2Button btnOdaberi, btnOdustani;
        private Label lblBroj;

        public frmOdabirPartnera()
        {
            InitializeForm();
            UcitajPartnere();
        }

        private void InitializeForm()
        {
            this.Text = "Odabir kupca";
            this.Size = new Size(680, 480);
            this.MinimumSize = new Size(580, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;

            Label lblNaslov = new Label();
            lblNaslov.Text = "👥  Odabir kupca";
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
            sep.Width = 650;

            // Dno
            Panel pnlDno = new Panel();
            pnlDno.Dock = DockStyle.Bottom;
            pnlDno.Height = 52;
            pnlDno.BackColor = AppColors.Background;
            this.Controls.Add(pnlDno);

            Panel sepDno = new Panel(); sepDno.Dock = DockStyle.Top; sepDno.Height = 1;
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

            // Pretraga
            txtPretraga = new Guna2TextBox();
            txtPretraga.PlaceholderText = "🔍  Pretraži po nazivu, OIB-u ili gradu...";
            txtPretraga.Location = new Point(15, 50);
            txtPretraga.Size = new Size(650, 34);
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

            // Grid
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
            pnlGrid.Width = 650; pnlGrid.Height = 300;

            dgvPartneri = new Guna2DataGridView();
            dgvPartneri.Dock = DockStyle.Fill;
            dgvPartneri.BackgroundColor = AppColors.CardBackground;
            dgvPartneri.BorderStyle = BorderStyle.None;
            dgvPartneri.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvPartneri.GridColor = Color.FromArgb(230, 232, 240);
            dgvPartneri.EnableHeadersVisualStyles = false;
            dgvPartneri.RowTemplate.Height = 32;
            dgvPartneri.ColumnHeadersHeight = 36;
            dgvPartneri.AllowUserToAddRows = false;
            dgvPartneri.ReadOnly = true;
            dgvPartneri.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPartneri.MultiSelect = false;
            dgvPartneri.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvPartneri.DefaultCellStyle.BackColor = AppColors.CardBackground;
            dgvPartneri.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            dgvPartneri.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            dgvPartneri.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvPartneri.DefaultCellStyle.Font = AppFonts.Regular;
            dgvPartneri.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);

            dgvPartneri.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            dgvPartneri.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPartneri.ColumnHeadersDefaultCellStyle.Font = AppFonts.RegularMedium;

            var kolone = new[]
            {
                new { Name="colNaziv", Header="Naziv",  Weight=220f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colOib",   Header="OIB",    Weight=110f, Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colGrad",  Header="Grad",   Weight=110f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colTel",   Header="Telefon",Weight=110f, Align=DataGridViewContentAlignment.MiddleLeft   },
            };
            foreach (var k in kolone)
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = k.Name; col.HeaderText = k.Header;
                col.FillWeight = k.Weight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.DefaultCellStyle.Alignment = k.Align;
                col.HeaderCell.Style.Alignment = k.Align;
                dgvPartneri.Columns.Add(col);
            }

            dgvPartneri.SelectionChanged += (s, e) =>
                btnOdaberi.Enabled = dgvPartneri.SelectedRows.Count > 0;
            dgvPartneri.CellDoubleClick += (s, e) =>
            { if (e.RowIndex >= 0) BtnOdaberi_Click(s, e); };

            pnlGrid.Controls.Add(dgvPartneri);
        }

        private void UcitajPartnere()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                _sviPartneri = RacuniRepository.GetKupci();
                PrimijeniFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška:\n" + ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { this.Cursor = Cursors.Default; }
        }

        private void PrimijeniFilter()
        {
            string q = txtPretraga.Text.Trim().ToLower();
            _filtrirani = string.IsNullOrEmpty(q)
                ? new List<Partner>(_sviPartneri)
                : _sviPartneri.FindAll(p =>
                    p.Naziv.ToLower().Contains(q) ||
                    p.OIB.ToLower().Contains(q) ||
                    p.Grad.ToLower().Contains(q));

            dgvPartneri.Rows.Clear();
            foreach (var p in _filtrirani)
                dgvPartneri.Rows.Add(p.Naziv, p.OIB, p.Grad, p.Telefon ?? "");

            lblBroj.Text = $"{_filtrirani.Count} / {_sviPartneri.Count} partnera";
            btnOdaberi.Enabled = false;
        }

        private void BtnOdaberi_Click(object sender, EventArgs e)
        {
            if (dgvPartneri.SelectedRows.Count == 0) return;
            int idx = dgvPartneri.SelectedRows[0].Index;
            if (idx >= 0 && idx < _filtrirani.Count)
            {
                OdabraniPartner = _filtrirani[idx];
                DialogResult = DialogResult.OK;
                Close();
            }
        }

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