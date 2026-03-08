namespace Trgovina.Forms
{
    partial class frmPreviewRacuna
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
            this.webPreview = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // webPreview
            // 
            this.webPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webPreview.Location = new System.Drawing.Point(0, 0);
            this.webPreview.MinimumSize = new System.Drawing.Size(20, 20);
            this.webPreview.Name = "webPreview";
            this.webPreview.Size = new System.Drawing.Size(800, 450);
            this.webPreview.TabIndex = 0;
            // 
            // frmPreviewRacuna
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.webPreview);
            this.Name = "frmPreviewRacuna";
            this.Text = "frmPreviewRacuna";
            this.Load += new System.EventHandler(this.frmPreviewRacuna_Load_1);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser webPreview;
    }
}