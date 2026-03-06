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
    public partial class frmArtikli : Form
    {
        private readonly Artikl _artikal;
        private readonly bool _editMode;
        private readonly List<GrupaArtikala> _grupe;

        private Guna2TextBox txtSifra, txtNaziv, txtOpis;
        private Guna2TextBox txtCijenaNabave, txtCijenaProdaje, txtKolicina;
        private Guna2ComboBox cmbGrupa, cmbJedinica, cmbPdv;
        private Guna2ToggleSwitch tglAktivan;
        private Label lblCijenaSPdv, lblMarza;
        private Guna2Button btnSpremi, btnOdustani;

        // Računamo ukupnu visinu sadržaja ovdje i postavljamo formu dinamički
        private int _contentHeight = 0;

        public frmArtikli(List<GrupaArtikala> grupe, Artikl artikal = null)
        {
            _grupe = grupe;
            _artikal = artikal;
            _editMode = artikal != null;

            InitializeForm();
            UcitajSifarnike();
            if (_editMode) PopuniPolja();
        }

        private void InitializeForm()
        {
            this.Text = _editMode ? "Uredi artikl" : "Dodaj novi artikl";
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.AutoScroll = false;

            KreirajSadrzaj();

            // Postavi visinu forme NAKON što je sadržaj kreiran
            int formHeight = _contentHeight + 20; // 20px bottom margin
            this.Size = new Size(620, formHeight);
            this.MinimumSize = new Size(620, formHeight);
        }

        private void KreirajSadrzaj()
        {
            int formW = 600;
            int cardX = 15;
            int cardW = formW - 30; // 570px
            int x1 = 15, x2 = cardW / 2 + 5;
            int inputW = cardW / 2 - 20; // ~265px svaki

            // ── Naslov ────────────────────────────────────────────────────────────
            Label lblNaslov = new Label();
            lblNaslov.Text = _editMode ? "✏️  Uredi artikl" : "➕  Novi artikl";
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

            // ─── Sadržaj kartice ──────────────────────────────────────────────────
            int y = 15;
            int lbH = 16;  // label visina
            int inH = 34;  // input visina
            int gap = 8;   // razmak između elementa iste sekcije
            int secGap = 14;  // razmak između sekcija

            // SEKCIJA 1: Osnovni podaci
            DodajSekcijuNaslov(card, "Osnovni podaci", x1, y); y += 26;

            DodajLabel(card, "Šifra artikla *", x1, y);
            DodajLabel(card, "Naziv artikla *", x2, y);
            y += lbH + 2;

            txtSifra = DodajTextBox(card, x1, y, inputW);
            txtNaziv = DodajTextBox(card, x2, y, inputW);
            y += inH + secGap;

            DodajLabel(card, "Opis / napomena", x1, y);
            y += gap;

            txtOpis = DodajTextBox(card, x1, y, cardW - 30);
            txtOpis.Height = 52;
            txtOpis.Multiline = true;
            y += 52 + inH;

            // SEKCIJA 2: Klasifikacija
            DodajSekcijuNaslov(card, "Klasifikacija", x1, y); y += 26;

            DodajLabel(card, "Grupa artikala", x1, y);
            DodajLabel(card, "Jedinica mjere", x2, y);
            y += lbH + gap;

            cmbGrupa = DodajComboBox(card, x1, y, inputW);
            cmbJedinica = DodajComboBox(card, x2, y, 120);
            y += inH + secGap;

            DodajLabel(card, "PDV stopa", x1, y);
            DodajLabel(card, "Aktivan", x2, y);
            y += lbH + gap;

            cmbPdv = DodajComboBox(card, x1, y, 160);

            tglAktivan = new Guna2ToggleSwitch();
            tglAktivan.Size = new Size(48, 24);
            tglAktivan.Location = new Point(x2, y + 5);
            tglAktivan.Checked = true;
            tglAktivan.CheckedState.FillColor = AppColors.Primary;
            tglAktivan.UncheckedState.FillColor = AppColors.BorderLight;
            card.Controls.Add(tglAktivan);
            y += inH + secGap;

            // SEKCIJA 3: Cijene i zaliha
            DodajSekcijuNaslov(card, "Cijene i zaliha", x1, y); y += 26;

            DodajLabel(card, "Cijena nabave (€)", x1, y);
            DodajLabel(card, "Cijena prodaje (€)", x2, y);
            y -= lbH + gap + 2;

            txtCijenaNabave = DodajTextBox(card, x1, y, inputW);
            txtCijenaProdaje = DodajTextBox(card, x2, y, inputW);
            txtCijenaNabave.TextChanged += (s, e) => IzracunajPdv();
            txtCijenaProdaje.TextChanged += (s, e) => IzracunajPdv();
            cmbPdv.SelectedIndexChanged += (s, e) => IzracunajPdv();
            y += inH * 2 + secGap + gap;

            // Info redak: cijena s PDV + marža
            lblCijenaSPdv = new Label();
            lblCijenaSPdv.Text = "Cijena s PDV: —";
            lblCijenaSPdv.Font = AppFonts.Regular;
            lblCijenaSPdv.ForeColor = AppColors.TextSecondary;
            lblCijenaSPdv.Location = new Point(x1, y);
            lblCijenaSPdv.AutoSize = true;
            card.Controls.Add(lblCijenaSPdv);

            lblMarza = new Label();
            lblMarza.Text = "Marža: —";
            lblMarza.Font = AppFonts.Regular;
            lblMarza.ForeColor = AppColors.Success;
            lblMarza.Location = new Point(x2, y);
            lblMarza.AutoSize = true;
            card.Controls.Add(lblMarza);
            y += 22 + secGap;

            DodajLabel(card, "Količina na zalihi", x1, y);
            y -= inH + 2;

            txtKolicina = DodajTextBox(card, x1, y, inputW);
            txtKolicina.Text = "0";
            y += inH + 90; // bottom padding kartice

            // Postavi visinu kartice točno na sadržaj
            card.Height = y;

            // ── Gumbi ispod kartice ───────────────────────────────────────────────
            int btnY = card.Location.Y + card.Height + 10;

            btnSpremi = new Guna2Button();
            btnSpremi.Text = _editMode ? "💾  Spremi izmjene" : "✔  Dodaj artikal";
            btnSpremi.Size = new Size(175, 38);
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
            btnOdustani.Location = new Point(cardX + 183, btnY);
            btnOdustani.FillColor = Color.FromArgb(210, 210, 215);
            btnOdustani.HoverState.FillColor = Color.FromArgb(190, 190, 195);
            btnOdustani.Font = AppFonts.Regular;
            btnOdustani.ForeColor = AppColors.TextPrimary;
            btnOdustani.BorderRadius = 8;
            btnOdustani.Cursor = Cursors.Hand;
            btnOdustani.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            this.Controls.Add(btnOdustani);

            // Ukupna visina = gumbi + 10px margin
            _contentHeight = btnY + 38 + 30;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ŠIFARNICI
        // ══════════════════════════════════════════════════════════════════════════

        private void UcitajSifarnike()
        {
            try
            {
                cmbGrupa.Items.Clear();
                cmbGrupa.Items.Add(new GrupaArtikala { Id = 0, Naziv = "— Odaberi grupu —" });
                foreach (var g in _grupe) cmbGrupa.Items.Add(g);
                cmbGrupa.SelectedIndex = 0;

                var jedinice = ArtikliRepository.GetJediniceMjere();
                cmbJedinica.Items.Clear();
                cmbJedinica.Items.Add(new JedinicaMjere { Id = 0, Naziv = "—", Skracenica = "" });
                foreach (var j in jedinice) cmbJedinica.Items.Add(j);
                cmbJedinica.SelectedIndex = 0;

                var pdvStope = ArtikliRepository.GetPdvStope();
                cmbPdv.Items.Clear();
                cmbPdv.Items.Add(new Pdv { Id = 0, Naziv = "— Bez PDV —", Stopa = 0 });
                foreach (var p in pdvStope) cmbPdv.Items.Add(p);
                cmbPdv.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri učitavanju šifarnika:\n" + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopuniPolja()
        {
            if (_artikal == null) return;

            txtSifra.Text = _artikal.Sifra;
            txtNaziv.Text = _artikal.Naziv;
            txtOpis.Text = _artikal.Opis;
            txtCijenaNabave.Text = _artikal.CijenaNabave.ToString("N2");
            txtCijenaProdaje.Text = _artikal.CijenaProdaje.ToString("N2");
            txtKolicina.Text = _artikal.Kolicina.ToString("N2");
            tglAktivan.Checked = _artikal.Aktivan;

            foreach (var item in cmbGrupa.Items)
                if (item is GrupaArtikala g && g.Id == _artikal.IdGrupe) { cmbGrupa.SelectedItem = item; break; }

            foreach (var item in cmbJedinica.Items)
                if (item is JedinicaMjere j && j.Id == _artikal.IdJediniceMjere) { cmbJedinica.SelectedItem = item; break; }

            foreach (var item in cmbPdv.Items)
                if (item is Pdv p && p.Id == _artikal.IdPdv) { cmbPdv.SelectedItem = item; break; }

            IzracunajPdv();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  LOGIKA
        // ══════════════════════════════════════════════════════════════════════════

        private void IzracunajPdv()
        {
            try
            {
                decimal.TryParse(txtCijenaProdaje.Text.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal prodaja);
                decimal.TryParse(txtCijenaNabave.Text.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal nabava);

                decimal pdvStopa = (cmbPdv.SelectedItem is Pdv pdv) ? pdv.Stopa : 0;
                decimal cijenaSPdv = prodaja * (1 + pdvStopa / 100);

                lblCijenaSPdv.Text = $"Cijena s PDV: {cijenaSPdv:N2} €";

                if (nabava > 0)
                {
                    decimal marza = (prodaja - nabava) / nabava * 100;
                    lblMarza.Text = $"Marža: {marza:N1}%";
                    lblMarza.ForeColor = marza >= 0 ? AppColors.Success : AppColors.Danger;
                }
                else
                {
                    lblMarza.Text = "Marža: —";
                    lblMarza.ForeColor = AppColors.TextSecondary;
                }
            }
            catch { }
        }

        private void BtnSpremi_Click(object sender, EventArgs e)
        {
            if (!Validiraj()) return;
            try
            {
                Artikl a = KreirajArtikalIzFormi();
                if (_editMode) { a.Id = _artikal.Id; ArtikliRepository.AzurirajArtikl(a); }
                else { ArtikliRepository.DodajArtikl(a); }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri spremanju:\n" + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool Validiraj()
        {
            if (string.IsNullOrWhiteSpace(txtSifra.Text))
            { PrikaziGreskuPolja(txtSifra, "Šifra artikla je obavezna!"); return false; }

            if (ArtikliRepository.SifraPostoji(txtSifra.Text.Trim(), _editMode ? _artikal.Id : 0))
            { PrikaziGreskuPolja(txtSifra, "Artikal s ovom šifrom već postoji!"); return false; }

            if (string.IsNullOrWhiteSpace(txtNaziv.Text))
            { PrikaziGreskuPolja(txtNaziv, "Naziv artikla je obavezan!"); return false; }

            if (!decimal.TryParse(txtCijenaProdaje.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _))
            { PrikaziGreskuPolja(txtCijenaProdaje, "Cijena prodaje nije ispravna!"); return false; }

            return true;
        }

        private Artikl KreirajArtikalIzFormi()
        {
            decimal.TryParse(txtCijenaNabave.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal nabava);
            decimal.TryParse(txtCijenaProdaje.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal prodaja);
            decimal.TryParse(txtKolicina.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal kolicina);

            return new Artikl
            {
                Sifra = txtSifra.Text.Trim(),
                Naziv = txtNaziv.Text.Trim(),
                Opis = txtOpis.Text.Trim(),
                CijenaNabave = nabava,
                CijenaProdaje = prodaja,
                Kolicina = kolicina,
                IdGrupe = (cmbGrupa.SelectedItem as GrupaArtikala)?.Id ?? 0,
                IdJediniceMjere = (cmbJedinica.SelectedItem as JedinicaMjere)?.Id ?? 0,
                IdPdv = (cmbPdv.SelectedItem as Pdv)?.Id ?? 0,
                PdvStopa = (cmbPdv.SelectedItem as Pdv)?.Stopa ?? 0,
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
            linija.Width = 540;
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

        private Guna2ComboBox DodajComboBox(Control parent, int x, int y, int width)
        {
            Guna2ComboBox cmb = new Guna2ComboBox();
            cmb.Size = new Size(width, 34);
            cmb.Location = new Point(x, y);
            cmb.FillColor = AppColors.Background;
            cmb.BorderColor = AppColors.BorderLight;
            cmb.FocusedColor = AppColors.Primary;
            cmb.Font = AppFonts.Regular;
            cmb.ForeColor = AppColors.TextPrimary;
            cmb.BorderRadius = 8;
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            parent.Controls.Add(cmb);
            return cmb;
        }
    }
}