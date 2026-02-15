using System.Drawing;
using System.Windows.Forms;
using Trgovina.Utils;

namespace Trgovina.Base
{
    public partial class frmBase : Form
    {
        private Point mouseLocation;

        protected frmBase()
        {
            // Default postavke za sve forme
            this.Font = AppFonts.Regular;
            this.BackColor = AppColors.Background;
            this.DoubleBuffered = true; // Sprječava flickering
        }

        protected void EnableFormDrag(Control control)
        {
            control.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    mouseLocation = e.Location;
                }
            };

            control.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    this.Location = new Point(
                        this.Location.X + (e.X - mouseLocation.X),
                        this.Location.Y + (e.Y - mouseLocation.Y)
                    );
                }
            };
        }
    }
}