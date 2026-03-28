using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Data.Models;
using Trgovina.Utils;

namespace Trgovina.Forms
{
    public partial class frmKalkulacija : Form
    {
        // ── State ─────────────────────────────────────────────────────────────
        private readonly Kalkulacija _kalkulacija;
        private readonly bool _editMode;
        private List<KalkulacijaStavka> _stavke = new List<KalkulacijaStavka>();

        private Partner _odabraniDobavljac = null;
        private Artikl _trenutniArtikl = null;

        // ── UI — zaglavlje ────────────────────────────────────────────────────
        private Guna2TextBox txtBrojKalkulacije, txtBrojDobRacuna, txtNapomena;
        private DateTimePicker dtpDatumKalkulacije;
        private Guna2TextBox txtDobavljacPrikaz;
        private Guna2Button btnOdaberiDobavljaca;

        // ── UI — stavke ───────────────────────────────────────────────────────
        private Guna2TextBox txtArtiklPrikaz;
        private Guna2Button btnOdaberiArtikl;
        private Guna2TextBox txtKolicina, txtNabavnaCijena, txtPdvStopa;
        private Guna2Button btnDodajStavku, btnUkloniStavku;
        private Guna2DataGridView dgvStavke;

        // ── UI — totali ───────────────────────────────────────────────────────
        private Label lblBezPdv, lblPdvIznos, lblSaPdv;

        // ── UI — akcijski gumbi ───────────────────────────────────────────────
        private Guna2Button btnSpremi, btnOdustani, btnProknjiži;

        // ═════════════════════════════════════════════════════════════════════
        //  KONSTRUKTOR
        // ═════════════════════════════════════════════════════════════════════

        public frmKalkulacija(Kalkulacija kalkulacija = null)
        {
            _kalkulacija = kalkulacija;
            _editMode = kalkulacija != null;

            if (_editMode && kalkulacija.Stavke != null)
                _stavke = new List<KalkulacijaStavka>(kalkulacija.Stavke);

            InitializeForm();

            if (_editMode)
                PopuniPolja();
            else
            {
                txtBrojKalkulacije.Text = KalkulacijeRepository.GeneriraBrojKalkulacije();
                dtpDatumKalkulacije.Value = DateTime.Today;
            }

            PopuniGridStavke();
            IzracunajTotale();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  INIT
        // ═════════════════════════════════════════════════════════════════════

        private void InitializeForm()
        {
            this.Text = _editMode
                ? $"Uredi kalkulaciju — {_kalkulacija.BrojKalkulacije}"
                : "Nova kalkulacija";
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(920, 680);
            this.Size = new Size(1060, 760);
            this.MaximizeBox = true;

            KreirajSadrzaj();
        }

        private void KreirajSadrzaj()
        {
            // ── Naslov ────────────────────────────────────────────────────────
            var lblNaslov = new Label
            {
                Text = _editMode ? "✏️  Uredi kalkulaciju" : "📋  Nova kalkulacija",
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

            // ── Dno — gumbi (dodaj PRIJE Fill panela) ────────────────────────
            var pnlDno = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = AppColors.Background };
            this.Controls.Add(pnlDno);

            var sepDno = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = AppColors.BorderLight };
            pnlDno.Controls.Add(sepDno);

            btnSpremi = new Guna2Button
            {
                Text = _editMode ? "💾  Spremi izmjene" : "✔  Kreiraj kalkulaciju",
                Size = new Size(190, 38),
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
                Location = new Point(213, 9),
                FillColor = Color.FromArgb(210, 210, 215),
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextPrimary,
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };
            btnOdustani.HoverState.FillColor = Color.FromArgb(190, 190, 195);
            btnOdustani.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlDno.Controls.Add(btnOdustani);

            // Proknjizi gumb — vidljiv samo u edit modu i samo ako nije proknjizeno
            btnProknjiži = new Guna2Button
            {
                Text = "📦  Proknjizi zalihe",
                Size = new Size(170, 38),
                Location = new Point(341, 9),
                FillColor = AppColors.Success,
                Font = AppFonts.Regular,
                ForeColor = Color.White,
                BorderRadius = 8,
                Cursor = Cursors.Hand,
                Visible = _editMode && !(_kalkulacija?.Proknjizeno ?? false)
            };
            btnProknjiži.HoverState.FillColor = ControlPaint.Light(AppColors.Success, 0.15f);
            btnProknjiži.Click += BtnProknjiži_Click;
            pnlDno.Controls.Add(btnProknjiži);

            // Proknjizeno badge (samo read)
            if (_editMode && (_kalkulacija?.Proknjizeno ?? false))
            {
                var lblProk = new Label
                {
                    Text = "✅  PROKNJIZENO — " + (_kalkulacija.DatumKnjizenja?.ToString("dd.MM.yyyy HH:mm") ?? ""),
                    Font = AppFonts.Regular,
                    ForeColor = AppColors.Success,
                    Location = new Point(341, 18),
                    AutoSize = true
                };
                pnlDno.Controls.Add(lblProk);
            }

            // ── Scroll panel ──────────────────────────────────────────────────
            var pnlScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = AppColors.Background
            };
            this.Controls.Add(pnlScroll);

            // ── Card: Zaglavlje ───────────────────────────────────────────────
            const int zaglavljeH = 200;
            var cardZaglavlje = KreirajCard(pnlScroll, 15, 48, 0, zaglavljeH);
            cardZaglavlje.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.SizeChanged += (s, e) => cardZaglavlje.Width = pnlScroll.ClientSize.Width - 30;
            cardZaglavlje.Width = this.ClientSize.Width - 30;
            KreirajZaglavljeCard(cardZaglavlje);

            // ── Card: Stavke ──────────────────────────────────────────────────
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
            cardStavke.Width = this.ClientSize.Width - 30;
            cardStavke.Height = this.ClientSize.Height - pnlDno.Height - stavkeTop - 10;

            KreirajStavkeCard(cardStavke);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  CARD: ZAGLAVLJE
        // ═════════════════════════════════════════════════════════════════════

        private void KreirajZaglavljeCard(Guna2Panel card)
        {
            int y = 15;
            int x1 = 15, x2 = 205, x3 = 395, x4 = 620;
            int inW = 175, inH = 34;

            DodajSekcijuNaslov(card, "Zaglavlje kalkulacije", x1, y); y += 26;

            // ── Red 1: Broj, Datum, Br. dobavljačevog računa ─────────────────
            DodajLabel(card, "Broj kalkulacije *", x1, y);
            DodajLabel(card, "Datum kalkulacije *", x2, y);
            DodajLabel(card, "Br. dobavljačevog računa", x3, y);
            y += 18;

            txtBrojKalkulacije = DodajTextBox(card, x1, y, inW);

            dtpDatumKalkulacije = new DateTimePicker
            {
                Size = new Size(inW, inH),
                Location = new Point(x2, y + 10),
                Format = DateTimePickerFormat.Short
            };
            card.Controls.Add(dtpDatumKalkulacije);

            txtBrojDobRacuna = DodajTextBox(card, x3, y, inW);
            txtBrojDobRacuna.PlaceholderText = "Npr. URA-2025-0001";

            y += inH + 14;

            // ── Red 2: Dobavljač + Napomena ───────────────────────────────────
            DodajLabel(card, "Dobavljač", x1, y + 5);
            DodajLabel(card, "Napomena", x4, y + 5);
            y += 18;

            txtDobavljacPrikaz = new Guna2TextBox
            {
                Size = new Size(inW + 170, inH),
                Location = new Point(x1, y),
                FillColor = Color.FromArgb(245, 246, 252),
                BorderColor = AppColors.BorderLight,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextPrimary,
                BorderRadius = 8,
                ReadOnly = true,
                PlaceholderText = "Kliknite za odabir dobavljača..."
            };
            card.Controls.Add(txtDobavljacPrikaz);

            btnOdaberiDobavljaca = new Guna2Button
            {
                Text = "🏭",
                Size = new Size(100, inH),
                Location = new Point(x1 + inW + 170 + 6, y + 20),
                FillColor = AppColors.Secondary,
                Font = AppFonts.Regular,
                ForeColor = Color.White,
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };
            btnOdaberiDobavljaca.HoverState.FillColor = AppColors.Primary;
            btnOdaberiDobavljaca.Click += BtnOdaberiDobavljaca_Click;
            card.Controls.Add(btnOdaberiDobavljaca);

            // Napomena — desno
            int napW = card.Width > 0 ? card.Width - x4 - 20 : 200;
            txtNapomena = DodajTextBox(card, x4, y, napW);
            txtNapomena.PlaceholderText = "Opcionalna napomena...";
            card.SizeChanged += (s, e) =>
                txtNapomena.Width = card.Width - x4 - 20;

            // Ako je proknjizeno — onemogući editiranje
            if (_editMode && (_kalkulacija?.Proknjizeno ?? false))
                ZakljucajFormu(card);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  CARD: STAVKE
        // ═════════════════════════════════════════════════════════════════════

        private void KreirajStavkeCard(Guna2Panel card)
        {
            int y = 15;
            DodajSekcijuNaslov(card, "Stavke kalkulacije", 15, y); y += 28;

            // ── Toolbar za unos ───────────────────────────────────────────────
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
            pnlUnos.Width = 1000;

            // Artikl
            txtArtiklPrikaz = new Guna2TextBox
            {
                PlaceholderText = "Odaberite artikl -> ...",
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
                Text = "📦",
                Size = new Size(82, 30),
                Location = new Point(242, 10),
                FillColor = AppColors.Secondary,
                Font = AppFonts.Regular,
                ForeColor = Color.White,
                BorderRadius = 6,
                Cursor = Cursors.Hand
            };
            btnOdaberiArtikl.HoverState.FillColor = AppColors.Primary;
            btnOdaberiArtikl.Click += BtnOdaberiArtikl_Click;
            pnlUnos.Controls.Add(btnOdaberiArtikl);

            // Kolicina
            DodajMiniLabel(pnlUnos, "Kol.:", 332, 14);
            txtKolicina = DodajMiniTextBox(pnlUnos, 370, 7, 64);
            txtKolicina.Text = "1";

            // Nabavna cijena (s PDV-om — onako kako piše na ulaznom računu)
            DodajMiniLabel(pnlUnos, "Cij. s PDV:", 445, 14);
            txtNabavnaCijena = DodajMiniTextBox(pnlUnos, 520, 7, 78);

            // PDV stopa
            DodajMiniLabel(pnlUnos, "PDV%:", 600, 14);
            txtPdvStopa = DodajMiniTextBox(pnlUnos, 650, 7, 50);
            txtPdvStopa.Text = "25";

            // Gumbi
            btnDodajStavku = new Guna2Button
            {
                Text = "➕ Dodaj",
                Size = new Size(102, 30),
                Location = new Point(720, 7),
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
                Location = new Point(840, 7),
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

            // ── Grid ─────────────────────────────────────────────────────────
            dgvStavke = new Guna2DataGridView
            {
                Location = new Point(15, y),
                BorderStyle = BorderStyle.None,
                BackgroundColor = AppColors.CardBackground,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(230, 232, 240),
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 30 },
                ColumnHeadersHeight = 34,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
                                          | AnchorStyles.Right | AnchorStyles.Bottom
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
                btnUkloniStavku.Enabled = dgvStavke.SelectedRows.Count > 0
                                       && !(_editMode && (_kalkulacija?.Proknjizeno ?? false));

            card.Controls.Add(dgvStavke);
            card.SizeChanged += (s, e) =>
            {
                dgvStavke.Width = card.Width - 30;
                dgvStavke.Height = card.Height - y - 108;
            };
            dgvStavke.Width = 1000;
            dgvStavke.Height = 200;

            KonfigurisiKolone();

            // ── Totali ────────────────────────────────────────────────────────
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

            // Zaključaj toolbar ako je proknjizeno
            if (_editMode && (_kalkulacija?.Proknjizeno ?? false))
            {
                pnlUnos.Enabled = false;
                btnSpremi.Enabled = false;
            }
        }

        private void KonfigurisiKolone()
        {
            dgvStavke.Columns.Clear();
            var kolone = new[]
            {
                new { Name="colRbr",    Header="Rbr",             Weight=30f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colSifra",  Header="Šifra",           Weight=62f,  Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colNaziv",  Header="Naziv artikla",   Weight=210f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colJM",     Header="J/M",             Weight=38f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colKol",    Header="Količina",        Weight=70f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colCijena", Header="Nab. cij. s PDV", Weight=95f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colPdv",    Header="PDV%",            Weight=50f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colBezPdv", Header="Bez PDV (€)",     Weight=88f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colSaPdv",  Header="S PDV (€)",       Weight=88f,  Align=DataGridViewContentAlignment.MiddleRight  },
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

        // ═════════════════════════════════════════════════════════════════════
        //  POPUNJAVANJE
        // ═════════════════════════════════════════════════════════════════════

        private void PopuniPolja()
        {
            if (_kalkulacija == null) return;
            txtBrojKalkulacije.Text = _kalkulacija.BrojKalkulacije;
            dtpDatumKalkulacije.Value = _kalkulacija.DatumKalkulacije;
            txtBrojDobRacuna.Text = _kalkulacija.BrojDobavljacevogRacuna ?? "";
            txtNapomena.Text = _kalkulacija.Napomena ?? "";

            if (_kalkulacija.DobavljacId.HasValue)
            {
                _odabraniDobavljac = new Partner
                {
                    Id = _kalkulacija.DobavljacId.Value,
                    Naziv = _kalkulacija.NazivDobavljaca
                };
                txtDobavljacPrikaz.Text = _kalkulacija.NazivDobavljaca;
            }
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
                    s.NazivJM,
                    s.Kolicina.ToString("N3"),
                    s.NabavnaCijena.ToString("N2"),
                    s.PdvStopa.ToString("0") + "%",
                    s.IznosBezPdv.ToString("N2"),
                    s.IznosSaPdv.ToString("N2")
                );
            }
            btnUkloniStavku.Enabled = false;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  EVENTI
        // ═════════════════════════════════════════════════════════════════════

        private void BtnOdaberiDobavljaca_Click(object sender, EventArgs e)
        {
            using (var popup = new frmOdabirPartnera())
            {
                if (popup.ShowDialog() == DialogResult.OK && popup.OdabraniPartner != null)
                {
                    _odabraniDobavljac = popup.OdabraniPartner;
                    txtDobavljacPrikaz.Text = _odabraniDobavljac.Naziv;
                    txtDobavljacPrikaz.BorderColor = AppColors.Success;
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
                    txtNabavnaCijena.Text = _trenutniArtikl.CijenaNabave.ToString("N2");
                    txtPdvStopa.Text = _trenutniArtikl.PdvStopa.ToString("0");
                    txtKolicina.Focus();
                    txtKolicina.SelectAll();
                }
            }
        }

        private void BtnDodajStavku_Click(object sender, EventArgs e)
        {
            if (_trenutniArtikl == null)
            { PrikaziWarning("Odaberite artikl!"); return; }

            if (!ParseDecimal(txtKolicina.Text, out decimal kolicina) || kolicina <= 0)
            { PrikaziGreskuPolja(txtKolicina, "Unesite ispravnu količinu!"); return; }

            if (!ParseDecimal(txtNabavnaCijena.Text, out decimal cijenasPdv) || cijenasPdv < 0)
            { PrikaziGreskuPolja(txtNabavnaCijena, "Unesite ispravnu nabavnu cijenu!"); return; }

            if (!ParseDecimal(txtPdvStopa.Text, out decimal pdvStopa) || pdvStopa < 0)
            { PrikaziGreskuPolja(txtPdvStopa, "Unesite ispravnu PDV stopu (npr. 25)!"); return; }

            // Preračun: cijena s PDV-om → bez PDV-a
            decimal jedBezPdv = pdvStopa > 0 ? cijenasPdv / (1 + pdvStopa / 100m) : cijenasPdv;
            decimal iznosBezPdv = Math.Round(kolicina * jedBezPdv, 2);
            decimal iznosSaPdv = Math.Round(kolicina * cijenasPdv, 2);
            decimal iznosPdv = Math.Round(iznosSaPdv - iznosBezPdv, 2);

            _stavke.Add(new KalkulacijaStavka
            {
                ArtiklId = _trenutniArtikl.Id,
                SifraArtikla = _trenutniArtikl.Sifra,
                NazivArtikla = _trenutniArtikl.Naziv,
                NazivJM = _trenutniArtikl.NazivJediniceMjere,
                Kolicina = kolicina,
                NabavnaCijena = cijenasPdv,
                PdvStopa = pdvStopa,
                IznosBezPdv = iznosBezPdv,
                IznosPdv = iznosPdv,
                IznosSaPdv = iznosSaPdv
            });

            PopuniGridStavke();
            IzracunajTotale();

            // Reset unosa
            _trenutniArtikl = null;
            txtArtiklPrikaz.Text = "";
            txtKolicina.Text = "1";
            txtNabavnaCijena.Text = "";
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
                IzracunajTotale();
            }
        }

        private void BtnProknjiži_Click(object sender, EventArgs e)
        {
            if (_kalkulacija == null) return;

            var res = MessageBox.Show(
                "Proknjižavanjem kalkulacije povećat će se zalihe svih artikala.\n\n" +
                "Proknjiženu kalkulaciju nije moguće mijenjati!\n\n" +
                "Jeste li sigurni?",
                "Proknjižavanje", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res != DialogResult.Yes) return;

            try
            {
                KalkulacijeRepository.ProknjiziKalkulaciju(_kalkulacija.Id);
                MessageBox.Show("Kalkulacija je uspješno proknjižena.\nZalihe su ažurirane.",
                    "Uspjeh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri proknjižavanju:\n" + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSpremi_Click(object sender, EventArgs e)
        {
            if (!Validiraj()) return;
            try
            {
                var k = KreirajKalkulacijuIzForme();
                if (_editMode)
                {
                    k.Id = _kalkulacija.Id;
                    KalkulacijeRepository.AzurirajKalkulaciju(k);
                }
                else
                {
                    KalkulacijeRepository.DodajKalkulaciju(k);
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

        // ═════════════════════════════════════════════════════════════════════
        //  VALIDACIJA + KREIRANJE OBJEKTA
        // ═════════════════════════════════════════════════════════════════════

        private bool Validiraj()
        {
            if (string.IsNullOrWhiteSpace(txtBrojKalkulacije.Text))
            { PrikaziGreskuPolja(txtBrojKalkulacije, "Broj kalkulacije je obavezan!"); return false; }

            if (KalkulacijeRepository.BrojKalkulacijePostoji(
                    txtBrojKalkulacije.Text.Trim(), _editMode ? _kalkulacija.Id : 0))
            { PrikaziGreskuPolja(txtBrojKalkulacije, "Kalkulacija s ovim brojem već postoji!"); return false; }

            if (_stavke.Count == 0)
            { PrikaziWarning("Kalkulacija mora imati barem jednu stavku!"); return false; }

            return true;
        }

        private Kalkulacija KreirajKalkulacijuIzForme()
        {
            return new Kalkulacija
            {
                BrojKalkulacije = txtBrojKalkulacije.Text.Trim(),
                DatumKalkulacije = dtpDatumKalkulacije.Value.Date,
                DobavljacId = _odabraniDobavljac?.Id,
                NazivDobavljaca = _odabraniDobavljac?.Naziv ?? "",
                BrojDobavljacevogRacuna = txtBrojDobRacuna.Text.Trim(),
                UkupnoBezPdv = _stavke.Sum(s => s.IznosBezPdv),
                UkupnoPdv = _stavke.Sum(s => s.IznosPdv),
                UkupnoSaPdv = _stavke.Sum(s => s.IznosSaPdv),
                Napomena = txtNapomena.Text.Trim(),
                Stavke = _stavke
            };
        }

        // ═════════════════════════════════════════════════════════════════════
        //  TOTALI
        // ═════════════════════════════════════════════════════════════════════

        private void IzracunajTotale()
        {
            decimal bezPdv = _stavke.Sum(s => s.IznosBezPdv);
            decimal pdv = _stavke.Sum(s => s.IznosPdv);
            decimal saPdv = _stavke.Sum(s => s.IznosSaPdv);
            lblBezPdv.Text = $"Ukupno bez PDV:   {bezPdv:N2} €";
            lblPdvIznos.Text = $"PDV:   {pdv:N2} €";
            lblSaPdv.Text = $"UKUPNO S PDV:   {saPdv:N2} €";
        }

        // ═════════════════════════════════════════════════════════════════════
        //  HELPER BUILDERS (isti stil kao frmRacun)
        // ═════════════════════════════════════════════════════════════════════

        private Guna2Panel KreirajCard(Control parent, int x, int y, int width, int height)
        {
            var card = new Guna2Panel
            {
                Location = new Point(x, y),
                FillColor = AppColors.CardBackground,
                BorderRadius = 10
            };
            card.ShadowDecoration.Enabled = true;
            card.ShadowDecoration.Depth = 6;
            if (width > 0) card.Width = width;
            if (height > 0) card.Height = height;
            parent.Controls.Add(card);
            return card;
        }

        private void DodajLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextSecondary,
                Location = new Point(x, y),
                AutoSize = true
            });
        }

        private void DodajMiniLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextSecondary,
                Location = new Point(x, y),
                AutoSize = true
            });
        }

        private void DodajSekcijuNaslov(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text.ToUpper(),
                Font = AppFonts.RegularMedium,
                ForeColor = AppColors.Primary,
                Location = new Point(x, y),
                AutoSize = true
            });
            var linija = new Panel
            {
                Height = 1,
                Location = new Point(x, y + 18),
                BackColor = AppColors.BorderLight,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            parent.Controls.Add(linija);
            parent.SizeChanged += (s, e) => linija.Width = parent.Width - x - 15;
            linija.Width = parent.Width > 0 ? parent.Width - x - 15 : 540;
        }

        private Guna2TextBox DodajTextBox(Control parent, int x, int y, int width)
        {
            var txt = new Guna2TextBox
            {
                Size = new Size(width, 34),
                Location = new Point(x, y),
                FillColor = AppColors.Background,
                BorderColor = AppColors.BorderLight,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextPrimary,
                BorderRadius = 8
            };
            txt.Enter += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.Primary;
            txt.Leave += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.BorderLight;
            parent.Controls.Add(txt);
            return txt;
        }

        private Guna2TextBox DodajMiniTextBox(Control parent, int x, int y, int width)
        {
            var txt = new Guna2TextBox
            {
                Size = new Size(width, 30),
                Location = new Point(x, y),
                FillColor = AppColors.Background,
                BorderColor = AppColors.BorderLight,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextPrimary,
                BorderRadius = 6
            };
            txt.Enter += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.Primary;
            txt.Leave += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.BorderLight;
            parent.Controls.Add(txt);
            return txt;
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

        private void ZakljucajFormu(Control parent)
        {
            // Onemogući sve input kontrole u roditeljskom panelu
            foreach (Control c in parent.Controls)
            {
                if (c is Guna2TextBox || c is DateTimePicker || c is Guna2ComboBox)
                    c.Enabled = false;
                if (c is Guna2Button btn && btn != btnOdustani)
                    btn.Enabled = false;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  UTILITY
        // ═════════════════════════════════════════════════════════════════════

        private static bool ParseDecimal(string text, out decimal value)
        {
            return decimal.TryParse(
                text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);
        }

        private void PrikaziWarning(string msg)
            => MessageBox.Show(msg, "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void PrikaziGreskuPolja(Control ctrl, string msg)
        {
            ctrl.Focus();
            MessageBox.Show(msg, "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}