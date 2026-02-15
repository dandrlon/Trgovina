using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Trgovina.Data;

namespace Trgovina.Forms
{
    public partial class frmPostavke : Form
    {
        public frmPostavke()
        {
            InitializeComponent();
            UcitajPostavke();
        }

        private void btnSpremi_Click(object sender, EventArgs e)
        {
            DatabaseHelper.SpremiPostavku("NazivTvrtke", txtNaziv.Text);
            DatabaseHelper.SpremiPostavku("OIB", txtOIB.Text);
            DatabaseHelper.SpremiPostavku("Adresa", txtAdresa.Text);
            DatabaseHelper.SpremiPostavku("Grad", txtGrad.Text);
            DatabaseHelper.SpremiPostavku("PostanskiBroj", txtPostanskiBroj.Text);
            DatabaseHelper.SpremiPostavku("Telefon", txtTelefon.Text);
            DatabaseHelper.SpremiPostavku("Email", txtEmail.Text);
            DatabaseHelper.SpremiPostavku("PDVsustav", chcPDV.Checked.ToString());

            MessageBox.Show("Postavke spremljene!", "Uspjeh",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UcitajPostavke()
        {
            txtNaziv.Text = DatabaseHelper.GetPostavka("NazivTvrtke");
            txtOIB.Text = DatabaseHelper.GetPostavka("OIB");
            txtAdresa.Text = DatabaseHelper.GetPostavka("Adresa");
            txtGrad.Text = DatabaseHelper.GetPostavka("Grad");
            txtPostanskiBroj.Text = DatabaseHelper.GetPostavka("PostanskiBroj");
            txtTelefon.Text = DatabaseHelper.GetPostavka("Telefon");
            txtEmail.Text = DatabaseHelper.GetPostavka("Email");
            chcPDV.Checked = Convert.ToBoolean(DatabaseHelper.GetPostavka("PDVsustav"));
        }
    }
}
