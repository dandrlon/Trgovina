using Guna.UI2.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Data.Models;
using Trgovina.Utils;

namespace Trgovina.Forms
{
    public partial class frmJedinicaMjere : Form
    {
        private readonly JedinicaMjere _jedinica;
        private readonly bool _editMode;

        private Guna2TextBox txtNaziv, txtSkracenica;
        private Guna2Button btnSpremi, btnOdustani;

        public frmJedinicaMjere(JedinicaMjere jedinica = null)
        {
            _jedinica = jedinica;
            _editMode = jedinica != null;
            InitializeForm();
            if (_editMode) PopuniPolja();
        }

        private void InitializeForm()
        {
            this.Text = _editMode ? "Uredi jedinicu mjere" : "Dodaj jedinicu mjere";
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Size = new Size(380, 255);

            this.Controls.Add(new Label { Text = _editMode ? "✏️  Uredi jedinicu mjere" : "➕  Nova jedinica mjere", Font = AppFonts.TitleSmall, ForeColor = AppColors.TextPrimary, Location = new Point(15, 12), AutoSize = true });
            this.Controls.Add(new Panel { Height = 1, Width = 350, Location = new Point(15, 38), BackColor = AppColors.BorderLight });

            Guna2Panel card = new Guna2Panel { Location = new Point(15, 45), Size = new Size(350, 120), FillColor = AppColors.CardBackground, BorderRadius = 10 };
            card.ShadowDecoration.Enabled = true;
            card.ShadowDecoration.Depth = 6;
            this.Controls.Add(card);

            int y = 15;
            DodajSekcijuNaslov(card, "Podaci o jedinici mjere", 15, y, 320); y += 26;

            DodajLabel(card, "Naziv *  (npr. Kilogram)", 15, y);
            DodajLabel(card, "Skraćenica *  (npr. kg)", 210, y); y += 18;

            txtNaziv = DodajTextBox(card, 15, y, 180);
            txtSkracenica = DodajTextBox(card, 210, y, 120);

            // Gumbi
            btnSpremi = new Guna2Button { Text = _editMode ? "💾  Spremi izmjene" : "✔  Dodaj jedinicu", Size = new Size(160, 38), Location = new Point(15, 182), FillColor = AppColors.Primary, Font = AppFonts.RegularMedium, ForeColor = AppColors.TextWhite, BorderRadius = 8, Cursor = Cursors.Hand };
            btnSpremi.HoverState.FillColor = AppColors.PrimaryLight;
            btnSpremi.Click += BtnSpremi_Click;
            this.Controls.Add(btnSpremi);

            btnOdustani = new Guna2Button { Text = "✖  Odustani", Size = new Size(110, 38), Location = new Point(183, 182), FillColor = Color.FromArgb(210, 210, 215), Font = AppFonts.Regular, ForeColor = AppColors.TextPrimary, BorderRadius = 8, Cursor = Cursors.Hand };
            btnOdustani.HoverState.FillColor = Color.FromArgb(190, 190, 195);
            btnOdustani.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            this.Controls.Add(btnOdustani);
        }

        private void PopuniPolja()
        {
            txtNaziv.Text = _jedinica.Naziv;
            txtSkracenica.Text = _jedinica.Skracenica;
        }

        private void BtnSpremi_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNaziv.Text))
            { PrikaziGresku(txtNaziv, "Naziv jedinice mjere je obavezan!"); return; }

            if (string.IsNullOrWhiteSpace(txtSkracenica.Text))
            { PrikaziGresku(txtSkracenica, "Skraćenica je obavezna!"); return; }

            if (JediniceMjereRepository.NazivPostoji(txtNaziv.Text.Trim(), _editMode ? _jedinica.Id : 0))
            { PrikaziGresku(txtNaziv, "Jedinica mjere s ovim nazivom već postoji!"); return; }

            try
            {
                var j = new JedinicaMjere { Naziv = txtNaziv.Text.Trim(), Skracenica = txtSkracenica.Text.Trim() };
                if (_editMode) { j.Id = _jedinica.Id; JediniceMjereRepository.Azuriraj(j); }
                else JediniceMjereRepository.Dodaj(j);
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