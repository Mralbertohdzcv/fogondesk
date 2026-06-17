using System;
using FogonDesk.Desktop.TouchInput;

namespace FogonDesk.Desktop
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            TouchInputSupport.InitializeApplication();
            System.Windows.Forms.Application.Run(new StartupHostForm(new DesktopCompositionRoot()));
        }
    }
}
