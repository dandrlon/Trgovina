using Guna.UI2.WinForms;
using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Trgovina.Base;
using Trgovina.Forms;
using Trgovina.UserControls;
using Trgovina.Utils;
using static Guna.UI2.WinForms.Suite.Descriptions;

namespace Trgovina
{
    public partial class frmMain : frmBase
    {
        private Guna2Panel pnlSidebar;
        private Guna2Panel pnlContent;
        private Form activeForm;
        private Guna2Button activeButton;
        private UserControl activeUserControl;

        public frmMain()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            this.Size = new Size(1400, 800);
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = AppColors.Background;

            CreateTopBar();
          
            CreateContentPanel();
            CreateSidebar();
            // Učitaj dashboard na start
            LoadUserControl(new DashboardControl());

        }

        private void CreateTopBar()
        {
            Guna2Panel pnlTop = new Guna2Panel();
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 70;
            pnlTop.FillColor = AppColors.CardBackground;
            pnlTop.ShadowDecoration.Enabled = true;
            pnlTop.ShadowDecoration.Depth = 8;

            // Logo i naslov
            /*Label lblTitle = new Label();
            lblTitle.Text = "ERP SUSTAV";
            lblTitle.Font = AppFonts.TitleMedium;
            lblTitle.ForeColor = AppColors.Primary;
            lblTitle.Location = new Point(270, 25);
            lblTitle.AutoSize = true;*/

            // User info
            Label lblUser = new Label();
            lblUser.Text = "Dobrodošli, Korisnik"; // UserSession.ImePrezime
            lblUser.Font = AppFonts.RegularMedium;
            lblUser.ForeColor = AppColors.TextSecondary;
            lblUser.BackColor = AppColors.CardBackground;
            lblUser.Location = new Point(750, 25);
            lblUser.AutoSize = true;

            // Window control buttons
            Guna2ControlBox btnClose = new Guna2ControlBox();
            btnClose.Parent = this; 
            btnClose.ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.CloseBox;
            btnClose.Size = new Size(45, 35);
            btnClose.Location = new Point(1340, 17);
            btnClose.FillColor = AppColors.CardBackground;
            btnClose.IconColor = AppColors.TextSecondary;
            btnClose.HoverState.FillColor = AppColors.Danger;
            btnClose.HoverState.IconColor = Color.White;
            //btnClose.BorderRadius = 5;
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Cursor = Cursors.Hand;

            Guna2ControlBox btnMaximize = new Guna2ControlBox();
            btnMaximize.Parent = this;
            btnMaximize.ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MaximizeBox;
            btnMaximize.Size = new Size(45, 35);
            btnMaximize.Location = new Point(1290, 17);
            btnMaximize.FillColor = AppColors.CardBackground;
            btnMaximize.IconColor = AppColors.TextSecondary;
            btnMaximize.HoverState.FillColor = Color.Transparent;
            btnMaximize.HoverState.IconColor = AppColors.Primary;
            //btnMaximize.BorderRadius = 5;
            btnMaximize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMaximize.Cursor = Cursors.Hand;

            Guna2ControlBox btnMinimize = new Guna2ControlBox();
            btnMinimize.Parent = this;
            btnMinimize.ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MinimizeBox;
            btnMinimize.Size = new Size(45, 35);
            btnMinimize.Location = new Point(1240, 17);
            btnMinimize.FillColor = AppColors.CardBackground;
            btnMinimize.IconColor = AppColors.TextSecondary;
            btnMinimize.HoverState.FillColor = Color.Transparent;
            btnMinimize.HoverState.IconColor = AppColors.Primary;
            //btnMinimize.BorderRadius = 5;
            btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMinimize.Cursor = Cursors.Hand;

            //pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(lblUser);

            this.Controls.Add(btnMinimize);
            this.Controls.Add(btnMaximize);
            this.Controls.Add(btnClose);

            // Bring to front da budu vidljivi
            btnMinimize.BringToFront();
            btnMaximize.BringToFront();
            btnClose.BringToFront();

            // Omogući pomicanje forme klikom na top bar
            pnlTop.MouseDown += PnlTop_MouseDown;

            this.Controls.Add(pnlTop);
        }

        // Dodaj ove metode za drag funkcionalnost
        private Point mouseLocation;

        private void PnlTop_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseLocation = e.Location;
                ((Panel)sender).MouseMove += PnlTop_MouseMove;
                ((Panel)sender).MouseUp += PnlTop_MouseUp;
            }
        }

        private void PnlTop_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Location = new Point(
                    this.Location.X + (e.X - mouseLocation.X),
                    this.Location.Y + (e.Y - mouseLocation.Y)
                );
            }
        }

        private void PnlTop_MouseUp(object sender, MouseEventArgs e)
        {
            ((Panel)sender).MouseMove -= PnlTop_MouseMove;
            ((Panel)sender).MouseUp -= PnlTop_MouseUp;
        }

        private void CreateSidebar()
        {
            pnlSidebar = new Guna2Panel();
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 250;
            pnlSidebar.FillColor = AppColors.SidebarBackground;

            // ── Logo ──────────────────────────────────────────────────────────────
            Guna2Panel pnlLogo = new Guna2Panel();
            pnlLogo.Size = new Size(250, 70);
            pnlLogo.Location = new Point(0, 0);
            pnlLogo.FillColor = AppColors.SidebarBackground;

            Label lblLogo = new Label();
            lblLogo.Text = "📦 TRGOVINA";
            lblLogo.Font = AppFonts.TitleMedium;
            lblLogo.BackColor = AppColors.Primary;
            lblLogo.ForeColor = AppColors.TextWhite;
            lblLogo.Location = new Point(20, 22);
            lblLogo.AutoSize = true;
            pnlLogo.Controls.Add(lblLogo);
            pnlSidebar.Controls.Add(pnlLogo);

            // ── Navigacija ─────────────────────────────────────────────────────────
            int yPos = 85;

            var btnDashboard = AddNavButton("📊  Dashboard", yPos,
                () => LoadUserControl(new DashboardControl())); yPos += 50;
            var btnArtikli = AddNavButton("📦  Artikli", yPos,
                () => LoadUserControl(new ArtikliControl())); yPos += 50;
            var btnPartneri = AddNavButton("👥  Partneri", yPos,
                () => LoadUserControl(new PartneriControl())); yPos += 50;
            var btnRacuni = AddNavButton("🧾  Računi", yPos,
                () => LoadUserControl(new RacuniControl())); yPos += 50;
            var btnKalkulacije = AddNavButton("📥  Kalkulacije", yPos,
                () => LoadUserControl(new KalkulacijeControl())); yPos += 50;
            var btnPonude = AddNavButton("📋  Ponude", yPos,
                () => LoadUserControl(new PonudeControl())); yPos += 50;
            var btnOtpremnice = AddNavButton("📦  Otpremnice", yPos,
                () => LoadUserControl(new OtpremnicaControl())); yPos += 50;
            var btnIzvjestaji = AddNavButton("📊  Izvještaji", yPos,
                () => LoadUserControl(new IzvjestajiControl())); yPos += 80;

            // ── Separator: ŠIFARNICI ───────────────────────────────────────────────
            DodajSeparatorSidebar("ŠIFARNICI", yPos); yPos += 50;

            AddNavButton("🗂️  Grupe artikala", yPos,
                () => LoadUserControl(new GrupeArtikalaControl())); yPos += 50;
            AddNavButton("🧾  PDV stope", yPos,
                () => LoadUserControl(new PdvStopeControl())); yPos += 50;
            AddNavButton("📐  Jedinice mjere", yPos,
                () => LoadUserControl(new JediniceMjereControl())); yPos += 80;

            // ── Separator: ADMINISTRACIJA ──────────────────────────────────────────
            DodajSeparatorSidebar("ADMINISTRACIJA", yPos); yPos += 50;

            AddNavButton("⚙️  Postavke", yPos,
                () => LoadUserControl(new PostavkeControl())); yPos += 50;


            Guna2Button btnLogout = new Guna2Button();
            btnLogout.Text = "🚪  Odjava";
            btnLogout.Size = new Size(230, 46);
            btnLogout.Location = new Point(10, yPos);
            btnLogout.TextAlign = HorizontalAlignment.Left;
            btnLogout.FillColor = AppColors.Primary;
            btnLogout.HoverState.FillColor = AppColors.Danger;
            btnLogout.Font = AppFonts.Navigation;
            btnLogout.ForeColor = AppColors.TextWhite;
            btnLogout.BorderRadius = 8;
            btnLogout.Cursor = Cursors.Hand;
            btnLogout.Click += BtnLogout_Click;
            pnlSidebar.Controls.Add(btnLogout);

            SetActiveButton(btnDashboard);
            this.Controls.Add(pnlSidebar);
        }

        private void DodajSeparatorSidebar(string naslov, int y)
        {
            Panel sep = new Panel();
            sep.Size = new Size(210, 1);
            sep.Location = new Point(20, y);
            sep.BackColor = Color.FromArgb(60, 255, 255, 255);
            pnlSidebar.Controls.Add(sep);

            Label lbl = new Label();
            lbl.Text = naslov;
            lbl.Font = new Font(AppFonts.Regular.FontFamily, 8f, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(160, 255, 255, 255);
            lbl.Location = new Point(22, y + 6);
            lbl.AutoSize = true;
            lbl.BackColor = AppColors.Primary;
            pnlSidebar.Controls.Add(lbl);
        }

        private Guna2Button AddNavButton(string text, int yPos, Action onClick)
        {
            Guna2Button btn = new Guna2Button();
            btn.Text = text;
            btn.Size = new Size(230, 50);
            btn.Location = new Point(10, yPos);
            btn.TextAlign = HorizontalAlignment.Left;
            btn.FillColor = AppColors.Primary;
            btn.HoverState.FillColor = AppColors.PrimaryLight;
            btn.Font = AppFonts.Navigation;
            btn.ForeColor = AppColors.TextWhite;
            btn.BorderRadius = 8;
            btn.Cursor = Cursors.Hand;

            btn.Refresh();

            if (onClick != null)
            {
                btn.Click += (s, e) =>
                {
                    SetActiveButton(btn);
                    onClick();
                };
            }

            pnlSidebar.Controls.Add(btn);
            btn.BringToFront(); 
            return btn;
        }

        private void SetActiveButton(Guna2Button button)
        {
            if (activeButton != null)
            {
                activeButton.FillColor = AppColors.Primary;
            }

            activeButton = button;
            activeButton.FillColor = AppColors.Secondary;
        }

        private void CreateContentPanel()
        {
            pnlContent = new Guna2Panel();
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.FillColor = AppColors.Background;
            pnlContent.Padding = new Padding(0);
            this.Controls.Add(pnlContent);
        }

        private void LoadForm(Form form)
        {
            if (activeForm != null)
                activeForm.Close();

            activeForm = form;
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            pnlContent.Controls.Clear();
            pnlContent.Controls.Add(form);
            form.Show();
        }

        private void LoadUserControl(UserControl control)
        {
            // Ukloni stari control
            if (activeUserControl != null)
            {
                pnlContent.Controls.Remove(activeUserControl);
                activeUserControl.Dispose();
            }

            // Dodaj novi
            activeUserControl = control;
            control.Dock = DockStyle.Fill;
            pnlContent.Controls.Clear();
            pnlContent.Controls.Add(control);
            control.BringToFront();
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Jeste li sigurni da se želite odjaviti?",
                "Odjava",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.Close();
                frmLogin loginForm = new frmLogin();
                loginForm.Show();
            }
        }
    }
}