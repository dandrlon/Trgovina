using Guna.UI2.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Data.Models;
using Trgovina.Utils;

namespace Trgovina.Forms
{
    public partial class frmGrupaArtikala : Form
    {
        private readonly GrupaArtikala _grupa;
        private readonly bool _editMode;

        private Guna2TextBox txtNaziv, txtOpis;
        private Guna2Button btnSpremi, btnOdustani;

        public frmGrupaArtikala(GrupaArtikala grupa = null)
        {
            _grupa = grupa;
            _editMode = grupa != null;
            InitializeForm();
            if (_editMode) PopuniPolja();
        }

        private void InitializeForm()
        {
            this.Text = _editMode ? "Uredi grupu artikala" : "Dodaj grupu artikala";
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Size = new Size(480, 360);

            // Naslov
            Label lblNaslov = new Label();
            lblNaslov.Text = _editMode ? "✏️  Uredi grupu" : "➕  Nova grupa artikala";
            lblNaslov.Font = AppFonts.TitleSmall;
            lblNaslov.ForeColor = AppColors.TextPrimary;
            lblNaslov.Location = new Point(15, 12);
            lblNaslov.AutoSize = true;
            this.Controls.Add(lblNaslov);

            Panel sep = new Panel { Height = 1, Width = 430, Location = new Point(15, 38), BackColor = AppColors.BorderLight };
            this.Controls.Add(sep);

            // Card
            Guna2Panel card = new Guna2Panel();
            card.Location = new Point(15, 45);
            card.Size = new Size(430, 205);
            card.FillColor = AppColors.CardBackground;
            card.BorderRadius = 10;
            card.ShadowDecoration.Enabled = true;
            card.ShadowDecoration.Depth = 6;
            this.Controls.Add(card);

            int y = 15;

            DodajSekcijuNaslov(card, "Podaci o grupi", 15, y, 390); y += 26;

            DodajLabel(card, "Naziv grupe *", 15, y); y += 18;
            txtNaziv = DodajTextBox(card, 15, y, 390); y += 52;

            DodajLabel(card, "Opis / napomena", 15, y); y += 8;
            txtOpis = DodajTextBox(card, 15, y, 390);
            txtOpis.Height = 52;
            txtOpis.Multiline = true;
            // Gumbi
            btnSpremi = new Guna2Button();
            btnSpremi.Text = _editMode ? "💾  Spremi izmjene" : "✔  Dodaj grupu";
            btnSpremi.Size = new Size(180, 38);
            btnSpremi.Location = new Point(15, 265);
            btnSpremi.FillColor = AppColors.Primary;
            btnSpremi.HoverState.FillColor = AppColors.PrimaryLight;
            btnSpremi.Font = AppFonts.RegularMedium;
            btnSpremi.ForeColor = AppColors.TextWhite;
            btnSpremi.BorderRadius = 8;
            btnSpremi.Cursor = Cursors.Hand;
            btnSpremi.Click += BtnSpremi_Click;
            this.Controls.Add(btnSpremi);

            btnOdustani = new Guna2Button();
            btnOdustani.Text = "✖  Odustani";
            btnOdustani.Size = new Size(150, 38);
            btnOdustani.Location = new Point(210, 265);
            btnOdustani.FillColor = Color.FromArgb(210, 210, 215);
            btnOdustani.HoverState.FillColor = Color.FromArgb(190, 190, 195);
            btnOdustani.Font = AppFonts.Regular;
            btnOdustani.ForeColor = AppColors.TextPrimary;
            btnOdustani.BorderRadius = 8;
            btnOdustani.Cursor = Cursors.Hand;
            btnOdustani.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            this.Controls.Add(btnOdustani);
        }

        private void PopuniPolja()
        {
            txtNaziv.Text = _grupa.Naziv;
            txtOpis.Text = _grupa.Opis;
        }

        private void BtnSpremi_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNaziv.Text))
            { PrikaziGresku(txtNaziv, "Naziv grupe je obavezan!"); return; }

            if (GrupeArtikalaRepository.NazivPostoji(txtNaziv.Text.Trim(), _editMode ? _grupa.Id : 0))
            { PrikaziGresku(txtNaziv, "Grupa s ovim nazivom već postoji!"); return; }

            try
            {
                var g = new GrupaArtikala { Naziv = txtNaziv.Text.Trim(), Opis = txtOpis.Text.Trim() };
                if (_editMode) { g.Id = _grupa.Id; GrupeArtikalaRepository.Azuriraj(g); }
                else GrupeArtikalaRepository.Dodaj(g);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri spremanju:\n" + ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrikaziGresku(Control ctrl, string poruka) { ctrl.Focus(); MessageBox.Show(poruka, "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning); }

        private void DodajLabel(Control p, string text, int x, int y)
        {
            var lbl = new Label { Text = text, Font = AppFonts.Regular, ForeColor = AppColors.TextSecondary, Location = new Point(x, y), AutoSize = true };
            p.Controls.Add(lbl);
        }

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