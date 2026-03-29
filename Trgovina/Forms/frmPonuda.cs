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
    public partial class frmPonuda : Form
    {
        // ─── State ────────────────────────────────────────────────────────
        private readonly Ponuda _ponuda;
        private readonly bool _editMode;
        private List<PonudaStavka> _stavke = new List<PonudaStavka>();
        private List<Prodavac> _prodavaci = new List<Prodavac>();

        private Partner _odabraniKupac = null;

        // ─── UI: zaglavlje ────────────────────────────────────────────────
        private Guna2TextBox txtBrojPonude, txtNapomena, txtUvjetiPlacanja, txtRokIsporuke;
        private DateTimePicker dtpDatumPonude, dtpDatumVazenja;
        private Guna2ComboBox cmbProdavac;
        private Guna2TextBox txtKupacPrikaz;
        private Guna2Button btnOdaberiKupca;

        // ─── UI: stavke ───────────────────────────────────────────────────
        private Guna2TextBox txtArtiklPrikaz;
        private Guna2Button btnOdaberiArtikl;
        private Guna2TextBox txtKolicina, txtCijena, txtPopust;
        private Guna2Button btnDodajStavku, btnUkloniStavku;
        private Artikl _trenutniArtikl = null;

        private Guna2DataGridView dgvStavke;
        private Label lblBezPdv, lblPdvIznos, lblSaPdv;
        private Guna2Button btnSpremi, btnOdustani;

        public int SpravljeniId { get; private set; }

        public frmPonuda(Ponuda ponuda = null)
        {
            _ponuda = ponuda;
            _editMode = ponuda != null;
            if (_editMode && ponuda.Stavke != null)
                _stavke = new List<PonudaStavka>(ponuda.Stavke);

            UcitajSifarnike();
            InitializeForm();
            UcitajProdavaceUCombo();

            if (_editMode)
                PopuniPolja();
            else
            {
                txtBrojPonude.Text = PonudaRepository.GeneriraBrojPonude();
                dtpDatumPonude.Value = DateTime.Today;
                dtpDatumVazenja.Value = DateTime.Today.AddDays(30);
            }

            PopuniGridStavke();
            IzracunajTotale();
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
            this.Text = _editMode ? $"Uredi ponudu — {_ponuda.BrojPonude}" : "Nova ponuda";
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(940, 700);
            this.Size = new Size(1080, 780);
            this.MaximizeBox = true;
            KreirajSadrzaj();
        }

        private void KreirajSadrzaj()
        {
            var lblNaslov = new Label
            {
                Text = _editMode ? "✏️  Uredi ponudu" : "📋  Nova ponuda",
                Font = AppFonts.TitleSmall,
                ForeColor = AppColors.TextPrimary,
                Location = new Point(15, 12),
                AutoSize = true
            };
            this.Controls.Add(lblNaslov);

            var sep = new Panel { Height = 1, Left = 15, Top = 38, BackColor = AppColors.BorderLight };
            sep.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(sep);
            this.SizeChanged += (s, e) => sep.Width = this.ClientSize.Width - 30;

            // ── Dno ───────────────────────────────────────────────────────
            var pnlDno = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = AppColors.Background };
            this.Controls.Add(pnlDno);
            pnlDno.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = AppColors.BorderLight });

            btnSpremi = new Guna2Button
            {
                Text = _editMode ? "💾  Spremi izmjene" : "✔  Kreiraj ponudu",
                Size = new Size(185, 38),
                Location = new Point(15, 9),
                FillColor = AppColors.Primary,
                Font = AppFonts.RegularMedium,
                ForeColor = AppColors.TextWhite,
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };
            btnSpremi.HoverState.FillColor = AppColors.PrimaryLight;
            btnSpremi.Click += BtnSpremi_Click;
            pnlDno.Controls.Add(btnSpremi);

            btnOdustani = new Guna2Button
            {
                Text = "✖  Odustani",
                Size = new Size(120, 38),
                Location = new Point(208, 9),
                FillColor = Color.FromArgb(210, 210, 215),
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextPrimary,
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };
            btnOdustani.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlDno.Controls.Add(btnOdustani);

            var btnPreview = new Guna2Button
            {
                Text = "👁  Preview",
                Size = new Size(120, 38),
                Location = new Point(336, 9),
                FillColor = Color.FromArgb(142, 68, 173),
                Font = AppFonts.Regular,
                ForeColor = Color.White,
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };
            btnPreview.HoverState.FillColor = ControlPaint.Light(Color.FromArgb(142, 68, 173), 0.15f);
            btnPreview.Click += BtnPreview_Click;
            pnlDno.Controls.Add(btnPreview);

            // ── Scroll panel ──────────────────────────────────────────────
            var pnlScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppColors.Background };
            this.Controls.Add(pnlScroll);

            const int zaglavljeH = 265;
            var cardZaglavlje = KreirajCard(pnlScroll, 15, 48, 0, zaglavljeH);
            cardZaglavlje.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            int stavkeTop = 48 + zaglavljeH + 10;
            var cardStavke = KreirajCard(pnlScroll, 15, stavkeTop, 0, 0);
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
        //  CARD: ZAGLAVLJE  (2 reda + uvjeti/rok)
        // ══════════════════════════════════════════════════════════════════

        private void KreirajZaglavljeCard(Guna2Panel card)
        {
            int y = 15;
            int x1 = 15, x2 = 205, x3 = 395, x4 = 585, x5 = 775;
            int inW = 175, inH = 34;

            DodajSekcijuNaslov(card, "Zaglavlje ponude", x1, y); y += 26;

            // Red 1
            DodajLabel(card, "Broj ponude *", x1, y);
            DodajLabel(card, "Datum ponude *", x2, y);
            DodajLabel(card, "Vrijedi do", x3, y);
            DodajLabel(card, "Prodavač", x4, y);
            y += 18;

            txtBrojPonude = DodajTextBox(card, x1, y, inW);

            dtpDatumPonude = new DateTimePicker { Size = new Size(inW, inH), Location = new Point(x2, y + 10), Format = DateTimePickerFormat.Short };
            card.Controls.Add(dtpDatumPonude);

            dtpDatumVazenja = new DateTimePicker { Size = new Size(inW, inH), Location = new Point(x3, y + 10), Format = DateTimePickerFormat.Short };
            card.Controls.Add(dtpDatumVazenja);

            cmbProdavac = DodajComboBox(card, x4, y + 5, inW);
            y += inH + 14;

            // Red 2 — kupac + napomena
            DodajLabel(card, "Kupac *", x1, y + 10);
            DodajLabel(card, "Napomena", x4, y + 10);
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
            y += inH + 14;

            // Red 3 — uvjeti plaćanja + rok isporuke
            DodajLabel(card, "Uvjeti plaćanja", x1, y + 15);
            DodajLabel(card, "Rok isporuke", x3, y + 15);
            y += 18;

            txtUvjetiPlacanja = DodajTextBox(card, x1, y, inW + 185);
            txtUvjetiPlacanja.PlaceholderText = "npr. 30 dana od isporuke...";

            txtRokIsporuke = DodajTextBox(card, x3, y, inW + 195);
            txtRokIsporuke.PlaceholderText = "npr. 7 radnih dana...";
        }

        // ══════════════════════════════════════════════════════════════════
        //  CARD: STAVKE  (isti pattern kao frmRacun, s popustom)
        // ══════════════════════════════════════════════════════════════════

        private void KreirajStavkeCard(Guna2Panel card)
        {
            int y = 15;
            DodajSekcijuNaslov(card, "Stavke ponude", 15, y); y += 28;

            // ── Toolbar unosa ─────────────────────────────────────────────
            var pnlUnos = new Guna2Panel
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
                Size = new Size(230, 30),
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
                Location = new Point(242, 7),
                FillColor = AppColors.Secondary,
                Font = AppFonts.Regular,
                ForeColor = Color.White,
                BorderRadius = 6,
                Cursor = Cursors.Hand
            };
            btnOdaberiArtikl.HoverState.FillColor = AppColors.Primary;
            btnOdaberiArtikl.Click += BtnOdaberiArtikl_Click;
            pnlUnos.Controls.Add(btnOdaberiArtikl);

            DodajMiniLabel(pnlUnos, "Kol.:", 338, 14);
            txtKolicina = DodajMiniTextBox(pnlUnos, 376, 7, 64);
            txtKolicina.Text = "1";

            DodajMiniLabel(pnlUnos, "Cijena:", 448, 14);
            txtCijena = DodajMiniTextBox(pnlUnos, 498, 7, 84);

            DodajMiniLabel(pnlUnos, "Pop%:", 590, 14);
            txtPopust = DodajMiniTextBox(pnlUnos, 635, 7, 56);
            txtPopust.Text = "0";

            btnDodajStavku = new Guna2Button
            {
                Text = "➕ Dodaj",
                Size = new Size(102, 30),
                Location = new Point(700, 7),
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
                Text = "🗑 Ukloni",
                Size = new Size(102, 30),
                Location = new Point(806, 7),
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
                dgvStavke.Height = card.Height - y - 108;
            };
            dgvStavke.Width = 990; dgvStavke.Height = 200;

            KonfigurisiKolone();

            // ── Totali ────────────────────────────────────────────────────
            var pnlTotali = new Panel
            {
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new Size(380, 56)
            };
            card.Controls.Add(pnlTotali);
            card.SizeChanged += (s, e) =>
                pnlTotali.Location = new Point(card.Width - 395, card.Height - 108);
            pnlTotali.Location = new Point(625, 330);

            lblBezPdv = KreirajTotalLabel(pnlTotali, "Ukupno bez PDV:", 0);
            lblPdvIznos = KreirajTotalLabel(pnlTotali, "PDV:", 20);
            lblSaPdv = KreirajTotalLabel(pnlTotali, "UKUPNO S PDV:", 40);
            lblSaPdv.Font = AppFonts.RegularMedium;
            lblSaPdv.ForeColor = AppColors.Primary;
        }

        private Label KreirajTotalLabel(Panel parent, string prefiks, int y)
        {
            var lbl = new Label
            {
                Text = $"{prefiks}  0,00 €",
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextPrimary,
                AutoSize = false,
                Size = new Size(380, 18),
                Location = new Point(0, y),
                TextAlign = ContentAlignment.MiddleRight
            };
            parent.Controls.Add(lbl);
            return lbl;
        }

        private void KonfigurisiKolone()
        {
            dgvStavke.Columns.Clear();
            var kolone = new[]
            {
                new { Name="colRbr",    Header="Rbr",           Weight=30f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colSifra",  Header="Šifra",         Weight=60f,  Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colNaziv",  Header="Naziv artikla", Weight=200f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colJM",     Header="J/M",           Weight=38f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colKol",    Header="Količina",      Weight=68f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colCijena", Header="Cijena (€)",    Weight=78f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colPopust", Header="Pop.%",         Weight=50f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colPdv",    Header="PDV%",          Weight=50f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colBezPdv", Header="Bez PDV (€)",   Weight=82f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colSaPdv",  Header="S PDV (€)",     Weight=82f,  Align=DataGridViewContentAlignment.MiddleRight  },
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
            if (_ponuda == null) return;
            txtBrojPonude.Text = _ponuda.BrojPonude;
            dtpDatumPonude.Value = _ponuda.DatumPonude;
            if (_ponuda.DatumVazenja.HasValue)
                dtpDatumVazenja.Value = _ponuda.DatumVazenja.Value;
            txtNapomena.Text = _ponuda.Napomena ?? "";
            txtUvjetiPlacanja.Text = _ponuda.UvjetiPlacanja ?? "";
            txtRokIsporuke.Text = _ponuda.RokIsporuke ?? "";

            _odabraniKupac = new Partner { Id = _ponuda.KupacId, Naziv = _ponuda.NazivKupca };
            txtKupacPrikaz.Text = _ponuda.NazivKupca;

            foreach (var item in cmbProdavac.Items)
                if (item is Prodavac pr && pr.Id == _ponuda.ProdavacId)
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
                    s.Rbr, s.SifraArtikla, s.NazivArtikla, s.NazivJediniceMjere,
                    s.Kolicina.ToString("N3"),
                    s.CijenaBezPdv.ToString("N2"),
                    s.Popust > 0 ? s.Popust.ToString("N1") + "%" : "—",
                    s.PdvStopa.ToString("0") + "%",
                    s.IznosBezPdv.ToString("N2"),
                    s.IznosSaPdv.ToString("N2")
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
                if (popup.ShowDialog() == DialogResult.OK && popup.OdabraniPartner != null)
                {
                    _odabraniKupac = popup.OdabraniPartner;
                    txtKupacPrikaz.Text = _odabraniKupac.Naziv;
                    txtKupacPrikaz.BorderColor = AppColors.Success;
                }
        }

        private void BtnOdaberiArtikl_Click(object sender, EventArgs e)
        {
            using (var popup = new frmOdabirArtikla())
                if (popup.ShowDialog() == DialogResult.OK && popup.OdabraniArtikl != null)
                {
                    _trenutniArtikl = popup.OdabraniArtikl;
                    txtArtiklPrikaz.Text = $"{_trenutniArtikl.Sifra}  —  {_trenutniArtikl.Naziv}";
                    decimal cijenaBezPdv = _trenutniArtikl.PdvStopa > 0
                        ? _trenutniArtikl.CijenaProdaje / (1 + _trenutniArtikl.PdvStopa / 100m)
                        : _trenutniArtikl.CijenaProdaje;
                    txtCijena.Text = cijenaBezPdv.ToString("N2");
                    txtKolicina.Focus(); txtKolicina.SelectAll();
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

            if (!decimal.TryParse(txtCijena.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal cijenaBezPdv) || cijenaBezPdv < 0)
            { PrikaziGresku(txtCijena, "Unesite ispravnu cijenu!"); return; }

            decimal.TryParse(txtPopust.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal popust);

            decimal cijenaNakonPopusta = cijenaBezPdv * (1 - popust / 100m);
            decimal iznosBezPdv = Math.Round(kolicina * cijenaNakonPopusta, 2);
            decimal iznosSaPdv = Math.Round(iznosBezPdv * (1 + _trenutniArtikl.PdvStopa / 100m), 2);
            decimal iznosPdv = Math.Round(iznosSaPdv - iznosBezPdv, 2);

            _stavke.Add(new PonudaStavka
            {
                ArtiklId = _trenutniArtikl.Id,
                SifraArtikla = _trenutniArtikl.Sifra,
                NazivArtikla = _trenutniArtikl.Naziv,
                NazivJediniceMjere = _trenutniArtikl.NazivJediniceMjere,
                Kolicina = kolicina,
                CijenaBezPdv = cijenaBezPdv,
                Popust = popust,
                PdvStopa = _trenutniArtikl.PdvStopa,
                IznosBezPdv = iznosBezPdv,
                IznosPdv = iznosPdv,
                IznosSaPdv = iznosSaPdv
            });

            PopuniGridStavke();
            IzracunajTotale();
            _trenutniArtikl = null;
            txtArtiklPrikaz.Text = ""; txtKolicina.Text = "1"; txtCijena.Text = ""; txtPopust.Text = "0";
            btnOdaberiArtikl.Focus();
        }

        private void BtnUkloniStavku_Click(object sender, EventArgs e)
        {
            if (dgvStavke.SelectedRows.Count == 0) return;
            int idx = dgvStavke.SelectedRows[0].Index;
            if (idx >= 0 && idx < _stavke.Count)
            { _stavke.RemoveAt(idx); PopuniGridStavke(); IzracunajTotale(); }
        }

        private void IzracunajTotale()
        {
            decimal bezPdv = _stavke.Sum(s => s.IznosBezPdv);
            decimal pdv = _stavke.Sum(s => s.IznosPdv);
            decimal saPdv = _stavke.Sum(s => s.IznosSaPdv);
            lblBezPdv.Text = $"Ukupno bez PDV:   {bezPdv:N2} €";
            lblPdvIznos.Text = $"PDV:   {pdv:N2} €";
            lblSaPdv.Text = $"UKUPNO S PDV:   {saPdv:N2} €";
        }

        // ══════════════════════════════════════════════════════════════════
        //  PREVIEW / SPREMI
        // ══════════════════════════════════════════════════════════════════

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            if (_stavke.Count == 0)
            { MessageBox.Show("Dodajte barem jednu stavku.", "Preview", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var privremena = KreirajPonuduIzFormi();
            try
            {
                string pdfPath = PonudaPdfHelper.GenerirajPdfTemp(privremena);
                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            }
            catch (Exception ex) { MessageBox.Show("Greška:\n" + ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnSpremi_Click(object sender, EventArgs e)
        {
            if (!Validiraj()) return;
            try
            {
                var p = KreirajPonuduIzFormi();
                if (_editMode) { p.Id = _ponuda.Id; PonudaRepository.AzurirajPonudu(p); SpravljeniId = p.Id; }
                else { SpravljeniId = PonudaRepository.DodajPonudu(p); }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { MessageBox.Show("Greška pri spremanju:\n" + ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private bool Validiraj()
        {
            if (string.IsNullOrWhiteSpace(txtBrojPonude.Text))
            { PrikaziGresku(txtBrojPonude, "Broj ponude je obavezan!"); return false; }
            if (PonudaRepository.BrojPonudePostoji(txtBrojPonude.Text.Trim(), _editMode ? _ponuda.Id : 0))
            { PrikaziGresku(txtBrojPonude, "Ponuda s ovim brojem već postoji!"); return false; }
            if (_odabraniKupac == null || _odabraniKupac.Id == 0)
            { MessageBox.Show("Odaberite kupca!", "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            if (_stavke.Count == 0)
            { MessageBox.Show("Ponuda mora imati barem jednu stavku!", "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            return true;
        }

        private Ponuda KreirajPonuduIzFormi()
        {
            int? prodavacId = (cmbProdavac.SelectedItem is Prodavac pr && pr.Id > 0) ? pr.Id : (int?)null;
            var p = new Ponuda
            {
                BrojPonude = txtBrojPonude.Text.Trim(),
                DatumPonude = dtpDatumPonude.Value.Date,
                DatumVazenja = dtpDatumVazenja.Value.Date,
                KupacId = _odabraniKupac.Id,
                NazivKupca = _odabraniKupac.Naziv,
                AdresaKupca = _odabraniKupac.Adresa,
                OibKupca = _odabraniKupac.OIB,
                ProdavacId = prodavacId,
                Napomena = txtNapomena.Text.Trim(),
                UvjetiPlacanja = txtUvjetiPlacanja.Text.Trim(),
                RokIsporuke = txtRokIsporuke.Text.Trim(),
                Status = _editMode ? _ponuda.Status : "KREIRANA",
                Stavke = _stavke
            };
            p.IzracunajTotale();
            return p;
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPER BUILDERS
        // ══════════════════════════════════════════════════════════════════

        private Guna2Panel KreirajCard(Control parent, int x, int y, int w, int h)
        {
            var card = new Guna2Panel { Location = new Point(x, y), FillColor = AppColors.CardBackground, BorderRadius = 10 };
            card.ShadowDecoration.Enabled = true; card.ShadowDecoration.Depth = 6;
            if (w > 0) card.Width = w;
            if (h > 0) card.Height = h;
            parent.Controls.Add(card); return card;
        }
        private void DodajLabel(Control p, string t, int x, int y) =>
            p.Controls.Add(new Label { Text = t, Font = AppFonts.Regular, ForeColor = AppColors.TextSecondary, Location = new Point(x, y), AutoSize = true });
        private void DodajMiniLabel(Control p, string t, int x, int y) =>
            p.Controls.Add(new Label { Text = t, Font = AppFonts.Regular, ForeColor = AppColors.TextSecondary, Location = new Point(x, y), AutoSize = true });
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
        private void PrikaziGresku(Control ctrl, string poruka) { ctrl.Focus(); MessageBox.Show(poruka, "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
}