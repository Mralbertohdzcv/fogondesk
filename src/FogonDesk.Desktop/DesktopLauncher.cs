using System.Windows.Forms;

namespace FogonDesk.Desktop
{
    public sealed class DesktopLauncher
    {
        private readonly DesktopCompositionRoot root;

        public DesktopLauncher(DesktopCompositionRoot root)
        {
            this.root = root;
        }

        public void Run()
        {
            Run(null);
        }

        public void Run(IWin32Window owner)
        {
            this.root.Logger.Info("DesktopLauncher.Run iniciado.");
            var startup = this.root.StartupWorkflowService.Initialize();
            this.root.Logger.Info("Startup inicializado. Success=" + startup.Success + ", Configurado=" + (startup.Data != null && startup.Data.IsConfigured));
            if (!startup.Success)
            {
                this.root.Logger.Warn("Startup falló: " + startup.Message);
                    MessageBox.Show(owner, startup.Message, "MrAlbertoCompany", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!startup.Data.IsConfigured)
            {
                this.root.Logger.Info("Mostrando SetupWizardForm.");
                using (var setupForm = new SetupWizardForm(this.root.InitialSetupService, this.root.TicketPrinter))
                {
                    if (setupForm.ShowDialog() != DialogResult.OK)
                    {
                        this.root.Logger.Warn("SetupWizardForm cancelado por el usuario.");
                        return;
                    }
                }

                startup = this.root.StartupWorkflowService.Initialize();
                this.root.Logger.Info("Startup reinicializado después del setup. Success=" + startup.Success + ", Configurado=" + (startup.Data != null && startup.Data.IsConfigured));
                if (!startup.Success || !startup.Data.IsConfigured)
                {
                    this.root.Logger.Warn("La configuración inicial no pudo completarse correctamente.");
                    MessageBox.Show(owner, "No fue posible completar la configuración inicial.", "MrAlbertoCompany", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            var keepRunning = true;
            while (keepRunning)
            {
                this.root.Logger.Info("Mostrando LoginForm.");
                using (var loginForm = new LoginForm(startup.Data.BusinessName, this.root.AuthenticationService))
                {
                    if (loginForm.ShowDialog() != DialogResult.OK)
                    {
                        this.root.Logger.Warn("LoginForm cancelado por el usuario.");
                        return;
                    }

                    this.root.Logger.Info("Acceso correcto. Mostrando OperationsMenuForm para " + loginForm.AuthenticatedUser.Username + ".");
                    using (var mainMenu = new OperationsMenuForm(startup.Data, loginForm.AuthenticatedUser, this.root.BackupService, this.root.TicketPrinter, this.root.CatalogApplicationService, this.root.UserAdministrationService, this.root.CashShiftApplicationService, this.root.SalesApplicationService, this.root.TicketPrintSettingsApplicationService, this.root.TelegramIntegrationService, this.root.OperationSettingsApplicationService))
                    {
                        mainMenu.ShowDialog();
                        keepRunning = mainMenu.LogoutRequested;
                        this.root.Logger.Info("OperationsMenuForm cerrado. LogoutRequested=" + keepRunning + ".");
                    }
                }
            }

            this.root.Logger.Info("DesktopLauncher.Run finalizado.");
        }
    }
}
