using Guna.UI2.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;
using Trgovina.Utils;

namespace Trgovina.Forms
{
    /// <summary>
    /// Mala popup forma za odabir jednog datuma.
    /// Koristi se npr. za datum valute pri fakturiranju otpremnice.
    /// </summary>
    public partial class frmOdabirDatuma : Form
    {
        public DateTime OdabraniDatum { get; private set; }

        private DateTimePicker dtp;

        public frmOdabirDatuma(string naslov, DateTime defaultVrijednost)
        {
            OdabraniDatum = defaultVrijednost;

            this.Text = "Odabir datuma";
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Size = new Size(320, 155);

            var lbl = new Label
            {
                Text = naslov,
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextPrimary,
                Location = new Point(15, 15),
                AutoSize = true
            };
            this.Controls.Add(lbl);

            dtp = new DateTimePicker
            {
                Size = new Size(280, 28),
                Location = new Point(15, 38),
                Format = DateTimePickerFormat.Short,
                Value = defaultVrijednost
            };
            this.Controls.Add(dtp);

            var btnOk = new Guna2Button
            {
                Text = "✔  Potvrdi",
                Size = new Size(120, 36),
                Location = new Point(15, 78),
                FillColor = AppColors.Primary,
                Font = AppFonts.Regular,
                ForeColor = Color.White,
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };
            btnOk.HoverState.FillColor = AppColors.PrimaryLight;
            btnOk.Click += (s, e) =>
            {
                OdabraniDatum = dtp.Value.Date;
                DialogResult = DialogResult.OK;
                Close();
            };
            this.Controls.Add(btnOk);

            var btnOdustani = new Guna2Button
            {
                Text = "✖  Odustani",
                Size = new Size(120, 36),
                Location = new Point(143, 78),
                FillColor = Color.FromArgb(210, 210, 215),
                Font = AppFonts.Regular,
                ForeColor = AppColors.TextPrimary,
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };
            btnOdustani.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            this.Controls.Add(btnOdustani);

            this.AcceptButton = btnOk;
            this.CancelButton = btnOdustani;
        }
    }
}