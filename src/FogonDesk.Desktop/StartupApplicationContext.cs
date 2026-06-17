using System;
using System.Drawing;
using System.Windows.Forms;

namespace FogonDesk.Desktop
{
    internal sealed class StartupHostForm : Form
    {
        private readonly DesktopCompositionRoot root;
        private bool launched;

        public StartupHostForm(DesktopCompositionRoot root)
        {
            this.root = root;

            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Opacity = 0D;
            this.Size = new Size(1, 1);
            this.Location = new Point(-32000, -32000);

            this.Shown += StartupHostFormShown;
        }

        private void StartupHostFormShown(object sender, EventArgs e)
        {
            if (this.launched)
            {
                return;
            }

            this.launched = true;
            this.root.Logger.Info("StartupHostForm.Shown");
            this.Hide();
            this.BeginInvoke(new Action(RunWorkflow));
        }

        private void RunWorkflow()
        {
            try
            {
                this.root.Logger.Info("StartupHostForm.RunWorkflow iniciado.");
                var launcher = new DesktopLauncher(this.root);
                launcher.Run(this);
                this.root.Logger.Info("StartupHostForm.RunWorkflow finalizado.");
            }
            catch (Exception exception)
            {
                this.root.Logger.Error("Error no controlado en StartupHostForm.", exception);
                MessageBox.Show("Se produjo un error al iniciar MrAlbertoCompany. Revisa el log local.", "MrAlbertoCompany", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Close();
            }
        }
    }
}
