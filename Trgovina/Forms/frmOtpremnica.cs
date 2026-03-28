using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Data.Models;
using Trgovina.Utils;

namespace Trgovina.Forms
{
    public partial class frmOtpremnica : Form
    {
        // ─── State ────────────────────────────────────────────────────────
        private readonly Otpremnica _otpremnica;
        private readonly bool _editMode;
        private List<OtpremnicaStavka> _stavke = new List<OtpremnicaStavka>();
        private List<Prodavac> _prodavaci = new List<Prodavac>();

        private Partner _odabraniKupac = null;

        // ─── UI: zaglavlje ────────────────────────────────────────────────
        private Guna2TextBox txtBrojOtpremnice, txtNapomena;
        private DateTimePicker dtpDatumOtpremnice, dtpDatumIsporuke;
        private Guna2ComboBox cmbProdavac;
        private Guna2TextBox txtKupacPrikaz;
        private Guna2Button btnOdaberiKupca;

        // ─── UI: stavke ───────────────────────────────────────────────────
        private Guna2TextBox txtArtiklPrikaz;
        private Guna2Button btnOdaberiArtikl;
        private Guna2TextBox txtKolicina, txtCijenaBezPdv;
        private Guna2Button btnDodajStavku, btnUkloniStavku;
        private Artikl _trenutniArtikl = null;

        private Guna2DataGridView dgvStavke;
        private Label lblUkupno;

        private Guna2Button btnSpremi, btnOdustani;

        // ─── Rezultat za pozivajuću formu ─────────────────────────────────
        public int SpravljeniId { get; private set; }

        public frmOtpremnica(Otpremnica otpremnica = null)
        {
            _otpremnica = otpremnica;
            _editMode = otpremnica != null;
            if (_editMode && otpremnica.Stavke != null)
                _stavke = new List<OtpremnicaStavka>(otpremnica.Stavke);

            UcitajSifarnike();
            InitializeForm();
            UcitajProdavaceUCombo();

            if (_editMode)
                PopuniPolja();
            else
            {
                txtBrojOtpremnice.Text = OtpremnicaRepository.GeneriraBrojOtpremnice();
                dtpDatumOtpremnice.Value = DateTime.Today;
                dtpDatumIsporuke.Value = DateTime.Today.AddDays(7);
            }

            PopuniGridStavke();
            IzracunajUkupno();
        }

        // ══════════════════════════════════════════════════════════════════
        //  INIT
        // ══════════════════════════════════════════════════════════════════

        private void UcitajSifarnike()
        {
            try { _prodavaci = RacuniRepository.GetProdavaci(); }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri učitavanju šifarnika:\n" + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeForm()
        {
            this.Text = _editMode ? $"Uredi otpremnicu — {_otpremnica.BrojOtpremnice}" : "Nova otpremnica";
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(900, 660);
            this.Size = new Size(1050, 740);
            this.MaximizeBox = true;

            KreirajSadrzaj();
        }

        private void KreirajSadrzaj()
        {
            Label lblNaslov = new Label();
            lblNaslov.Text = _editMode ? "✏️  Uredi otpremnicu" : "📦  Nova otpremnica";
            lblNaslov.Font = AppFonts.TitleSmall;
            lblNaslov.ForeColor = AppColors.TextPrimary;
            lblNaslov.Location = new Point(15, 12);
            lblNaslov.AutoSize = true;
            this.Controls.Add(lblNaslov);

            Panel sep = new Panel { Height = 1, Left = 15, Top = 38, BackColor = AppColors.BorderLight };
            sep.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(sep);
            this.SizeChanged += (s, e) => sep.Width = this.ClientSize.Width - 30;

            // ── Dno ───────────────────────────────────────────────────────
            Panel pnlDno = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = AppColors.Background };
            this.Controls.Add(pnlDno);
            new Panel { Dock = DockStyle.Top, Height = 1, BackColor = AppColors.BorderLight }.Also(p => pnlDno.Controls.Add(p));

            btnSpremi = new Guna2Button();
            btnSpremi.Text = _editMode ? "💾  Spremi izmjene" : "✔  Kreiraj otpremnicu";
            btnSpremi.Size = new Size(200, 38); btnSpremi.Location = new Point(15, 9);
            btnSpremi.FillColor = AppColors.Primary;
            btnSpremi.HoverState.FillColor = AppColors.PrimaryLight;
            btnSpremi.Font = AppFonts.RegularMedium; btnSpremi.ForeColor = AppColors.TextWhite;
            btnSpremi.BorderRadius = 8; btnSpremi.Cursor = Cursors.Hand;
            btnSpremi.Click += BtnSpremi_Click;
            pnlDno.Controls.Add(btnSpremi);

            btnOdustani = new Guna2Button();
            btnOdustani.Text = "✖  Odustani";
            btnOdustani.Size = new Size(120, 38); btnOdustani.Location = new Point(222, 9);
            btnOdustani.FillColor = Color.FromArgb(210, 210, 215);
            btnOdustani.HoverState.FillColor = Color.FromArgb(190, 190, 195);
            btnOdustani.Font = AppFonts.Regular; btnOdustani.ForeColor = AppColors.TextPrimary;
            btnOdustani.BorderRadius = 8; btnOdustani.Cursor = Cursors.Hand;
            btnOdustani.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlDno.Controls.Add(btnOdustani);

            Guna2Button btnPreview = new Guna2Button();
            btnPreview.Text = "👁  Preview";
            btnPreview.Size = new Size(120, 38); btnPreview.Location = new Point(350, 9);
            btnPreview.FillColor = Color.FromArgb(142, 68, 173);
            btnPreview.HoverState.FillColor = ControlPaint.Light(Color.FromArgb(142, 68, 173), 0.15f);
            btnPreview.Font = AppFonts.Regular; btnPreview.ForeColor = Color.White;
            btnPreview.BorderRadius = 8; btnPreview.Cursor = Cursors.Hand;
            btnPreview.Click += BtnPreview_Click;
            pnlDno.Controls.Add(btnPreview);

            // ── Scroll panel ──────────────────────────────────────────────
            Panel pnlScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppColors.Background };
            this.Controls.Add(pnlScroll);

            const int zaglavljeH = 185;
            Guna2Panel cardZaglavlje = KreirajCard(pnlScroll, 15, 48, 0, zaglavljeH);
            cardZaglavlje.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            int stavkeTop = 48 + zaglavljeH + 10;
            Guna2Panel cardStavke = KreirajCard(pnlScroll, 15, stavkeTop, 0, 0);
            cardStavke.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            void Resize(object s, EventArgs e)
            {
                int w = pnlScroll.ClientSize.Width - 30;
                cardZaglavlje.Width = w;
                cardStavke.Width = w;
                cardStavke.Height = pnlScroll.ClientSize.Height - stavkeTop - 10;
            }
            this.SizeChanged += Resize;
            cardZaglavlje.Width = this.ClientSize.Width - 30;
            cardStavke.Width = this.ClientSize.Width - 30;
            cardStavke.Height = this.ClientSize.Height - pnlDno.Height - stavkeTop - 10;

            KreirajZaglavljeCard(cardZaglavlje);
            KreirajStavkeCard(cardStavke);
        }

        // ══════════════════════════════════════════════════════════════════
        //  CARD: ZAGLAVLJE
        // ══════════════════════════════════════════════════════════════════

        private void KreirajZaglavljeCard(Guna2Panel card)
        {
            int y = 15;
            int x1 = 15, x2 = 205, x3 = 395, x4 = 585, x5 = 775;
            int inW = 175, inH = 34;

            DodajSekcijuNaslov(card, "Zaglavlje otpremnice", x1, y); y += 26;

            // Red 1 — labele
            DodajLabel(card, "Broj otpremnice *", x1, y);
            DodajLabel(card, "Datum otpremnice *", x2, y);
            DodajLabel(card, "Datum isporuke", x3, y);
            DodajLabel(card, "Prodavač", x4, y);
            y += 18;

            txtBrojOtpremnice = DodajTextBox(card, x1, y, inW);

            dtpDatumOtpremnice = new DateTimePicker { Size = new Size(inW, inH), Location = new Point(x2, y + 10), Format = DateTimePickerFormat.Short };
            card.Controls.Add(dtpDatumOtpremnice);

            dtpDatumIsporuke = new DateTimePicker { Size = new Size(inW, inH), Location = new Point(x3, y + 10), Format = DateTimePickerFormat.Short };
            card.Controls.Add(dtpDatumIsporuke);

            cmbProdavac = DodajComboBox(card, x4, y + 5, inW);
            y += inH + 14;

            // Red 2 — kupac + napomena
            DodajLabel(card, "Kupac *", x1, y + 5);
            DodajLabel(card, "Napomena", x4, y + 5);
            y += 18;

            txtKupacPrikaz = new Guna2TextBox
            {
                Size = new Size(inW + 185, inH),
                Location = new Point(x1, y),
                FillColor = Color.FromArgb(245, 246, 252),
                BorderColor = AppColors.BorderLight,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextPrimary,
                BorderRadius = 8,
                ReadOnly = true,
                PlaceholderText = "Kliknite za odabir kupca..."
            };
            card.Controls.Add(txtKupacPrikaz);

            btnOdaberiKupca = new Guna2Button
            {
                Text = "👥  Odaberi",
                Size = new Size(100, inH),
                Location = new Point(x1 + inW + 185 + 6, y + 20),
                FillColor = AppColors.Secondary,
                Font = AppFonts.Regular,
                ForeColor = Color.White,
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };
            btnOdaberiKupca.HoverState.FillColor = AppColors.Primary;
            btnOdaberiKupca.Click += BtnOdaberiKupca_Click;
            card.Controls.Add(btnOdaberiKupca);

            txtNapomena = DodajTextBox(card, x4, y, inW + 195);
            txtNapomena.PlaceholderText = "Opcionalna napomena...";
        }

        // ══════════════════════════════════════════════════════════════════
        //  CARD: STAVKE
        // ══════════════════════════════════════════════════════════════════

        private void KreirajStavkeCard(Guna2Panel card)
        {
            int y = 15;
            DodajSekcijuNaslov(card, "Stavke otpremnice", 15, y); y += 28;

            // ── Toolbar ───────────────────────────────────────────────────
            Guna2Panel pnlUnos = new Guna2Panel
            {
                Location = new Point(15, y),
                Height = 44,
                FillColor = Color.FromArgb(245, 246, 252),
                BorderRadius = 8,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            card.Controls.Add(pnlUnos);
            card.SizeChanged += (s, e) => pnlUnos.Width = card.Width - 30;
            pnlUnos.Width = 990;

            txtArtiklPrikaz = new Guna2TextBox
            {
                PlaceholderText = "Kliknite 'Odaberi artikl'...",
                Size = new Size(240, 30),
                Location = new Point(6, 7),
                FillColor = AppColors.Background,
                BorderColor = AppColors.BorderLight,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextPrimary,
                BorderRadius = 6,
                ReadOnly = true
            };
            pnlUnos.Controls.Add(txtArtiklPrikaz);

            btnOdaberiArtikl = new Guna2Button
            {
                Text = "📦  Artikl",
                Size = new Size(88, 30),
                Location = new Point(252, 7),
                FillColor = AppColors.Secondary,
                Font = AppFonts.Regular,
                ForeColor = Color.White,
                BorderRadius = 6,
                Cursor = Cursors.Hand
            };
            btnOdaberiArtikl.HoverState.FillColor = AppColors.Primary;
            btnOdaberiArtikl.Click += BtnOdaberiArtikl_Click;
            pnlUnos.Controls.Add(btnOdaberiArtikl);

            DodajMiniLabel(pnlUnos, "Kol.:", 348, 14);
            txtKolicina = DodajMiniTextBox(pnlUnos, 386, 7, 68);
            txtKolicina.Text = "1";

            DodajMiniLabel(pnlUnos, "Cijena bez PDV:", 462, 14);
            txtCijenaBezPdv = DodajMiniTextBox(pnlUnos, 568, 7, 88);

            btnDodajStavku = new Guna2Button
            {
                Text = "➕  Dodaj",
                Size = new Size(102, 30),
                Location = new Point(668, 7),
                FillColor = AppColors.Success,
                Font = AppFonts.Regular,
                ForeColor = Color.White,
                BorderRadius = 6,
                Cursor = Cursors.Hand
            };
            btnDodajStavku.HoverState.FillColor = ControlPaint.Light(AppColors.Success, 0.15f);
            btnDodajStavku.Click += BtnDodajStavku_Click;
            pnlUnos.Controls.Add(btnDodajStavku);

            btnUkloniStavku = new Guna2Button
            {
                Text = "🗑  Ukloni",
                Size = new Size(102, 30),
                Location = new Point(788, 7),
                FillColor = AppColors.Danger,
                Font = AppFonts.Regular,
                ForeColor = Color.White,
                BorderRadius = 6,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnUkloniStavku.HoverState.FillColor = ControlPaint.Light(AppColors.Danger, 0.15f);
            btnUkloniStavku.Click += BtnUkloniStavku_Click;
            pnlUnos.Controls.Add(btnUkloniStavku);

            y += 52;

            // ── Grid ──────────────────────────────────────────────────────
            dgvStavke = new Guna2DataGridView
            {
                Location = new Point(15, y),
                BorderStyle = BorderStyle.None,
                BackgroundColor = AppColors.CardBackground,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(230, 232, 240),
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 32 },
                ColumnHeadersHeight = 36,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            dgvStavke.DefaultCellStyle.BackColor = AppColors.CardBackground;
            dgvStavke.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            dgvStavke.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            dgvStavke.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvStavke.DefaultCellStyle.Font = AppFonts.Regular;
            dgvStavke.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);
            dgvStavke.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            dgvStavke.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvStavke.ColumnHeadersDefaultCellStyle.Font = AppFonts.RegularMedium;
            dgvStavke.SelectionChanged += (s, e) =>
                btnUkloniStavku.Enabled = dgvStavke.SelectedRows.Count > 0;

            card.Controls.Add(dgvStavke);
            card.SizeChanged += (s, e) =>
            {
                dgvStavke.Width = card.Width - 30;
                dgvStavke.Height = card.Height - y - 70;
            };
            dgvStavke.Width = 990; dgvStavke.Height = 200;

            KonfigurisiKolone();

            // ── Ukupno ────────────────────────────────────────────────────
            Panel pnlUkupno = new Panel
            {
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new Size(320, 22)
            };
            card.Controls.Add(pnlUkupno);
            card.SizeChanged += (s, e) =>
                pnlUkupno.Location = new Point(card.Width - 335, card.Height - 62);
            pnlUkupno.Location = new Point(655, 350);

            lblUkupno = new Label
            {
                Text = "Ukupno bez PDV:   0,00 €",
                Font = AppFonts.RegularMedium,
                ForeColor = AppColors.Primary,
                AutoSize = false,
                Size = new Size(320, 20),
                Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlUkupno.Controls.Add(lblUkupno);
        }

        private void KonfigurisiKolone()
        {
            dgvStavke.Columns.Clear();
            var kolone = new[]
            {
                new { Name="colRbr",    Header="Rbr",           Weight=30f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colSifra",  Header="Šifra",         Weight=65f,  Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colNaziv",  Header="Naziv artikla", Weight=230f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colJM",     Header="J/M",           Weight=40f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colKol",    Header="Količina",      Weight=75f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colCijena", Header="Cijena bez PDV", Weight=95f, Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colIznos",  Header="Iznos bez PDV", Weight=95f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colPdv",    Header="PDV%",          Weight=52f,  Align=DataGridViewContentAlignment.MiddleCenter },
            };
            foreach (var k in kolone)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    Name = k.Name,
                    HeaderText = k.Header,
                    FillWeight = k.Weight,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                };
                col.DefaultCellStyle.Alignment = k.Align;
                col.HeaderCell.Style.Alignment = k.Align;
                dgvStavke.Columns.Add(col);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  POPUNJAVANJE
        // ══════════════════════════════════════════════════════════════════

        private void UcitajProdavaceUCombo()
        {
            cmbProdavac.Items.Clear();
            cmbProdavac.Items.Add(new Prodavac { Id = 0, Ime = "—", Prezime = "" });
            foreach (var p in _prodavaci) cmbProdavac.Items.Add(p);
            cmbProdavac.SelectedIndex = 0;
        }

        private void PopuniPolja()
        {
            if (_otpremnica == null) return;
            txtBrojOtpremnice.Text = _otpremnica.BrojOtpremnice;
            dtpDatumOtpremnice.Value = _otpremnica.DatumOtpremnice;
            if (_otpremnica.DatumIsporuke.HasValue)
                dtpDatumIsporuke.Value = _otpremnica.DatumIsporuke.Value;
            txtNapomena.Text = _otpremnica.Napomena ?? "";

            _odabraniKupac = new Partner { Id = _otpremnica.KupacId, Naziv = _otpremnica.NazivKupca };
            txtKupacPrikaz.Text = _otpremnica.NazivKupca;

            foreach (var item in cmbProdavac.Items)
                if (item is Prodavac pr && pr.Id == _otpremnica.ProdavacId)
                { cmbProdavac.SelectedItem = item; break; }
        }

        private void PopuniGridStavke()
        {
            dgvStavke.Rows.Clear();
            int rbr = 1;
            foreach (var s in _stavke)
            {
                s.Rbr = rbr++;
                dgvStavke.Rows.Add(
                    s.Rbr,
                    s.SifraArtikla,
                    s.NazivArtikla,
                    s.NazivJediniceMjere,
                    s.Kolicina.ToString("N3"),
                    s.CijenaBezPdv.ToString("N4"),
                    s.IznosBezPdv.ToString("N2"),
                    s.PdvStopa.ToString("0") + "%"
                );
            }
            btnUkloniStavku.Enabled = false;
        }

        // ══════════════════════════════════════════════════════════════════
        //  EVENTI — PICKERI
        // ══════════════════════════════════════════════════════════════════

        private void BtnOdaberiKupca_Click(object sender, EventArgs e)
        {
            using (var popup = new frmOdabirPartnera())
            {
                if (popup.ShowDialog() == DialogResult.OK && popup.OdabraniPartner != null)
                {
                    _odabraniKupac = popup.OdabraniPartner;
                    txtKupacPrikaz.Text = _odabraniKupac.Naziv;
                    txtKupacPrikaz.BorderColor = AppColors.Success;
                }
            }
        }

        private void BtnOdaberiArtikl_Click(object sender, EventArgs e)
        {
            using (var popup = new frmOdabirArtikla())
            {
                if (popup.ShowDialog() == DialogResult.OK && popup.OdabraniArtikl != null)
                {
                    _trenutniArtikl = popup.OdabraniArtikl;
                    txtArtiklPrikaz.Text = $"{_trenutniArtikl.Sifra}  —  {_trenutniArtikl.Naziv}";
                    // Postavi cijenu bez PDV-a iz artikla
                    decimal cijenaBezPdv = _trenutniArtikl.PdvStopa > 0
                        ? _trenutniArtikl.CijenaProdaje / (1 + _trenutniArtikl.PdvStopa / 100m)
                        : _trenutniArtikl.CijenaProdaje;
                    txtCijenaBezPdv.Text = cijenaBezPdv.ToString("N4");
                    txtKolicina.Focus();
                    txtKolicina.SelectAll();
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  EVENTI — STAVKE
        // ══════════════════════════════════════════════════════════════════

        private void BtnDodajStavku_Click(object sender, EventArgs e)
        {
            if (_trenutniArtikl == null)
            { MessageBox.Show("Odaberite artikl!", "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (!decimal.TryParse(txtKolicina.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal kolicina) || kolicina <= 0)
            { PrikaziGresku(txtKolicina, "Unesite ispravnu količinu!"); return; }

            if (!decimal.TryParse(txtCijenaBezPdv.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal cijena) || cijena < 0)
            { PrikaziGresku(txtCijenaBezPdv, "Unesite ispravnu cijenu!"); return; }

            var stavka = new OtpremnicaStavka
            {
                ArtiklId = _trenutniArtikl.Id,
                SifraArtikla = _trenutniArtikl.Sifra,
                NazivArtikla = _trenutniArtikl.Naziv,
                NazivJediniceMjere = _trenutniArtikl.NazivJediniceMjere,
                Kolicina = kolicina,
                CijenaBezPdv = cijena,
                IznosBezPdv = Math.Round(kolicina * cijena, 2),
                PdvStopa = _trenutniArtikl.PdvStopa
            };

            _stavke.Add(stavka);
            PopuniGridStavke();
            IzracunajUkupno();

            _trenutniArtikl = null;
            txtArtiklPrikaz.Text = "";
            txtKolicina.Text = "1";
            txtCijenaBezPdv.Text = "";
            btnOdaberiArtikl.Focus();
        }

        private void BtnUkloniStavku_Click(object sender, EventArgs e)
        {
            if (dgvStavke.SelectedRows.Count == 0) return;
            int idx = dgvStavke.SelectedRows[0].Index;
            if (idx >= 0 && idx < _stavke.Count)
            {
                _stavke.RemoveAt(idx);
                PopuniGridStavke();
                IzracunajUkupno();
            }
        }

        private void IzracunajUkupno()
        {
            decimal ukupno = _stavke.Sum(s => s.IznosBezPdv);
            lblUkupno.Text = $"Ukupno bez PDV:   {ukupno:N2} €";
        }

        // ══════════════════════════════════════════════════════════════════
        //  PREVIEW
        // ══════════════════════════════════════════════════════════════════

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            if (_stavke.Count == 0)
            {
                MessageBox.Show("Dodajte barem jednu stavku za prikaz previewa.",
                    "Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var privremena = KreirajOtpremnicuIzFormi();
            try
            {
                string pdfPath = OtpremnicaPdfHelper.GenerirajPdfTemp(privremena);
                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri prikazu previewa:\n" + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  SPREMI
        // ══════════════════════════════════════════════════════════════════

        private void BtnSpremi_Click(object sender, EventArgs e)
        {
            if (!Validiraj()) return;
            try
            {
                Otpremnica o = KreirajOtpremnicuIzFormi();
                if (_editMode)
                {
                    o.Id = _otpremnica.Id;
                    OtpremnicaRepository.AzurirajOtpremnicu(o);
                    SpravljeniId = o.Id;
                }
                else
                {
                    SpravljeniId = OtpremnicaRepository.DodajOtpremnicu(o);
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

        private bool Validiraj()
        {
            if (string.IsNullOrWhiteSpace(txtBrojOtpremnice.Text))
            { PrikaziGresku(txtBrojOtpremnice, "Broj otpremnice je obavezan!"); return false; }

            if (OtpremnicaRepository.BrojOtpremnicaPostoji(
                txtBrojOtpremnice.Text.Trim(), _editMode ? _otpremnica.Id : 0))
            { PrikaziGresku(txtBrojOtpremnice, "Otpremnica s ovim brojem već postoji!"); return false; }

            if (_odabraniKupac == null || _odabraniKupac.Id == 0)
            {
                MessageBox.Show("Odaberite kupca!", "Validacija",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnOdaberiKupca.Focus(); return false;
            }

            if (_stavke.Count == 0)
            {
                MessageBox.Show("Otpremnica mora imati barem jednu stavku!",
                    "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private Otpremnica KreirajOtpremnicuIzFormi()
        {
            int? prodavacId = (cmbProdavac.SelectedItem is Prodavac pr && pr.Id > 0) ? pr.Id : (int?)null;
            var o = new Otpremnica
            {
                BrojOtpremnice = txtBrojOtpremnice.Text.Trim(),
                DatumOtpremnice = dtpDatumOtpremnice.Value.Date,
                DatumIsporuke = dtpDatumIsporuke.Value.Date,
                KupacId = _odabraniKupac.Id,
                NazivKupca = _odabraniKupac.Naziv,
                AdresaKupca = _odabraniKupac.Adresa,
                OibKupca = _odabraniKupac.OIB,
                ProdavacId = prodavacId,
                Napomena = txtNapomena.Text.Trim(),
                Status = "KREIRANA",
                Stavke = _stavke
            };
            o.UkupnoVrijednost = o.IzracunajUkupno();
            return o;
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPER BUILDERS (isti pattern kao frmRacun)
        // ══════════════════════════════════════════════════════════════════

        private Guna2Panel KreirajCard(Control parent, int x, int y, int width, int height)
        {
            var card = new Guna2Panel { Location = new Point(x, y), FillColor = AppColors.CardBackground, BorderRadius = 10 };
            card.ShadowDecoration.Enabled = true; card.ShadowDecoration.Depth = 6;
            if (width > 0) card.Width = width;
            if (height > 0) card.Height = height;
            parent.Controls.Add(card);
            return card;
        }
        private void DodajLabel(Control p, string t, int x, int y)
        {
            p.Controls.Add(new Label { Text = t, Font = AppFonts.Regular, ForeColor = AppColors.TextSecondary, Location = new Point(x, y), AutoSize = true });
        }
        private void DodajMiniLabel(Control p, string t, int x, int y)
        {
            p.Controls.Add(new Label { Text = t, Font = AppFonts.Regular, ForeColor = AppColors.TextSecondary, Location = new Point(x, y), AutoSize = true });
        }
        private void DodajSekcijuNaslov(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label { Text = text.ToUpper(), Font = AppFonts.RegularMedium, ForeColor = AppColors.Primary, Location = new Point(x, y), AutoSize = true });
            var linija = new Panel { Height = 1, Location = new Point(x, y + 18), BackColor = AppColors.BorderLight, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            parent.Controls.Add(linija);
            parent.SizeChanged += (s, e) => linija.Width = parent.Width - x - 15;
            linija.Width = parent.Width > 0 ? parent.Width - x - 15 : 540;
        }
        private Guna2TextBox DodajTextBox(Control p, int x, int y, int w)
        {
            var txt = new Guna2TextBox { Size = new Size(w, 34), Location = new Point(x, y), FillColor = AppColors.Background, BorderColor = AppColors.BorderLight, Font = AppFonts.Regular, ForeColor = AppColors.TextPrimary, BorderRadius = 8 };
            txt.Enter += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.Primary;
            txt.Leave += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.BorderLight;
            p.Controls.Add(txt); return txt;
        }
        private Guna2TextBox DodajMiniTextBox(Control p, int x, int y, int w)
        {
            var txt = new Guna2TextBox { Size = new Size(w, 30), Location = new Point(x, y), FillColor = AppColors.Background, BorderColor = AppColors.BorderLight, Font = AppFonts.Regular, ForeColor = AppColors.TextPrimary, BorderRadius = 6 };
            txt.Enter += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.Primary;
            txt.Leave += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.BorderLight;
            p.Controls.Add(txt); return txt;
        }
        private Guna2ComboBox DodajComboBox(Control p, int x, int y, int w)
        {
            var cmb = new Guna2ComboBox { Size = new Size(w, 34), Location = new Point(x, y), FillColor = AppColors.Background, BorderColor = AppColors.BorderLight, FocusedColor = AppColors.Primary, Font = AppFonts.Regular, ForeColor = AppColors.TextPrimary, BorderRadius = 8, DropDownStyle = ComboBoxStyle.DropDownList };
            p.Controls.Add(cmb); return cmb;
        }
        private void PrikaziGresku(Control ctrl, string poruka)
        {
            ctrl.Focus();
            MessageBox.Show(poruka, "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ── Extension za čitljiviji init ─────────────────────────────────────
    internal static class ControlExtensions
    {
        public static T Also<T>(this T self, Action<T> action) { action(self); return self; }
    }
}