using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Utils;

namespace Trgovina.UserControls
{
    /// <summary>
    /// UserControl za konfiguraciju fiskalizacije:
    ///   - Podaci tvrtke (OIB, IBAN, adresa...)
    ///   - FINA certifikat (putanja, lozinka, test/produkcija)
    ///   - Postavke poslovnice i naplatnog uređaja
    ///   - eRačun middleware (API ključ, URL)
    ///   - Test veza s CIS-om
    /// </summary>
    public partial class PostavkeControl : UserControl
    {
        // ─── Tab paneli ────────────────────────────────────────────────────────────
        private Guna2Panel pnlTabs;
        private Guna2Panel pnlContent;
        private Guna2Button btnTabTvrtka, btnTabFiskal, btnTabEracun, btnTabInfo;
        private Guna2Button activeTab;

        // ─── Tab 1: Tvrtka ─────────────────────────────────────────────────────────
        private Guna2TextBox txtNazivTvrtke, txtAdresa, txtGrad, txtPbr;
        private Guna2TextBox txtOib, txtPdvId, txtTelefon, txtEmail, txtIban;
        private Guna2ToggleSwitch tglUSustavuPdv;

        // ─── Tab 2: Fiskalizacija 1.0 ──────────────────────────────────────────────
        private Guna2TextBox txtCertPutanja, txtCertLozinka;
        private Guna2TextBox txtOibOperatera, txtOznakaPoslovnice, txtOznakaNaplatnog;
        private Guna2ToggleSwitch tglTestOkolina;
        private Label lblTestOkolinaInfo;
        private Guna2Button btnOdaberiCert, btnTestCis;
        private Label lblTestRezultat;

        // ─── Tab 3: eRačun ─────────────────────────────────────────────────────────
        private Guna2TextBox txtEracunApiKljuc, txtEracunApiUrl;
        private Guna2ToggleSwitch tglEracunAktivan;
        private Label lblEracunInfo;

        // ══════════════════════════════════════════════════════════════════════════
        //  KONSTRUKTOR
        // ══════════════════════════════════════════════════════════════════════════

        public PostavkeControl()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            KreirajUI();
            UcitajPostavke();
            this.BackColor = AppColors.Background;

            // Forsiraj ispravne dimenzije odmah
            if (pnlContent != null && this.Height > 0)
                pnlContent.Height = this.Height - 140;
            if (pnlTabs != null && this.Width > 0)
                pnlTabs.Width = this.Width - 40;
        }


        // ══════════════════════════════════════════════════════════════════════════
        //  GRADNJA UI
        // ══════════════════════════════════════════════════════════════════════════

        private void KreirajUI()
        {
            // ── Naslov ──────────────────────────────────────────────────────────────
            Label lblNaslov = new Label();
            lblNaslov.Text = "⚙️  Postavke fiskalizacije";
            lblNaslov.Font = AppFonts.TitleSmall;
            lblNaslov.ForeColor = AppColors.TextPrimary;
            lblNaslov.Location = new Point(20, 56);
            lblNaslov.AutoSize = true;
            this.Controls.Add(lblNaslov);

            Label lblOpis = new Label();
            lblOpis.Text = "Konfiguracija FINA certifikata, poslovnice i eRačun servisa";
            lblOpis.Font = AppFonts.Regular;
            lblOpis.ForeColor = AppColors.TextSecondary;
            lblOpis.Location = new Point(22, 84);
            lblOpis.AutoSize = true;
            this.Controls.Add(lblOpis);

            Panel sepH = new Panel();
            sepH.Height = 1; sepH.Location = new Point(20, 104);
            sepH.BackColor = AppColors.BorderLight;
            sepH.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(sepH);
            this.SizeChanged += (s, e) => sepH.Width = this.Width - 40;
            sepH.Width = 1200;

            // ── Tab traka ───────────────────────────────────────────────────────────
            pnlTabs = new Guna2Panel();
            pnlTabs.Location = new Point(20, 122);
            pnlTabs.Height = 46;
            pnlTabs.FillColor = AppColors.CardBackground;
            pnlTabs.BorderRadius = 8;
            pnlTabs.ShadowDecoration.Enabled = true;
            pnlTabs.ShadowDecoration.Depth = 4;
            pnlTabs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(pnlTabs);
            this.SizeChanged += (s, e) => pnlTabs.Width = this.Width - 40;
            pnlTabs.Width = 1200;

            btnTabTvrtka = KreirajTabGumb("🏢  Podaci tvrtke", 8);
            btnTabFiskal = KreirajTabGumb("📡  Fiskalizacija 1.0 (B2C)", 206);
            //btnTabEracun = KreirajTabGumb("📧  eRačun 2.0 (B2B)", 404);
            btnTabInfo = KreirajTabGumb("ℹ️  Upute", 404);

            btnTabTvrtka.Click += (s, e) => PrikaziTab(1);
            btnTabFiskal.Click += (s, e) => PrikaziTab(2);
            //btnTabEracun.Click += (s, e) => PrikaziTab(3);
            btnTabInfo.Click += (s, e) => PrikaziTab(4);

            // ── Content panel ───────────────────────────────────────────────────────
            pnlContent = new Guna2Panel();
            pnlContent.Location = new Point(20, 124);
            pnlContent.FillColor = AppColors.CardBackground;
            pnlContent.BorderRadius = 10;
            pnlContent.ShadowDecoration.Enabled = true;
            pnlContent.ShadowDecoration.Depth = 6;
            pnlContent.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                                 AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(pnlContent);
            this.SizeChanged += (s, e) =>
            {
                pnlContent.Width = this.Width - 40;
                pnlContent.Height = this.Height - 140;
            };
            pnlContent.Width = 1200;
            pnlContent.Height = 600;

            // Prikaži prvi tab
            PrikaziTab(1);
        }

        private Guna2Button KreirajTabGumb(string tekst, int x)
        {
            var btn = new Guna2Button();
            btn.Text = tekst;
            btn.Size = new Size(190, 34);
            btn.Location = new Point(x, 5);
            btn.FillColor = Color.Transparent;
            btn.ForeColor = AppColors.TextSecondary;
            btn.HoverState.FillColor = Color.FromArgb(245, 246, 252);
            btn.Font = AppFonts.Regular;
            btn.BorderRadius = 6;
            btn.Cursor = Cursors.Hand;
            pnlTabs.Controls.Add(btn);
            return btn;
        }

        private void PrikaziTab(int tab)
        {
            // Reset svih tab gumbi
            foreach (var btn in new[] { btnTabTvrtka, btnTabFiskal, btnTabInfo })
            {
                btn.FillColor = Color.Transparent;
                btn.ForeColor = AppColors.TextSecondary;
                btn.Font = AppFonts.Regular;
            }

            // Aktivni tab
            Guna2Button aktivni = tab == 1 ? btnTabTvrtka :
                                  tab == 2 ? btnTabFiskal : btnTabInfo;
            aktivni.FillColor = AppColors.Primary;
            aktivni.ForeColor = Color.White;
            aktivni.Font = AppFonts.RegularMedium;
            activeTab = aktivni;

            pnlContent.Controls.Clear();

            switch (tab)
            {
                case 1: KreirajTabTvrtka(); break;
                case 2: KreirajTabFiskal(); break;
                case 3: KreirajTabEracun(); break;
                case 4: KreirajTabInfo(); break;
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  TAB 1: PODACI TVRTKE
        // ══════════════════════════════════════════════════════════════════════════

        private void KreirajTabTvrtka()
        {
            int y = 30, x1 = 20, x2 = 360, x3 = 700;
            int inW = 310, inH = 34;

            DodajSekciju(pnlContent, "Osnovni podaci tvrtke", x1, y); y += 30;

            DodajLabel(pnlContent, "Naziv tvrtke *", x1, y);
            txtNazivTvrtke = DodajTextBox(pnlContent, x1, y + 16, 680, "npr. Moja Tvrtka d.o.o.");
            y += 75;

            DodajLabel(pnlContent, "OIB *", x1, y);
            DodajLabel(pnlContent, "PDV ID", x2, y);
            DodajLabel(pnlContent, "IBAN *", x3, y);
            //y -= 5;

            txtOib = DodajTextBox(pnlContent, x1, y, inW, "12345678901");
            txtPdvId = DodajTextBox(pnlContent, x2, y, inW, "HR12345678901");
            txtIban = DodajTextBox(pnlContent, x3, y, inW, "HR12...");
            y += 75;

            DodajLabel(pnlContent, "Adresa *", x1, y);
            DodajLabel(pnlContent, "Poštanski broj", x2, y);
            DodajLabel(pnlContent, "Grad *", x3, y);
            y -= 5;

            txtAdresa = DodajTextBox(pnlContent, x1, y, inW, "Ulica i broj");
            txtPbr = DodajTextBox(pnlContent, x2, y, inW, "10000");
            txtGrad = DodajTextBox(pnlContent, x3, y, inW, "Zagreb");
            y += 75;

            DodajLabel(pnlContent, "Telefon", x1, y);
            DodajLabel(pnlContent, "Email", x2, y);
            y -= 16;

            txtTelefon = DodajTextBox(pnlContent, x1, y, inW, "+385 1 234 5678");
            txtEmail = DodajTextBox(pnlContent, x2, y, inW, "info@tvrtka.hr");
            y += 80;

            DodajSekciju(pnlContent, "PDV", x1, y); y += 30;

            tglUSustavuPdv = new Guna2ToggleSwitch();
            tglUSustavuPdv.Size = new Size(52, 26);
            tglUSustavuPdv.Location = new Point(x1, y);
            tglUSustavuPdv.Checked = true;
            tglUSustavuPdv.CheckedState.FillColor = AppColors.Success;
            tglUSustavuPdv.UncheckedState.FillColor = AppColors.BorderLight;
            pnlContent.Controls.Add(tglUSustavuPdv);

            Label lblPdvInfo = new Label();
            lblPdvInfo.Text = "Obveznik PDV-a (utječe na sadržaj XML poruke prema CIS-u)";
            lblPdvInfo.Font = AppFonts.Regular;
            lblPdvInfo.ForeColor = AppColors.TextSecondary;
            lblPdvInfo.Location = new Point(x1 + 60, y + 4);
            lblPdvInfo.AutoSize = true;
            pnlContent.Controls.Add(lblPdvInfo);

            y += 50;
            DodajGumbSpremi(pnlContent, x1, y, SpremiBtnTvrtka_Click);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  TAB 2: FISKALIZACIJA 1.0
        // ══════════════════════════════════════════════════════════════════════════

        private void KreirajTabFiskal()
        {
            int y = 40, x1 = 20, x2 = 420, x3 = 660;
            int inW = 360;

            DodajSekciju(pnlContent, "FINA aplikativni certifikat", x1, y); y += 30;

            // Info box
            Panel pnlInfo = new Panel();
            pnlInfo.Location = new Point(x1, y);
            pnlInfo.Size = new Size(900, 52);
            pnlInfo.BackColor = Color.FromArgb(235, 245, 251);
            pnlContent.Controls.Add(pnlInfo);

            Label lblInfoTekst = new Label();
            lblInfoTekst.Text =
                "💡  FINA aplikativni certifikat za fiskalizaciju nabavlja se na FINA šalterima ili online.\n" +
                "    Certifikat dolazi kao .p12 datoteka s lozinkom. Godišnja naknada ~100 €.";
            lblInfoTekst.Font = AppFonts.Regular;
            lblInfoTekst.ForeColor = Color.FromArgb(52, 73, 94);
            lblInfoTekst.Location = new Point(8, 8);
            lblInfoTekst.AutoSize = true;
            pnlInfo.Controls.Add(lblInfoTekst);
            y += 62;

            DodajLabel(pnlContent, "Putanja do .p12 certifikata *", x1, y + 5);
            y += 16;

            txtCertPutanja = DodajTextBox(pnlContent, x1, y - 5, inW + 60, @"C:\Certifikati\fiskal.p12");
            txtCertPutanja.ReadOnly = false;

            btnOdaberiCert = new Guna2Button();
            btnOdaberiCert.Text = "📂  Odaberi";
            btnOdaberiCert.Size = new Size(110, 34);
            btnOdaberiCert.Location = new Point(x1 + inW + 68, y + 15);
            btnOdaberiCert.FillColor = AppColors.Secondary;
            btnOdaberiCert.HoverState.FillColor = AppColors.Primary;
            btnOdaberiCert.Font = AppFonts.Regular;
            btnOdaberiCert.ForeColor = Color.White;
            btnOdaberiCert.BorderRadius = 8;
            btnOdaberiCert.Cursor = Cursors.Hand;
            btnOdaberiCert.Click += BtnOdaberiCert_Click;
            pnlContent.Controls.Add(btnOdaberiCert);
            y += 70;

            DodajLabel(pnlContent, "Lozinka certifikata *", x1, y);
            y -= 5;

            txtCertLozinka = DodajTextBox(pnlContent, x1, y, 250, "");
            txtCertLozinka.PasswordChar = '●';
            y += 70;

            DodajSekciju(pnlContent, "Podaci poslovnice i operatera", x1, y); y += 30;

            DodajLabel(pnlContent, "OIB operatera (ako se razlikuje od OIB tvrtke)", x1, y);
            y -= 16;
            txtOibOperatera = DodajTextBox(pnlContent, x1, y, 200, "ostavite prazno = OIB tvrtke");
            y += 90;

            DodajLabel(pnlContent, "Oznaka poslovnog prostora *", x1, y);
            DodajLabel(pnlContent, "Oznaka naplatnog uređaja *", x2, y);
            y -= 20;

            txtOznakaPoslovnice = DodajTextBox(pnlContent, x1, y, 120, "1");
            txtOznakaNaplatnog = DodajTextBox(pnlContent, x2, y, 120, "1");
            y += 100;

            DodajSekciju(pnlContent, "Okolina", x1, y); y += 30;

            tglTestOkolina = new Guna2ToggleSwitch();
            tglTestOkolina.Size = new Size(52, 26);
            tglTestOkolina.Location = new Point(x1, y);
            tglTestOkolina.Checked = true;
            tglTestOkolina.CheckedState.FillColor = Color.FromArgb(243, 156, 18);
            tglTestOkolina.UncheckedState.FillColor = AppColors.Success;
            pnlContent.Controls.Add(tglTestOkolina);

            lblTestOkolinaInfo = new Label();
            lblTestOkolinaInfo.Font = AppFonts.RegularMedium;
            lblTestOkolinaInfo.Location = new Point(x1 + 62, y + 3);
            lblTestOkolinaInfo.AutoSize = true;
            pnlContent.Controls.Add(lblTestOkolinaInfo);
            AzurirajTestLabel();

            tglTestOkolina.CheckedChanged += (s, e) => AzurirajTestLabel();
            y += 50;

            // Gumb za test veze
            btnTestCis = new Guna2Button();
            btnTestCis.Text = "🔌  Testiraj vezu s CIS-om";
            btnTestCis.Size = new Size(210, 38);
            btnTestCis.Location = new Point(x1, y);
            btnTestCis.FillColor = Color.FromArgb(52, 152, 219);
            btnTestCis.HoverState.FillColor = Color.FromArgb(41, 128, 185);
            btnTestCis.Font = AppFonts.Regular;
            btnTestCis.ForeColor = Color.White;
            btnTestCis.BorderRadius = 8;
            btnTestCis.Cursor = Cursors.Hand;
            btnTestCis.Click += BtnTestCis_Click;
            pnlContent.Controls.Add(btnTestCis);

            lblTestRezultat = new Label();
            lblTestRezultat.Text = "";
            lblTestRezultat.Font = AppFonts.Regular;
            lblTestRezultat.ForeColor = AppColors.TextSecondary;
            lblTestRezultat.Location = new Point(x1 + 220, y + 9);
            lblTestRezultat.AutoSize = true;
            pnlContent.Controls.Add(lblTestRezultat);

            y += 55;
            DodajGumbSpremi(pnlContent, x1, y, SpremiBtnFiskal_Click);
        }

        private void AzurirajTestLabel()
        {
            if (lblTestOkolinaInfo == null) return;
            if (tglTestOkolina.Checked)
            {
                lblTestOkolinaInfo.Text = "🟡 TEST okolina (cistest.apis-it.hr) — ne šalje stvarne podatke";
                lblTestOkolinaInfo.ForeColor = Color.FromArgb(243, 156, 18);
            }
            else
            {
                lblTestOkolinaInfo.Text = "🟢 PRODUKCIJA (cis.porezna-uprava.hr) — STVARNO slanje!";
                lblTestOkolinaInfo.ForeColor = AppColors.Success;
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  TAB 3: eRACUN (Fiskalizacija 2.0)
        // ══════════════════════════════════════════════════════════════════════════

        private void KreirajTabEracun()
        {
            int y = 20, x1 = 20;
            int inW = 500;

            DodajSekciju(pnlContent, "eRačun — Fiskalizacija 2.0 (B2B)", x1, y); y += 50;

            // Upozorenje
            Panel pnlWarn = new Panel();
            pnlWarn.Location = new Point(x1, y);
            pnlWarn.Size = new Size(900, 120);
            pnlWarn.BackColor = Color.FromArgb(253, 243, 227);
            pnlContent.Controls.Add(pnlWarn);

            Label lblWarnTekst = new Label();
            lblWarnTekst.Text =
                "⚠️  Fiskalizacija 2.0 obvezna je od 1.1.2026. za sve PDV obveznike.\n\n" +
                "    Za slanje eRačuna potreban je posrednik (middleware) koji podržava UBL 2.1 XML\n" +
                "    i Peppol AS4 protokol. Preporučujemo: Moj-eRačun, Mikroe, EDICOM ili FINA eRačun REST API.\n" +
                "    Unesite API ključ koji dobijete od odabranog posrednika.";
            lblWarnTekst.Font = AppFonts.Regular;
            lblWarnTekst.ForeColor = Color.FromArgb(120, 66, 18);
            lblWarnTekst.Location = new Point(10, 10);
            lblWarnTekst.Size = new Size(880, 100);
            pnlWarn.Controls.Add(lblWarnTekst);
            y += 92;

            // Toggle aktivan
            tglEracunAktivan = new Guna2ToggleSwitch();
            tglEracunAktivan.Size = new Size(52, 26);
            tglEracunAktivan.Location = new Point(x1, y);
            tglEracunAktivan.Checked = false;
            tglEracunAktivan.CheckedState.FillColor = AppColors.Success;
            tglEracunAktivan.UncheckedState.FillColor = AppColors.BorderLight;
            pnlContent.Controls.Add(tglEracunAktivan);

            lblEracunInfo = new Label();
            lblEracunInfo.Text = "eRačun integracija aktivna";
            lblEracunInfo.Font = AppFonts.Regular;
            lblEracunInfo.ForeColor = AppColors.TextSecondary;
            lblEracunInfo.Location = new Point(x1 + 62, y + 4);
            lblEracunInfo.AutoSize = true;
            pnlContent.Controls.Add(lblEracunInfo);
            y += 50;

            DodajLabel(pnlContent, "API URL posrednika", x1, y);
            y -= 5;
            txtEracunApiUrl = DodajTextBox(pnlContent, x1, y, inW,
                "https://api.moj-eracun.hr/v1");
            y += 75;

            DodajLabel(pnlContent, "API ključ (Bearer token)", x1, y);
            y -= 16;
            txtEracunApiKljuc = DodajTextBox(pnlContent, x1, y, inW, "");
            txtEracunApiKljuc.PasswordChar = '●';
            y += 80;

            // Preporučeni posrednici
            DodajSekciju(pnlContent, "Preporučeni posrednici za eRačun", x1, y); y += 30;

            var posrednici = new[]
            {
                ("Moj-eRačun",  "www.moj-eracun.hr",     "HR rješenje, Peppol certified"),
                ("Mikroe",      "www.mikroe.hr",          "Desktop/cloud, domaća podrška"),
                ("FINA eRačun", "www.fina.hr/eracun",     "Državna platforma, B2G i B2B"),
                ("EDICOM",      "edicomgroup.com",         "Međunarodna mreža, AS4 certified"),
            };

            foreach (var (naziv, url, opis) in posrednici)
            {
                Label lbl = new Label();
                lbl.Text = $"• {naziv} — {url}  ({opis})";
                lbl.Font = AppFonts.Regular;
                lbl.ForeColor = AppColors.TextSecondary;
                lbl.Location = new Point(x1 + 10, y);
                lbl.AutoSize = true;
                pnlContent.Controls.Add(lbl);
                y += 20;
            }

            y += 10;
            DodajGumbSpremi(pnlContent, x1, y, SpremiBtnEracun_Click);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  TAB 4: UPUTE
        // ══════════════════════════════════════════════════════════════════════════

        private void KreirajTabInfo()
        {
            int y = 40, x1 = 20;

            DodajSekciju(pnlContent, "Zakonski okvir i upute za implementaciju", x1, y); y += 30;

            var upute = new[]
            {
                ("📋 Zakon", "Zakon o fiskalizaciji (NN 89/2025) — na snazi od 1.9.2025."),
                ("🏪 B2C Fisk. 1.0",
                    "Svi računi potrošačima (gotovina, kartica, virman od 1.1.2026.) moraju dobiti\n" +
                    "    JIR od CIS sustava Porezne uprave. Potreban: FINA fiskal certifikat."),
                ("🏢 B2B Fisk. 2.0",
                    "Od 1.1.2026. svi PDV obveznici moraju izdavati eRačune u UBL 2.1 formatu\n" +
                    "    kroz Peppol mrežu (FINA/Moj-eRačun). Od 1.1.2027. i ostali obveznici."),
                ("🔑 ZKI",
                    "Zaštitni kod izdavatelja — izračunava se lokalno RSA potpisom + MD5.\n" +
                    "    Ispisuje se na računu i može biti osnova za naknadnu fiskalizaciju."),
                ("📌 JIR",
                    "Jedinstveni identifikator računa — UUID koji vraća CIS nakon prihvata.\n" +
                    "    Obvezan je na računu uz ZKI i QR kod (od 2026.)."),
                ("⏰ Offline fallback",
                    "Ako CIS nije dostupan, smijete izdati račun s YZ-om, ali JIR morate\n" +
                    "    dostaviti u roku 48 sati od uspostave veze."),
                ("💰 Kazne",
                    "Neusklađenost: 2.650 € – 66.000 € (tvrtka), 265 € – 6.650 € (fizička osoba).\n" +
                    "    Za eRačun: 1.330 € – 13.300 € (tvrtka), 130 € – 1.330 € (fizička osoba)."),
            };

            foreach (var (naslov, tekst) in upute)
            {
                Label lblNaslov = new Label();
                lblNaslov.Text = naslov;
                lblNaslov.Font = AppFonts.RegularMedium;
                lblNaslov.ForeColor = AppColors.Primary;
                lblNaslov.Location = new Point(x1, y);
                lblNaslov.AutoSize = true;
                pnlContent.Controls.Add(lblNaslov);
                y += 20;

                Label lblTekst = new Label();
                lblTekst.Text = "    " + tekst;
                lblTekst.Font = AppFonts.Regular;
                lblTekst.ForeColor = AppColors.TextPrimary;
                lblTekst.Location = new Point(x1, y);
                lblTekst.Size = new Size(900, 40);
                pnlContent.Controls.Add(lblTekst);
                y += 46;
            }

            y += 10;
            // Link na Poreznu upravu
            Label lblLink = new Label();
            lblLink.Text = "🔗  Stranica porezne uprave: https://porezna-uprava.gov.hr";
            lblLink.Font = AppFonts.Regular;
            lblLink.ForeColor = Color.FromArgb(52, 152, 219);
            lblLink.Location = new Point(x1, y);
            lblLink.AutoSize = true;
            lblLink.Cursor = Cursors.Hand;
            lblLink.Click += (s, e) =>
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "https://porezna-uprava.gov.hr")
                { UseShellExecute = true });
            pnlContent.Controls.Add(lblLink);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  EVENTI
        // ══════════════════════════════════════════════════════════════════════════

        private void BtnOdaberiCert_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Odaberi FINA fiskal certifikat";
                dlg.Filter = "PKCS#12 certifikat (*.pfx;*.p12)|*.pfx;*.p12|Sve datoteke|*.*";
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtCertPutanja.Text = dlg.FileName;
                    txtCertLozinka.Focus();
                }
            }
        }

        private async void BtnTestCis_Click(object sender, EventArgs e)
        {
            lblTestRezultat.Text = "⏳ Testiranje...";
            lblTestRezultat.ForeColor = AppColors.TextSecondary;
            btnTestCis.Enabled = false;

            try
            {
                // Provjeri postoji li certifikat
                string certPath = txtCertPutanja.Text.Trim();
                if (string.IsNullOrWhiteSpace(certPath) || !File.Exists(certPath))
                {
                    lblTestRezultat.Text = "❌ Certifikat nije pronađen na navedenoj putanji.";
                    lblTestRezultat.ForeColor = AppColors.Danger;
                    return;
                }

                // Pokušaj učitati certifikat
                System.Security.Cryptography.X509Certificates.X509Certificate2 cert;
                try
                {
                    cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                        certPath, txtCertLozinka.Text,
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.MachineKeySet);
                }
                catch
                {
                    lblTestRezultat.Text = "❌ Pogrešna lozinka certifikata ili oštećena datoteka.";
                    lblTestRezultat.ForeColor = AppColors.Danger;
                    return;
                }

                // ECHO poziv na CIS
                bool testOkolina = tglTestOkolina?.Checked ?? true;
                string endpoint = testOkolina
                    ? "https://cistest.apis-it.hr:8449/FiskalizacijaServiceTest"
                    : "https://cis.porezna-uprava.hr:8449/FiskalizacijaService";

                string echoPayload = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
                  <soapenv:Body>
                    <tns:EchoRequest xmlns:tns=""http://www.apis-it.hr/fin/2012/types/f73"">test-trgovina</tns:EchoRequest>
                  </soapenv:Body>
                </soapenv:Envelope>";

                System.Net.ServicePointManager.ServerCertificateValidationCallback = (s, c, ch, err) => true;
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(endpoint);
                request.Method = "POST";
                request.ContentType = "text/xml; charset=UTF-8";
                request.Headers.Add("SOAPAction",
                    "http://e-porezna.porezna-uprava.hr/fiskalizacija/2012/services/FiskalizacijaService/echo");
                request.Timeout = 8000;
                request.ClientCertificates.Add(cert);

                byte[] data = System.Text.Encoding.UTF8.GetBytes(echoPayload);
                request.ContentLength = data.Length;
                using (var stream = await request.GetRequestStreamAsync())
                    await stream.WriteAsync(data, 0, data.Length);

                using (var response = await request.GetResponseAsync())
                using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    string odgovor = await reader.ReadToEndAsync();
                    if (odgovor.Contains("test-trgovina"))
                    {
                        lblTestRezultat.Text = $"✅ Veza s CIS-om uspješna! ({(testOkolina ? "TEST" : "PRODUKCIJA")})";
                        lblTestRezultat.ForeColor = AppColors.Success;
                    }
                    else
                    {
                        lblTestRezultat.Text = "⚠️ CIS je odgovorio ali ECHO nije ispravan.";
                        lblTestRezultat.ForeColor = Color.FromArgb(243, 156, 18);
                    }
                }
            }
            catch (Exception ex)
            {
                lblTestRezultat.Text = $"❌ Greška: {ex.Message}";
                lblTestRezultat.ForeColor = AppColors.Danger;
            }
            finally
            {
                btnTestCis.Enabled = true;
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  UČITAVANJE I SPREMANJE POSTAVKI
        // ══════════════════════════════════════════════════════════════════════════

        private void UcitajPostavke()
        {
            try
            {
                var p = DatabaseHelper.GetSvePostavke();
                string Get(string k, string d = "") =>
                    p.ContainsKey(k) && !string.IsNullOrEmpty(p[k]) ? p[k] : d;

                // Tab 1 — Tvrtka
                if (txtNazivTvrtke != null) txtNazivTvrtke.Text = Get("NazivTvrtke");
                if (txtOib != null) txtOib.Text = Get("OIB");
                if (txtPdvId != null) txtPdvId.Text = Get("PdvId");
                if (txtIban != null) txtIban.Text = Get("IBAN");
                if (txtAdresa != null) txtAdresa.Text = Get("Adresa");
                if (txtPbr != null) txtPbr.Text = Get("PostanskiBroj");
                if (txtGrad != null) txtGrad.Text = Get("Grad");
                if (txtTelefon != null) txtTelefon.Text = Get("Telefon");
                if (txtEmail != null) txtEmail.Text = Get("Email");
                if (tglUSustavuPdv != null) tglUSustavuPdv.Checked = Get("USustavuPDV", "true") == "true";

                // Tab 2 — Fiskalizacija
                if (txtCertPutanja != null) txtCertPutanja.Text = Get("FiskalCertifikatPutanja");
                if (txtCertLozinka != null) txtCertLozinka.Text = Get("FiskalCertifikatLozinka");
                if (txtOibOperatera != null) txtOibOperatera.Text = Get("FiskalOibOperatera");
                if (txtOznakaPoslovnice != null) txtOznakaPoslovnice.Text = Get("FiskalOznakaPoslovnice", "1");
                if (txtOznakaNaplatnog != null) txtOznakaNaplatnog.Text = Get("FiskalOznakaNaplatnog", "1");
                if (tglTestOkolina != null) tglTestOkolina.Checked = Get("FiskalTestOkolina", "true") == "true";

                // Tab 3 — eRačun
                if (txtEracunApiUrl != null) txtEracunApiUrl.Text = Get("EracunApiUrl", "https://api.moj-eracun.hr/v1");
                if (txtEracunApiKljuc != null) txtEracunApiKljuc.Text = Get("EracunApiKljuc");
                if (tglEracunAktivan != null) tglEracunAktivan.Checked = Get("EracunAktivan", "false") == "true";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri učitavanju postavki:\n" + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SpremiBtnTvrtka_Click(object sender, EventArgs e)
        {
            try
            {
                var p = new Dictionary<string, string>
                {
                    ["NazivTvrtke"] = txtNazivTvrtke?.Text?.Trim() ?? "",
                    ["OIB"] = txtOib?.Text?.Trim() ?? "",
                    ["PdvId"] = txtPdvId?.Text?.Trim() ?? "",
                    ["IBAN"] = txtIban?.Text?.Trim() ?? "",
                    ["Adresa"] = txtAdresa?.Text?.Trim() ?? "",
                    ["PostanskiBroj"] = txtPbr?.Text?.Trim() ?? "",
                    ["Grad"] = txtGrad?.Text?.Trim() ?? "",
                    ["Telefon"] = txtTelefon?.Text?.Trim() ?? "",
                    ["Email"] = txtEmail?.Text?.Trim() ?? "",
                    ["USustavuPDV"] = (tglUSustavuPdv?.Checked ?? true) ? "true" : "false",
                };
                DatabaseHelper.SpremiPostavke(p);
                PrikaziUspjeh("✅ Podaci tvrtke su spremljeni.");
            }
            catch (Exception ex) { PrikaziGresku(ex.Message); }
        }

        private void SpremiBtnFiskal_Click(object sender, EventArgs e)
        {
            try
            {
                var p = new Dictionary<string, string>
                {
                    ["FiskalCertifikatPutanja"] = txtCertPutanja?.Text?.Trim() ?? "",
                    ["FiskalCertifikatLozinka"] = txtCertLozinka?.Text ?? "",
                    ["FiskalOibOperatera"] = txtOibOperatera?.Text?.Trim() ?? "",
                    ["FiskalOznakaPoslovnice"] = txtOznakaPoslovnice?.Text?.Trim() ?? "1",
                    ["FiskalOznakaNaplatnog"] = txtOznakaNaplatnog?.Text?.Trim() ?? "1",
                    ["FiskalTestOkolina"] = (tglTestOkolina?.Checked ?? true) ? "true" : "false",
                };
                DatabaseHelper.SpremiPostavke(p);
                PrikaziUspjeh("✅ Postavke fiskalizacije su spremljene.");
            }
            catch (Exception ex) { PrikaziGresku(ex.Message); }
        }

        private void SpremiBtnEracun_Click(object sender, EventArgs e)
        {
            try
            {
                var p = new Dictionary<string, string>
                {
                    ["EracunApiUrl"] = txtEracunApiUrl?.Text?.Trim() ?? "",
                    ["EracunApiKljuc"] = txtEracunApiKljuc?.Text ?? "",
                    ["EracunAktivan"] = (tglEracunAktivan?.Checked ?? false) ? "true" : "false",
                };
                DatabaseHelper.SpremiPostavke(p);
                PrikaziUspjeh("✅ Postavke eRačuna su spremljene.");
            }
            catch (Exception ex) { PrikaziGresku(ex.Message); }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  HELPER BUILDERI
        // ══════════════════════════════════════════════════════════════════════════

        private void DodajSekciju(Control parent, string tekst, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = tekst.ToUpper();
            lbl.Font = AppFonts.RegularMedium;
            lbl.ForeColor = AppColors.Primary;
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            parent.Controls.Add(lbl);

            Panel linija = new Panel();
            linija.Height = 1;
            linija.Location = new Point(x, y + 19);
            linija.BackColor = AppColors.BorderLight;
            linija.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            parent.Controls.Add(linija);
            parent.SizeChanged += (s, e) => linija.Width = parent.Width - x - 20;
            linija.Width = parent.Width > 0 ? parent.Width - x - 20 : 900;
        }

        private void DodajLabel(Control parent, string tekst, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = tekst;
            lbl.Font = AppFonts.Regular;
            lbl.ForeColor = AppColors.TextSecondary;
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            parent.Controls.Add(lbl);
        }

        private Guna2TextBox DodajTextBox(Control parent, int x, int y, int width, string placeholder = "")
        {
            var txt = new Guna2TextBox();
            txt.Size = new Size(width, 34);
            txt.Location = new Point(x, y);
            txt.FillColor = AppColors.Background;
            txt.BorderColor = AppColors.BorderLight;
            txt.Font = AppFonts.Regular;
            txt.ForeColor = AppColors.TextPrimary;
            txt.BorderRadius = 8;
            txt.PlaceholderText = placeholder;
            txt.Enter += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.Primary;
            txt.Leave += (s, e) => ((Guna2TextBox)s).BorderColor = AppColors.BorderLight;
            parent.Controls.Add(txt);
            return txt;
        }

        private void DodajGumbSpremi(Control parent, int x, int y, EventHandler handler)
        {
            var btn = new Guna2Button();
            btn.Text = "💾  Spremi postavke";
            btn.Size = new Size(180, 40);
            btn.Location = new Point(x, y);
            btn.FillColor = AppColors.Primary;
            btn.HoverState.FillColor = AppColors.PrimaryLight;
            btn.Font = AppFonts.RegularMedium;
            btn.ForeColor = Color.White;
            btn.BorderRadius = 8;
            btn.Cursor = Cursors.Hand;
            btn.Click += handler;
            parent.Controls.Add(btn);
        }

        private void PrikaziUspjeh(string poruka)
        {
            MessageBox.Show(poruka, "Uspjeh", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PrikaziGresku(string poruka)
        {
            MessageBox.Show("Greška pri spremanju:\n" + poruka,
                "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}