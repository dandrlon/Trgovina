using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Utils;

namespace Trgovina.UserControls
{
    public partial class IzvjestajiControl : UserControl
    {
        // ── Kategorije i izvještaji ───────────────────────────────────────────────
        private readonly List<IzvjestajKategorija> kategorije = new List<IzvjestajKategorija>
        {
            new IzvjestajKategorija("💰  Prodaja", new List<IzvjestajItem>
            {
                new IzvjestajItem("Prodaja po periodu",       "Pregled ukupne prodaje u odabranom vremenskom periodu", IzvjestajTip.ProdajaPoPerodu),
                new IzvjestajItem("Prodaja po kupcu",         "Rang lista kupaca prema ukupnoj vrijednosti kupnje",    IzvjestajTip.ProdajaPoKupcu),
                new IzvjestajItem("Prodaja po artiklu",       "Najprodavaniji artikli – količina i vrijednost",        IzvjestajTip.ProdajaPoArtiklu),
                new IzvjestajItem("Prodaja po prodavaču",     "Učinak prodavača u odabranom periodu",                 IzvjestajTip.ProdajaPoProduavacu),
                new IzvjestajItem("Dnevni promet",            "Promet po danima za odabrani period",                  IzvjestajTip.DnevniPromet),
            }),
            new IzvjestajKategorija("🧾  Računi", new List<IzvjestajItem>
            {
                new IzvjestajItem("Nepodmireni računi",       "Računi koji nisu plaćeni",                             IzvjestajTip.NepodmireniRacuni),
                new IzvjestajItem("Pregled računa",           "Svi računi u odabranom periodu s detaljem statusa",    IzvjestajTip.PregledRacuna),
                new IzvjestajItem("PDV izvještaj",            "Iznosi PDV-a grupirani po stopama i periodu",          IzvjestajTip.PdvIzvjestaj),
            }),
            new IzvjestajKategorija("📦  Zalihe", new List<IzvjestajItem>
            {
                new IzvjestajItem("Stanje zaliha",            "Trenutna zaliha svih artikala s minimalnim limitima",  IzvjestajTip.StanjeZaliha),
                new IzvjestajItem("Artikli ispod minimuma",   "Artikli kojima je zaliha ispod minimalne razine",      IzvjestajTip.ArtikliIspodMinimuma),
                new IzvjestajItem("Kretanje zaliha",          "Ulaz/izlaz artikla u odabranom periodu",               IzvjestajTip.KretanjeZaliha),
            }),
            new IzvjestajKategorija("📥  Kalkulacije", new List<IzvjestajItem>
            {
                new IzvjestajItem("Pregled kalkulacija",      "Sve kalkulacije (ulazne fakture) u periodu",           IzvjestajTip.PregledKalkulacija),
                new IzvjestajItem("Nabava po dobavljaču",     "Ukupna nabava grupirana po dobavljačima",              IzvjestajTip.NabavaPoDobaavljacu),
            }),
            new IzvjestajKategorija("👥  Partneri", new List<IzvjestajItem>
            {
                new IzvjestajItem("Pregled partnera",         "Popis svih aktivnih partnera",                         IzvjestajTip.PregledPartnera),
                new IzvjestajItem("Top kupci",                "10 kupaca s najvećim prometom",                        IzvjestajTip.TopKupci),
                new IzvjestajItem("Top dobavljači",           "10 dobavljača s najvećom nabavom",                     IzvjestajTip.TopDobavljaci),
            }),
            new IzvjestajKategorija("📋  Ponude", new List<IzvjestajItem>
            {
                new IzvjestajItem("Pregled ponuda",           "Sve ponude s prikazom statusa i vrijednosti",          IzvjestajTip.PregledPonuda),
                new IzvjestajItem("Konverzija ponuda",        "Omjer prihvaćenih i odbijenih ponuda",                 IzvjestajTip.KonverzijaPonuda),
            }),
            new IzvjestajKategorija("🚚  Otpremnice", new List<IzvjestajItem>
            {
                new IzvjestajItem("Pregled otpremnica",       "Sve otpremnice u odabranom periodu",                   IzvjestajTip.PregledOtpremnica),
                new IzvjestajItem("Nefakturirane otpremnice", "Isporučena roba bez priloženog računa",                IzvjestajTip.NefakturiraneOtpremnice),
            }),
        };

        // ── UI komponente ─────────────────────────────────────────────────────────
        private Guna2Panel pnlLeft;
        private Guna2Panel pnlRight;
        private Guna2Panel pnlFilter;
        private Guna2Panel pnlGrid;
        private DataGridView dgvResults;
        private DateTimePicker dtpOd;
        private DateTimePicker dtpDo;
        private Guna2ComboBox cmbPartner;
        private Guna2ComboBox cmbArtikl;
        private Guna2Button btnGeneriraj;
        private Guna2Button btnExportExcel;
        private Guna2Button btnExportPdf;
        private Guna2Button btnPrint;
        private Label lblNaslovIzvjestaja;
        private Label lblOpisIzvjestaja;
        private Label lblUkupno;
        private Panel pnlKategorijeSadrzaj;

        private IzvjestajTip aktivniIzvjestaj = IzvjestajTip.ProdajaPoPerodu;
        private Guna2Button aktivniBtn = null;
        private DataTable trenutniPodaci = null; // čuvamo zadnji rezultat za export

        // ── Connection string iz DatabaseHelper – identično ostalim repozitorijima ─
        private static readonly string ConnStr = DatabaseHelper.ConnectionString;

        public IzvjestajiControl()
        {
            InitializeComponent();
            BuildUI();
            PopulateComboBoxes();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "IzvjestajiControl";
            this.ResumeLayout(false);
        }

        // ── Gradnja UI-ja ─────────────────────────────────────────────────────────
        private void BuildUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Background;

            // ── Naslov stranice ──────────────────────────────────────────────────
            Guna2Panel pnlHeader = new Guna2Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 70;
            pnlHeader.FillColor = AppColors.CardBackground;
            pnlHeader.ShadowDecoration.Enabled = true;
            pnlHeader.ShadowDecoration.Depth = 4;

            Label lblTitle = new Label();
            lblTitle.Text = "📊  Izvještaji";
            lblTitle.Font = AppFonts.TitleMedium;
            lblTitle.ForeColor = AppColors.Primary;
            lblTitle.Location = new Point(25, 20);
            lblTitle.AutoSize = true;
            pnlHeader.Controls.Add(lblTitle);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Generiranje i pregled poslovnih izvještaja";
            lblSubtitle.Font = AppFonts.Regular;
            lblSubtitle.ForeColor = AppColors.TextSecondary;
            lblSubtitle.Location = new Point(27, 45);
            lblSubtitle.AutoSize = true;
            pnlHeader.Controls.Add(lblSubtitle);

            // ── Glavni layout – Fill PRIJE Top! ──────────────────────────────────
            Guna2Panel pnlMain = new Guna2Panel();
            pnlMain.Dock = DockStyle.Fill;
            pnlMain.FillColor = AppColors.Background;
            pnlMain.Padding = new Padding(15);
            this.Controls.Add(pnlMain);

            this.Controls.Add(pnlHeader); // Top – dodaje se NAKON Fill

            // ── Desni panel – gradi se PRVI (labele moraju postojati prije lijevog) ─
            pnlRight = new Guna2Panel();
            pnlRight.Dock = DockStyle.Fill;
            pnlRight.FillColor = AppColors.Background;
            pnlRight.Padding = new Padding(10, 0, 0, 0);
            pnlMain.Controls.Add(pnlRight);

            BuildRightPanel();

            // ── Lijevi panel – lista izvještaja ──────────────────────────────────
            pnlLeft = new Guna2Panel();
            pnlLeft.Dock = DockStyle.Left;
            pnlLeft.Width = 270;
            pnlLeft.FillColor = AppColors.CardBackground;
            pnlLeft.BorderRadius = 12;
            pnlLeft.ShadowDecoration.Enabled = true;
            pnlLeft.ShadowDecoration.Depth = 5;

            Panel pnlLeftScrollable = new Panel();
            pnlLeftScrollable.Dock = DockStyle.Fill;
            pnlLeftScrollable.AutoScroll = true;
            pnlLeftScrollable.BackColor = AppColors.CardBackground;

            pnlKategorijeSadrzaj = new Panel();
            pnlKategorijeSadrzaj.AutoSize = true;
            pnlKategorijeSadrzaj.Width = 260;
            pnlKategorijeSadrzaj.BackColor = AppColors.CardBackground;

            int yK = 10;
            IzvjestajItem prviBez = null;
            Guna2Button prviBtn = null;

            foreach (var kat in kategorije)
            {
                Label lblKat = new Label();
                lblKat.Text = kat.Naziv;
                lblKat.Font = new Font(AppFonts.TitleSmall.FontFamily, 9f, FontStyle.Bold);
                lblKat.ForeColor = AppColors.TextSecondary;
                lblKat.Location = new Point(12, yK);
                lblKat.AutoSize = true;
                lblKat.BackColor = AppColors.CardBackground;
                pnlKategorijeSadrzaj.Controls.Add(lblKat);
                yK += 28;

                foreach (var izv in kat.Izvjestaji)
                {
                    var btn = CreateIzvjestajButton(izv, yK);
                    pnlKategorijeSadrzaj.Controls.Add(btn);
                    yK += 44;

                    if (prviBtn == null) { prviBtn = btn; prviBez = izv; }
                }
                yK += 8;
            }

            if (prviBtn != null)
                SetActiveIzvjestajButton(prviBtn, prviBez);

            pnlKategorijeSadrzaj.Height = yK + 10;
            pnlLeftScrollable.Controls.Add(pnlKategorijeSadrzaj);
            pnlLeft.Controls.Add(pnlLeftScrollable);
            pnlMain.Controls.Add(pnlLeft);
        }

        private Guna2Button CreateIzvjestajButton(IzvjestajItem izv, int yPos)
        {
            Guna2Button btn = new Guna2Button();
            btn.Text = "  " + izv.Naziv;
            btn.Size = new Size(248, 38);
            btn.Location = new Point(8, yPos);
            btn.TextAlign = HorizontalAlignment.Left;
            btn.FillColor = AppColors.CardBackground;
            btn.HoverState.FillColor = Color.FromArgb(230, 240, 255);
            btn.Font = AppFonts.Regular;
            btn.ForeColor = AppColors.TextPrimary;
            btn.BorderRadius = 8;
            btn.Cursor = Cursors.Hand;
            btn.Tag = izv;

            btn.Click += (s, e) => SetActiveIzvjestajButton(btn, izv);
            return btn;
        }

        private void SetActiveIzvjestajButton(Guna2Button btn, IzvjestajItem izv)
        {
            if (aktivniBtn != null)
            {
                aktivniBtn.FillColor = AppColors.CardBackground;
                aktivniBtn.ForeColor = AppColors.TextPrimary;
            }

            aktivniBtn = btn;
            btn.FillColor = AppColors.Primary;
            btn.ForeColor = Color.White;

            aktivniIzvjestaj = izv.Tip;
            lblNaslovIzvjestaja.Text = "📊  " + izv.Naziv;
            lblOpisIzvjestaja.Text = izv.Opis;
            AzurirajFiltere();

            dgvResults.DataSource = null;
            trenutniPodaci = null;
            lblUkupno.Text = "Odaberite period i kliknite 'Generiraj izvještaj'";
        }

        private void BuildRightPanel()
        {
            // ── Grid panel – Fill PRVI ────────────────────────────────────────────
            pnlGrid = new Guna2Panel();
            pnlGrid.Dock = DockStyle.Fill;
            pnlGrid.FillColor = AppColors.CardBackground;
            pnlGrid.BorderRadius = 12;
            pnlGrid.ShadowDecoration.Enabled = true;
            pnlGrid.ShadowDecoration.Depth = 4;
            pnlGrid.Padding = new Padding(10);

            Guna2Panel pnlStatus = new Guna2Panel();
            pnlStatus.Dock = DockStyle.Bottom;
            pnlStatus.Height = 36;
            pnlStatus.FillColor = Color.FromArgb(245, 247, 250);

            lblUkupno = new Label();
            lblUkupno.Text = "Odaberite period i kliknite 'Generiraj izvještaj'";
            lblUkupno.Font = AppFonts.Regular;
            lblUkupno.ForeColor = AppColors.TextSecondary;
            lblUkupno.Location = new Point(12, 10);
            lblUkupno.AutoSize = true;
            pnlStatus.Controls.Add(lblUkupno);
            pnlGrid.Controls.Add(pnlStatus);

            dgvResults = new DataGridView();
            dgvResults.Dock = DockStyle.Fill;
            dgvResults.BackgroundColor = AppColors.CardBackground;
            dgvResults.BorderStyle = BorderStyle.None;
            dgvResults.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvResults.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvResults.EnableHeadersVisualStyles = false;
            dgvResults.GridColor = Color.FromArgb(230, 232, 240);
            dgvResults.RowHeadersVisible = false;
            dgvResults.AllowUserToAddRows = false;
            dgvResults.ReadOnly = true;
            dgvResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvResults.Font = AppFonts.Regular;
            dgvResults.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Primary;
            dgvResults.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvResults.ColumnHeadersDefaultCellStyle.Font =
                new Font(AppFonts.TitleSmall.FontFamily, 9f, FontStyle.Bold);
            dgvResults.ColumnHeadersHeight = 38;
            dgvResults.RowTemplate.Height = 32;
            dgvResults.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 225, 255);
            dgvResults.DefaultCellStyle.SelectionForeColor = AppColors.TextPrimary;
            dgvResults.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 255);

            pnlGrid.Controls.Add(dgvResults);
            pnlRight.Controls.Add(pnlGrid); // Fill – dodan PRVI

            // ── Naslov izvještaja – Top, dodaje se NAKON Fill ─────────────────────
            Guna2Panel pnlNaslov = new Guna2Panel();
            pnlNaslov.Dock = DockStyle.Top;
            pnlNaslov.Height = 75;
            pnlNaslov.FillColor = AppColors.CardBackground;
            pnlNaslov.BorderRadius = 12;
            pnlNaslov.ShadowDecoration.Enabled = true;
            pnlNaslov.ShadowDecoration.Depth = 4;

            lblNaslovIzvjestaja = new Label();
            lblNaslovIzvjestaja.Text = "📊  Prodaja po periodu";
            lblNaslovIzvjestaja.Font = AppFonts.TitleSmall;
            lblNaslovIzvjestaja.ForeColor = AppColors.Primary;
            lblNaslovIzvjestaja.Location = new Point(20, 12);
            lblNaslovIzvjestaja.AutoSize = true;
            pnlNaslov.Controls.Add(lblNaslovIzvjestaja);

            lblOpisIzvjestaja = new Label();
            lblOpisIzvjestaja.Text = "Pregled ukupne prodaje u odabranom vremenskom periodu";
            lblOpisIzvjestaja.Font = AppFonts.Regular;
            lblOpisIzvjestaja.ForeColor = AppColors.TextSecondary;
            lblOpisIzvjestaja.Location = new Point(22, 42);
            lblOpisIzvjestaja.AutoSize = true;
            pnlNaslov.Controls.Add(lblOpisIzvjestaja);

            pnlRight.Controls.Add(pnlNaslov);

            // ── Filter panel ──────────────────────────────────────────────────────
            pnlFilter = new Guna2Panel();
            pnlFilter.Dock = DockStyle.Top;
            pnlFilter.Height = 80;
            pnlFilter.FillColor = AppColors.CardBackground;
            pnlFilter.BorderRadius = 12;
            pnlFilter.ShadowDecoration.Enabled = true;
            pnlFilter.ShadowDecoration.Depth = 4;
            pnlFilter.Padding = new Padding(15, 10, 15, 10);

            int xF = 15;

            AddFilterLabel(pnlFilter, "Od datuma:", xF, 12);
            dtpOd = new DateTimePicker();
            dtpOd.Format = DateTimePickerFormat.Short;
            dtpOd.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dtpOd.Location = new Point(xF, 34);
            dtpOd.Size = new Size(130, 26);
            dtpOd.Font = AppFonts.Regular;
            pnlFilter.Controls.Add(dtpOd);
            xF += 145;

            AddFilterLabel(pnlFilter, "Do datuma:", xF, 12);
            dtpDo = new DateTimePicker();
            dtpDo.Format = DateTimePickerFormat.Short;
            dtpDo.Value = DateTime.Now;
            dtpDo.Location = new Point(xF, 34);
            dtpDo.Size = new Size(130, 26);
            dtpDo.Font = AppFonts.Regular;
            pnlFilter.Controls.Add(dtpDo);
            xF += 145;

            AddFilterLabel(pnlFilter, "Partner:", xF, 12);
            cmbPartner = new Guna2ComboBox();
            cmbPartner.Location = new Point(xF, 32);
            cmbPartner.Size = new Size(160, 30);
            cmbPartner.Font = AppFonts.Regular;
            cmbPartner.DropDownStyle = ComboBoxStyle.DropDownList;
            pnlFilter.Controls.Add(cmbPartner);
            xF += 175;

            AddFilterLabel(pnlFilter, "Artikl:", xF, 12);
            cmbArtikl = new Guna2ComboBox();
            cmbArtikl.Location = new Point(xF, 32);
            cmbArtikl.Size = new Size(160, 30);
            cmbArtikl.Font = AppFonts.Regular;
            cmbArtikl.DropDownStyle = ComboBoxStyle.DropDownList;
            pnlFilter.Controls.Add(cmbArtikl);
            xF += 175;

            btnGeneriraj = new Guna2Button();
            btnGeneriraj.Text = "🔍  Generiraj";
            btnGeneriraj.Location = new Point(xF, 28);
            btnGeneriraj.Size = new Size(130, 36);
            btnGeneriraj.FillColor = AppColors.Primary;
            btnGeneriraj.HoverState.FillColor = AppColors.PrimaryLight;
            btnGeneriraj.Font = AppFonts.Regular;
            btnGeneriraj.ForeColor = Color.White;
            btnGeneriraj.BorderRadius = 8;
            btnGeneriraj.Cursor = Cursors.Hand;
            btnGeneriraj.Click += BtnGeneriraj_Click;
            pnlFilter.Controls.Add(btnGeneriraj);
            xF += 145;

            btnExportExcel = CreateToolButton("📊 Excel", xF, AppColors.Success); xF += 85;
            btnExportExcel.Click += BtnExportExcel_Click;
            pnlFilter.Controls.Add(btnExportExcel);

            btnExportPdf = CreateToolButton("📄 PDF", xF, AppColors.Danger); xF += 85;
            btnExportPdf.Click += BtnExportPdf_Click;
            pnlFilter.Controls.Add(btnExportPdf);

            btnPrint = CreateToolButton("🖨️ Print", xF, AppColors.TextSecondary);
            btnPrint.Click += BtnPrint_Click;
            pnlFilter.Controls.Add(btnPrint);

            pnlRight.Controls.Add(pnlFilter);
        }

        // ── Helper UI metode ──────────────────────────────────────────────────────
        private void AddFilterLabel(Control parent, string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = AppFonts.Regular;
            lbl.ForeColor = AppColors.TextSecondary;
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            parent.Controls.Add(lbl);
        }

        private Guna2Button CreateToolButton(string text, int xPos, Color color)
        {
            Guna2Button btn = new Guna2Button();
            btn.Text = text;
            btn.Location = new Point(xPos, 30);
            btn.Size = new Size(80, 34);
            btn.FillColor = color;
            btn.HoverState.FillColor = ControlPaint.Dark(color, 0.1f);
            btn.Font = new Font(AppFonts.Regular.FontFamily, 8f);
            btn.ForeColor = Color.White;
            btn.BorderRadius = 8;
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private void AzurirajFiltere()
        {
            bool showPartner = aktivniIzvjestaj == IzvjestajTip.ProdajaPoKupcu
                            || aktivniIzvjestaj == IzvjestajTip.NepodmireniRacuni
                            || aktivniIzvjestaj == IzvjestajTip.PregledRacuna
                            || aktivniIzvjestaj == IzvjestajTip.PregledPartnera;

            bool showArtikl = aktivniIzvjestaj == IzvjestajTip.ProdajaPoArtiklu
                            || aktivniIzvjestaj == IzvjestajTip.KretanjeZaliha
                            || aktivniIzvjestaj == IzvjestajTip.StanjeZaliha;

            cmbPartner.Visible = showPartner;
            cmbArtikl.Visible = showArtikl;
        }

        // ── ComboBox punjenje ─────────────────────────────────────────────────────
        private void PopulateComboBoxes()
        {
            try
            {
                cmbPartner.Items.Clear();
                cmbPartner.Items.Add(new ComboItem(0, "(Svi partneri)"));
                using (var conn = new SqlConnection(ConnStr))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        "SELECT id, naziv FROM partneri WHERE aktivan=1 ORDER BY naziv", conn))
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            cmbPartner.Items.Add(new ComboItem(dr.GetInt32(0), dr.GetString(1)));
                }
                cmbPartner.SelectedIndex = 0;

                cmbArtikl.Items.Clear();
                cmbArtikl.Items.Add(new ComboItem(0, "(Svi artikli)"));
                using (var conn = new SqlConnection(ConnStr))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        "SELECT id, naziv FROM artikli WHERE aktivan=1 ORDER BY naziv", conn))
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            cmbArtikl.Items.Add(new ComboItem(dr.GetInt32(0), dr.GetString(1)));
                }
                cmbArtikl.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri učitavanju podataka: " + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Generiranje izvještaja ────────────────────────────────────────────────
        private void BtnGeneriraj_Click(object sender, EventArgs e)
        {
            if (dtpOd.Value > dtpDo.Value)
            {
                MessageBox.Show("Datum 'Od' ne može biti veći od datuma 'Do'!",
                    "Upozorenje", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnGeneriraj.Enabled = false;
                btnGeneriraj.Text = "⏳  Učitavam...";
                Application.DoEvents();

                DataTable dt = null;

                switch (aktivniIzvjestaj)
                {
                    case IzvjestajTip.ProdajaPoPerodu: dt = GetProdajaPoPerioduData(); break;
                    case IzvjestajTip.ProdajaPoKupcu: dt = GetProdajaPoKupcuData(); break;
                    case IzvjestajTip.ProdajaPoArtiklu: dt = GetProdajaPoArtikluData(); break;
                    case IzvjestajTip.ProdajaPoProduavacu: dt = GetProdajaPoProduavacuData(); break;
                    case IzvjestajTip.DnevniPromet: dt = GetDnevniPrometData(); break;
                    case IzvjestajTip.NepodmireniRacuni: dt = GetNepodmireniRacuniData(); break;
                    case IzvjestajTip.PregledRacuna: dt = GetPregledRacunaData(); break;
                    case IzvjestajTip.PdvIzvjestaj: dt = GetPdvIzvjestajData(); break;
                    case IzvjestajTip.StanjeZaliha: dt = GetStanjeZalihaData(); break;
                    case IzvjestajTip.ArtikliIspodMinimuma: dt = GetArtikliIspodMinimumaData(); break;
                    case IzvjestajTip.KretanjeZaliha: dt = GetKretanjeZalihaData(); break;
                    case IzvjestajTip.PregledKalkulacija: dt = GetPregledKalkulacijaData(); break;
                    case IzvjestajTip.NabavaPoDobaavljacu: dt = GetNabavaPoDobaavljacuData(); break;
                    case IzvjestajTip.PregledPartnera: dt = GetPregledPartneraData(); break;
                    case IzvjestajTip.TopKupci: dt = GetTopKupciData(); break;
                    case IzvjestajTip.TopDobavljaci: dt = GetTopDobavljaciData(); break;
                    case IzvjestajTip.PregledPonuda: dt = GetPregledPonudaData(); break;
                    case IzvjestajTip.KonverzijaPonuda: dt = GetKonverzijaPonudaData(); break;
                    case IzvjestajTip.PregledOtpremnica: dt = GetPregledOtpremnicaData(); break;
                    case IzvjestajTip.NefakturiraneOtpremnice: dt = GetNefakturiraneOtpremnicaData(); break;
                }

                trenutniPodaci = dt;
                dgvResults.DataSource = dt;
                FormatGrid();

                int rowCount = dt?.Rows.Count ?? 0;
                lblUkupno.Text = $"Prikazano: {rowCount} zapisa  |  Period: {dtpOd.Value:dd.MM.yyyy} – {dtpDo.Value:dd.MM.yyyy}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri generiranju izvještaja:\n" + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGeneriraj.Enabled = true;
                btnGeneriraj.Text = "🔍  Generiraj";
            }
        }

        private void FormatGrid()
        {
            if (dgvResults.DataSource == null) return;

            foreach (DataGridViewColumn col in dgvResults.Columns)
            {
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                col.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);

                if (col.ValueType == typeof(decimal) || col.ValueType == typeof(double)
                    || col.ValueType == typeof(int))
                {
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    string n = col.Name.ToLower();
                    if (n.Contains("iznos") || n.Contains("vrijednost") ||
                        n.Contains("ukupno") || n.Contains("cijena") || n.Contains("pdv"))
                        col.DefaultCellStyle.Format = "N2";
                }
            }
        }

        // ── Export i print ────────────────────────────────────────────────────────

        /// <summary>Export u CSV koji se otvara u Excelu.</summary>
        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            if (trenutniPodaci == null || trenutniPodaci.Rows.Count == 0)
            {
                MessageBox.Show("Nema podataka za export. Generirajte izvještaj prvo.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV (Excel) datoteke (*.csv)|*.csv";
                sfd.FileName = $"Izvjestaj_{Sanitize(lblNaslovIzvjestaja.Text)}_{DateTime.Now:yyyyMMdd}";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    using (var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8))
                    {
                        // BOM za ispravno otvaranje u Excelu
                        sw.Write('\uFEFF');

                        var headers = dgvResults.Columns.Cast<DataGridViewColumn>()
                                          .Select(c => $"\"{c.HeaderText}\"");
                        sw.WriteLine(string.Join(";", headers));

                        foreach (DataGridViewRow row in dgvResults.Rows)
                        {
                            var cells = row.Cells.Cast<DataGridViewCell>()
                                            .Select(c => $"\"{c.Value}\"");
                            sw.WriteLine(string.Join(";", cells));
                        }
                    }

                    if (MessageBox.Show("Izvještaj je uspješno exportan.\n\nŽelite li otvoriti datoteku?",
                            "Uspjeh", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Greška pri exportu: " + ex.Message,
                        "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// PDF export – koristi IzvjestajiPdfHelper, identičan stil kao RacunPdfHelper.
        /// </summary>
        private void BtnExportPdf_Click(object sender, EventArgs e)
        {
            if (trenutniPodaci == null || trenutniPodaci.Rows.Count == 0)
            {
                MessageBox.Show("Nema podataka za export. Generirajte izvještaj prvo.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            IzvjestajiPdfHelper.SpremiPdf(
                naslovIzvjestaja: lblNaslovIzvjestaja.Text.Replace("📊  ", "").Trim(),
                opisIzvjestaja: lblOpisIzvjestaja.Text,
                datumOd: dtpOd.Value,
                datumDo: dtpDo.Value,
                podaci: trenutniPodaci);
        }

        /// <summary>
        /// Print – generira temp PDF i šalje na printer, identično RacunPdfHelper.IspisiRacun.
        /// </summary>
        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (trenutniPodaci == null || trenutniPodaci.Rows.Count == 0)
            {
                MessageBox.Show("Nema podataka za ispis. Generirajte izvještaj prvo.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string tempPdf = IzvjestajiPdfHelper.GenerirajPdfTemp(
                    naslovIzvjestaja: lblNaslovIzvjestaja.Text.Replace("📊  ", "").Trim(),
                    opisIzvjestaja: lblOpisIzvjestaja.Text,
                    datumOd: dtpOd.Value,
                    datumDo: dtpDo.Value,
                    podaci: trenutniPodaci);

                using (var dlg = new PrintDialog())
                {
                    dlg.Document = new System.Drawing.Printing.PrintDocument
                    { DocumentName = lblNaslovIzvjestaja.Text };

                    if (dlg.ShowDialog() == DialogResult.OK)
                        Process.Start(new ProcessStartInfo(tempPdf)
                        {
                            Verb = "print",
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri ispisu:\n" + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  SQL UPITI
        // ════════════════════════════════════════════════════════════════════

        private DataTable ExecuteQuery(string sql, SqlParameter[] parameters = null)
        {
            var dt = new DataTable();
            using (var conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    using (var da = new SqlDataAdapter(cmd))
                        da.Fill(dt);
                }
            }
            return dt;
        }

        private SqlParameter[] BaseParams()
        {
            return new[]
            {
                new SqlParameter("@od", dtpOd.Value.Date),
                new SqlParameter("@do", dtpDo.Value.Date.AddDays(1).AddSeconds(-1))
            };
        }

        // 1. Prodaja po periodu
        private DataTable GetProdajaPoPerioduData() => ExecuteQuery(@"
            SELECT
                FORMAT(r.datum_racuna,'MM.yyyy')              AS [Mjesec],
                COUNT(r.id)                                   AS [Broj računa],
                SUM(r.ukupno_bez_pdv)                         AS [Ukupno bez PDV],
                SUM(r.ukupno_pdv)                             AS [Iznos PDV],
                SUM(r.ukupno_sa_pdv)                          AS [Ukupno sa PDV],
                SUM(CASE WHEN r.placeno=1 THEN 1 ELSE 0 END)  AS [Plaćeno (kom)],
                SUM(CASE WHEN r.placeno=0 THEN 1 ELSE 0 END)  AS [Neplaćeno (kom)]
            FROM racuni r
            WHERE r.datum_racuna BETWEEN @od AND @do
            GROUP BY FORMAT(r.datum_racuna,'MM.yyyy')
            ORDER BY MIN(r.datum_racuna)", BaseParams());

        // 2. Prodaja po kupcu
        private DataTable GetProdajaPoKupcuData() => ExecuteQuery(@"
            SELECT
                pa.naziv                                      AS [Partner],
                pa.oib                                        AS [OIB],
                COUNT(r.id)                                   AS [Broj računa],
                SUM(r.ukupno_bez_pdv)                         AS [Ukupno bez PDV],
                SUM(r.ukupno_sa_pdv)                          AS [Ukupno sa PDV],
                SUM(CASE WHEN r.placeno=1 THEN 1 ELSE 0 END)  AS [Plaćeno (kom)],
                SUM(CASE WHEN r.placeno=0 THEN 1 ELSE 0 END)  AS [Neplaćeno (kom)]
            FROM racuni r
            INNER JOIN partneri pa ON pa.id = r.kupac_id
            WHERE r.datum_racuna BETWEEN @od AND @do
            GROUP BY pa.naziv, pa.oib
            ORDER BY SUM(r.ukupno_sa_pdv) DESC", BaseParams());

        // 3. Prodaja po artiklu
        private DataTable GetProdajaPoArtikluData() => ExecuteQuery(@"
            SELECT
                a.sifra                         AS [Šifra],
                a.naziv                         AS [Naziv artikla],
                ga.naziv                        AS [Grupa],
                SUM(rs.kolicina)                AS [Ukupna kol.],
                AVG(rs.prodajna_cijena)         AS [Prosj. cijena],
                SUM(rs.iznos_bez_pdv)           AS [Iznos bez PDV],
                SUM(rs.iznos_sa_pdv)            AS [Iznos sa PDV]
            FROM racun_stavke rs
            INNER JOIN racuni r            ON r.id  = rs.racun_id
            INNER JOIN artikli a           ON a.id  = rs.artikl_id
            LEFT  JOIN grupe_artikala ga   ON ga.id = a.grupa_id
            WHERE r.datum_racuna BETWEEN @od AND @do
            GROUP BY a.sifra, a.naziv, ga.naziv
            ORDER BY SUM(rs.iznos_sa_pdv) DESC", BaseParams());

        // 4. Prodaja po prodavaču
        private DataTable GetProdajaPoProduavacuData() => ExecuteQuery(@"
            SELECT
                pr.ime + ' ' + pr.prezime   AS [Prodavač],
                COUNT(r.id)                 AS [Broj računa],
                SUM(r.ukupno_bez_pdv)       AS [Ukupno bez PDV],
                SUM(r.ukupno_sa_pdv)        AS [Ukupno sa PDV]
            FROM racuni r
            INNER JOIN prodavaci pr ON pr.id = r.prodavac_id
            WHERE r.datum_racuna BETWEEN @od AND @do
            GROUP BY pr.ime, pr.prezime
            ORDER BY SUM(r.ukupno_sa_pdv) DESC", BaseParams());

        // 5. Dnevni promet
        private DataTable GetDnevniPrometData() => ExecuteQuery(@"
            SELECT
                CONVERT(varchar,r.datum_racuna,104)  AS [Datum],
                COUNT(r.id)                          AS [Broj računa],
                SUM(r.ukupno_bez_pdv)                AS [Bez PDV],
                SUM(r.ukupno_pdv)                    AS [PDV],
                SUM(r.ukupno_sa_pdv)                 AS [Sa PDV]
            FROM racuni r
            WHERE r.datum_racuna BETWEEN @od AND @do
            GROUP BY CONVERT(varchar,r.datum_racuna,104), CAST(r.datum_racuna AS date)
            ORDER BY CAST(r.datum_racuna AS date)", BaseParams());

        // 6. Nepodmireni računi
        private DataTable GetNepodmireniRacuniData() => ExecuteQuery(@"
            SELECT
                r.broj_racuna                                AS [Broj računa],
                CONVERT(varchar,r.datum_racuna,104)          AS [Datum],
                pa.naziv                                     AS [Kupac],
                r.ukupno_sa_pdv                              AS [Ukupno],
                DATEDIFF(day, r.datum_racuna, GETDATE())     AS [Dana starosti],
                r.status                                     AS [Status]
            FROM racuni r
            INNER JOIN partneri pa ON pa.id = r.kupac_id
            WHERE r.placeno = 0
              AND r.datum_racuna BETWEEN @od AND @do
            ORDER BY r.datum_racuna", BaseParams());

        // 7. Pregled računa
        private DataTable GetPregledRacunaData() => ExecuteQuery(@"
            SELECT
                r.broj_racuna                                AS [Broj računa],
                CONVERT(varchar,r.datum_racuna,104)          AS [Datum],
                pa.naziv                                     AS [Kupac],
                pr.ime + ' ' + pr.prezime                   AS [Prodavač],
                r.ukupno_bez_pdv                             AS [Bez PDV],
                r.ukupno_pdv                                 AS [PDV],
                r.ukupno_sa_pdv                              AS [Sa PDV],
                CASE WHEN r.placeno=1 THEN 'Da' ELSE 'Ne' END AS [Plaćeno],
                r.status                                     AS [Status]
            FROM racuni r
            INNER JOIN partneri pa  ON pa.id = r.kupac_id
            INNER JOIN prodavaci pr ON pr.id = r.prodavac_id
            WHERE r.datum_racuna BETWEEN @od AND @do
            ORDER BY r.datum_racuna DESC", BaseParams());

        // 8. PDV izvještaj
        private DataTable GetPdvIzvjestajData() => ExecuteQuery(@"
            SELECT
                FORMAT(r.datum_racuna,'MM.yyyy')  AS [Mjesec],
                rs.pdv_stopa                      AS [Stopa PDV (%)],
                SUM(rs.iznos_bez_pdv)             AS [Osnovica],
                SUM(rs.iznos_pdv)                 AS [Iznos PDV],
                SUM(rs.iznos_sa_pdv)              AS [Ukupno]
            FROM racun_stavke rs
            INNER JOIN racuni r ON r.id = rs.racun_id
            WHERE r.datum_racuna BETWEEN @od AND @do
            GROUP BY FORMAT(r.datum_racuna,'MM.yyyy'), rs.pdv_stopa,
                     YEAR(r.datum_racuna), MONTH(r.datum_racuna)
            ORDER BY YEAR(r.datum_racuna), MONTH(r.datum_racuna), rs.pdv_stopa", BaseParams());

        // 9. Stanje zaliha
        private DataTable GetStanjeZalihaData() => ExecuteQuery(@"
            SELECT
                a.sifra                          AS [Šifra],
                a.naziv                          AS [Naziv],
                ga.naziv                         AS [Grupa],
                jm.kratica                       AS [JM],
                a.kolicina                       AS [Zaliha],
                a.min_zaliha                     AS [Min. zaliha],
                a.nabavna_cijena                 AS [Nabavna cijena],
                a.prodajna_cijena                AS [Prodajna cijena],
                (a.kolicina * a.nabavna_cijena)  AS [Vrijednost zalihe]
            FROM artikli a
            LEFT JOIN grupe_artikala ga ON ga.id = a.grupa_id
            LEFT JOIN jedinice_mjere jm ON jm.id = a.jedinica_mjere_id
            WHERE a.aktivan = 1
            ORDER BY a.naziv");

        // 10. Artikli ispod minimuma
        private DataTable GetArtikliIspodMinimumaData() => ExecuteQuery(@"
            SELECT
                a.sifra                        AS [Šifra],
                a.naziv                        AS [Naziv],
                ga.naziv                       AS [Grupa],
                jm.kratica                     AS [JM],
                a.kolicina                     AS [Zaliha],
                a.min_zaliha                   AS [Min. zaliha],
                (a.min_zaliha - a.kolicina)    AS [Nedostaje]
            FROM artikli a
            LEFT JOIN grupe_artikala ga ON ga.id = a.grupa_id
            LEFT JOIN jedinice_mjere jm ON jm.id = a.jedinica_mjere_id
            WHERE a.aktivan = 1 AND a.kolicina < a.min_zaliha
            ORDER BY (a.min_zaliha - a.kolicina) DESC");

        // 11. Kretanje zaliha
        private DataTable GetKretanjeZalihaData()
        {
            int artId = (cmbArtikl.SelectedItem is ComboItem ai) ? ai.Id : 0;
            return ExecuteQuery(@"
                SELECT [Datum],[Dokument],[Opis],[Kolicina],[Smjer]
                FROM (
                    SELECT
                        CONVERT(varchar,k.datum_kalkulacije,104) AS [Datum],
                        k.broj_kalkulacije                       AS [Dokument],
                        'Kalkulacija – ulaz'                     AS [Opis],
                        ks.kolicina                              AS [Kolicina],
                        'Ulaz (+)'                               AS [Smjer]
                    FROM kalkulacija_stavke ks
                    INNER JOIN kalkulacije k ON k.id = ks.kalkulacija_id
                    WHERE ks.artikl_id = COALESCE(NULLIF(@artikl_id,0), ks.artikl_id)
                      AND k.datum_kalkulacije BETWEEN @od AND @do
                    UNION ALL
                    SELECT
                        CONVERT(varchar,o.datum_isporuke,104),
                        o.broj_otpremnice,
                        'Otpremnica – izlaz',
                        os.kolicina,
                        'Izlaz (-)'
                    FROM otpremnica_stavke os
                    INNER JOIN otpremnice o ON o.id = os.otpremnica_id
                    WHERE os.artikl_id = COALESCE(NULLIF(@artikl_id,0), os.artikl_id)
                      AND o.datum_isporuke BETWEEN @od AND @do
                ) t
                ORDER BY [Datum]",
                new SqlParameter[]
                {
                    new SqlParameter("@od",       dtpOd.Value.Date),
                    new SqlParameter("@do",       dtpDo.Value.Date.AddDays(1).AddSeconds(-1)),
                    new SqlParameter("@artikl_id", artId)
                });
        }

        // 12. Pregled kalkulacija
        private DataTable GetPregledKalkulacijaData() => ExecuteQuery(@"
            SELECT
                k.broj_kalkulacije                        AS [Broj],
                CONVERT(varchar,k.datum_kalkulacije,104)  AS [Datum],
                pa.naziv                                  AS [Dobavljač],
                k.broj_dobavljacevog_racuna               AS [Br. fakture dobavljača],
                k.ukupno_bez_pdv                          AS [Bez PDV],
                k.ukupno_pdv                              AS [PDV],
                k.ukupno_sa_pdv                           AS [Sa PDV],
                CASE WHEN k.proknjizeno=1 THEN 'Da' ELSE 'Ne' END AS [Proknjiženo]
            FROM kalkulacije k
            INNER JOIN partneri pa ON pa.id = k.dobavljac_id
            WHERE k.datum_kalkulacije BETWEEN @od AND @do
            ORDER BY k.datum_kalkulacije DESC", BaseParams());

        // 13. Nabava po dobavljaču
        private DataTable GetNabavaPoDobaavljacuData() => ExecuteQuery(@"
            SELECT
                pa.naziv                AS [Dobavljač],
                COUNT(k.id)             AS [Broj kalkulacija],
                SUM(k.ukupno_bez_pdv)   AS [Ukupno bez PDV],
                SUM(k.ukupno_pdv)       AS [PDV],
                SUM(k.ukupno_sa_pdv)    AS [Ukupno sa PDV]
            FROM kalkulacije k
            INNER JOIN partneri pa ON pa.id = k.dobavljac_id
            WHERE k.datum_kalkulacije BETWEEN @od AND @do
            GROUP BY pa.naziv
            ORDER BY SUM(k.ukupno_sa_pdv) DESC", BaseParams());

        // 14. Pregled partnera
        private DataTable GetPregledPartneraData() => ExecuteQuery(@"
            SELECT
                pa.naziv          AS [Naziv],
                pa.oib            AS [OIB],
                pa.grad           AS [Grad],
                pa.telefon        AS [Telefon],
                pa.email          AS [Email],
                pa.kontakt_osoba  AS [Kontakt osoba],
                CASE WHEN pa.aktivan=1 THEN 'Aktivan' ELSE 'Neaktivan' END AS [Status]
            FROM partneri pa
            ORDER BY pa.naziv");

        // 15. Top kupci
        private DataTable GetTopKupciData() => ExecuteQuery(@"
            SELECT TOP 10
                pa.naziv                                      AS [Kupac],
                COUNT(r.id)                                   AS [Br. računa],
                SUM(r.ukupno_sa_pdv)                          AS [Ukupno promet],
                SUM(CASE WHEN r.placeno=1 THEN 1 ELSE 0 END)  AS [Plaćeno (kom)],
                SUM(CASE WHEN r.placeno=0 THEN 1 ELSE 0 END)  AS [Neplaćeno (kom)]
            FROM racuni r
            INNER JOIN partneri pa ON pa.id = r.kupac_id
            WHERE r.datum_racuna BETWEEN @od AND @do
            GROUP BY pa.naziv
            ORDER BY SUM(r.ukupno_sa_pdv) DESC", BaseParams());

        // 16. Top dobavljači
        private DataTable GetTopDobavljaciData() => ExecuteQuery(@"
            SELECT TOP 10
                pa.naziv                AS [Dobavljač],
                COUNT(k.id)             AS [Br. kalkulacija],
                SUM(k.ukupno_sa_pdv)    AS [Ukupna nabava]
            FROM kalkulacije k
            INNER JOIN partneri pa ON pa.id = k.dobavljac_id
            WHERE k.datum_kalkulacije BETWEEN @od AND @do
            GROUP BY pa.naziv
            ORDER BY SUM(k.ukupno_sa_pdv) DESC", BaseParams());

        // 17. Pregled ponuda
        private DataTable GetPregledPonudaData() => ExecuteQuery(@"
            SELECT
                po.broj_ponude                          AS [Broj ponude],
                CONVERT(varchar,po.datum_ponude,104)    AS [Datum],
                pa.naziv                                AS [Kupac],
                po.ukupno_sa_pdv                        AS [Ukupno sa PDV],
                po.status                               AS [Status],
                CONVERT(varchar,po.rok_isporuke,104)    AS [Rok isporuke],
                CONVERT(varchar,po.datum_slanja,104)    AS [Datum slanja],
                CONVERT(varchar,po.datum_odgovora,104)  AS [Datum odgovora]
            FROM ponude po
            INNER JOIN partneri pa ON pa.id = po.kupac_id
            WHERE po.datum_ponude BETWEEN @od AND @do
            ORDER BY po.datum_ponude DESC", BaseParams());

        // 18. Konverzija ponuda
        private DataTable GetKonverzijaPonudaData() => ExecuteQuery(@"
            SELECT
                po.status        AS [Status],
                COUNT(po.id)     AS [Broj ponuda],
                SUM(po.ukupno_sa_pdv) AS [Ukupna vrijednost],
                CAST(COUNT(po.id)*100.0 /
                     NULLIF(SUM(COUNT(po.id)) OVER(), 0)
                     AS decimal(5,2))  AS [Udio (%)]
            FROM ponude po
            WHERE po.datum_ponude BETWEEN @od AND @do
            GROUP BY po.status", BaseParams());

        // 19. Pregled otpremnica
        private DataTable GetPregledOtpremnicaData() => ExecuteQuery(@"
            SELECT
                o.broj_otpremnice                          AS [Broj otpremnice],
                CONVERT(varchar,o.datum_isporuke,104)      AS [Datum isporuke],
                pa.naziv                                   AS [Kupac],
                r.broj_racuna                              AS [Račun],
                o.ukupno_vrijednost                        AS [Ukupno],
                CASE WHEN o.fakturirano=1 THEN 'Da' ELSE 'Ne' END AS [Fakturirano],
                o.status                                   AS [Status]
            FROM otpremnice o
            INNER JOIN partneri pa ON pa.id = o.kupac_id
            LEFT  JOIN racuni r    ON r.id  = o.racun_id
            WHERE o.datum_isporuke BETWEEN @od AND @do
            ORDER BY o.datum_isporuke DESC", BaseParams());

        // 20. Nefakturirane otpremnice
        private DataTable GetNefakturiraneOtpremnicaData() => ExecuteQuery(@"
            SELECT
                o.broj_otpremnice                         AS [Broj otpremnice],
                CONVERT(varchar,o.datum_isporuke,104)     AS [Datum isporuke],
                pa.naziv                                  AS [Kupac],
                o.ukupno_vrijednost                       AS [Ukupno],
                DATEDIFF(day, o.datum_isporuke, GETDATE()) AS [Dana čekanja]
            FROM otpremnice o
            INNER JOIN partneri pa ON pa.id = o.kupac_id
            WHERE (o.fakturirano = 0 OR o.fakturirano IS NULL)
              AND o.datum_isporuke BETWEEN @od AND @do
            ORDER BY o.datum_isporuke", BaseParams());

        // ── Utility ───────────────────────────────────────────────────────────────
        private static string Sanitize(string s)
            => s?.Replace("/", "-").Replace("\\", "-").Replace(":", "-")
                 .Replace(" ", "_").Replace("*", "").Replace("📊", "").Trim() ?? "izvjestaj";
    }

    // ── Pomoćne klase ─────────────────────────────────────────────────────────
    public enum IzvjestajTip
    {
        ProdajaPoPerodu, ProdajaPoKupcu, ProdajaPoArtiklu, ProdajaPoProduavacu, DnevniPromet,
        NepodmireniRacuni, PregledRacuna, PdvIzvjestaj,
        StanjeZaliha, ArtikliIspodMinimuma, KretanjeZaliha,
        PregledKalkulacija, NabavaPoDobaavljacu,
        PregledPartnera, TopKupci, TopDobavljaci,
        PregledPonuda, KonverzijaPonuda,
        PregledOtpremnica, NefakturiraneOtpremnice
    }

    public class IzvjestajItem
    {
        public string Naziv { get; }
        public string Opis { get; }
        public IzvjestajTip Tip { get; }
        public IzvjestajItem(string naziv, string opis, IzvjestajTip tip)
        { Naziv = naziv; Opis = opis; Tip = tip; }
    }

    public class IzvjestajKategorija
    {
        public string Naziv { get; }
        public List<IzvjestajItem> Izvjestaji { get; }
        public IzvjestajKategorija(string naziv, List<IzvjestajItem> stavke)
        { Naziv = naziv; Izvjestaji = stavke; }
    }

    public class ComboItem
    {
        public int Id { get; }
        public string Label { get; }
        public ComboItem(int id, string label) { Id = id; Label = label; }
        public override string ToString() => Label;
    }
}