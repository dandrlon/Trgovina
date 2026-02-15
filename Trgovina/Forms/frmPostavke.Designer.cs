namespace Trgovina.Forms
{
    partial class frmPostavke
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlBackground = new Guna.UI2.WinForms.Guna2Panel();
            this.pnlLoginCard = new Guna.UI2.WinForms.Guna2Panel();
            this.btnSpremi = new Guna.UI2.WinForms.Guna2Button();
            this.txtOIB = new Guna.UI2.WinForms.Guna2TextBox();
            this.txtNaziv = new Guna.UI2.WinForms.Guna2TextBox();
            this.guna2HtmlLabel1 = new Guna.UI2.WinForms.Guna2HtmlLabel();
            this.txtAdresa = new Guna.UI2.WinForms.Guna2TextBox();
            this.txtGrad = new Guna.UI2.WinForms.Guna2TextBox();
            this.txtPostanskiBroj = new Guna.UI2.WinForms.Guna2TextBox();
            this.txtTelefon = new Guna.UI2.WinForms.Guna2TextBox();
            this.txtEmail = new Guna.UI2.WinForms.Guna2TextBox();
            this.chcPDV = new Guna.UI2.WinForms.Guna2CheckBox();
            this.btnOdustani = new Guna.UI2.WinForms.Guna2Button();
            this.pnlBackground.SuspendLayout();
            this.pnlLoginCard.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlBackground
            // 
            this.pnlBackground.Controls.Add(this.pnlLoginCard);
            this.pnlBackground.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBackground.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlBackground.Location = new System.Drawing.Point(0, 0);
            this.pnlBackground.Name = "pnlBackground";
            this.pnlBackground.Size = new System.Drawing.Size(673, 954);
            this.pnlBackground.TabIndex = 1;
            // 
            // pnlLoginCard
            // 
            this.pnlLoginCard.BackColor = System.Drawing.Color.Transparent;
            this.pnlLoginCard.BorderRadius = 20;
            this.pnlLoginCard.Controls.Add(this.btnOdustani);
            this.pnlLoginCard.Controls.Add(this.chcPDV);
            this.pnlLoginCard.Controls.Add(this.txtEmail);
            this.pnlLoginCard.Controls.Add(this.txtTelefon);
            this.pnlLoginCard.Controls.Add(this.txtPostanskiBroj);
            this.pnlLoginCard.Controls.Add(this.txtGrad);
            this.pnlLoginCard.Controls.Add(this.txtAdresa);
            this.pnlLoginCard.Controls.Add(this.btnSpremi);
            this.pnlLoginCard.Controls.Add(this.txtOIB);
            this.pnlLoginCard.Controls.Add(this.txtNaziv);
            this.pnlLoginCard.Controls.Add(this.guna2HtmlLabel1);
            this.pnlLoginCard.FillColor = System.Drawing.Color.White;
            this.pnlLoginCard.Location = new System.Drawing.Point(67, 68);
            this.pnlLoginCard.Name = "pnlLoginCard";
            this.pnlLoginCard.ShadowDecoration.Enabled = true;
            this.pnlLoginCard.Size = new System.Drawing.Size(516, 718);
            this.pnlLoginCard.TabIndex = 0;
            // 
            // btnSpremi
            // 
            this.btnSpremi.BorderRadius = 8;
            this.btnSpremi.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnSpremi.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnSpremi.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnSpremi.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnSpremi.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(123)))), ((int)(((byte)(255)))));
            this.btnSpremi.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btnSpremi.ForeColor = System.Drawing.Color.White;
            this.btnSpremi.Location = new System.Drawing.Point(73, 612);
            this.btnSpremi.Name = "btnSpremi";
            this.btnSpremi.Size = new System.Drawing.Size(122, 45);
            this.btnSpremi.TabIndex = 3;
            this.btnSpremi.Text = "Spremi";
            this.btnSpremi.Click += new System.EventHandler(this.btnSpremi_Click);
            // 
            // txtOIB
            // 
            this.txtOIB.BorderRadius = 8;
            this.txtOIB.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtOIB.DefaultText = "";
            this.txtOIB.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtOIB.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtOIB.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtOIB.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtOIB.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtOIB.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtOIB.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtOIB.Location = new System.Drawing.Point(73, 124);
            this.txtOIB.Name = "txtOIB";
            this.txtOIB.PlaceholderText = "OIB";
            this.txtOIB.SelectedText = "";
            this.txtOIB.Size = new System.Drawing.Size(350, 40);
            this.txtOIB.TabIndex = 2;
            // 
            // txtNaziv
            // 
            this.txtNaziv.BorderRadius = 8;
            this.txtNaziv.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtNaziv.DefaultText = "";
            this.txtNaziv.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtNaziv.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtNaziv.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtNaziv.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtNaziv.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtNaziv.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtNaziv.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtNaziv.Location = new System.Drawing.Point(73, 69);
            this.txtNaziv.Name = "txtNaziv";
            this.txtNaziv.PlaceholderText = "Naziv tvrtke";
            this.txtNaziv.SelectedText = "";
            this.txtNaziv.Size = new System.Drawing.Size(350, 40);
            this.txtNaziv.TabIndex = 1;
            // 
            // guna2HtmlLabel1
            // 
            this.guna2HtmlLabel1.BackColor = System.Drawing.Color.Transparent;
            this.guna2HtmlLabel1.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.guna2HtmlLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(40)))), ((int)(((byte)(65)))));
            this.guna2HtmlLabel1.Location = new System.Drawing.Point(187, 23);
            this.guna2HtmlLabel1.Name = "guna2HtmlLabel1";
            this.guna2HtmlLabel1.Size = new System.Drawing.Size(61, 22);
            this.guna2HtmlLabel1.TabIndex = 0;
            this.guna2HtmlLabel1.Text = "<font size=\'3\'>Postavke</font>";
            this.guna2HtmlLabel1.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtAdresa
            // 
            this.txtAdresa.BorderRadius = 8;
            this.txtAdresa.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtAdresa.DefaultText = "";
            this.txtAdresa.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtAdresa.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtAdresa.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtAdresa.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtAdresa.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtAdresa.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtAdresa.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtAdresa.Location = new System.Drawing.Point(73, 189);
            this.txtAdresa.Name = "txtAdresa";
            this.txtAdresa.PlaceholderText = "Adresa";
            this.txtAdresa.SelectedText = "";
            this.txtAdresa.Size = new System.Drawing.Size(350, 40);
            this.txtAdresa.TabIndex = 4;
            // 
            // txtGrad
            // 
            this.txtGrad.BorderRadius = 8;
            this.txtGrad.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtGrad.DefaultText = "";
            this.txtGrad.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtGrad.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtGrad.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtGrad.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtGrad.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtGrad.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtGrad.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtGrad.Location = new System.Drawing.Point(73, 257);
            this.txtGrad.Name = "txtGrad";
            this.txtGrad.PlaceholderText = "Grad";
            this.txtGrad.SelectedText = "";
            this.txtGrad.Size = new System.Drawing.Size(350, 40);
            this.txtGrad.TabIndex = 5;
            // 
            // txtPostanskiBroj
            // 
            this.txtPostanskiBroj.BorderRadius = 8;
            this.txtPostanskiBroj.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtPostanskiBroj.DefaultText = "";
            this.txtPostanskiBroj.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtPostanskiBroj.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtPostanskiBroj.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtPostanskiBroj.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtPostanskiBroj.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtPostanskiBroj.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtPostanskiBroj.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtPostanskiBroj.Location = new System.Drawing.Point(73, 334);
            this.txtPostanskiBroj.Name = "txtPostanskiBroj";
            this.txtPostanskiBroj.PlaceholderText = "Poštanski broj";
            this.txtPostanskiBroj.SelectedText = "";
            this.txtPostanskiBroj.Size = new System.Drawing.Size(350, 40);
            this.txtPostanskiBroj.TabIndex = 6;
            // 
            // txtTelefon
            // 
            this.txtTelefon.BorderRadius = 8;
            this.txtTelefon.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtTelefon.DefaultText = "";
            this.txtTelefon.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtTelefon.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtTelefon.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtTelefon.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtTelefon.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtTelefon.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtTelefon.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtTelefon.Location = new System.Drawing.Point(73, 398);
            this.txtTelefon.Name = "txtTelefon";
            this.txtTelefon.PlaceholderText = "Telefon";
            this.txtTelefon.SelectedText = "";
            this.txtTelefon.Size = new System.Drawing.Size(350, 40);
            this.txtTelefon.TabIndex = 7;
            // 
            // txtEmail
            // 
            this.txtEmail.BorderRadius = 8;
            this.txtEmail.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtEmail.DefaultText = "";
            this.txtEmail.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtEmail.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtEmail.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtEmail.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtEmail.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtEmail.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtEmail.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtEmail.Location = new System.Drawing.Point(73, 460);
            this.txtEmail.Name = "txtEmail";
            this.txtEmail.PlaceholderText = "Email";
            this.txtEmail.SelectedText = "";
            this.txtEmail.Size = new System.Drawing.Size(350, 40);
            this.txtEmail.TabIndex = 8;
            // 
            // chcPDV
            // 
            this.chcPDV.AutoSize = true;
            this.chcPDV.CheckedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.chcPDV.CheckedState.BorderRadius = 0;
            this.chcPDV.CheckedState.BorderThickness = 0;
            this.chcPDV.CheckedState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.chcPDV.Location = new System.Drawing.Point(73, 533);
            this.chcPDV.Name = "chcPDV";
            this.chcPDV.Size = new System.Drawing.Size(151, 17);
            this.chcPDV.TabIndex = 9;
            this.chcPDV.Text = "Tvrtka je u sustavu PDV-a";
            this.chcPDV.UncheckedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(137)))), ((int)(((byte)(149)))));
            this.chcPDV.UncheckedState.BorderRadius = 0;
            this.chcPDV.UncheckedState.BorderThickness = 0;
            this.chcPDV.UncheckedState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(137)))), ((int)(((byte)(149)))));
            // 
            // btnOdustani
            // 
            this.btnOdustani.BorderRadius = 8;
            this.btnOdustani.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnOdustani.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnOdustani.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnOdustani.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnOdustani.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(123)))), ((int)(((byte)(255)))));
            this.btnOdustani.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btnOdustani.ForeColor = System.Drawing.Color.White;
            this.btnOdustani.Location = new System.Drawing.Point(301, 612);
            this.btnOdustani.Name = "btnOdustani";
            this.btnOdustani.Size = new System.Drawing.Size(122, 45);
            this.btnOdustani.TabIndex = 10;
            this.btnOdustani.Text = "Odustani";
            // 
            // frmPostavke
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(673, 954);
            this.Controls.Add(this.pnlBackground);
            this.Name = "frmPostavke";
            this.Text = "frmPostavke";
            this.pnlBackground.ResumeLayout(false);
            this.pnlLoginCard.ResumeLayout(false);
            this.pnlLoginCard.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Guna.UI2.WinForms.Guna2Panel pnlBackground;
        private Guna.UI2.WinForms.Guna2Panel pnlLoginCard;
        private Guna.UI2.WinForms.Guna2CheckBox chcPDV;
        private Guna.UI2.WinForms.Guna2TextBox txtEmail;
        private Guna.UI2.WinForms.Guna2TextBox txtTelefon;
        private Guna.UI2.WinForms.Guna2TextBox txtPostanskiBroj;
        private Guna.UI2.WinForms.Guna2TextBox txtGrad;
        private Guna.UI2.WinForms.Guna2TextBox txtAdresa;
        private Guna.UI2.WinForms.Guna2Button btnSpremi;
        private Guna.UI2.WinForms.Guna2TextBox txtOIB;
        private Guna.UI2.WinForms.Guna2TextBox txtNaziv;
        private Guna.UI2.WinForms.Guna2HtmlLabel guna2HtmlLabel1;
        private Guna.UI2.WinForms.Guna2Button btnOdustani;
    }
}