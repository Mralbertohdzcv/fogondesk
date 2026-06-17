using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Desktop
{
    public sealed class MainMenuForm : Form
    {
        private readonly IBackupService backupService;
        private readonly ITicketPrinter ticketPrinter;
        private readonly AppStartupState startupState;
        private readonly AuthenticatedUserView authenticatedUser;
        private readonly ICatalogApplicationService catalogApplicationService;
        private readonly ISalesApplicationService salesApplicationService;

        public MainMenuForm(
            AppStartupState startupState,
            AuthenticatedUserView authenticatedUser,
            IBackupService backupService,
            ITicketPrinter ticketPrinter,
            ICatalogApplicationService catalogApplicationService,
            ISalesApplicationService salesApplicationService)
        {
            this.startupState = startupState;
            this.authenticatedUser = authenticatedUser;
            this.backupService = backupService;
            this.ticketPrinter = ticketPrinter;
            this.catalogApplicationService = catalogApplicationService;
            this.salesApplicationService = salesApplicationService;

            Text = "MrAlbertoCompany";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.WhiteSmoke;
            Font = new Font("Segoe UI", 10F);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.FromArgb(27, 67, 50)
            };

            var title = new Label
            {
                Dock = DockStyle.Top,
                Height = 42,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                Text = "  " + this.startupState.BusinessName,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var subtitle = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 10F),
                Text = "  Usuario: " + this.authenticatedUser.DisplayName + " | Rol: " + this.authenticatedUser.RoleCode + " | Impresora: " + (string.IsNullOrWhiteSpace(this.startupState.ActivePrinterName) ? "No configurada" : this.startupState.ActivePrinterName),
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(subtitle);
            header.Controls.Add(title);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                Padding = new Padding(24),
                BackColor = Color.WhiteSmoke
            };
            for (var i = 0; i < 3; i++)
            {
                body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                body.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            }

            body.Controls.Add(CreateActionButton("Punto de venta", Color.FromArgb(228, 108, 10), OpenPointOfSale), 0, 0);
            body.Controls.Add(CreateActionButton("Administración", Color.FromArgb(52, 73, 94), delegate { ShowPendingModule("Administración"); }), 1, 0);
            body.Controls.Add(CreateActionButton("Caja y turnos", Color.FromArgb(33, 158, 188), delegate { ShowPendingModule("Caja y turnos"); }), 2, 0);
            body.Controls.Add(CreateActionButton("Órdenes pendientes", Color.FromArgb(42, 157, 143), delegate { ShowPendingModule("Órdenes pendientes"); }), 0, 1);
            body.Controls.Add(CreateActionButton("Reportes", Color.FromArgb(96, 108, 56), delegate { ShowPendingModule("Reportes"); }), 1, 1);
            body.Controls.Add(CreateActionButton("Respaldo local", Color.FromArgb(69, 123, 157), CreateBackup), 2, 1);
            body.Controls.Add(CreateActionButton("Ticket de prueba", Color.FromArgb(38, 70, 83), PrintTestTicket), 0, 2);
            body.Controls.Add(CreateActionButton("Cerrar sesión", Color.FromArgb(130, 27, 52), delegate { this.LogoutRequested = true; Close(); }), 1, 2);
            body.Controls.Add(CreateActionButton("Salir", Color.FromArgb(90, 90, 90), delegate { this.LogoutRequested = false; Close(); }), 2, 2);

            Controls.Add(body);
            Controls.Add(header);
        }

        public bool LogoutRequested { get; private set; }

        private void OpenPointOfSale()
        {
            using (var pointOfSaleForm = new PointOfSaleForm(this.startupState, this.authenticatedUser, null, this.catalogApplicationService, this.salesApplicationService, this.ticketPrinter))
            {
                pointOfSaleForm.ShowDialog(this);
            }
        }

        private void CreateBackup()
        {
            var result = this.backupService.CreateBackup(this.authenticatedUser.Username);
            MessageBox.Show(
                result.Message + (result.Success ? Environment.NewLine + result.Data.FilePath : string.Empty),
                "Respaldo local",
                MessageBoxButtons.OK,
                result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void PrintTestTicket()
        {
            var printerName = this.startupState.ActivePrinterName;
            if (string.IsNullOrWhiteSpace(printerName))
            {
                var printers = this.ticketPrinter.GetInstalledPrinters();
                if (printers.Count > 0)
                {
                    printerName = printers[0];
                }
            }

            if (string.IsNullOrWhiteSpace(printerName))
            {
                MessageBox.Show(
                    "No hay impresoras disponibles para la prueba.",
                    "Impresión",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var job = new TicketPrintJob
            {
                PrinterName = printerName,
                Title = "Prueba de impresión",
                Lines = new List<string>
                {
                    this.startupState.BusinessName,
                    "Usuario: " + this.authenticatedUser.DisplayName,
                    "Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    "--------------------------------",
                    "Este es un ticket de prueba.",
                    "La venta no depende de la impresión.",
                    "--------------------------------",
                    "Gracias por usar MrAlbertoCompany"
                }
            };

            var result = this.ticketPrinter.Print(job);
            MessageBox.Show(
                result.Message,
                "Impresión",
                MessageBoxButtons.OK,
                result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private static void ShowPendingModule(string moduleName)
        {
            MessageBox.Show(
                moduleName + " quedará conectado en las fases siguientes del MVP.",
                "Módulo en construcción",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static Button CreateActionButton(string text, Color color, Action action)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                Text = text,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                Margin = new Padding(12)
            };
            button.Click += delegate { action(); };
            return button;
        }
    }
}
