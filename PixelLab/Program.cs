using System;
using System.Windows.Forms;
using PixelLab.UI.Forms;

namespace PixelLab
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new MainForm());
        }
    }
}