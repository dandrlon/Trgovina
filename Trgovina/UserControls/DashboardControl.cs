using Guna.UI2.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;
using Trgovina.Utils;

namespace Trgovina.UserControls
{
    public partial class DashboardControl : UserControl
    {
        public DashboardControl()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppColors.Background;
            this.Padding = new Padding(20);

            CreateDashboardCards();
        }

        private void CreateDashboardCards()
        {
            // Stat cards na vrhu
            int xPos = 280;
            int yPos = 50;

            AddStatCard("Ukupna prodaja", "45.250,00 €", "📈 +12.5%", Color.FromArgb(46, 213, 115), xPos, yPos);
            xPos += 280;
            AddStatCard("Računi danas", "23", "📦 +5", Color.FromArgb(52, 152, 219), xPos, yPos);
            xPos += 280;
            AddStatCard("Vrijednost robe na skladištu", "143.450,00 €", "📊 -3.000 €", Color.FromArgb(155, 89, 182), xPos, yPos);
            xPos += 280;
            //AddStatCard("Aktivni partneri", "89", "👥 +2", Color.FromArgb(241, 196, 15), xPos, yPos);

            // Grafikoni i tabele ispod
            yPos = 220;
            CreateRecentOrdersPanel(220, yPos);
            CreateQuickActionsPanel(700, yPos);
        }

        private void AddStatCard(string title, string value, string change, Color accentColor, int x, int y)
        {
            Guna2Panel card = new Guna2Panel();
            card.Size = new Size(260, 140);
            card.Location = new Point(x, y);
            card.FillColor = AppColors.CardBackground;
            card.BorderRadius = 12;
            card.ShadowDecoration.Enabled = true;
            card.ShadowDecoration.Depth = 10;
            card.ShadowDecoration.Color = Color.FromArgb(30, 0, 0, 0);

            // Accent line na vrhu
            Guna2Panel accentLine = new Guna2Panel();
            accentLine.Size = new Size(260, 4);
            accentLine.Location = new Point(0, 0);
            accentLine.FillColor = accentColor;
            card.Controls.Add(accentLine);

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.Font = AppFonts.Regular;
            lblTitle.ForeColor = AppColors.TextSecondary;
            lblTitle.Location = new Point(15, 20);
            lblTitle.AutoSize = true;
            card.Controls.Add(lblTitle);

            // Value
            Label lblValue = new Label();
            lblValue.Text = value;
            lblValue.Font = AppFonts.TitleLarge;
            lblValue.ForeColor = AppColors.TextPrimary;
            lblValue.Location = new Point(15, 50);
            lblValue.AutoSize = true;
            card.Controls.Add(lblValue);

            // Change indicator
            Label lblChange = new Label();
            lblChange.Text = change;
            lblChange.Font = AppFonts.Regular;
            lblChange.ForeColor = accentColor;
            lblChange.Location = new Point(15, 100);
            lblChange.AutoSize = true;
            card.Controls.Add(lblChange);

            this.Controls.Add(card);
        }

        private void CreateRecentOrdersPanel(int x, int y)
        {
            Guna2Panel panel = new Guna2Panel();
            panel.Size = new Size(460, 350);
            panel.Location = new Point(x, y);
            panel.FillColor = AppColors.CardBackground;
            panel.BorderRadius = 12;
            panel.ShadowDecoration.Enabled = true;
            panel.ShadowDecoration.Depth = 10;

            Label lblTitle = new Label();
            lblTitle.Text = "Nedavni računi";
            lblTitle.Font = AppFonts.TitleSmall;
            lblTitle.ForeColor = AppColors.TextPrimary;
            lblTitle.Location = new Point(10, 20);
            lblTitle.AutoSize = true;
            panel.Controls.Add(lblTitle);

            // DataGridView za narudžbe
            Guna2DataGridView dgv = new Guna2DataGridView();
            dgv.Size = new Size(380, 320);
            dgv.Location = new Point(10, 60);
            dgv.BackgroundColor = AppColors.CardBackground;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.GridColor = AppColors.BorderLight;
            dgv.DefaultCellStyle.BackColor = AppColors.CardBackground;
            dgv.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = AppColors.PrimaryLight;
            dgv.DefaultCellStyle.SelectionForeColor = AppColors.TextWhite;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Secondary;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = AppColors.TextWhite;
            dgv.ColumnHeadersHeight = 40;
            dgv.RowTemplate.Height = 35;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Sample data
            dgv.Columns.Add("Broj", "Broj");
            dgv.Columns.Add("Partner", "Partner");
            dgv.Columns.Add("Iznos", "Iznos");
            dgv.Columns.Add("Status", "Status");

            dgv.Rows.Add("R-2024-001", "ABC d.o.o.", "1.250,00 KM", "Plaćeno");
            dgv.Rows.Add("R-2024-002", "XYZ trgovina", "890,00 KM", "Na čekanju");
            dgv.Rows.Add("R-2024-003", "Test firma", "2.100,00 KM", "Plaćeno");

            panel.Controls.Add(dgv);
            this.Controls.Add(panel);
        }

        private void CreateQuickActionsPanel(int x, int y)
        {
            Guna2Panel panel = new Guna2Panel();
            panel.Size = new Size(460, 350);
            panel.Location = new Point(x, y);
            panel.FillColor = AppColors.CardBackground;
            panel.BorderRadius = 12;
            panel.ShadowDecoration.Enabled = true;
            panel.ShadowDecoration.Depth = 10;

            Label lblTitle = new Label();
            lblTitle.Text = "Brze akcije";
            lblTitle.Font = AppFonts.TitleSmall;
            lblTitle.ForeColor = AppColors.TextPrimary;
            lblTitle.Location = new Point(20, 20);
            lblTitle.AutoSize = true;
            panel.Controls.Add(lblTitle);

            // Action buttons
            int btnY = 70;
            AddActionButton(panel, "➕ Nova narudžba", btnY);
            btnY += 70;
            AddActionButton(panel, "📦 Dodaj artikl", btnY);
            btnY += 70;
            AddActionButton(panel, "👤 Novi partner", btnY);
            btnY += 70;
            AddActionButton(panel, "📊 Generiraj izvještaj", btnY);

            this.Controls.Add(panel);
        }

        private void AddActionButton(Guna2Panel parent, string text, int y)
        {
            Guna2Button btn = new Guna2Button();
            btn.Text = text;
            btn.Size = new Size(400, 50);
            btn.Location = new Point(20, y);
            btn.FillColor = AppColors.Primary;
            btn.HoverState.FillColor = AppColors.PrimaryLight;
            btn.Font = AppFonts.RegularMedium;
            btn.ForeColor = AppColors.TextWhite;
            btn.BorderRadius = 8;
            btn.TextAlign = HorizontalAlignment.Left;
            btn.Cursor = Cursors.Hand;
            parent.Controls.Add(btn);
        }
    }
}