using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Trgovina.Forms
{
    public partial class frmPreviewRacuna : Form
    {
        private string _pdfPath;

        public frmPreviewRacuna(string pdfPath)
        {
            InitializeComponent();
            _pdfPath = pdfPath;
        }

        private void frmPreviewRacuna_Load_1(object sender, EventArgs e)
        {
            //webPreview.Navigate(_pdfPath);
            Process.Start(new ProcessStartInfo(_pdfPath)
            {
                UseShellExecute = true
            });
        }
    }
}