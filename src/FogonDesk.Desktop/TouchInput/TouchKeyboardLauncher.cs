using System;
using System.Diagnostics;
using System.IO;

namespace FogonDesk.Desktop.TouchInput
{
    internal static class TouchKeyboardLauncher
    {
        public static void Show()
        {
            var tabTipPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
                "microsoft shared",
                "ink",
                "TabTip.exe");

            if (!File.Exists(tabTipPath))
            {
                return;
            }

            try
            {
                Process.Start(tabTipPath);
            }
            catch
            {
                // TabTip may be unavailable on some devices.
            }
        }
    }
}
