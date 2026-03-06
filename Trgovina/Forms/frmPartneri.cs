using Guna.UI2.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Data.Models;
using Trgovina.Utils;

namespace Trgovina.Forms
{
    public partial class frmPartneri : Form
    {
        private readonly Partner _partner;
        private readonly bool _editMode;

        private Guna2TextBox txtNaziv, txtAdresa, txtGrad, txtPostanskiBroj;
        private Guna2TextBox txtDrzava, txtOIB, txtTelefon, txtEmail;
        private Guna2TextBox txtKontaktOsoba, txtNapomena;
        private Guna2ToggleSwitch tglAktivan;
        private Guna2Button btnSpremi, btnOdustani;

        private int _contentHeight = 0;

        public frmPartneri(Partner partner = null)
        {
            _partner = partner;
            _editMode = partner != null;

            InitializeForm();
            if (_editMode) PopuniPolja();
        }

        private void InitializeForm()
        {
            this.Text = _editMode ? "Uredi partnera" : "Dodaj novog partnera";
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.AutoScroll = false;

            KreirajSadrzaj();

            int formHeight = _contentHeight + 20;
            this.Size = new Size(640, formHeight + 20);
            this.MinimumSize = new Size(640, formHeight);
        }

        private void KreirajSadrzaj()
        {
            int formW = 620;
            int cardX = 15;
            int cardW = formW - 30; // 590px
            int x1 = 15, x2 = cardW / 2 + 5;
            int inputW = cardW / 2 - 20; // ~275px

            // ── Naslov ────────────────────────────────────────────────────────────
            Label lblNaslov = new Label();
            lblNaslov.Text = _editMode ? "✏️  Uredi partnera" : "➕  Novi partner";
            lblNaslov.Font = AppFonts.TitleSmall;
            lblNaslov.ForeColor = AppColors.TextPrimary;
            lblNaslov.Location = new Point(cardX, 12);
            lblNaslov.AutoSize = true;
            this.Controls.Add(lblNaslov);

            Panel sep = new Panel();
            sep.Height = 1;
            sep.Width = cardW;
            sep.Location = new Point(cardX, 38);
            sep.BackColor = AppColors.BorderLight;
            this.Controls.Add(sep);

            // ── Card ──────────────────────────────────────────────────────────────
            Guna2Panel card = new Guna2Panel();
            card.Location = new Point(cardX, 45);
            card.Width = cardW;
            card.FillColor = AppColors.CardBackground;
            card.BorderRadius = 10;
            card.ShadowDecoration.Enabled = true;
            card.ShadowDecoration.Depth = 6;
            this.Controls.Add(card);

            int y = 15;
            int lbH = 16;
            int inH = 34;
            int gap = 8;
            int secGap = 18;

            // ── SEKCIJA 1: Osnovni podaci ─────────────────────────────────────────
            DodajSekcijuNaslov(card, "Osnovni podaci", x1, y); y += 26;

            DodajLabel(card, "Naziv partnera *", x1, y);
            DodajLabel(card, "OIB *", x2, y);
            y += lbH + 2;

            txtNaziv = DodajTextBox(card, x1, y, inputW);
            txtOIB = DodajTextBox(card, x2, y, inputW);
            y += inH + secGap;

            DodajLabel(card, "Aktivan", x1, y);
            y += lbH + 2;

            tglAktivan = new Guna2ToggleSwitch();
            tglAktivan.Size = new Size(48, 24);
            tglAktivan.Location = new Point(x1, y + 5);
            tglAktivan.Checked = true;
            tglAktivan.CheckedState.FillColor = AppColors.Primary;
            tglAktivan.UncheckedState.FillColor = AppColors.BorderLight;
            card.Controls.Add(tglAktivan);
            y += inH + secGap;

            // ── SEKCIJA 2: Adresa ─────────────────────────────────────────────────
            DodajSekcijuNaslov(card, "Adresa", x1, y); y += 26;

            DodajLabel(card, "Adresa", x1, y);

            y -= gap;

            txtAdresa = DodajTextBox(card, x1, y, cardW - 30);
            y += inH * 2 + secGap;

            DodajLabel(card, "Grad", x1, y);
            DodajLabel(card, "Poštanski broj", x2, y);

            y-= lbH;

            txtGrad = DodajTextBox(card, x1, y, inputW);
            txtPostanskiBroj = DodajTextBox(card, x2, y, 140);

            y += inH * 2 + secGap;

            DodajLabel(card, "Država", x1, y);
            y -= lbH + gap;

            txtDrzava = DodajTextBox(card, x1, y, inputW);
            txtDrzava.Text = "Hrvatska";
            y += inH * 2 + secGap * 2;

            // ── SEKCIJA 3: Kontakt ────────────────────────────────────────────────
            DodajSekcijuNaslov(card, "Kontakt", x1, y); y += 26;

            DodajLabel(card, "Telefon", x1, y);
            DodajLabel(card, "E-mail", x2, y);
            y -= lbH + secGap + 2;

            txtTelefon = DodajTextBox(card, x1, y, inputW);
            txtEmail = DodajTextBox(card, x2, y, inputW);
            y += inH * 2 + secGap * 2;

            DodajLabel(card, "Kontakt osoba", x1, y);
            y -= inH + gap;

            txtKontaktOsoba = DodajTextBox(card, x1, y, inputW);
            y += inH * 3 + secGap;

            // ── SEKCIJA 4: Napomena ───────────────────────────────────────────────
            DodajSekcijuNaslov(card, "Napomena", x1, y); y += 26;

            DodajLabel(card, "Napomena / dodatne informacije", x1, y);
            y -= inH + secGap;

            txtNapomena = DodajTextBox(card, x1, y, cardW - 30);
            txtNapomena.Height = 52;
            txtNapomena.Multiline = true;
            y += 52 + 15;

            card.Height = y + inH * 2 + secGap;

            // ── Gumbi ─────────────────────────────────────────────────────────────
            int btnY = card.Location.Y + card.Height + 10;

            btnSpremi = new Guna2Button();
            btnSpremi.Text = _editMode ? "💾  Spremi izmjene" : "✔  Dodaj partnera";
            btnSpremi.Size = new Size(180, 38);
            btnSpremi.Location = new Point(cardX, btnY);
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
            btnOdustani.Size = new Size(120, 38);
            btnOdustani.Location = new Point(cardX + 188, btnY);
            btnOdustani.FillColor = Color.FromArgb(210, 210, 215);
            btnOdustani.HoverState.FillColor = Color.FromArgb(190, 190, 195);
            btnOdustani.Font = AppFonts.Regular;
            btnOdustani.ForeColor = AppColors.TextPrimary;
            btnOdustani.BorderRadius = 8;
            btnOdustani.Cursor = Cursors.Hand;
            btnOdustani.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            this.Controls.Add(btnOdustani);

            _contentHeight = btnY + 38 + 10;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  POPUNJAVANJE POLJA (edit mode)
        // ══════════════════════════════════════════════════════════════════════════

        private void PopuniPolja()
        {
            if (_partner == null) return;

            txtNaziv.Text = _partner.Naziv;
            txtAdresa.Text = _partner.Adresa;
            txtGrad.Text = _partner.Grad;
            txtPostanskiBroj.Text = _partner.PostanskiBroj;
            txtDrzava.Text = _partner.Drzava;
            txtOIB.Text = _partner.OIB;
            txtTelefon.Text = _partner.Telefon;
            txtEmail.Text = _partner.Email;
            txtKontaktOsoba.Text = _partner.KontaktOsoba;
            txtNapomena.Text = _partner.Napomena;
            tglAktivan.Checked = _partner.Aktivan;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  SPREMI
        // ══════════════════════════════════════════════════════════════════════════

        private void BtnSpremi_Click(object sender, EventArgs e)
        {
            if (!Validiraj()) return;

            try
            {
                Partner p = KreirajPartneraIzFormi();

                if (_editMode)
                {
                    p.Id = _partner.Id;
                    PartneriRepository.AzurirajPartnera(p);
                }
                else
                {
                    PartneriRepository.DodajPartnera(p);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri spremanju:\n" + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  VALIDACIJA
        // ══════════════════════════════════════════════════════════════════════════

        private bool Validiraj()
        {
            if (string.IsNullOrWhiteSpace(txtNaziv.Text))
            {
                PrikaziGreskuPolja(txtNaziv, "Naziv partnera je obavezan!");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtEmail.Text) &&
                !txtEmail.Text.Contains("@"))
            {
                PrikaziGreskuPolja(txtEmail, "E-mail adresa nije ispravna!");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtOIB.Text) &&
                txtOIB.Text.Trim().Length != 11)
            {
                PrikaziGreskuPolja(txtOIB, "OIB mora imati točno 11 znamenki!");
                return false;
            }

            return true;
        }

        private Partner KreirajPartneraIzFormi()
        {
            return new Partner
            {
                Naziv = txtNaziv.Text.Trim(),
                Adresa = txtAdresa.Text.Trim(),
                Grad = txtGrad.Text.Trim(),
                PostanskiBroj = txtPostanskiBroj.Text.Trim(),
                Drzava = txtDrzava.Text.Trim(),
                OIB = txtOIB.Text.Trim(),
                Telefon = txtTelefon.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                KontaktOsoba = txtKontaktOsoba.Text.Trim(),
                Napomena = txtNapomena.Text.Trim(),
                Aktivan = tglAktivan.Checked
            };
        }

        private void PrikaziGreskuPolja(Control ctrl, string poruka)
        {
            ctrl.Focus();
            MessageBox.Show(poruka, "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  HELPER BUILDERS
        // ══════════════════════════════════════════════════════════════════════════

        private void DodajLabel(Control parent, string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = AppFonts.Regular;
            lbl.ForeColor = AppColors.TextSecondary;
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            parent.Controls.Add(lbl);
        }

        private void DodajSekcijuNaslov(Control parent, string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text.ToUpper();
            lbl.Font = AppFonts.RegularMedium;
            lbl.ForeColor = AppColors.Primary;
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            parent.Controls.Add(lbl);

            Panel linija = new Panel();
            linija.Height = 1;
            linija.Width = 560;
            linija.Location = new Point(x, y + 18);
            linija.BackColor = AppColors.BorderLight;
            parent.Controls.Add(linija);
        }

        private Guna2TextBox DodajTextBox(Control parent, int x, int y, int width)
        {
            Guna2TextBox txt = new Guna2TextBox();
            txt.Size = new Size(width, 34);
            txt.Location = new Point(x, y);
            txt.FillColor = AppColors.Background;
            txt.BorderColor = AppColors.BorderLight;
            txt.Font = AppFonts.Regular;
            txt.ForeColor = AppColors.TextPrimary;
            txt.BorderRadius = 8;
            txt.Enter += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.Primary;
            txt.Leave += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.BorderLight;
            parent.Controls.Add(txt);
            return txt;
        }
    }
}