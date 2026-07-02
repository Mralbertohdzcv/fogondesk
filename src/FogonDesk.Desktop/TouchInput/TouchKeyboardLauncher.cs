using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FogonDesk.Desktop.TouchInput
{
    internal static class TouchKeyboardLauncher
    {
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;

        private static Timer repositionTimer;
        private static Form ownerForm;
        private static TextBox ownerInput;
        private static int attemptsLeft;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        public static void Show(TextBox input, Form owner)
        {
            if (FindKeyboardWindow() == IntPtr.Zero)
            {
                var tabTipPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
                    "microsoft shared",
                    "ink",
                    "TabTip.exe");

                if (TryStart(tabTipPath))
                {
                    // Launched the modern touch keyboard.
                }
                else
                {
                    var oskPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.System),
                        "osk.exe");
                    TryStart(oskPath);
                }
            }

            ownerForm = owner;
            ownerInput = input;
            attemptsLeft = 50;

            if (repositionTimer == null)
            {
                repositionTimer = new Timer { Interval = 120 };
                repositionTimer.Tick += RepositionTick;
            }

            repositionTimer.Start();
        }

        private static bool TryStart(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                Process.Start(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static IntPtr FindKeyboardWindow()
        {
            var handle = FindWindow("IPTip_Main_Window", null);
            return handle != IntPtr.Zero ? handle : FindWindow("OSKMainClass", null);
        }

        private static void RepositionTick(object sender, EventArgs e)
        {
            attemptsLeft--;
            if (TryRepositionKeyboard() || attemptsLeft <= 0)
            {
                repositionTimer.Stop();
            }
        }

        private static bool TryRepositionKeyboard()
        {
            if (ownerForm == null || ownerForm.IsDisposed)
            {
                return true;
            }

            var handle = FindKeyboardWindow();

            if (handle == IntPtr.Zero || !IsWindowVisible(handle))
            {
                return false;
            }

            var workingArea = Screen.FromControl(ownerForm).WorkingArea;
            var width = Math.Max(ownerForm.Width, 700);
            var height = 230;
            var x = ownerForm.Left + ((ownerForm.Width - width) / 2);
            var y = ownerForm.Bottom + 12;

            if (y + height > workingArea.Bottom)
            {
                y = workingArea.Bottom - height - 8;
            }

            if (x < workingArea.Left)
            {
                x = workingArea.Left + 8;
            }

            if (x + width > workingArea.Right)
            {
                x = workingArea.Right - width - 8;
            }

            SetWindowPos(handle, IntPtr.Zero, x, y, width, height, SwpNoZOrder | SwpNoActivate);

            if (ownerInput != null && !ownerInput.IsDisposed && !ownerInput.Focused)
            {
                ownerInput.Focus();
            }

            return true;
        }
    }
}
