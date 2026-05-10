using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Trgovina.Data;
using Trgovina.Data.Models;
using Trgovina.Utils;

namespace Trgovina.UserControls
{
    public partial class DashboardControl : UserControl
    {
        // ── Polja ────────────────────────────────────────────────────────────────
        private readonly DashboardRepository _repo = new DashboardRepository(DatabaseHelper.ConnectionString);
        private readonly CultureInfo _hr = new CultureInfo("hr-HR");

        private readonly Label[] _lblVrijednosti = new Label[4];
        private Guna2DataGridView _dgvRacuni;
        private Chart _chartProdaja;

        private FlowLayoutPanel flowStats;
        private Panel pnlBottomContainer;
        private Guna2Panel pnlRecentOrders;
        private Guna2Panel pnlGrafikon;

        // ── Konstruktor ──────────────────────────────────────────────────────────
        public DashboardControl()
        {
            InitializeComponent();
            InitializeUI();
            UcitajPodatke();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  INICIJALIZACIJA UI
        // ══════════════════════════════════════════════════════════════════════════

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Background;
            this.Padding = new Padding(15, 10, 15, 15);

            KreirajDonjePanele();
            KreirajStatKartice();
            KreirajHeader();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  UČITAVANJE PODATAKA IZ BAZE
        // ══════════════════════════════════════════════════════════════════════════

        private void UcitajPodatke()
        {
            try
            {
                var stats = _repo.DohvatiStats();
                var racuni = _repo.DohvatiNedavneRacune();
                var prodaja = _repo.DohvatiProdajuPo30Dana();

                OsvjeziKartice(stats);
                OsvjeziDGV(racuni);
                OsvjeziGrafikon(prodaja);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri učitavanju dashboarda:\n" + ex.Message,
                                "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OsvjeziKartice(DashboardStats s)
        {
            _lblVrijednosti[0].Text = s.UkupnaProdajaMjesec.ToString("N2", _hr) + " €";
            _lblVrijednosti[1].Text = s.BrojRacunaDanas.ToString();
            _lblVrijednosti[2].Text = s.VrijednostRobe.ToString("N2", _hr) + " €";
            _lblVrijednosti[3].Text = s.AktivniPartneri.ToString();
        }

        private void OsvjeziDGV(List<NedavniRacunRow> racuni)
        {
            _dgvRacuni.Rows.Clear();
            foreach (var r in racuni)
            {
                bool placeno = r.Status == "PLAĆENO";
                int idx = _dgvRacuni.Rows.Add(
                    r.BrojRacuna,
                    r.Partner,
                    r.Iznos.ToString("N2", _hr) + " €",
                    placeno ? "✔ Plaćeno" : "⏱ Čeka"
                );
                _dgvRacuni.Rows[idx].Cells["colStatus"].Style.ForeColor =
                    placeno ? Color.FromArgb(46, 213, 115) : Color.FromArgb(241, 196, 15);
            }
        }

        private void OsvjeziGrafikon(Dictionary<DateTime, decimal> prodaja)
        {
            _chartProdaja.Series["Prodaja"].Points.Clear();

            decimal kumulativ = 0m;
            for (int i = 29; i >= 0; i--)
            {
                DateTime dan = DateTime.Today.AddDays(-i);
                decimal iznos = prodaja.ContainsKey(dan) ? prodaja[dan] : 0m;
                kumulativ += iznos;

                DataPoint point = new DataPoint();
                point.SetValueXY(dan, (double)kumulativ);
                point.AxisLabel = dan.ToString("dd.MM", _hr);
                _chartProdaja.Series["Prodaja"].Points.Add(point);
            }
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
            lblDatum.Text = DateTime.Now.ToString("dddd, dd. MMMM yyyy.", _hr);
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
            flowStats.Height = 156;
            flowStats.FlowDirection = FlowDirection.LeftToRight;
            flowStats.WrapContents = true;
            flowStats.AutoScroll = false;
            flowStats.Padding = new Padding(0);
            flowStats.BackColor = AppColors.Background;
            flowStats.Resize += FlowStats_Resize;

            DodajStatKarticu("Ukupna prodaja", "...", "📈", Color.FromArgb(46, 213, 115), 0);
            DodajStatKarticu("Računi danas", "...", "📦", Color.FromArgb(52, 152, 219), 1);
            DodajStatKarticu("Vrijednost robe", "...", "📊", Color.FromArgb(155, 89, 182), 2);
            DodajStatKarticu("Aktivni partneri", "...", "👥", Color.FromArgb(241, 196, 15), 3);

            this.Controls.Add(flowStats);
        }

        private void FlowStats_Resize(object sender, EventArgs e)
        {
            int cardWidth = flowStats.ClientSize.Width / 4 - 12;
            foreach (Control ctrl in flowStats.Controls)
                ctrl.Width = cardWidth;
        }

        private void DodajStatKarticu(string naslov, string vrijednost,
                                       string ikona, Color boja, int index)
        {
            Guna2Panel card = new Guna2Panel();
            card.Height = 140;
            card.Margin = new Padding(0, 0, 10, 0);
            card.FillColor = AppColors.CardBackground;
            card.BorderRadius = 12;
            card.ShadowDecoration.Enabled = true;
            card.ShadowDecoration.Depth = 8;
            card.ShadowDecoration.Color = Color.FromArgb(20, 0, 0, 0);

            Guna2Panel accent = new Guna2Panel();
            accent.Dock = DockStyle.Top;
            accent.Height = 4;
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
            _lblVrijednosti[index] = lblVrijednost;

            Label lblIkona = new Label();
            lblIkona.Text = ikona;
            lblIkona.Font = AppFonts.Regular;
            lblIkona.ForeColor = boja;
            lblIkona.Location = new Point(15, 98);
            lblIkona.AutoSize = true;
            card.Controls.Add(lblIkona);

            flowStats.Controls.Add(card);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  DONJI PANELI
        // ══════════════════════════════════════════════════════════════════════════

        private void KreirajDonjePanele()
        {
            pnlBottomContainer = new Panel();
            pnlBottomContainer.Dock = DockStyle.Fill;
            pnlBottomContainer.BackColor = AppColors.Background;
            pnlBottomContainer.Padding = new Padding(0, 8, 0, 0);

            pnlRecentOrders = KreirajNedavniRacuniPanel();
            pnlGrafikon = KreirajGrafikonPanel();

            pnlBottomContainer.Controls.Add(pnlRecentOrders);
            pnlBottomContainer.Controls.Add(pnlGrafikon);

            pnlBottomContainer.SizeChanged += PozicionirajDonjePanele;
            pnlBottomContainer.Resize += PozicionirajDonjePanele;

            this.Controls.Add(pnlBottomContainer);
        }

        private void PozicionirajDonjePanele(object sender, EventArgs e)
        {
            int containerW = pnlBottomContainer.ClientSize.Width;
            int containerH = pnlBottomContainer.ClientSize.Height - 8;

            // Guard — čekaj dok container ne dobije prave dimenzije
            if (containerW <= 0 || containerH <= 0) return;

            int gap = 12;
            int halfW = (containerW - gap) / 2;

            pnlRecentOrders.Location = new Point(0, 8);
            pnlRecentOrders.Size = new Size(halfW, containerH);

            pnlGrafikon.Location = new Point(halfW + gap, 8);
            pnlGrafikon.Size = new Size(halfW, containerH);
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

            _dgvRacuni = new Guna2DataGridView();
            _dgvRacuni.Dock = DockStyle.Fill;
            _dgvRacuni.BackgroundColor = AppColors.CardBackground;
            _dgvRacuni.BorderStyle = BorderStyle.None;
            _dgvRacuni.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            _dgvRacuni.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            _dgvRacuni.GridColor = Color.FromArgb(230, 232, 240);
            _dgvRacuni.RowTemplate.Height = 34;
            _dgvRacuni.ColumnHeadersHeight = 38;
            _dgvRacuni.AllowUserToAddRows = false;
            _dgvRacuni.AllowUserToDeleteRows = false;
            _dgvRacuni.ReadOnly = true;
            _dgvRacuni.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _dgvRacuni.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _dgvRacuni.EnableHeadersVisualStyles = false;

            _dgvRacuni.DefaultCellStyle.BackColor = AppColors.CardBackground;
            _dgvRacuni.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            _dgvRacuni.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            _dgvRacuni.DefaultCellStyle.SelectionForeColor = Color.White;
            _dgvRacuni.DefaultCellStyle.Font = AppFonts.Regular;
            _dgvRacuni.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);

            _dgvRacuni.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            _dgvRacuni.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _dgvRacuni.ColumnHeadersDefaultCellStyle.Font = AppFonts.RegularMedium;

            _dgvRacuni.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBroj", HeaderText = "Broj", FillWeight = 70 });
            _dgvRacuni.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPartner", HeaderText = "Partner", FillWeight = 130 });
            _dgvRacuni.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIznos", HeaderText = "Iznos", FillWeight = 80 });
            _dgvRacuni.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Status", FillWeight = 70 });

            _dgvRacuni.Columns["colIznos"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvRacuni.Columns["colStatus"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            panel.Controls.Add(_dgvRacuni);
            return panel;
        }

        // ──────────────────────────────────────────────────────────────────────────
        //  GRAFIKON PRODAJE
        // ──────────────────────────────────────────────────────────────────────────

        private Guna2Panel KreirajGrafikonPanel()
        {
            Guna2Panel panel = new Guna2Panel();
            panel.FillColor = AppColors.CardBackground;
            panel.BorderRadius = 12;
            panel.ShadowDecoration.Enabled = true;
            panel.ShadowDecoration.Depth = 8;
            panel.ShadowDecoration.Color = Color.FromArgb(20, 0, 0, 0);
            panel.Padding = new Padding(15);

            Label lblNaslov = new Label();
            lblNaslov.Text = "📈  Prodaja — zadnjih 30 dana";
            lblNaslov.Font = AppFonts.TitleSmall;
            lblNaslov.ForeColor = AppColors.TextPrimary;
            lblNaslov.Dock = DockStyle.Top;
            lblNaslov.Height = 32;
            panel.Controls.Add(lblNaslov);

            _chartProdaja = new Chart();
            _chartProdaja.Dock = DockStyle.Fill;
            _chartProdaja.BackColor = AppColors.CardBackground;
            _chartProdaja.BorderlineColor = Color.Transparent;
            _chartProdaja.BorderlineDashStyle = ChartDashStyle.NotSet;

            // Chart area
            ChartArea area = new ChartArea("Default");
            area.BackColor = AppColors.CardBackground;

            // X osa
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 7f);
            area.AxisX.LabelStyle.ForeColor = Color.FromArgb(120, 130, 150);
            area.AxisX.LineColor = Color.FromArgb(220, 225, 235);
            area.AxisX.MajorGrid.LineColor = Color.Transparent;
            area.AxisX.MajorTickMark.Enabled = false;
            area.AxisX.Interval = 5; // label svaki 5. dan

            // Y osa
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 7f);
            area.AxisY.LabelStyle.ForeColor = Color.FromArgb(120, 130, 150);
            area.AxisY.LineColor = Color.Transparent;
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(240, 242, 247);
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            area.AxisY.MajorTickMark.Enabled = false;
            area.AxisY.LabelStyle.Format = "#,0";

            area.InnerPlotPosition = new ElementPosition(8, 5, 90, 88);

            _chartProdaja.ChartAreas.Add(area);

            // Serija
            Series series = new Series("Prodaja");
            series.ChartType = SeriesChartType.Area;
            series.Color = Color.FromArgb(80, 52, 152, 219);       // fill proziran
            series.BorderColor = Color.FromArgb(52, 152, 219);     // linija
            series.BorderWidth = 2;
            series.XValueType = ChartValueType.DateTime;
            series.IsValueShownAsLabel = false;
            series.MarkerStyle = MarkerStyle.Circle;
            series.MarkerSize = 4;
            series.MarkerColor = Color.FromArgb(52, 152, 219);

            _chartProdaja.Series.Add(series);

            // Legend isključi
            _chartProdaja.Legends.Clear();

            panel.Controls.Add(_chartProdaja);
            return panel;
        }
    }
}