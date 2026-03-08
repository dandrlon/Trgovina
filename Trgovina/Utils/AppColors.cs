using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trgovina.Utils
{
    public static class AppColors
    {
        // Glavne boje
        public static Color Primary = Color.FromArgb(41, 50, 65);      // Tamno plava
        public static Color PrimaryLight = Color.FromArgb(53, 64, 82); // Hover stanje
        public static Color Secondary = Color.FromArgb(108, 117, 125); // Siva
        public static Color Accent = Color.FromArgb(0, 123, 255);      // Plava

        // Stanja
        public static Color Success = Color.FromArgb(40, 167, 69);     // Zelena
        public static Color Danger = Color.FromArgb(220, 53, 69);      // Crvena
        public static Color Warning = Color.FromArgb(255, 193, 7);     // Žuta
        public static Color Info = Color.FromArgb(23, 162, 184);       // Cyan

        // Background
        public static Color Background = Color.FromArgb(248, 249, 250);
        public static Color CardBackground = Color.White;
        public static Color SidebarBackground = Color.FromArgb(41, 50, 65);

        // Text
        public static Color TextPrimary = Color.FromArgb(33, 37, 41);
        public static Color TextSecondary = Color.FromArgb(108, 117, 125);
        public static Color TextWhite = Color.White;

        // Border
        public static Color BorderLight = Color.FromArgb(222, 226, 230);

        // Login specific
        public static Color LoginGradientStart = Color.FromArgb(41, 50, 65);
        public static Color LoginGradientEnd = Color.FromArgb(0, 123, 255);
    }

    public static class AppFonts
    {
        // Naslovi
        public static Font TitleLarge = new Font("Segoe UI", 16, FontStyle.Bold);
        public static Font TitleMedium = new Font("Segoe UI", 14, FontStyle.Bold);
        public static Font TitleSmall = new Font("Segoe UI", 12, FontStyle.Bold);

        // Regularni tekst
        public static Font Regular = new Font("Segoe UI", 10, FontStyle.Regular);
        public static Font RegularMedium = new Font("Segoe UI", 11, FontStyle.Regular);
        public static Font RegularLarge = new Font("Segoe UI", 12, FontStyle.Regular);

        // Navigacija
        public static Font Navigation = new Font("Segoe UI", 11, FontStyle.Regular);

        // Buttons
        public static Font Button = new Font("Segoe UI", 10, FontStyle.Regular);

        // Login
        public static Font LoginTitle = new Font("Segoe UI", 24, FontStyle.Bold);
        public static Font LoginSubtitle = new Font("Segoe UI", 11, FontStyle.Regular);
    }

}