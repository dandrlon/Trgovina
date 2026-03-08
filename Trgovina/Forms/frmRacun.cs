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
    public partial class frmRacun : Form
    {
        // ─── State ────────────────────────────────────────────────────────────
        private readonly Racun _racun;
        private readonly bool _editMode;
        private List<RacunStavka> _stavke = new List<RacunStavka>();
        private List<Prodavac> _prodavaci = new List<Prodavac>();

        // Odabrani kupac (iz popup-a)
        private Partner _odabraniKupac = null;

        // ─── UI ───────────────────────────────────────────────────────────────
        private Guna2TextBox txtBrojRacuna, txtNapomena;
        private DateTimePicker dtpDatumRacuna, dtpDatumValute;
        private Guna2ComboBox cmbProdavac;
        private Guna2ToggleSwitch tglPlaceno;

        // Kupac picker
        private Guna2TextBox txtKupacPrikaz;
        private Guna2Button btnOdaberiKupca;

        // Stavke — artikl picker
        private Guna2TextBox txtArtiklPrikaz;
        private Guna2Button btnOdaberiArtikl;
        private Guna2TextBox txtKolicina, txtCijena, txtPopust;
        private Guna2Button btnDodajStavku, btnUkloniStavku;
        private Artikl _trenutniArtikl = null;

        // Stavke grid
        private Guna2DataGridView dgvStavke;

        // Totali
        private Label lblBezPdv, lblPdvIznos, lblSaPdv;

        private Guna2Button btnSpremi, btnOdustani;

        public frmRacun(Racun racun = null)
        {
            _racun = racun;
            _editMode = racun != null;
            if (_editMode && racun.Stavke != null)
                _stavke = new List<RacunStavka>(racun.Stavke);

            UcitajSifarnike();
            InitializeForm();
            UcitajProdavaceUCombo();

            if (_editMode)
                PopuniPolja();
            else
            {
                txtBrojRacuna.Text = RacuniRepository.GeneriraBrojRacuna();
                dtpDatumRacuna.Value = DateTime.Today;
                dtpDatumValute.Value = DateTime.Today.AddDays(30);
            }

            PopuniGridStavke();
            IzracunajTotale();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  INIT
        // ══════════════════════════════════════════════════════════════════════

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
            this.Text = _editMode ? $"Uredi račun — {_racun.BrojRacuna}" : "Novi račun";
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(900, 680);
            this.Size = new Size(1050, 760);
            this.MaximizeBox = true;

            KreirajSadrzaj();
        }

        private void KreirajSadrzaj()
        {
            // Naslov
            Label lblNaslov = new Label();
            lblNaslov.Text = _editMode ? "✏️  Uredi račun" : "🧾  Novi račun";
            lblNaslov.Font = AppFonts.TitleSmall;
            lblNaslov.ForeColor = AppColors.TextPrimary;
            lblNaslov.Location = new Point(15, 12);
            lblNaslov.AutoSize = true;
            this.Controls.Add(lblNaslov);

            Panel sep = new Panel();
            sep.Height = 1; sep.Left = 15; sep.Top = 38;
            sep.BackColor = AppColors.BorderLight;
            sep.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(sep);
            this.SizeChanged += (s, e) => sep.Width = this.ClientSize.Width - 30;
            sep.Width = 1020;

            // Dno — gumbi
            Panel pnlDno = new Panel();
            pnlDno.Dock = DockStyle.Bottom;
            pnlDno.Height = 56;
            pnlDno.BackColor = AppColors.Background;
            this.Controls.Add(pnlDno);

            Panel sepDno = new Panel(); sepDno.Dock = DockStyle.Top; sepDno.Height = 1;
            sepDno.BackColor = AppColors.BorderLight; pnlDno.Controls.Add(sepDno);

            btnSpremi = new Guna2Button();
            btnSpremi.Text = _editMode ? "💾  Spremi izmjene" : "✔  Kreiraj račun";
            btnSpremi.Size = new Size(175, 38); btnSpremi.Location = new Point(15, 9);
            btnSpremi.FillColor = AppColors.Primary;
            btnSpremi.HoverState.FillColor = AppColors.PrimaryLight;
            btnSpremi.Font = AppFonts.RegularMedium; btnSpremi.ForeColor = AppColors.TextWhite;
            btnSpremi.BorderRadius = 8; btnSpremi.Cursor = Cursors.Hand;
            btnSpremi.Click += BtnSpremi_Click;
            pnlDno.Controls.Add(btnSpremi);

            btnOdustani = new Guna2Button();
            btnOdustani.Text = "✖  Odustani";
            btnOdustani.Size = new Size(120, 38); btnOdustani.Location = new Point(198, 9);
            btnOdustani.FillColor = Color.FromArgb(210, 210, 215);
            btnOdustani.HoverState.FillColor = Color.FromArgb(190, 190, 195);
            btnOdustani.Font = AppFonts.Regular; btnOdustani.ForeColor = AppColors.TextPrimary;
            btnOdustani.BorderRadius = 8; btnOdustani.Cursor = Cursors.Hand;
            btnOdustani.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlDno.Controls.Add(btnOdustani);

            // Preview gumb — HTML preview bez spremanja u bazu
            Guna2Button btnPreview = new Guna2Button();
            btnPreview.Text = "👁  Preview";
            btnPreview.Size = new Size(130, 38); btnPreview.Location = new Point(326, 9);
            btnPreview.FillColor = Color.FromArgb(142, 68, 173);
            btnPreview.HoverState.FillColor = ControlPaint.Light(Color.FromArgb(142, 68, 173), 0.15f);
            btnPreview.Font = AppFonts.Regular; btnPreview.ForeColor = Color.White;
            btnPreview.BorderRadius = 8; btnPreview.Cursor = Cursors.Hand;
            btnPreview.Click += BtnPreview_Click;
            pnlDno.Controls.Add(btnPreview);
            Panel pnlScroll = new Panel();
            pnlScroll.Dock = DockStyle.Fill;
            pnlScroll.BackColor = AppColors.Background;
            this.Controls.Add(pnlScroll);

            // Card: Zaglavlje
            Guna2Panel cardZaglavlje = KreirajCard(pnlScroll, 15, 48, 0, 148);
            cardZaglavlje.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.SizeChanged += (s, e) => cardZaglavlje.Width = this.ClientSize.Width - 30;
            KreirajZaglavljeCard(cardZaglavlje);

            // Card: Stavke
            Guna2Panel cardStavke = KreirajCard(pnlScroll, 15, 204, 0, 0);
            cardStavke.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.SizeChanged += (s, e) =>
            {
                cardStavke.Width = this.ClientSize.Width - 30;
                cardStavke.Height = this.ClientSize.Height - pnlDno.Height - 204 - 10;
            };
            cardStavke.Width = 1020; cardStavke.Height = 430;
            KreirajStavkeCard(cardStavke);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CARD: ZAGLAVLJE
        // ══════════════════════════════════════════════════════════════════════

        private void KreirajZaglavljeCard(Guna2Panel card)
        {
            int y = 15;
            int x1 = 15, x2 = 205, x3 = 395, x4 = 585, x5 = 775;
            int inW = 175, inH = 34;

            DodajSekcijuNaslov(card, "Zaglavlje računa", x1, y); y += 26;

            // Red 1
            DodajLabel(card, "Broj računa *", x1, y);
            DodajLabel(card, "Datum računa *", x2, y);
            DodajLabel(card, "Datum valute", x3, y);
            DodajLabel(card, "Prodavač", x4, y);
            DodajLabel(card, "Plaćeno", x5, y);
            y += 18;

            txtBrojRacuna = DodajTextBox(card, x1, y, inW);

            dtpDatumRacuna = new DateTimePicker();
            dtpDatumRacuna.Size = new Size(inW, inH);
            dtpDatumRacuna.Location = new Point(x2, y);
            dtpDatumRacuna.Format = DateTimePickerFormat.Short;
            card.Controls.Add(dtpDatumRacuna);

            dtpDatumValute = new DateTimePicker();
            dtpDatumValute.Size = new Size(inW, inH);
            dtpDatumValute.Location = new Point(x3, y);
            dtpDatumValute.Format = DateTimePickerFormat.Short;
            card.Controls.Add(dtpDatumValute);

            cmbProdavac = DodajComboBox(card, x4, y, inW);

            tglPlaceno = new Guna2ToggleSwitch();
            tglPlaceno.Size = new Size(48, 24);
            tglPlaceno.Location = new Point(x5, y + 5);
            tglPlaceno.Checked = false;
            tglPlaceno.CheckedState.FillColor = AppColors.Success;
            tglPlaceno.UncheckedState.FillColor = AppColors.BorderLight;
            card.Controls.Add(tglPlaceno);
            y += inH + 14;

            // Red 2: Kupac picker + napomena
            DodajLabel(card, "Kupac *", x1, y);
            DodajLabel(card, "Napomena", x4, y);
            y += 18;

            // Kupac: textbox (read-only prikaz) + gumb za odabir
            txtKupacPrikaz = new Guna2TextBox();
            txtKupacPrikaz.Size = new Size(inW + 185, inH); // šire
            txtKupacPrikaz.Location = new Point(x1, y);
            txtKupacPrikaz.FillColor = Color.FromArgb(245, 246, 252);
            txtKupacPrikaz.BorderColor = AppColors.BorderLight;
            txtKupacPrikaz.Font = AppFonts.Regular;
            txtKupacPrikaz.ForeColor = AppColors.TextPrimary;
            txtKupacPrikaz.BorderRadius = 8;
            txtKupacPrikaz.ReadOnly = true;
            txtKupacPrikaz.PlaceholderText = "Kliknite za odabir kupca...";
            card.Controls.Add(txtKupacPrikaz);

            btnOdaberiKupca = new Guna2Button();
            btnOdaberiKupca.Text = "👥  Odaberi";
            btnOdaberiKupca.Size = new Size(100, inH);
            btnOdaberiKupca.Location = new Point(x1 + inW + 185 + 6, y);
            btnOdaberiKupca.FillColor = AppColors.Secondary;
            btnOdaberiKupca.HoverState.FillColor = AppColors.Primary;
            btnOdaberiKupca.Font = AppFonts.Regular;
            btnOdaberiKupca.ForeColor = Color.White;
            btnOdaberiKupca.BorderRadius = 8;
            btnOdaberiKupca.Cursor = Cursors.Hand;
            btnOdaberiKupca.Click += BtnOdaberiKupca_Click;
            card.Controls.Add(btnOdaberiKupca);

            txtNapomena = DodajTextBox(card, x4, y, inW + 195);
            txtNapomena.PlaceholderText = "Opcionalna napomena...";
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CARD: STAVKE
        // ══════════════════════════════════════════════════════════════════════

        private void KreirajStavkeCard(Guna2Panel card)
        {
            int y = 15;
            DodajSekcijuNaslov(card, "Stavke računa", 15, y); y += 28;

            // ── Toolbar za unos stavke ─────────────────────────────────────────
            Guna2Panel pnlUnos = new Guna2Panel();
            pnlUnos.Location = new Point(15, y);
            pnlUnos.Height = 44;
            pnlUnos.FillColor = Color.FromArgb(245, 246, 252);
            pnlUnos.BorderRadius = 8;
            pnlUnos.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(pnlUnos);
            card.SizeChanged += (s, e) => pnlUnos.Width = card.Width - 30;
            pnlUnos.Width = 990;

            // Artikl: read-only tekst + gumb
            txtArtiklPrikaz = new Guna2TextBox();
            txtArtiklPrikaz.PlaceholderText = "Kliknite 'Odaberi artikl'...";
            txtArtiklPrikaz.Size = new Size(240, 30);
            txtArtiklPrikaz.Location = new Point(6, 7);
            txtArtiklPrikaz.FillColor = AppColors.Background;
            txtArtiklPrikaz.BorderColor = AppColors.BorderLight;
            txtArtiklPrikaz.Font = AppFonts.Regular;
            txtArtiklPrikaz.ForeColor = AppColors.TextPrimary;
            txtArtiklPrikaz.BorderRadius = 6;
            txtArtiklPrikaz.ReadOnly = true;
            pnlUnos.Controls.Add(txtArtiklPrikaz);

            btnOdaberiArtikl = new Guna2Button();
            btnOdaberiArtikl.Text = "📦  Artikl";
            btnOdaberiArtikl.Size = new Size(88, 30);
            btnOdaberiArtikl.Location = new Point(252, 7);
            btnOdaberiArtikl.FillColor = AppColors.Secondary;
            btnOdaberiArtikl.HoverState.FillColor = AppColors.Primary;
            btnOdaberiArtikl.Font = AppFonts.Regular;
            btnOdaberiArtikl.ForeColor = Color.White;
            btnOdaberiArtikl.BorderRadius = 6;
            btnOdaberiArtikl.Cursor = Cursors.Hand;
            btnOdaberiArtikl.Click += BtnOdaberiArtikl_Click;
            pnlUnos.Controls.Add(btnOdaberiArtikl);

            DodajMiniLabel(pnlUnos, "Kol.:", 348, 14);
            txtKolicina = DodajMiniTextBox(pnlUnos, 376, 7, 68);
            txtKolicina.Text = "1";

            DodajMiniLabel(pnlUnos, "Cijena:", 452, 14);
            txtCijena = DodajMiniTextBox(pnlUnos, 494, 7, 88);

            DodajMiniLabel(pnlUnos, "Pop%:", 590, 14);
            txtPopust = DodajMiniTextBox(pnlUnos, 624, 7, 58);
            txtPopust.Text = "0";

            btnDodajStavku = new Guna2Button();
            btnDodajStavku.Text = "➕  Dodaj";
            btnDodajStavku.Size = new Size(88, 30);
            btnDodajStavku.Location = new Point(690, 7);
            btnDodajStavku.FillColor = AppColors.Success;
            btnDodajStavku.HoverState.FillColor = ControlPaint.Light(AppColors.Success, 0.15f);
            btnDodajStavku.Font = AppFonts.Regular;
            btnDodajStavku.ForeColor = Color.White;
            btnDodajStavku.BorderRadius = 6;
            btnDodajStavku.Cursor = Cursors.Hand;
            btnDodajStavku.Click += BtnDodajStavku_Click;
            pnlUnos.Controls.Add(btnDodajStavku);

            btnUkloniStavku = new Guna2Button();
            btnUkloniStavku.Text = "🗑  Ukloni";
            btnUkloniStavku.Size = new Size(88, 30);
            btnUkloniStavku.Location = new Point(786, 7);
            btnUkloniStavku.FillColor = AppColors.Danger;
            btnUkloniStavku.HoverState.FillColor = ControlPaint.Light(AppColors.Danger, 0.15f);
            btnUkloniStavku.Font = AppFonts.Regular;
            btnUkloniStavku.ForeColor = Color.White;
            btnUkloniStavku.BorderRadius = 6;
            btnUkloniStavku.Cursor = Cursors.Hand;
            btnUkloniStavku.Enabled = false;
            btnUkloniStavku.Click += BtnUkloniStavku_Click;
            pnlUnos.Controls.Add(btnUkloniStavku);

            y += 52;

            // ── Grid stavki ───────────────────────────────────────────────────
            dgvStavke = new Guna2DataGridView();
            dgvStavke.Location = new Point(15, y);
            dgvStavke.BorderStyle = BorderStyle.None;
            dgvStavke.BackgroundColor = AppColors.CardBackground;
            dgvStavke.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvStavke.GridColor = Color.FromArgb(230, 232, 240);
            dgvStavke.EnableHeadersVisualStyles = false;
            dgvStavke.RowTemplate.Height = 32;
            dgvStavke.ColumnHeadersHeight = 36;
            dgvStavke.AllowUserToAddRows = false;
            dgvStavke.ReadOnly = true;
            dgvStavke.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvStavke.MultiSelect = false;
            dgvStavke.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvStavke.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

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
                dgvStavke.Height = card.Height - y - 78;
            };
            dgvStavke.Width = 990; dgvStavke.Height = 240;

            KonfigurisiKoloneStavki();

            // ── Totali ────────────────────────────────────────────────────────
            Panel pnlTotali = new Panel();
            pnlTotali.BackColor = Color.Transparent;
            pnlTotali.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            pnlTotali.Size = new Size(380, 56);
            card.Controls.Add(pnlTotali);
            card.SizeChanged += (s, e) =>
                pnlTotali.Location = new Point(card.Width - 395, card.Height - 64);
            pnlTotali.Location = new Point(625, 370);

            lblBezPdv = KreirajTotalLabel(pnlTotali, "Ukupno bez PDV:", 0);
            lblPdvIznos = KreirajTotalLabel(pnlTotali, "PDV:", 20);
            lblSaPdv = KreirajTotalLabel(pnlTotali, "UKUPNO S PDV:", 40);
            lblSaPdv.Font = AppFonts.RegularMedium;
            lblSaPdv.ForeColor = AppColors.Primary;
        }

        private Label KreirajTotalLabel(Panel parent, string prefiks, int y)
        {
            Label lbl = new Label();
            lbl.Text = $"{prefiks}  0,00 €";
            lbl.Font = AppFonts.Regular;
            lbl.ForeColor = AppColors.TextPrimary;
            lbl.AutoSize = false;
            lbl.Size = new Size(380, 18);
            lbl.Location = new Point(0, y);
            lbl.TextAlign = ContentAlignment.MiddleRight;
            parent.Controls.Add(lbl);
            return lbl;
        }

        private void KonfigurisiKoloneStavki()
        {
            dgvStavke.Columns.Clear();
            var kolone = new[]
            {
                new { Name="colRbr",    Header="Rbr",           Weight=35f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colSifra",  Header="Šifra",         Weight=68f,  Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colNaziv",  Header="Naziv artikla", Weight=210f, Align=DataGridViewContentAlignment.MiddleLeft   },
                new { Name="colJM",     Header="J/M",           Weight=40f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colKol",    Header="Količina",      Weight=70f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colCijena", Header="Cijena (€)",    Weight=80f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colPopust", Header="Pop.%",         Weight=52f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colPdv",    Header="PDV%",          Weight=52f,  Align=DataGridViewContentAlignment.MiddleCenter },
                new { Name="colBezPdv", Header="Bez PDV (€)",   Weight=85f,  Align=DataGridViewContentAlignment.MiddleRight  },
                new { Name="colSaPdv",  Header="S PDV (€)",     Weight=85f,  Align=DataGridViewContentAlignment.MiddleRight  },
            };
            foreach (var k in kolone)
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = k.Name; col.HeaderText = k.Header;
                col.FillWeight = k.Weight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.DefaultCellStyle.Alignment = k.Align;
                col.HeaderCell.Style.Alignment = k.Align;
                dgvStavke.Columns.Add(col);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  POPUNJAVANJE
        // ══════════════════════════════════════════════════════════════════════

        private void UcitajProdavaceUCombo()
        {
            cmbProdavac.Items.Clear();
            cmbProdavac.Items.Add(new Prodavac { Id = 0, Ime = "—", Prezime = "" });
            foreach (var p in _prodavaci) cmbProdavac.Items.Add(p);
            cmbProdavac.SelectedIndex = 0;
        }

        private void PopuniPolja()
        {
            if (_racun == null) return;
            txtBrojRacuna.Text = _racun.BrojRacuna;
            dtpDatumRacuna.Value = _racun.DatumRacuna;
            dtpDatumValute.Value = _racun.DatumValute ?? DateTime.Today.AddDays(30);
            txtNapomena.Text = _racun.Napomena ?? "";
            tglPlaceno.Checked = _racun.Placeno;

            // Kupac
            _odabraniKupac = new Partner
            {
                Id = _racun.KupacId,
                Naziv = _racun.NazivKupca
            };
            txtKupacPrikaz.Text = _racun.NazivKupca;

            // Prodavač
            foreach (var item in cmbProdavac.Items)
                if (item is Prodavac pr && pr.Id == _racun.ProdavacId)
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
                    s.ProdajnaCijena.ToString("N2"),
                    s.Popust > 0 ? s.Popust.ToString("N1") + "%" : "—",
                    s.PdvStopa.ToString("0") + "%",
                    s.IznosBezPdv.ToString("N2"),
                    s.IznosSaPdv.ToString("N2")
                );
            }
            btnUkloniStavku.Enabled = false;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  EVENTI — PICKER POPUPI
        // ══════════════════════════════════════════════════════════════════════

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
                    txtCijena.Text = _trenutniArtikl.CijenaProdaje.ToString("N2");
                    txtKolicina.Focus();
                    txtKolicina.SelectAll();
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  EVENTI — STAVKE
        // ══════════════════════════════════════════════════════════════════════

        private void BtnDodajStavku_Click(object sender, EventArgs e)
        {
            if (_trenutniArtikl == null)
            { MessageBox.Show("Odaberite artikl!", "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (!decimal.TryParse(txtKolicina.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal kolicina) || kolicina <= 0)
            { PrikaziGreskuPolja(txtKolicina, "Unesite ispravnu količinu!"); return; }

            if (!decimal.TryParse(txtCijena.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal cijena) || cijena < 0)
            { PrikaziGreskuPolja(txtCijena, "Unesite ispravnu cijenu!"); return; }

            decimal.TryParse(txtPopust.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal popust);

            decimal cijenaNetoBezPopusta = cijena / (1 + _trenutniArtikl.PdvStopa / 100);
            decimal cijenaNetoCijenaPopust = cijenaNetoBezPopusta * (1 - popust / 100);
            decimal bezPdv = Math.Round(kolicina * cijenaNetoCijenaPopust, 2);
            decimal saPdv = Math.Round(kolicina * cijena * (1 - popust / 100), 2);
            decimal pdvIznos = Math.Round(saPdv - bezPdv, 2);

            var stavka = new RacunStavka
            {
                ArtiklId = _trenutniArtikl.Id,
                SifraArtikla = _trenutniArtikl.Sifra,
                NazivArtikla = _trenutniArtikl.Naziv,
                NazivJediniceMjere = _trenutniArtikl.NazivJediniceMjere,
                Kolicina = kolicina,
                ProdajnaCijena = cijena,
                Popust = popust,
                PdvStopa = _trenutniArtikl.PdvStopa,
                IznosBezPdv = bezPdv,
                IznosPdv = pdvIznos,
                IznosSaPdv = saPdv
            };

            _stavke.Add(stavka);
            PopuniGridStavke();
            IzracunajTotale();

            // Reset
            _trenutniArtikl = null;
            txtArtiklPrikaz.Text = "";
            txtKolicina.Text = "1";
            txtCijena.Text = "";
            txtPopust.Text = "0";
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

        private void IzracunajTotale()
        {
            decimal bezPdv = _stavke.Sum(s => s.IznosBezPdv);
            decimal pdv = _stavke.Sum(s => s.IznosPdv);
            decimal saPdv = _stavke.Sum(s => s.IznosSaPdv);
            lblBezPdv.Text = $"Ukupno bez PDV:   {bezPdv:N2} €";
            lblPdvIznos.Text = $"PDV:   {pdv:N2} €";
            lblSaPdv.Text = $"UKUPNO S PDV:   {saPdv:N2} €";
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PREVIEW — HTML prikaz prije spremanja
        // ══════════════════════════════════════════════════════════════════════

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            if (_stavke.Count == 0)
            {
                MessageBox.Show("Dodajte barem jednu stavku za prikaz previewa.",
                    "Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var privremeni = new Racun
            {
                BrojRacuna = string.IsNullOrWhiteSpace(txtBrojRacuna.Text) ? "(bez broja)" : txtBrojRacuna.Text.Trim(),
                DatumRacuna = dtpDatumRacuna.Value.Date,
                DatumValute = dtpDatumValute.Value.Date,
                NazivKupca = _odabraniKupac?.Naziv ?? "(kupac nije odabran)",
                NazivProdavaca = (cmbProdavac.SelectedItem is Prodavac pr && pr.Id > 0) ? pr.ToString() : null,
                UkupnoBezPdv = _stavke.Sum(s => s.IznosBezPdv),
                UkupnoPdv = _stavke.Sum(s => s.IznosPdv),
                UkupnoSaPdv = _stavke.Sum(s => s.IznosSaPdv),
                Placeno = tglPlaceno.Checked,
                Status = tglPlaceno.Checked ? "PLAĆENO" : "KREIRAN",
                Napomena = txtNapomena.Text.Trim(),
                Stavke = _stavke
            };

            try
            {
                string pdfPath = RacunPdfHelper.GenerirajPdfTemp(privremeni);

                Process.Start(new ProcessStartInfo(pdfPath)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri prikazu previewa:\n" + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  SPREMI
        // ══════════════════════════════════════════════════════════════════════

        private void BtnSpremi_Click(object sender, EventArgs e)
        {
            if (!Validiraj()) return;
            try
            {
                Racun r = KreirajRacunIzFormi();
                if (_editMode) { r.Id = _racun.Id; RacuniRepository.AzurirajRacun(r); }
                else { RacuniRepository.DodajRacun(r); }
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
            if (string.IsNullOrWhiteSpace(txtBrojRacuna.Text))
            { PrikaziGreskuPolja(txtBrojRacuna, "Broj računa je obavezan!"); return false; }

            if (RacuniRepository.BrojRacunaPostoji(txtBrojRacuna.Text.Trim(), _editMode ? _racun.Id : 0))
            { PrikaziGreskuPolja(txtBrojRacuna, "Račun s ovim brojem već postoji!"); return false; }

            if (_odabraniKupac == null || _odabraniKupac.Id == 0)
            {
                MessageBox.Show("Odaberite kupca!", "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnOdaberiKupca.Focus();
                return false;
            }

            if (_stavke.Count == 0)
            {
                MessageBox.Show("Račun mora imati barem jednu stavku!",
                    "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private Racun KreirajRacunIzFormi()
        {
            int? prodavacId = (cmbProdavac.SelectedItem is Prodavac pr && pr.Id > 0) ? pr.Id : (int?)null;
            return new Racun
            {
                BrojRacuna = txtBrojRacuna.Text.Trim(),
                DatumRacuna = dtpDatumRacuna.Value.Date,
                DatumValute = dtpDatumValute.Value.Date,
                KupacId = _odabraniKupac.Id,
                ProdavacId = prodavacId,
                UkupnoBezPdv = _stavke.Sum(s => s.IznosBezPdv),
                UkupnoPdv = _stavke.Sum(s => s.IznosPdv),
                UkupnoSaPdv = _stavke.Sum(s => s.IznosSaPdv),
                Placeno = tglPlaceno.Checked,
                Status = tglPlaceno.Checked ? "PLAĆENO" : "KREIRAN",
                Napomena = txtNapomena.Text.Trim(),
                Stavke = _stavke
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPER BUILDERS
        // ══════════════════════════════════════════════════════════════════════

        private Guna2Panel KreirajCard(Control parent, int x, int y, int width, int height)
        {
            Guna2Panel card = new Guna2Panel();
            card.Location = new Point(x, y);
            if (width > 0) card.Width = width;
            if (height > 0) card.Height = height;
            card.FillColor = AppColors.CardBackground;
            card.BorderRadius = 10;
            card.ShadowDecoration.Enabled = true;
            card.ShadowDecoration.Depth = 6;
            parent.Controls.Add(card);
            return card;
        }

        private void DodajLabel(Control parent, string text, int x, int y)
        {
            Label lbl = new Label(); lbl.Text = text;
            lbl.Font = AppFonts.Regular; lbl.ForeColor = AppColors.TextSecondary;
            lbl.Location = new Point(x, y); lbl.AutoSize = true;
            parent.Controls.Add(lbl);
        }

        private void DodajMiniLabel(Control parent, string text, int x, int y)
        {
            Label lbl = new Label(); lbl.Text = text;
            lbl.Font = AppFonts.Regular; lbl.ForeColor = AppColors.TextSecondary;
            lbl.Location = new Point(x, y); lbl.AutoSize = true;
            parent.Controls.Add(lbl);
        }

        private void DodajSekcijuNaslov(Control parent, string text, int x, int y)
        {
            Label lbl = new Label(); lbl.Text = text.ToUpper();
            lbl.Font = AppFonts.RegularMedium; lbl.ForeColor = AppColors.Primary;
            lbl.Location = new Point(x, y); lbl.AutoSize = true;
            parent.Controls.Add(lbl);
            Panel linija = new Panel(); linija.Height = 1;
            linija.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            linija.Location = new Point(x, y + 18);
            linija.BackColor = AppColors.BorderLight;
            parent.Controls.Add(linija);
            parent.SizeChanged += (s, e) => linija.Width = parent.Width - x - 15;
            linija.Width = parent.Width > 0 ? parent.Width - x - 15 : 540;
        }

        private Guna2TextBox DodajTextBox(Control parent, int x, int y, int width)
        {
            Guna2TextBox txt = new Guna2TextBox();
            txt.Size = new Size(width, 34); txt.Location = new Point(x, y);
            txt.FillColor = AppColors.Background; txt.BorderColor = AppColors.BorderLight;
            txt.Font = AppFonts.Regular; txt.ForeColor = AppColors.TextPrimary;
            txt.BorderRadius = 8;
            txt.Enter += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.Primary;
            txt.Leave += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.BorderLight;
            parent.Controls.Add(txt);
            return txt;
        }

        private Guna2TextBox DodajMiniTextBox(Control parent, int x, int y, int width)
        {
            Guna2TextBox txt = new Guna2TextBox();
            txt.Size = new Size(width, 30); txt.Location = new Point(x, y);
            txt.FillColor = AppColors.Background; txt.BorderColor = AppColors.BorderLight;
            txt.Font = AppFonts.Regular; txt.ForeColor = AppColors.TextPrimary;
            txt.BorderRadius = 6;
            txt.Enter += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.Primary;
            txt.Leave += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.BorderLight;
            parent.Controls.Add(txt);
            return txt;
        }

        private Guna2ComboBox DodajComboBox(Control parent, int x, int y, int width)
        {
            Guna2ComboBox cmb = new Guna2ComboBox();
            cmb.Size = new Size(width, 34); cmb.Location = new Point(x, y);
            cmb.FillColor = AppColors.Background; cmb.BorderColor = AppColors.BorderLight;
            cmb.FocusedColor = AppColors.Primary; cmb.Font = AppFonts.Regular;
            cmb.ForeColor = AppColors.TextPrimary; cmb.BorderRadius = 8;
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            parent.Controls.Add(cmb);
            return cmb;
        }

        private void PrikaziGreskuPolja(Control ctrl, string poruka)
        {
            ctrl.Focus();
            MessageBox.Show(poruka, "Validacija", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}