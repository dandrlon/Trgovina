using Guna.UI2.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Data.Models;
using Trgovina.Utils;

namespace Trgovina.Forms
{
    public partial class frmPdvStopa : Form
    {
        private readonly Pdv _pdv;
        private readonly bool _editMode;

        private Guna2TextBox txtNaziv, txtStopa;
        private Guna2Button btnSpremi, btnOdustani;

        public frmPdvStopa(Pdv pdv = null)
        {
            _pdv = pdv;
            _editMode = pdv != null;
            InitializeForm();
            if (_editMode) PopuniPolja();
        }

        private void InitializeForm()
        {
            this.Text = _editMode ? "Uredi PDV stopu" : "Dodaj PDV stopu";
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Size = new Size(400, 280);

            Label lblNaslov = new Label { Text = _editMode ? "✏️  Uredi PDV stopu" : "➕  Nova PDV stopa", Font = AppFonts.TitleSmall, ForeColor = AppColors.TextPrimary, Location = new Point(15, 12), AutoSize = true };
            this.Controls.Add(lblNaslov);
            this.Controls.Add(new Panel { Height = 1, Width = 350, Location = new Point(15, 38), BackColor = AppColors.BorderLight });

            Guna2Panel card = new Guna2Panel { Location = new Point(15, 45), Size = new Size(350, 130), FillColor = AppColors.CardBackground, BorderRadius = 10 };
            card.ShadowDecoration.Enabled = true;
            card.ShadowDecoration.Depth = 6;
            this.Controls.Add(card);

            int y = 15;
            DodajSekcijuNaslov(card, "Podaci o PDV stopi", 15, y, 320); y += 26;

            DodajLabel(card, "Naziv (npr. PDV 25%)", 15, y);
            DodajLabel(card, "Stopa (%)", 190, y); y += 18;

            txtNaziv = DodajTextBox(card, 15, y, 160);
            txtStopa = DodajTextBox(card, 190, y, 140);
            y += 42;

            // Gumbi
            btnSpremi = new Guna2Button { Text = _editMode ? "💾  Spremi" : "✔  Dodaj stopu", Size = new Size(155, 38), Location = new Point(15, 188), FillColor = AppColors.Primary, Font = AppFonts.RegularMedium, ForeColor = AppColors.TextWhite, BorderRadius = 8, Cursor = Cursors.Hand };
            btnSpremi.HoverState.FillColor = AppColors.PrimaryLight;
            btnSpremi.Click += BtnSpremi_Click;
            this.Controls.Add(btnSpremi);

            btnOdustani = new Guna2Button { Text = "✖  Odustani", Size = new Size(150, 38), Location = new Point(178, 188), FillColor = Color.FromArgb(210, 210, 215), Font = AppFonts.Regular, ForeColor = AppColors.TextPrimary, BorderRadius = 8, Cursor = Cursors.Hand };
            btnOdustani.HoverState.FillColor = Color.FromArgb(190, 190, 195);
            btnOdustani.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            this.Controls.Add(btnOdustani);
        }

        private void PopuniPolja()
        {
            txtNaziv.Text = _pdv.Naziv;
            txtStopa.Text = _pdv.Stopa.ToString("N2");
        }

        private void BtnSpremi_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNaziv.Text))
            { PrikaziGresku(txtNaziv, "Naziv PDV stope je obavezan!"); return; }

            if (!decimal.TryParse(txtStopa.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal stopa) || stopa < 0 || stopa > 100)
            { PrikaziGresku(txtStopa, "Stopa mora biti broj između 0 i 100!"); return; }

            if (PDVRepository.NazivPostoji(txtNaziv.Text.Trim(), _editMode ? _pdv.Id : 0))
            { PrikaziGresku(txtNaziv, "PDV stopa s ovim nazivom već postoji!"); return; }

            try
            {
                var p = new Pdv { Naziv = txtNaziv.Text.Trim(), Stopa = stopa };
                if (_editMode) { p.Id = _pdv.Id; PDVRepository.Azuriraj(p); }
                else PDVRepository.Dodaj(p);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri spremanju:\n" + ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrikaziGresku(Control ctrl, string poruka) { ctrl.Focus(); MessageBox.Show(poruka, "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning); }

        private void DodajLabel(Control p, string text, int x, int y) =>
            p.Controls.Add(new Label { Text = text, Font = AppFonts.Regular, ForeColor = AppColors.TextSecondary, Location = new Point(x, y), AutoSize = true });

        private void DodajSekcijuNaslov(Control p, string text, int x, int y, int width)
        {
            p.Controls.Add(new Label { Text = text.ToUpper(), Font = AppFonts.RegularMedium, ForeColor = AppColors.Primary, Location = new Point(x, y), AutoSize = true });
            p.Controls.Add(new Panel { Height = 1, Width = width, Location = new Point(x, y + 18), BackColor = AppColors.BorderLight });
        }

        private Guna2TextBox DodajTextBox(Control p, int x, int y, int width)
        {
            var txt = new Guna2TextBox { Size = new Size(width, 34), Location = new Point(x, y), FillColor = AppColors.Background, BorderColor = AppColors.BorderLight, Font = AppFonts.Regular, ForeColor = AppColors.TextPrimary, BorderRadius = 8 };
            txt.Enter += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.Primary;
            txt.Leave += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.BorderLight;
            p.Controls.Add(txt);
            return txt;
        }
    }
}