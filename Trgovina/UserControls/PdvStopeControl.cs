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
    public partial class PdvStopeControl : UserControl
    {
        private List<Pdv> _stope = new List<Pdv>();
        private Pdv _odabrana = null;

        private Guna2DataGridView dgvStope;
        private Guna2TextBox txtPretraga;
        private Guna2Button btnDodaj, btnUredi, btnObrisi, btnOsvjezi;
        private Label lblBroj, lblStatus;

        public PdvStopeControl()
        {
            InitializeComponent();
            InitializeUI();
            UcitajStope();
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
            pnl.Controls.Add(new Label { Text = "🧾  PDV stope", Font = AppFonts.TitleLarge, ForeColor = AppColors.TextPrimary, Location = new Point(0, 2), AutoSize = true });
            pnl.Controls.Add(lblBroj);
            this.Controls.Add(pnl);
        }

        private void KreirajToolbar()
        {
            Guna2Panel pnl = new Guna2Panel { Dock = DockStyle.Top, Height = 56, FillColor = AppColors.CardBackground, BorderRadius = 10, Margin = new Padding(0, 0, 0, 8) };
            pnl.ShadowDecoration.Enabled = true; pnl.ShadowDecoration.Depth = 5;

            int y = 10;
            txtPretraga = new Guna2TextBox { PlaceholderText = "🔍  Pretraži PDV stope...", Size = new Size(220, 36), Location = new Point(10, y), FillColor = AppColors.Background, BorderColor = AppColors.BorderLight, Font = AppFonts.Regular, ForeColor = AppColors.TextPrimary, BorderRadius = 8 };
            txtPretraga.Enter += (s, e) => txtPretraga.BorderColor = AppColors.Primary;
            txtPretraga.Leave += (s, e) => txtPretraga.BorderColor = AppColors.BorderLight;
            txtPretraga.TextChanged += (s, e) => UcitajStope();
            pnl.Controls.Add(txtPretraga);

            btnOsvjezi = KreirajGumb("🔄  Osvježi", AppColors.Secondary); btnOsvjezi.Click += (s, e) => UcitajStope(); pnl.Controls.Add(btnOsvjezi);
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

            dgvStope = new Guna2DataGridView { Dock = DockStyle.Fill, BackgroundColor = AppColors.CardBackground, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, GridColor = Color.FromArgb(230, 232, 240), EnableHeadersVisualStyles = false, RowTemplate = { Height = 36 }, ColumnHeadersHeight = 40, AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dgvStope.DefaultCellStyle.BackColor = AppColors.CardBackground;
            dgvStope.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            dgvStope.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            dgvStope.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvStope.DefaultCellStyle.Font = AppFonts.Regular;
            dgvStope.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dgvStope.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);
            dgvStope.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            dgvStope.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvStope.ColumnHeadersDefaultCellStyle.Font = AppFonts.RegularMedium;

            foreach (var k in new[] {
                (Name: "colId",    Header: "ID",        Min: 50,  Weight: 50f,  Align: DataGridViewContentAlignment.MiddleCenter),
                (Name: "colNaziv", Header: "Naziv",     Min: 200, Weight: 280f, Align: DataGridViewContentAlignment.MiddleLeft),
                (Name: "colStopa", Header: "Stopa (%)", Min: 100, Weight: 120f, Align: DataGridViewContentAlignment.MiddleRight),
                (Name: "colFakt",  Header: "Faktor",    Min: 100, Weight: 120f, Align: DataGridViewContentAlignment.MiddleRight),
            })
            {
                var col = new DataGridViewTextBoxColumn { Name = k.Name, HeaderText = k.Header, MinimumWidth = k.Min, FillWeight = k.Weight, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
                col.DefaultCellStyle.Alignment = k.Align;
                col.HeaderCell.Style.Alignment = k.Align;
                dgvStope.Columns.Add(col);
            }

            dgvStope.SelectionChanged += (s, e) => {
                _odabrana = dgvStope.SelectedRows.Count > 0 && dgvStope.SelectedRows[0].Index < _stope.Count ? _stope[dgvStope.SelectedRows[0].Index] : null;
                btnUredi.Enabled = _odabrana != null; btnObrisi.Enabled = _odabrana != null;
            };
            dgvStope.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnUredi_Click(s, e); };

            pnlGrid.Controls.Add(dgvStope);
            pnlGrid.Controls.Add(lblStatus);
            this.Controls.Add(pnlGrid);
        }

        private void UcitajStope()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                _stope = PDVRepository.GetSvePdvStope(txtPretraga?.Text ?? "");
                dgvStope.Rows.Clear();
                foreach (var p in _stope)
                    dgvStope.Rows.Add(p.Id, p.Naziv, $"{p.Stopa:N2} %", $"{1 + p.Stopa / 100:N4}");
                _odabrana = null;
                btnUredi.Enabled = false; btnObrisi.Enabled = false;
                lblBroj.Text = $"Ukupno {_stope.Count} PDV stopa";
                lblStatus.Text = $"  Prikazano: {_stope.Count} rezultata";
            }
            catch (Exception ex) { MessageBox.Show("Greška:\n" + ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { this.Cursor = Cursors.Default; }
        }

        private void BtnDodaj_Click(object sender, EventArgs e)
        { if (new frmPdvStopa().ShowDialog() == DialogResult.OK) UcitajStope(); }

        private void BtnUredi_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            var p = PDVRepository.GetPdvById(_odabrana.Id);
            if (p != null && new frmPdvStopa(p).ShowDialog() == DialogResult.OK) UcitajStope();
        }

        private void BtnObrisi_Click(object sender, EventArgs e)
        {
            if (_odabrana == null) return;
            if (MessageBox.Show($"Sigurno želiš obrisati PDV stopu:\n\n{_odabrana.Naziv} ({_odabrana.Stopa}%)?", "Potvrda", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try { PDVRepository.Obrisi(_odabrana.Id); UcitajStope(); }
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