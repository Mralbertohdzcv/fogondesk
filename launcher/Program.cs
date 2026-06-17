using System;
using System.Drawing;
using System.Windows.Forms;
using FogonDesk.Desktop;

namespace SysPOS
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RootStartupHostForm(new DesktopCompositionRoot()));
        }

        private sealed class RootStartupHostForm : Form
        {
            private readonly DesktopCompositionRoot root;
            private bool launched;

            public RootStartupHostForm(DesktopCompositionRoot root)
            {
                this.root = root;

                this.ShowInTaskbar = false;
                this.StartPosition = FormStartPosition.Manual;
                this.FormBorderStyle = FormBorderStyle.None;
                this.Opacity = 0D;
                this.Size = new Size(1, 1);
                this.Location = new Point(-32000, -32000);

                this.Shown += RootStartupHostFormShown;
            }

            private void RootStartupHostFormShown(object sender, EventArgs e)
            {
                if (this.launched)
                {
                    return;
                }

                this.launched = true;
                this.Hide();
                this.BeginInvoke(new Action(RunWorkflow));
            }

            private void RunWorkflow()
            {
                try
                {
                    var launcher = new DesktopLauncher(this.root);
                    launcher.Run(this);
                }
                catch (Exception exception)
                {
                    this.root.Logger.Error("Error no controlado en el lanzador raíz.", exception);
                    MessageBox.Show(
                        "Se produjo un error al iniciar FogonDesk POS. Revisa el log local.",
                        "FogonDesk POS",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    this.Close();
                }
            }
        }
    }
}