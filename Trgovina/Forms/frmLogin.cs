using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Trgovina.Data;
using Trgovina.Utils;

namespace Trgovina
{
    public partial class frmLogin : Form
    {
        private string connectionString = DatabaseHelper.ConnectionString;
        private Guna2TextBox txtKorisnickoIme;
        private Guna2TextBox txtLozinka;
        private Guna2Button btnPrijava;
        private Label lblError;
        private Point mouseLocation;

        public frmLogin()
        {
            InitializeComponent();
            InitializeLoginUI();
            this.KeyPreview = true;
            this.KeyDown += LoginForm_KeyDown;
        }

        private void InitializeLoginUI()
        {
            // Setup
            this.Size = new Size(1200, 700);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = AppColors.Background;

            // Glavni kontejner
            Guna2ShadowPanel shadowPanel = new Guna2ShadowPanel();
            shadowPanel.Size = new Size(900, 550);
            shadowPanel.Location = new Point(150, 75);
            shadowPanel.BackColor = Color.White;
            shadowPanel.ShadowColor = Color.Black;
            shadowPanel.ShadowDepth = 50;
            shadowPanel.ShadowShift = 8;

            // Lijevi panel - Branding
            Guna2Panel pnlLeft = new Guna2Panel();
            pnlLeft.Size = new Size(450, 550);
            pnlLeft.Location = new Point(0, 0);
            pnlLeft.FillColor = AppColors.Primary;

            //Samo dekoracija
            Guna2CircleButton circle1 = new Guna2CircleButton();
            circle1.Size = new Size(200, 200);
            circle1.Location = new Point(-50, -50);
            circle1.FillColor = Color.FromArgb(30, 255, 255, 255);
            circle1.BackColor = AppColors.Primary;
            circle1.DisabledState.FillColor = Color.FromArgb(30, 255, 255, 255);
            circle1.Enabled = false;

            Guna2CircleButton circle2 = new Guna2CircleButton();
            circle2.Size = new Size(150, 150);
            circle2.Location = new Point(320, 420);
            circle2.FillColor = Color.FromArgb(30, 255, 255, 255);
            circle2.BackColor = AppColors.Primary;
            circle2.DisabledState.FillColor = Color.FromArgb(30, 255, 255, 255);
            circle2.Enabled = false;

            // Ikona brenda - trgovina
            Label lblIcon = new Label();
            lblIcon.Text = "🏪";
            lblIcon.Font = new Font("Segoe UI", 80);
            lblIcon.ForeColor = Color.White;
            lblIcon.BackColor = AppColors.Primary;
            lblIcon.AutoSize = true;
            lblIcon.Location = new Point(125, 100);

            // Naziv aplikacije
            Label lblAppName = new Label();
            lblAppName.Text = "TRGOVINA";
            lblAppName.Font = new Font("Segoe UI", 28, FontStyle.Bold);
            lblAppName.ForeColor = Color.White;
            lblAppName.BackColor = AppColors.Primary;
            lblAppName.AutoSize = true;
            lblAppName.Location = new Point(115, 240);

            // Podnaslov
            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Sustav za upravljanje poslovanjem trgovine";
            lblSubtitle.Font = new Font("Segoe UI", 11);
            lblSubtitle.ForeColor = Color.FromArgb(220, 220, 220);
            lblSubtitle.BackColor = AppColors.Primary;
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(80, 295);

            // Verzija
            Label lblVersion = new Label();
            lblVersion.Text = "v1.0.0";
            lblVersion.Font = new Font("Segoe UI", 9);
            lblVersion.ForeColor = Color.FromArgb(180, 180, 180);
            lblVersion.BackColor = AppColors.Primary;
            lblVersion.AutoSize = true;
            lblVersion.Location = new Point(200, 510);

            pnlLeft.Controls.Add(circle1);
            pnlLeft.Controls.Add(circle2);
            pnlLeft.Controls.Add(lblIcon);
            pnlLeft.Controls.Add(lblAppName);
            pnlLeft.Controls.Add(lblSubtitle);
            pnlLeft.Controls.Add(lblVersion);

            // Desni panel - Login forma
            Guna2Panel pnlRight = new Guna2Panel();
            pnlRight.Size = new Size(450, 550);
            pnlRight.Location = new Point(450, 0);
            pnlRight.FillColor = Color.White;

            Label lblLoginTitle = new Label();
            lblLoginTitle.Text = "Prijava";
            lblLoginTitle.Font = new Font("Segoe UI", 26, FontStyle.Bold);
            lblLoginTitle.ForeColor = AppColors.Primary;
            lblLoginTitle.AutoSize = true;
            lblLoginTitle.Location = new Point(60, 20);

            Label lblWelcome = new Label();
            lblWelcome.Text = "Dobrodošli! Prijavite se na svoj račun.";
            lblWelcome.Font = new Font("Segoe UI", 10);
            lblWelcome.ForeColor = AppColors.TextSecondary;
            lblWelcome.AutoSize = true;
            lblWelcome.Location = new Point(60, 80);

            lblError = new Label();
            lblError.Text = "";
            lblError.Font = new Font("Segoe UI", 9);
            lblError.ForeColor = AppColors.Danger;
            lblError.AutoSize = true;
            lblError.MaximumSize = new Size(330, 0);
            lblError.Location = new Point(60, 120);
            lblError.Visible = false;

            Label lblUsernameLabel = new Label();
            lblUsernameLabel.Text = "KORISNIČKO IME";
            lblUsernameLabel.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblUsernameLabel.ForeColor = AppColors.TextSecondary;
            lblUsernameLabel.AutoSize = true;
            lblUsernameLabel.Location = new Point(60, 150);

            txtKorisnickoIme = new Guna2TextBox();
            txtKorisnickoIme.Size = new Size(330, 45);
            txtKorisnickoIme.Location = new Point(50, 130);
            txtKorisnickoIme.BorderRadius = 8;
            txtKorisnickoIme.BorderThickness = 2;
            txtKorisnickoIme.PlaceholderText = "Unesite korisničko ime";
            txtKorisnickoIme.Font = new Font("Segoe UI", 11);
            txtKorisnickoIme.BorderColor = AppColors.BorderLight;
            txtKorisnickoIme.FocusedState.BorderColor = AppColors.Accent;
            txtKorisnickoIme.IconLeft = null; 

            Label lblPasswordLabel = new Label();
            lblPasswordLabel.Text = "LOZINKA";
            lblPasswordLabel.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblPasswordLabel.ForeColor = AppColors.TextSecondary;
            lblPasswordLabel.AutoSize = true;
            lblPasswordLabel.Location = new Point(60, 260);

            txtLozinka = new Guna2TextBox();
            txtLozinka.Size = new Size(330, 45);
            txtLozinka.Location = new Point(50, 210);
            txtLozinka.BorderRadius = 8;
            txtLozinka.BorderThickness = 2;
            txtLozinka.PlaceholderText = "Unesite lozinku";
            txtLozinka.Font = new Font("Segoe UI", 11);
            txtLozinka.PasswordChar = '●';
            txtLozinka.BorderColor = AppColors.BorderLight;
            txtLozinka.FocusedState.BorderColor = AppColors.Accent;
            txtLozinka.IconLeft = null; // Properties.Resources.lock_icon

            Guna2CheckBox chkShowPassword = new Guna2CheckBox();
            chkShowPassword.Text = "Prikaži lozinku";
            chkShowPassword.Font = new Font("Segoe UI", 9);
            chkShowPassword.ForeColor = AppColors.TextSecondary;
            chkShowPassword.Location = new Point(60, 350);
            chkShowPassword.AutoSize = true;
            chkShowPassword.CheckedState.BorderColor = AppColors.Accent;
            chkShowPassword.CheckedState.FillColor = AppColors.Accent;
            chkShowPassword.UncheckedState.BorderColor = AppColors.BorderLight;
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                txtLozinka.PasswordChar = chkShowPassword.Checked ? '\0' : '●';
            };

            btnPrijava = new Guna2Button();
            btnPrijava.Text = "PRIJAVI SE";
            btnPrijava.Size = new Size(330, 50);
            btnPrijava.Location = new Point(60, 415);
            btnPrijava.BorderRadius = 8;
            btnPrijava.FillColor = AppColors.Accent;
            btnPrijava.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnPrijava.ForeColor = Color.White;
            btnPrijava.HoverState.FillColor = Color.FromArgb(0, 100, 220);
            btnPrijava.Cursor = Cursors.Hand;
            btnPrijava.Click += btnPrijava_Click;

            Label lblFooter = new Label();
            lblFooter.Text = "© 2026 Trgovina ERP. Sva prava pridržana.";
            lblFooter.Font = new Font("Segoe UI", 8);
            lblFooter.ForeColor = AppColors.TextSecondary;
            lblFooter.AutoSize = true;
            lblFooter.Location = new Point(95, 510);

            pnlRight.Controls.Add(lblLoginTitle);
            pnlRight.Controls.Add(lblWelcome);
            pnlRight.Controls.Add(lblError);
            pnlRight.Controls.Add(lblUsernameLabel);
            pnlRight.Controls.Add(txtKorisnickoIme);
            pnlRight.Controls.Add(lblPasswordLabel);
            pnlRight.Controls.Add(txtLozinka);
            pnlRight.Controls.Add(chkShowPassword);
            pnlRight.Controls.Add(btnPrijava);
            pnlRight.Controls.Add(lblFooter);

            shadowPanel.Controls.Add(pnlLeft);
            shadowPanel.Controls.Add(pnlRight);

            // Kontrole
            Guna2ControlBox btnClose = new Guna2ControlBox();
            btnClose.Parent = this;
            btnClose.ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.CloseBox;
            btnClose.Size = new Size(45, 45);
            btnClose.Location = new Point(1120, 15);
            btnClose.FillColor = Color.Transparent;
            btnClose.IconColor = AppColors.TextSecondary;
            btnClose.HoverState.FillColor = AppColors.Danger;
            btnClose.HoverState.IconColor = Color.White;
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Cursor = Cursors.Hand;

            Guna2ControlBox btnMinimize = new Guna2ControlBox();
            btnMinimize.Parent = this;
            btnMinimize.ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MinimizeBox;
            btnMinimize.Size = new Size(45, 45);
            btnMinimize.Location = new Point(1070, 15);
            btnMinimize.FillColor = Color.Transparent;
            btnMinimize.IconColor = AppColors.TextSecondary;
            btnMinimize.HoverState.FillColor = Color.White;
            btnMinimize.HoverState.IconColor = AppColors.Primary;
            btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMinimize.Cursor = Cursors.Hand;

            this.Controls.Add(shadowPanel);
            this.Controls.Add(btnClose);
            this.Controls.Add(btnMinimize);
            btnClose.BringToFront();
            btnMinimize.BringToFront();

            // Da se može forma micati kad se drzi mis na panelu
            pnlLeft.MouseDown += Panel_MouseDown;
            pnlRight.MouseDown += Panel_MouseDown;
            shadowPanel.MouseDown += Panel_MouseDown;
        }

        private void Panel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseLocation = e.Location;
                ((Control)sender).MouseMove += Panel_MouseMove;
                ((Control)sender).MouseUp += Panel_MouseUp;
            }
        }

        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Location = new Point(
                    this.Location.X + (e.X - mouseLocation.X),
                    this.Location.Y + (e.Y - mouseLocation.Y)
                );
            }
        }

        private void Panel_MouseUp(object sender, MouseEventArgs e)
        {
            ((Control)sender).MouseMove -= Panel_MouseMove;
            ((Control)sender).MouseUp -= Panel_MouseUp;
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnPrijava_Click(sender, e);
            }
        }

        private void btnPrijava_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;

            // Validacija
            if (string.IsNullOrWhiteSpace(txtKorisnickoIme.Text))
            {
                ShowError("❌ Molimo unesite korisničko ime.");
                txtKorisnickoIme.Focus();
                txtKorisnickoIme.BorderColor = AppColors.Danger;
                return;
            }

            if (string.IsNullOrWhiteSpace(txtLozinka.Text))
            {
                ShowError("❌ Molimo unesite lozinku.");
                txtLozinka.Focus();
                txtLozinka.BorderColor = AppColors.Danger;
                return;
            }

            // Reset border colors
            txtKorisnickoIme.BorderColor = AppColors.BorderLight;
            txtLozinka.BorderColor = AppColors.BorderLight;

            // Da se može kliknuti samo jedan put
            btnPrijava.Enabled = false;
            btnPrijava.Text = "PRIJAVLJIVANJE...";

            // Provjeri prijavu
            if (ProvjeriPrijavu(txtKorisnickoIme.Text, txtLozinka.Text))
            {
                this.Hide();
                frmMain mainForm = new frmMain();
                mainForm.ShowDialog();
                this.Close();
            }
            else
            {
                ShowError("❌ Pogrešno korisničko ime ili lozinka!");
                txtLozinka.Clear();
                txtLozinka.Focus();
                txtKorisnickoIme.BorderColor = AppColors.Danger;
                txtLozinka.BorderColor = AppColors.Danger;
                btnPrijava.Enabled = true;
                btnPrijava.Text = "PRIJAVI SE";
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }

        private bool ProvjeriPrijavu(string korisnickoIme, string lozinka)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT id, ime, prezime, uloga, aktivan 
                       FROM prodavaci 
                       WHERE korisnicko_ime = @korisnickoIme 
                       AND lozinka_hash = @lozinkaHash";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@korisnickoIme", korisnickoIme);
                        cmd.Parameters.AddWithValue("@lozinkaHash", HashLozinku(lozinka));

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bool aktivan = reader.GetBoolean(reader.GetOrdinal("aktivan"));

                                if (!aktivan)
                                {
                                    ShowError("⚠️ Vaš korisnički račun je deaktiviran!");
                                    return false;
                                }

                                int prodavacId = reader.GetInt32(reader.GetOrdinal("id"));
                                AzurirajZadnjuPrijavu(prodavacId);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"⚠️ Greška pri povezivanju: {ex.Message}");
            }

            return false;
        }

        private void AzurirajZadnjuPrijavu(int korisnikId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE prodavaci SET zadnja_prijava = GETDATE() WHERE id = @id";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", korisnikId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private string HashLozinku(string lozinka)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(lozinka));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}