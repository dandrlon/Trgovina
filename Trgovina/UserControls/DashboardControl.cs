using Guna.UI2.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;
using Trgovina.Utils;

namespace Trgovina.UserControls
{
    public partial class DashboardControl : UserControl
    {
        private FlowLayoutPanel flowStats;
        private Panel pnlBottomContainer;
        private Guna2Panel pnlRecentOrders;
        private Guna2Panel pnlQuickActions;

        public DashboardControl()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Background;
            this.Padding = new Padding(15, 10, 15, 15);

            // Redoslijed: Fill prvo, Top odozdo prema gore
            KreirajDonjePanele();  // Fill
            KreirajStatKartice();  // Top
            KreirajHeader();       // Top (prikazuje se na vrhu)
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  HEADER
        // ══════════════════════════════════════════════════════════════════════════

        private void KreirajHeader()
        {
            Guna2Panel pnlHeader = new Guna2Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 52;
            pnlHeader.FillColor = AppColors.Background;

            Label lblNaslov = new Label();
            lblNaslov.Text = "📊  Dashboard";
            lblNaslov.Font = AppFonts.TitleLarge;
            lblNaslov.ForeColor = AppColors.TextPrimary;
            lblNaslov.Location = new Point(0, 2);
            lblNaslov.AutoSize = true;

            Label lblDatum = new Label();
            lblDatum.Text = DateTime.Now.ToString("dddd, dd. MMMM yyyy.");
            lblDatum.Font = AppFonts.Regular;
            lblDatum.ForeColor = AppColors.TextSecondary;
            lblDatum.AutoSize = true;
            lblDatum.Location = new Point(0, 30);

            pnlHeader.Controls.Add(lblNaslov);
            pnlHeader.Controls.Add(lblDatum);
            this.Controls.Add(pnlHeader);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  STAT KARTICE
        // ══════════════════════════════════════════════════════════════════════════

        private void KreirajStatKartice()
        {
            flowStats = new FlowLayoutPanel();
            flowStats.Dock = DockStyle.Top;
            flowStats.Height = 156; // 140 + 16 margin
            flowStats.FlowDirection = FlowDirection.LeftToRight;
            flowStats.WrapContents = true;
            flowStats.AutoScroll = false;
            flowStats.Padding = new Padding(0);
            flowStats.BackColor = AppColors.Background;
            flowStats.Resize += FlowStats_Resize;

            DodajStatKarticu("Ukupna prodaja", "45.250,00 €", "📈 +12.5%", Color.FromArgb(46, 213, 115));
            DodajStatKarticu("Računi danas", "23", "📦 +5", Color.FromArgb(52, 152, 219));
            DodajStatKarticu("Vrijednost robe", "143.450,00 €", "📊 -3.000 €", Color.FromArgb(155, 89, 182));
            DodajStatKarticu("Aktivni partneri", "89", "👥 +2", Color.FromArgb(241, 196, 15));

            this.Controls.Add(flowStats);
        }
        private void FlowStats_Resize(object sender, EventArgs e)
        {
            int gap = 0; // margin koji koristiš
            int totalGap = gap * 3; // 4 kartice -> 3 razmaka
            int cardWidth = (flowStats.ClientSize.Width - totalGap) / 4 - 12;

            foreach (Control ctrl in flowStats.Controls)
            {
                ctrl.Width = cardWidth;
            }
        }

        private void DodajStatKarticu(string naslov, string vrijednost, string promjena, Color boja)
        {
            Guna2Panel card = new Guna2Panel();
            card.Height = 140;
            card.Margin = new Padding(0, 0, 10, 0);
            card.FillColor = AppColors.CardBackground;
            card.BorderRadius = 12;
            card.ShadowDecoration.Enabled = true;
            card.ShadowDecoration.Depth = 8;
            card.ShadowDecoration.Color = Color.FromArgb(20, 0, 0, 0);

            // Accent bar na vrhu
            Guna2Panel accent = new Guna2Panel();
            accent.Dock = DockStyle.Top;
            accent.Height = 4;
            accent.Location = new Point(0, 0);
            accent.FillColor = boja;
            card.Controls.Add(accent);

            Label lblNaslov = new Label();
            lblNaslov.Text = naslov;
            lblNaslov.Font = AppFonts.Regular;
            lblNaslov.ForeColor = AppColors.TextSecondary;
            lblNaslov.Location = new Point(15, 18);
            lblNaslov.AutoSize = true;
            card.Controls.Add(lblNaslov);

            Label lblVrijednost = new Label();
            lblVrijednost.Text = vrijednost;
            lblVrijednost.Font = AppFonts.TitleLarge;
            lblVrijednost.ForeColor = AppColors.TextPrimary;
            lblVrijednost.Location = new Point(15, 48);
            lblVrijednost.AutoSize = true;
            card.Controls.Add(lblVrijednost);

            Label lblPromjena = new Label();
            lblPromjena.Text = promjena;
            lblPromjena.Font = AppFonts.Regular;
            lblPromjena.ForeColor = boja;
            lblPromjena.Location = new Point(15, 98);
            lblPromjena.AutoSize = true;
            card.Controls.Add(lblPromjena);

            flowStats.Controls.Add(card);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  DONJI PANELI (50% Nedavni računi | 50% Brze akcije)
        // ══════════════════════════════════════════════════════════════════════════

        private void KreirajDonjePanele()
        {
            pnlBottomContainer = new Panel();
            pnlBottomContainer.Dock = DockStyle.Fill;
            pnlBottomContainer.BackColor = AppColors.Background;
            pnlBottomContainer.Padding = new Padding(0, 8, 0, 0);

            pnlRecentOrders = KreirajNedavniRacuniPanel();
            pnlQuickActions = KreirajBrzeAkcijePanel();

            pnlBottomContainer.Controls.Add(pnlRecentOrders);
            pnlBottomContainer.Controls.Add(pnlQuickActions);

            // Pozicioniraj 50/50 na SizeChanged
            pnlBottomContainer.SizeChanged += PozicionirajDonjePanele;
            pnlBottomContainer.Resize += PozicionirajDonjePanele;

            this.Controls.Add(pnlBottomContainer);
        }

        private void PozicionirajDonjePanele(object sender, EventArgs e)
        {
            int containerW = pnlBottomContainer.ClientSize.Width;
            int containerH = pnlBottomContainer.ClientSize.Height - 8; // -8 zbog paddinga
            int gap = 12;
            int halfW = (containerW - gap) / 2;

            // Lijevi panel (Nedavni računi)
            pnlRecentOrders.Location = new Point(0, 8);
            pnlRecentOrders.Size = new Size(halfW, containerH);

            // Desni panel (Brze akcije)
            pnlQuickActions.Location = new Point(halfW + gap, 8);
            pnlQuickActions.Size = new Size(halfW, containerH);
        }

        // ──────────────────────────────────────────────────────────────────────────
        //  NEDAVNI RAČUNI
        // ──────────────────────────────────────────────────────────────────────────

        private Guna2Panel KreirajNedavniRacuniPanel()
        {
            Guna2Panel panel = new Guna2Panel();
            panel.FillColor = AppColors.CardBackground;
            panel.BorderRadius = 12;
            panel.ShadowDecoration.Enabled = true;
            panel.ShadowDecoration.Depth = 8;
            panel.ShadowDecoration.Color = Color.FromArgb(20, 0, 0, 0);
            panel.Padding = new Padding(15);

            Label lblNaslov = new Label();
            lblNaslov.Text = "Nedavni računi";
            lblNaslov.Font = AppFonts.TitleSmall;
            lblNaslov.ForeColor = AppColors.TextPrimary;
            lblNaslov.Dock = DockStyle.Top;
            lblNaslov.Height = 32;
            panel.Controls.Add(lblNaslov);

            Guna2DataGridView dgv = new Guna2DataGridView();
            dgv.Dock = DockStyle.Fill;
            dgv.BackgroundColor = AppColors.CardBackground;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.GridColor = Color.FromArgb(230, 232, 240);
            dgv.RowTemplate.Height = 34;
            dgv.ColumnHeadersHeight = 38;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.EnableHeadersVisualStyles = false;

            dgv.DefaultCellStyle.BackColor = AppColors.CardBackground;
            dgv.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.DefaultCellStyle.Font = AppFonts.Regular;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);

            dgv.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = AppFonts.RegularMedium;

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBroj", HeaderText = "Broj", FillWeight = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPartner", HeaderText = "Partner", FillWeight = 130 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIznos", HeaderText = "Iznos", FillWeight = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Status", FillWeight = 70 });

            dgv.Columns["colIznos"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgv.Columns["colStatus"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgv.Rows.Add("R-2024-001", "ABC d.o.o.", "1.250,00 €", "✔ Plaćeno");
            dgv.Rows.Add("R-2024-002", "XYZ trgovina", "890,00 €", "⏱ Čeka");
            dgv.Rows.Add("R-2024-003", "Test firma", "2.100,00 €", "✔ Plaćeno");
            dgv.Rows.Add("R-2024-004", "Klijent d.o.o.", "3.450,00 €", "✔ Plaćeno");
            dgv.Rows.Add("R-2024-005", "Nova firma", "650,00 €", "⏱ Čeka");

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["colStatus"].Value?.ToString().Contains("Plaćeno") == true)
                    row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(46, 213, 115);
                else
                    row.Cells["colStatus"].Style.ForeColor = Color.FromArgb(241, 196, 15);
            }

            panel.Controls.Add(dgv);
            return panel;
        }

        // ──────────────────────────────────────────────────────────────────────────
        //  BRZE AKCIJE
        // ──────────────────────────────────────────────────────────────────────────

        private Guna2Panel KreirajBrzeAkcijePanel()
        {
            Guna2Panel panel = new Guna2Panel();
            panel.FillColor = AppColors.CardBackground;
            panel.BorderRadius = 12;
            panel.ShadowDecoration.Enabled = true;
            panel.ShadowDecoration.Depth = 8;
            panel.ShadowDecoration.Color = Color.FromArgb(20, 0, 0, 0);
            panel.Padding = new Padding(15);

            Label lblNaslov = new Label();
            lblNaslov.Text = "Brze akcije";
            lblNaslov.Font = AppFonts.TitleSmall;
            lblNaslov.ForeColor = AppColors.TextPrimary;
            lblNaslov.Dock = DockStyle.Top;
            lblNaslov.Height = 32;
            panel.Controls.Add(lblNaslov);

            // Container za gumbe
            Panel pnlButtons = new Panel();
            pnlButtons.Dock = DockStyle.Fill;
            pnlButtons.BackColor = AppColors.CardBackground;
            pnlButtons.Padding = new Padding(0, 5, 0, 0);

            int y = 35;
            DodajAkcijskiGumb(pnlButtons, "➕  Nova narudžba", AppColors.Primary, 0, y); y += 58;
            DodajAkcijskiGumb(pnlButtons, "📦  Dodaj artikal", AppColors.Success, 0, y); y += 58;
            DodajAkcijskiGumb(pnlButtons, "👤  Novi partner", AppColors.Primary, 0, y); y += 58;
            DodajAkcijskiGumb(pnlButtons, "📊  Generiraj izvještaj", AppColors.Secondary, 0, y); y += 58;
            DodajAkcijskiGumb(pnlButtons, "⚙️  Postavke sustava", AppColors.Secondary, 0, y);

            panel.Controls.Add(pnlButtons);
            return panel;
        }

        private void DodajAkcijskiGumb(Panel parent, string text, Color boja, int x, int y)
        {
            Guna2Button btn = new Guna2Button();
            btn.Text = text;
            btn.Size = new Size(parent.ClientSize.Width - 5, 50);
            btn.Location = new Point(x, y);
            btn.FillColor = boja;
            btn.HoverState.FillColor = ControlPaint.Light(boja, 0.15f);
            btn.Font = AppFonts.RegularMedium;
            btn.ForeColor = AppColors.TextWhite;
            btn.BorderRadius = 8;
            btn.TextAlign = HorizontalAlignment.Left;
            btn.Cursor = Cursors.Hand;
            btn.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            parent.Controls.Add(btn);
        }
    }
}
