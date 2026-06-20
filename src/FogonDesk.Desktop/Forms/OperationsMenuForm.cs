using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;
using FogonDesk.Configuration;
using FogonDesk.Domain.Common;
using FogonDesk.Printing;
using FogonDesk.Desktop.TouchInput;

namespace FogonDesk.Desktop
{
    public sealed class OperationsMenuForm : Form
    {
        private readonly IBackupService backupService;
        private readonly ITicketPrinter ticketPrinter;
        private readonly AppStartupState startupState;
        private readonly AuthenticatedUserView authenticatedUser;
        private readonly ICatalogApplicationService catalogApplicationService;
        private readonly IUserAdministrationService userAdministrationService;
        private readonly ICashShiftApplicationService cashShiftApplicationService;
        private readonly ISalesApplicationService salesApplicationService;
        private readonly ITicketPrintSettingsApplicationService ticketPrintSettingsApplicationService;
        private readonly ITelegramIntegrationService telegramIntegrationService;
        private readonly IOperationSettingsApplicationService operationSettingsApplicationService;
        private readonly Label shiftStatusLabel;
        private readonly Button administrationButton;
        private readonly List<PendingTicketDraft> pendingTickets = new List<PendingTicketDraft>();
        private readonly BindingList<CartLineView> cashierCartLines = new BindingList<CartLineView>();
        private readonly List<Control> cashierOrderControls = new List<Control>();
        private IList<CategoryViewModel> cashierCategories = new List<CategoryViewModel>();
        private int? selectedCashierCategoryId;
        private FlowLayoutPanel cashierProductsPanel;
        private DataGridView cashierCartGrid;
        private ComboBox cashierPaymentMethodComboBox;
        private ComboBox cashierOrderKindComboBox;
        private Label cashierTotalLabel;
        private Button cashierNewOrderButton;
        private Button cashierReprintButton;
        private Button cashierChargeButton;
        private Button cashierPendingButton;
        private Label cashierCartTitleLabel;
        private Label cashierCartTicketLabel;
        private Label cashierShiftStatusCompactLabel;
        private FlowLayoutPanel pendingTicketsFlowPanel;
        private Label pendingSummaryLabel;
        private Label cashierCurrentOrderLabel;
        private PendingTicketDraft activeCashierPendingTicket;
        private IList<string> lastReceiptLines;
        private string lastReceiptPrinterName;
        private string lastReceiptTitle;
        private int nextTakeAwayNumber = 1;
        private string cashierOrderNote = string.Empty;
        private bool cashierOrderStarted;
        private IList<DigitalPlatformConfigurationView> configuredDigitalPlatforms = new List<DigitalPlatformConfigurationView>();
        private int configuredDiningTableCount = 5;
        private decimal? cashierDigitalPlatformUnitPrice;
        private string cashierDigitalPlatformName = string.Empty;
        private string cashierDigitalPlatformPricingMode = string.Empty;

        public OperationsMenuForm(
            AppStartupState startupState,
            AuthenticatedUserView authenticatedUser,
            IBackupService backupService,
            ITicketPrinter ticketPrinter,
            ICatalogApplicationService catalogApplicationService,
            IUserAdministrationService userAdministrationService,
            ICashShiftApplicationService cashShiftApplicationService,
            ISalesApplicationService salesApplicationService,
            ITicketPrintSettingsApplicationService ticketPrintSettingsApplicationService,
            ITelegramIntegrationService telegramIntegrationService,
            IOperationSettingsApplicationService operationSettingsApplicationService)
        {
            this.startupState = startupState;
            this.authenticatedUser = authenticatedUser;
            this.backupService = backupService;
            this.ticketPrinter = ticketPrinter;
            this.catalogApplicationService = catalogApplicationService;
            this.userAdministrationService = userAdministrationService;
            this.cashShiftApplicationService = cashShiftApplicationService;
            this.salesApplicationService = salesApplicationService;
            this.ticketPrintSettingsApplicationService = ticketPrintSettingsApplicationService;
            this.telegramIntegrationService = telegramIntegrationService;
            this.operationSettingsApplicationService = operationSettingsApplicationService;

            LoadOperationSettings();

            Text = string.IsNullOrWhiteSpace(this.startupState.BusinessName) ? "Operaciones" : this.startupState.BusinessName;
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(238, 242, 246);
            Font = new Font("Segoe UI", 10F);

            if (string.Equals(this.authenticatedUser.RoleCode, SystemRoles.Cashier, StringComparison.OrdinalIgnoreCase))
            {
                this.shiftStatusLabel = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), ForeColor = Color.DimGray, Padding = new Padding(12, 8, 12, 8) };
                this.administrationButton = null;
                LoadPendingTicketsFromStorage();
                BuildCashierDashboard();
                Shown += delegate
                {
                    RefreshDashboard();
                    InitializeCashierCatalog();
                };
                return;
            }

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 104,
                BackColor = Color.FromArgb(27, 67, 50)
            };

            var title = new Label
            {
                Dock = DockStyle.Top,
                Height = 48,
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
                Text = "  Usuario: " + this.authenticatedUser.DisplayName + " | Rol: " + this.authenticatedUser.RoleCode + " | Estación: " + (string.IsNullOrWhiteSpace(this.startupState.StationName) ? this.startupState.StationCode : this.startupState.StationName),
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(subtitle);
            header.Controls.Add(title);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(24),
                BackColor = BackColor
            };

            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var overviewCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(18)
            };
            var overviewLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
            overviewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            overviewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            overviewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            overviewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            overviewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            overviewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            overviewLayout.Controls.Add(new Label
            {
                Text = "Panel del operador",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 67, 50)
            }, 0, 0);
            overviewLayout.Controls.Add(CreateInfoCard("Estación", string.IsNullOrWhiteSpace(this.startupState.StationName) ? this.startupState.StationCode : this.startupState.StationName), 0, 1);
            overviewLayout.Controls.Add(CreateInfoCard("Rol activo", this.authenticatedUser.RoleCode), 0, 2);
            this.shiftStatusLabel = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), ForeColor = Color.DimGray, Padding = new Padding(12, 8, 12, 8) };
            overviewLayout.Controls.Add(CreateStatusCard("Caja actual", this.shiftStatusLabel), 0, 3);
            overviewLayout.Controls.Add(new Panel(), 0, 4);
            overviewLayout.Controls.Add(CreateFooterButtons(), 0, 5);
            overviewCard.Controls.Add(overviewLayout);

            var actionsGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Margin = new Padding(18, 0, 0, 0)
            };
            actionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            actionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            actionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            actionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            actionsGrid.Controls.Add(CreateActionButton("Punto de venta", "Cobro rápido con productos y ticket", Color.FromArgb(228, 108, 10), OpenPointOfSale), 0, 0);
            this.administrationButton = CreateActionButton("Administración", "Alta de categorías, productos y usuarios", Color.FromArgb(52, 73, 94), OpenAdministration);
            actionsGrid.Controls.Add(this.administrationButton, 1, 0);
            actionsGrid.Controls.Add(CreateActionButton("Estadísticas y reportes", "Ventas, tickets, cancelaciones y utilidad", Color.FromArgb(33, 158, 188), OpenStatisticsAndReports), 0, 1);
            actionsGrid.Controls.Add(CreateActionButton("Respaldo local", "Genera una copia de seguridad local", Color.FromArgb(69, 123, 157), CreateBackup), 1, 1);
            actionsGrid.Controls.Add(CreateActionButton("Plataformas y mesas", "Configura plataformas digitales y mesas", Color.FromArgb(38, 70, 83), OpenOperationSettings), 0, 2);
            actionsGrid.Controls.Add(CreateActionButton("Configuración de ticket e impresión", "Impresora, diseño, fuentes y ticket", Color.FromArgb(96, 108, 56), OpenTicketPrintSettings), 1, 2);
            actionsGrid.Controls.Add(CreateActionButton("Telegram", "Bot, chats vinculados y codigos", Color.FromArgb(122, 94, 41), OpenTelegramSettings), 0, 3);
            actionsGrid.Controls.Add(CreateActionButton("Ticket de prueba", "Imprime ticket para validar formato", Color.FromArgb(88, 95, 107), PrintTestTicket), 1, 3);

            body.Controls.Add(overviewCard, 0, 0);
            body.Controls.Add(actionsGrid, 1, 0);

            Controls.Add(body);
            Controls.Add(header);
            Shown += delegate { RefreshDashboard(); };
        }

        public bool LogoutRequested { get; private set; }

        private void LoadOperationSettings()
        {
            if (this.operationSettingsApplicationService == null)
            {
                this.configuredDiningTableCount = this.startupState.DiningTableCount <= 0 ? 5 : this.startupState.DiningTableCount;
                return;
            }

            var settings = this.operationSettingsApplicationService.GetSettings();
            if (settings == null)
            {
                this.configuredDiningTableCount = this.startupState.DiningTableCount <= 0 ? 5 : this.startupState.DiningTableCount;
                return;
            }

            this.configuredDiningTableCount = settings.DiningTableCount <= 0 ? 5 : settings.DiningTableCount;
            this.startupState.DiningTableCount = this.configuredDiningTableCount;
            this.configuredDigitalPlatforms = (settings.DigitalPlatforms ?? new List<DigitalPlatformConfigurationView>())
                .Where(item => item != null && item.IsActive)
                .OrderBy(item => item.Name)
                .ToList();
        }

        private void OpenPointOfSale()
        {
            OpenPointOfSale(null, string.Empty);
        }

        private void OpenPointOfSale(OrderKind? orderKind, string note)
        {
            OpenPointOfSale(orderKind, note, null, null);
        }

        private void OpenPointOfSale(OrderKind? orderKind, string note, IList<SaleLineDraft> initialItems, PendingTicketDraft sourcePending)
        {
            var activeShift = EnsureActiveShift();
            if (activeShift == null)
            {
                return;
            }

            using (var pointOfSaleForm = new PointOfSaleForm(this.startupState, this.authenticatedUser, activeShift, this.catalogApplicationService, this.salesApplicationService, this.ticketPrinter, orderKind, note, initialItems))
            {
                pointOfSaleForm.ShowDialog(this);
                if (pointOfSaleForm.PendingTicketSaved)
                {
                    if (sourcePending != null)
                    {
                        this.pendingTickets.Remove(sourcePending);
                    }

                    this.pendingTickets.Add(new PendingTicketDraft
                    {
                        Name = pointOfSaleForm.PendingTicketName,
                        OrderKind = pointOfSaleForm.PendingTicketOrderKind,
                        Note = pointOfSaleForm.PendingTicketNote,
                        DigitalPlatformName = string.Empty,
                        DigitalPlatformPricingMode = string.Empty,
                        DigitalPlatformUnitPrice = null,
                        Items = pointOfSaleForm.PendingTicketItems,
                        Total = pointOfSaleForm.PendingTicketTotal
                    });
                    RefreshPendingTicketsView();
                    return;
                }

                if (sourcePending != null)
                {
                    this.pendingTickets.Remove(sourcePending);
                    RefreshPendingTicketsView();
                }

                if (pointOfSaleForm.LastReceiptLines != null && pointOfSaleForm.LastReceiptLines.Count > 0)
                {
                    this.lastReceiptLines = pointOfSaleForm.LastReceiptLines;
                    this.lastReceiptPrinterName = pointOfSaleForm.LastReceiptPrinterName;
                    this.lastReceiptTitle = pointOfSaleForm.LastReceiptTitle;
                }
            }

            RefreshDashboard();
        }

        private CashShiftSummaryView EnsureActiveShift()
        {
            var activeShift = this.cashShiftApplicationService.GetActiveShift(this.startupState.StationCode);
            if (activeShift.Success)
            {
                return activeShift.Data;
            }

            var openResult = this.cashShiftApplicationService.OpenShift(new OpenCashShiftRequest
            {
                StationCode = this.startupState.StationCode,
                UserId = this.authenticatedUser.UserId,
                UserName = this.authenticatedUser.Username,
                OpeningCash = 0m
            });

            if (!openResult.Success)
            {
                MessageBox.Show(openResult.Message, "Punto de venta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            return openResult.Data;
        }

        private void OpenAdministration()
        {
            if (string.Equals(this.authenticatedUser.RoleCode, SystemRoles.Cashier, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("El rol cajero no tiene acceso a la administración.", "Administración", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var administrationForm = new AdministrationForm(this.catalogApplicationService, this.userAdministrationService))
            {
                administrationForm.ShowDialog(this);
            }
        }

        private void OpenCashShift()
        {
            using (var cashShiftForm = new CashShiftForm(this.startupState, this.authenticatedUser, this.cashShiftApplicationService))
            {
                cashShiftForm.ShowDialog(this);
            }

            RefreshDashboard();
        }

        private void OpenOperationSettings()
        {
            if (this.operationSettingsApplicationService == null)
            {
                MessageBox.Show("La configuración de operación no está disponible.", "Plataformas y mesas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new OperationSettingsForm(this.startupState, this.operationSettingsApplicationService))
            {
                form.ShowDialog(this);
            }

            LoadOperationSettings();
        }

        private void OpenStatisticsAndReports()
        {
            using (var form = new StatisticsReportsForm(this.startupState, this.cashShiftApplicationService, this.salesApplicationService, this.authenticatedUser))
            {
                form.ShowDialog(this);
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

        private void OpenTicketPrintSettings()
        {
            using (var settingsForm = new TicketPrintSettingsForm(this.startupState, this.authenticatedUser, this.ticketPrinter, this.ticketPrintSettingsApplicationService))
            {
                settingsForm.ShowDialog(this);
            }
        }

        private void OpenTelegramSettings()
        {
            if (this.telegramIntegrationService == null)
            {
                MessageBox.Show("La integración de Telegram no está disponible.", "Telegram", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new TelegramSettingsForm(this.telegramIntegrationService))
            {
                form.ShowDialog(this);
            }
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

        private void RefreshDashboard()
        {
            var activeShift = this.cashShiftApplicationService.GetActiveShift(this.startupState.StationCode);
            if (this.shiftStatusLabel != null)
            {
                this.shiftStatusLabel.Text = activeShift.Success
                    ? activeShift.Data.Folio + " | Ventas: $" + activeShift.Data.SalesTotal.ToString("N2") + " | Esperado: $" + activeShift.Data.ExpectedCash.ToString("N2")
                    : "Sin corte abierto. Abre caja antes de operar ventas.";
            }

            if (this.cashierShiftStatusCompactLabel != null)
            {
                this.cashierShiftStatusCompactLabel.Text = this.shiftStatusLabel == null ? string.Empty : this.shiftStatusLabel.Text;
            }

            if (this.administrationButton != null)
            {
                this.administrationButton.Enabled = !string.Equals(this.authenticatedUser.RoleCode, SystemRoles.Cashier, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void BuildCashierDashboard()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 74,
                ColumnCount = 3,
                BackColor = Color.FromArgb(45, 160, 255),
                Padding = new Padding(12, 10, 12, 10)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420F));
            header.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Punto de Venta",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                Padding = new Padding(8, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            header.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = this.authenticatedUser.DisplayName,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                Padding = new Padding(0, 0, 8, 0),
                TextAlign = ContentAlignment.MiddleCenter
            }, 1, 0);
            header.Controls.Add(BuildCashierHeaderActions(), 2, 0);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(14, 10, 14, 10),
                BackColor = Color.FromArgb(192, 233, 255)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 165F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var pendingPanel = BuildPendingTicketsPanel();
            var productsPanel = BuildCashierProductsPanel();
            var cartPanel = BuildCashierCartPanel();

            root.Controls.Add(pendingPanel, 0, 0);
            root.Controls.Add(productsPanel, 0, 1);
            root.Controls.Add(cartPanel, 1, 0);
            root.SetRowSpan(cartPanel, 2);

            Controls.Add(root);
            Controls.Add(header);
        }

        private Control BuildCashierHeaderActions()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0, 2, 0, 2),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false
            };

            panel.Controls.Add(CreateHeaderActionButton("Corte activo y ventas", Color.FromArgb(245, 158, 11), ShowCashierDailySummaryDialog));
            panel.Controls.Add(CreateHeaderActionButton("Cerrar sesión", Color.FromArgb(220, 80, 85), delegate
            {
                this.LogoutRequested = true;
                Close();
            }));
            return panel;
        }

        private void ShowCashierDailySummaryDialog()
        {
            var activeShift = ResolveOpenShiftForSummary();
            if (activeShift == null)
            {
                MessageBox.Show("No hay un corte abierto para consultar ventas.", "Corte y ventas", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var recentShifts = this.cashShiftApplicationService
                .GetRecentShifts(this.startupState.StationCode, 30)
                .Where(item => item != null)
                .OrderByDescending(item => item.OpenedUtc)
                .Take(2)
                .ToList();
            if (!recentShifts.Any(item => item.ShiftId == activeShift.ShiftId))
            {
                recentShifts.Insert(0, activeShift);
                recentShifts = recentShifts
                    .OrderByDescending(item => item.OpenedUtc)
                    .GroupBy(item => item.ShiftId)
                    .Select(group => group.First())
                    .Take(2)
                    .ToList();
            }

            var shiftOptions = recentShifts
                .Select(item => new ShiftSelectionItem
                {
                    ShiftId = item.ShiftId,
                    Folio = item.Folio,
                    Status = item.Status,
                    OpenedUtc = item.OpenedUtc
                })
                .ToList();

            using (var dialog = new Form())
            {
                dialog.Text = "Corte y ventas";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(980, 580);
                dialog.FormBorderStyle = FormBorderStyle.Sizable;
                dialog.MinimumSize = new Size(900, 520);
                dialog.BackColor = Color.White;
                dialog.Font = new Font("Segoe UI", 10F);

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 8, Padding = new Padding(16) };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

                var shiftSelectorPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
                shiftSelectorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
                shiftSelectorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                shiftSelectorPanel.Controls.Add(new Label
                {
                    Text = "Corte:",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
                }, 0, 0);
                var shiftCombo = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DataSource = shiftOptions
                };
                shiftSelectorPanel.Controls.Add(shiftCombo, 1, 0);

                var summaryTitleLabel = new Label { Dock = DockStyle.Fill, Text = string.Empty, Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(27, 67, 50), TextAlign = ContentAlignment.MiddleLeft };
                var summaryCountsLabel = new Label { Dock = DockStyle.Fill, Text = string.Empty, TextAlign = ContentAlignment.MiddleLeft };
                var summaryConfirmedLabel = new Label { Dock = DockStyle.Fill, Text = string.Empty, TextAlign = ContentAlignment.MiddleLeft };
                var summaryCancelledLabel = new Label { Dock = DockStyle.Fill, Text = string.Empty, TextAlign = ContentAlignment.MiddleLeft };
                var summaryNetLabel = new Label { Dock = DockStyle.Fill, Text = string.Empty, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };

                layout.Controls.Add(shiftSelectorPanel, 0, 0);
                layout.Controls.Add(summaryTitleLabel, 0, 1);
                layout.Controls.Add(summaryCountsLabel, 0, 2);
                layout.Controls.Add(summaryConfirmedLabel, 0, 3);
                layout.Controls.Add(summaryCancelledLabel, 0, 4);
                layout.Controls.Add(summaryNetLabel, 0, 5);

                var detailHost = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
                detailHost.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
                detailHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                var salesList = new ListBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 10F),
                    HorizontalScrollbar = true
                };

                var detailPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(10, 0, 0, 0) };
                detailPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                detailPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                detailPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                detailPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                detailPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                detailPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                var saleHeaderLabel = new Label { Dock = DockStyle.Fill, Text = "Selecciona un ticket vendido para ver su detalle.", Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
                var saleMeta1Label = new Label { Dock = DockStyle.Fill, Text = string.Empty, TextAlign = ContentAlignment.MiddleLeft };
                var saleMeta2Label = new Label { Dock = DockStyle.Fill, Text = string.Empty, TextAlign = ContentAlignment.MiddleLeft };
                var saleMeta3Label = new Label { Dock = DockStyle.Fill, Text = string.Empty, TextAlign = ContentAlignment.MiddleLeft };
                var cancelTicketButton = new Button
                {
                    Text = "Cancelar ticket",
                    Width = 140,
                    Height = 32,
                    BackColor = Color.FromArgb(220, 80, 85),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Enabled = false
                };
                cancelTicketButton.FlatAppearance.BorderSize = 0;

                var detailActions = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft,
                    WrapContents = false,
                    Margin = new Padding(0)
                };
                detailActions.Controls.Add(cancelTicketButton);

                ShiftSelectionItem selectedShift = null;
                IList<SoldTicketSummary> soldTickets = new List<SoldTicketSummary>();
                Button confirmShiftButton = null;

                var itemsGrid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AutoGenerateColumns = false,
                    AllowUserToAddRows = false,
                    ReadOnly = true,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };
                itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Producto", DataPropertyName = "ProductName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
                itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cant.", DataPropertyName = "Quantity", Width = 70 });
                itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unit.", DataPropertyName = "UnitPrice", Width = 85, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
                itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Total", DataPropertyName = "LineTotal", Width = 95, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });

                salesList.SelectedIndexChanged += delegate
                {
                    var selected = salesList.SelectedItem as SoldTicketSummary;
                    if (selected == null)
                    {
                        itemsGrid.DataSource = null;
                        saleHeaderLabel.Text = "Selecciona un ticket vendido para ver su detalle.";
                        saleMeta1Label.Text = string.Empty;
                        saleMeta2Label.Text = string.Empty;
                        saleMeta3Label.Text = string.Empty;
                        cancelTicketButton.Enabled = false;
                        return;
                    }

                    saleHeaderLabel.Text = "Ticket " + selected.Folio + " | " + selected.OrderDisplayName;
                    saleMeta1Label.Text = "Estado: " + selected.Status + " | Fecha: " + selected.SoldLocal.ToString("dd/MM/yyyy HH:mm") + " | Cajero: " + selected.CashierDisplayName;
                    saleMeta2Label.Text = "Total: $" + selected.Total.ToString("N2");
                    saleMeta3Label.Text = string.IsNullOrWhiteSpace(selected.Note) ? string.Empty : "Nota: " + selected.Note;
                    cancelTicketButton.Enabled = selected.Status == SaleStatus.Confirmada;
                    itemsGrid.DataSource = selected.Items.Select(item => new
                    {
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        LineTotal = item.LineTotal
                    }).ToList();
                };

                cancelTicketButton.Click += delegate
                {
                    var selected = salesList.SelectedItem as SoldTicketSummary;
                    if (selected == null)
                    {
                        MessageBox.Show("Selecciona un ticket para cancelar.", "Corte y ventas", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (selected.Status != SaleStatus.Confirmada)
                    {
                        MessageBox.Show("El ticket seleccionado ya está cancelado.", "Corte y ventas", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var answer = MessageBox.Show(
                        "¿Seguro que deseas cancelar el ticket " + selected.Folio + "?",
                        "Cancelar ticket",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (answer != DialogResult.Yes)
                    {
                        return;
                    }

                    var cancelResult = this.salesApplicationService.CancelSale(new CancelSaleRequest
                    {
                        SaleId = selected.SaleId,
                        UserId = this.authenticatedUser.UserId,
                        UserName = this.authenticatedUser.Username,
                        CashShiftId = selectedShift == null ? (int?)null : selectedShift.ShiftId
                    });

                    MessageBox.Show(cancelResult.Message, "Cancelar ticket", MessageBoxButtons.OK, cancelResult.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                    if (!cancelResult.Success)
                    {
                        return;
                    }

                    ApplyShiftSummary(selectedShift);

                    RefreshDashboard();
                };

                detailPanel.Controls.Add(detailActions, 0, 0);
                detailPanel.Controls.Add(saleHeaderLabel, 0, 1);
                detailPanel.Controls.Add(saleMeta1Label, 0, 2);
                detailPanel.Controls.Add(saleMeta2Label, 0, 3);
                detailPanel.Controls.Add(saleMeta3Label, 0, 4);
                detailPanel.Controls.Add(itemsGrid, 0, 5);

                detailHost.Controls.Add(salesList, 0, 0);
                detailHost.Controls.Add(detailPanel, 1, 0);
                layout.Controls.Add(detailHost, 0, 6);

                var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
                var closeButton = new Button { Text = "Cerrar", Width = 110, Height = 32, DialogResult = DialogResult.Cancel };
                confirmShiftButton = new Button
                {
                    Text = "Confirmar Corte",
                    Width = 150,
                    Height = 32,
                    BackColor = Color.FromArgb(27, 67, 50),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                confirmShiftButton.FlatAppearance.BorderSize = 0;

                confirmShiftButton.Click += delegate
                {
                    if (selectedShift == null || !string.Equals(selectedShift.Status, "abierta", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("Solo puedes cerrar un corte que esté abierto.", "Confirmar corte", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var answer = MessageBox.Show(
                        "¿Deseas confirmar y cerrar el corte activo " + selectedShift.Folio + "?",
                        "Confirmar corte",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (answer != DialogResult.Yes)
                    {
                        return;
                    }

                    var countedCash = PromptActualCashForCloseShift(activeShift.ExpectedCash);
                    if (!countedCash.HasValue)
                    {
                        return;
                    }

                    var confirmedTickets = soldTickets.Where(item => item.Status == SaleStatus.Confirmada).ToList();
                    var cancelledTickets = soldTickets.Where(item => item.Status == SaleStatus.Cancelada).ToList();
                    var confirmedTotal = confirmedTickets.Sum(item => item.Total);
                    var cancelledTotal = cancelledTickets.Sum(item => item.Total);
                    var netTotal = confirmedTotal - cancelledTotal;
                    var cashTotal = soldTickets.Where(item => item.PaymentMethod == PaymentMethod.Efectivo).Sum(item => item.Status == SaleStatus.Cancelada ? -item.Total : item.Total);
                    var cardTotal = soldTickets.Where(item => item.PaymentMethod == PaymentMethod.Tarjeta).Sum(item => item.Status == SaleStatus.Cancelada ? -item.Total : item.Total);
                    var transferTotal = soldTickets.Where(item => item.PaymentMethod == PaymentMethod.Transferencia).Sum(item => item.Status == SaleStatus.Cancelada ? -item.Total : item.Total);
                    var investmentTotal = soldTickets.Sum(item => item.Status == SaleStatus.Cancelada ? -item.EstimatedCostTotal : item.EstimatedCostTotal);
                    var profitTotal = soldTickets.Sum(item => item.Status == SaleStatus.Cancelada ? -item.EstimatedProfitTotal : item.EstimatedProfitTotal);

                    var closeResult = this.cashShiftApplicationService.CloseShift(new CloseCashShiftRequest
                    {
                        ShiftId = selectedShift == null ? 0 : selectedShift.ShiftId,
                        UserId = this.authenticatedUser.UserId,
                        UserName = this.authenticatedUser.Username,
                        ActualCash = countedCash.Value,
                        TelegramSummaryMessage = "[CORTE] " + selectedShift.Folio + " cerrado por " + (this.authenticatedUser.Username ?? string.Empty)
                            + ". Neto: $" + netTotal.ToString("N2")
                            + " | Efectivo: $" + cashTotal.ToString("N2")
                            + " | Tarjeta: $" + cardTotal.ToString("N2")
                            + " | Transferencia: $" + transferTotal.ToString("N2")
                            + " | Inversion: $" + investmentTotal.ToString("N2")
                            + " | Ganancia: $" + profitTotal.ToString("N2")
                            + " | Esperado: $" + activeShift.ExpectedCash.ToString("N2")
                            + " | Real: $" + countedCash.Value.ToString("N2") + "."
                    });

                    MessageBox.Show(closeResult.Message, "Confirmar corte", MessageBoxButtons.OK, closeResult.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                    if (!closeResult.Success)
                    {
                        return;
                    }

                    RefreshDashboard();
                    dialog.Close();
                };

                buttons.Controls.Add(closeButton);
                buttons.Controls.Add(confirmShiftButton);
                layout.Controls.Add(buttons, 0, 7);

                dialog.Controls.Add(layout);
                dialog.CancelButton = closeButton;

                void ApplyShiftSummary(ShiftSelectionItem shift)
                {
                    selectedShift = shift;
                    if (selectedShift == null)
                    {
                        return;
                    }

                    soldTickets = LoadSoldTicketsForShift(selectedShift.ShiftId);
                    var confirmedCount = soldTickets.Count(item => item.Status == SaleStatus.Confirmada);
                    var cancelledCount = soldTickets.Count(item => item.Status == SaleStatus.Cancelada);
                    var confirmedTotal = soldTickets.Where(item => item.Status == SaleStatus.Confirmada).Sum(item => item.Total);
                    var cancelledTotal = soldTickets.Where(item => item.Status == SaleStatus.Cancelada).Sum(item => item.Total);
                    var netTotal = confirmedTotal - cancelledTotal;
                    var cashTotal = soldTickets.Where(item => item.PaymentMethod == PaymentMethod.Efectivo).Sum(item => item.Status == SaleStatus.Cancelada ? -item.Total : item.Total);
                    var cardTotal = soldTickets.Where(item => item.PaymentMethod == PaymentMethod.Tarjeta).Sum(item => item.Status == SaleStatus.Cancelada ? -item.Total : item.Total);
                    var transferTotal = soldTickets.Where(item => item.PaymentMethod == PaymentMethod.Transferencia).Sum(item => item.Status == SaleStatus.Cancelada ? -item.Total : item.Total);
                    var investmentTotal = soldTickets.Sum(item => item.Status == SaleStatus.Cancelada ? -item.EstimatedCostTotal : item.EstimatedCostTotal);
                    var profitTotal = soldTickets.Sum(item => item.Status == SaleStatus.Cancelada ? -item.EstimatedProfitTotal : item.EstimatedProfitTotal);

                    var statusText = string.Equals(selectedShift.Status, "abierta", StringComparison.OrdinalIgnoreCase) ? "Activo" : "Cerrado";
                    summaryTitleLabel.Text = "Resumen del corte " + selectedShift.Folio + " (" + statusText + ")";
                    summaryCountsLabel.Text = "Tickets vendidos: " + soldTickets.Count + "  |  Confirmadas: " + confirmedCount + "  |  Canceladas: " + cancelledCount;
                    summaryConfirmedLabel.Text = "Ventas confirmadas: $" + confirmedTotal.ToString("N2");
                    summaryCancelledLabel.Text = "Cancelaciones: $" + cancelledTotal.ToString("N2");
                    summaryNetLabel.Text = "Total neto: $" + netTotal.ToString("N2")
                        + " | Efectivo: $" + cashTotal.ToString("N2")
                        + " | Tarjeta: $" + cardTotal.ToString("N2")
                        + " | Transferencia: $" + transferTotal.ToString("N2")
                        + Environment.NewLine
                        + "Inversion estimada: $" + investmentTotal.ToString("N2")
                        + " | Ganancia estimada: $" + profitTotal.ToString("N2");

                    salesList.BeginUpdate();
                    salesList.Items.Clear();
                    foreach (var ticket in soldTickets)
                    {
                        salesList.Items.Add(ticket);
                    }
                    salesList.EndUpdate();

                    itemsGrid.DataSource = null;
                    saleHeaderLabel.Text = soldTickets.Count == 0 ? "No hay tickets registrados en este corte." : "Selecciona un ticket vendido para ver su detalle.";
                    saleMeta1Label.Text = string.Empty;
                    saleMeta2Label.Text = string.Empty;
                    saleMeta3Label.Text = string.Empty;
                    cancelTicketButton.Enabled = false;
                    confirmShiftButton.Enabled = string.Equals(selectedShift.Status, "abierta", StringComparison.OrdinalIgnoreCase);

                    if (salesList.Items.Count > 0)
                    {
                        salesList.SelectedIndex = 0;
                    }
                }

                shiftCombo.SelectedIndexChanged += delegate
                {
                    ApplyShiftSummary(shiftCombo.SelectedItem as ShiftSelectionItem);
                };

                var initial = shiftOptions.FirstOrDefault(item => item.ShiftId == activeShift.ShiftId) ?? shiftOptions.FirstOrDefault();
                if (initial != null)
                {
                    shiftCombo.SelectedItem = initial;
                    ApplyShiftSummary(initial);
                }

                dialog.ShowDialog(this);
            }
        }

        private CashShiftSummaryView ResolveOpenShiftForSummary()
        {
            var activeShiftResult = this.cashShiftApplicationService.GetActiveShift(this.startupState.StationCode);
            if (activeShiftResult.Success && activeShiftResult.Data != null)
            {
                return activeShiftResult.Data;
            }

            return LoadOpenShiftForStationFromDatabase(this.startupState.StationCode);
        }

        private static CashShiftSummaryView LoadOpenShiftForStationFromDatabase(string stationCode)
        {
            try
            {
                var normalizedStation = (stationCode ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(normalizedStation))
                {
                    return null;
                }

                var paths = StationPathsFactory.CreateDefault();
                StationPathsFactory.EnsureCreated(paths);
                if (!File.Exists(paths.DatabaseFilePath))
                {
                    return null;
                }

                using (var connection = new SQLiteConnection("Data Source=" + paths.DatabaseFilePath + ";Version=3;Foreign Keys=True;"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
SELECT id, folio, station_code, opening_cash, expected_cash, COALESCE(actual_cash, 0), sales_total, estimated_profit_total
FROM cash_shifts
WHERE status = 'abierta'
  AND trim(station_code) = @stationCode
ORDER BY id DESC
LIMIT 1;";
                        command.Parameters.AddWithValue("@stationCode", normalizedStation);

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return null;
                            }

                            return new CashShiftSummaryView
                            {
                                ShiftId = reader.GetInt32(0),
                                Folio = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                StationCode = reader.IsDBNull(2) ? normalizedStation : reader.GetString(2),
                                OpeningCash = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3), CultureInfo.InvariantCulture),
                                ExpectedCash = reader.IsDBNull(4) ? 0m : Convert.ToDecimal(reader.GetValue(4), CultureInfo.InvariantCulture),
                                ActualCash = reader.IsDBNull(5) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(5), CultureInfo.InvariantCulture),
                                SalesTotal = reader.IsDBNull(6) ? 0m : Convert.ToDecimal(reader.GetValue(6), CultureInfo.InvariantCulture),
                                EstimatedProfitTotal = reader.IsDBNull(7) ? 0m : Convert.ToDecimal(reader.GetValue(7), CultureInfo.InvariantCulture),
                                Status = "abierta"
                            };
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private IList<SoldTicketSummary> LoadSoldTicketsForShift(int shiftId)
        {
            var tickets = new List<SoldTicketSummary>();
            try
            {
                if (shiftId <= 0)
                {
                    return tickets;
                }

                var paths = StationPathsFactory.CreateDefault();
                StationPathsFactory.EnsureCreated(paths);
                if (!File.Exists(paths.DatabaseFilePath))
                {
                    return tickets;
                }

                using (var connection = new SQLiteConnection("Data Source=" + paths.DatabaseFilePath + ";Version=3;Foreign Keys=True;"))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
SELECT s.id, s.folio, s.order_kind, s.status, s.sold_utc, s.total, s.note, COALESCE(u.display_name, ''), COALESCE(s.payment_summary, ''), COALESCE(s.estimated_cost_total, 0), COALESCE(s.estimated_profit_total, 0)
FROM sales s
LEFT JOIN users u ON u.id = s.sold_by_user_id
WHERE s.cash_shift_id = @shiftId
ORDER BY s.sold_utc DESC;";
                                                command.Parameters.AddWithValue("@shiftId", shiftId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var ticket = new SoldTicketSummary
                                {
                                    SaleId = reader.GetInt32(0),
                                    Folio = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                    OrderKind = reader.IsDBNull(2) ? OrderKind.Mostrador : (OrderKind)Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture),
                                    Status = reader.IsDBNull(3) ? SaleStatus.Confirmada : (SaleStatus)Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
                                    SoldLocal = reader.IsDBNull(4) ? DateTime.Now : DateTime.SpecifyKind(DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), DateTimeKind.Utc).ToLocalTime(),
                                    Total = reader.IsDBNull(5) ? 0m : Convert.ToDecimal(reader.GetValue(5), CultureInfo.InvariantCulture),
                                    Note = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                    CashierDisplayName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                                    PaymentMethod = ParsePaymentMethod(reader.IsDBNull(8) ? string.Empty : reader.GetString(8)),
                                    EstimatedCostTotal = reader.IsDBNull(9) ? 0m : Convert.ToDecimal(reader.GetValue(9), CultureInfo.InvariantCulture),
                                    EstimatedProfitTotal = reader.IsDBNull(10) ? 0m : Convert.ToDecimal(reader.GetValue(10), CultureInfo.InvariantCulture)
                                };
                                ticket.OrderDisplayName = BuildSoldTicketOrderDisplayName(ticket.OrderKind, ticket.Note);
                                tickets.Add(ticket);
                            }
                        }
                    }

                    foreach (var ticket in tickets)
                    {
                        using (var itemCommand = connection.CreateCommand())
                        {
                            itemCommand.CommandText = @"
SELECT product_name_snapshot, quantity, unit_price, line_total
FROM sale_items
WHERE sale_id = @saleId
ORDER BY id ASC;";
                            itemCommand.Parameters.AddWithValue("@saleId", ticket.SaleId);
                            using (var itemReader = itemCommand.ExecuteReader())
                            {
                                while (itemReader.Read())
                                {
                                    ticket.Items.Add(new SoldTicketLine
                                    {
                                        ProductName = itemReader.IsDBNull(0) ? string.Empty : itemReader.GetString(0),
                                        Quantity = itemReader.IsDBNull(1) ? 0m : Convert.ToDecimal(itemReader.GetValue(1), CultureInfo.InvariantCulture),
                                        UnitPrice = itemReader.IsDBNull(2) ? 0m : Convert.ToDecimal(itemReader.GetValue(2), CultureInfo.InvariantCulture),
                                        LineTotal = itemReader.IsDBNull(3) ? 0m : Convert.ToDecimal(itemReader.GetValue(3), CultureInfo.InvariantCulture)
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                return tickets;
            }

            return tickets;
        }

        private static string BuildSoldTicketOrderDisplayName(OrderKind orderKind, string note)
        {
            if (orderKind == OrderKind.Mesa && !string.IsNullOrWhiteSpace(note) && note.StartsWith("Mesa ", StringComparison.OrdinalIgnoreCase))
            {
                return note.Trim();
            }

            if (orderKind == OrderKind.ParaLlevar && !string.IsNullOrWhiteSpace(note) && note.StartsWith("Cliente:", StringComparison.OrdinalIgnoreCase))
            {
                return "Para llevar";
            }

            return orderKind.ToString();
        }

        private static PaymentMethod ParsePaymentMethod(string raw)
        {
            var value = (raw ?? string.Empty).Trim();
            if (string.Equals(value, PaymentMethod.Tarjeta.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return PaymentMethod.Tarjeta;
            }

            if (string.Equals(value, PaymentMethod.Transferencia.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return PaymentMethod.Transferencia;
            }

            return PaymentMethod.Efectivo;
        }

        private Control BuildCashierProductsPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.White,
                Padding = new Padding(12, 10, 12, 12),
                Margin = new Padding(8, 0, 8, 0)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var titleRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));

            this.cashierReprintButton = new Button
            {
                Text = "Reimprimir último",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(21, 128, 61),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Margin = new Padding(6, 0, 6, 0)
            };
            this.cashierReprintButton.FlatAppearance.BorderSize = 0;
            this.cashierReprintButton.Click += delegate { ReprintLastTicket(); };

            this.cashierNewOrderButton = new Button
            {
                Text = "Nueva orden",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Margin = new Padding(6, 0, 6, 0)
            };
            this.cashierNewOrderButton.FlatAppearance.BorderSize = 0;
            this.cashierNewOrderButton.Click += delegate { ShowOrderTypeDialog(); };

            this.cashierProductsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(4)
            };
            this.cashierOrderControls.Add(this.cashierProductsPanel);

            titleRow.Controls.Add(new Label
            {
                Text = "Productos",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 84, 132),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            titleRow.Controls.Add(this.cashierNewOrderButton, 1, 0);
            titleRow.Controls.Add(this.cashierReprintButton, 2, 0);

            this.cashierShiftStatusCompactLabel = new Label { Dock = DockStyle.Fill, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 9F), TextAlign = ContentAlignment.MiddleLeft };

            panel.Controls.Add(titleRow, 0, 0);
            panel.Controls.Add(this.cashierShiftStatusCompactLabel, 0, 1);
            panel.Controls.Add(this.cashierProductsPanel, 0, 2);
            return panel;
        }

        private Control BuildCashierCartPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                BackColor = Color.White,
                Padding = new Padding(12),
                Margin = new Padding(8, 0, 0, 0)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

            this.cashierCartGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                DataSource = this.cashierCartLines,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };
            this.cashierCartGrid.RowTemplate.Height = 36;
            this.cashierCartGrid.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "QtyMinus",
                HeaderText = string.Empty,
                UseColumnTextForButtonValue = false,
                Width = 34,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(243, 244, 246),
                    ForeColor = Color.FromArgb(55, 65, 81),
                    SelectionBackColor = Color.FromArgb(243, 244, 246),
                    SelectionForeColor = Color.FromArgb(55, 65, 81),
                    Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
                }
            });
            this.cashierCartGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Quantity",
                HeaderText = "Cant.",
                DataPropertyName = "Quantity",
                Width = 48,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
                }
            });
            this.cashierCartGrid.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "QtyPlus",
                HeaderText = string.Empty,
                UseColumnTextForButtonValue = false,
                Width = 34,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(232, 240, 254),
                    ForeColor = Color.FromArgb(30, 64, 175),
                    SelectionBackColor = Color.FromArgb(232, 240, 254),
                    SelectionForeColor = Color.FromArgb(30, 64, 175),
                    Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
                }
            });
            this.cashierCartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nombre", DataPropertyName = "ProductName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            this.cashierCartGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Price",
                HeaderText = "Precio",
                DataPropertyName = "UnitPrice",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            this.cashierCartGrid.CellMouseClick += CashierCartGridCellMouseClick;
            this.cashierCartGrid.CellFormatting += CashierCartGridCellFormatting;

            this.cashierOrderKindComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            this.cashierPaymentMethodComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            this.cashierCurrentOrderLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.DimGray, Text = "Menú bloqueado. Presiona Nueva orden." };
            this.cashierTotalLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold), Text = "$0.00" };
            this.cashierOrderControls.Add(this.cashierOrderKindComboBox);
            this.cashierOrderControls.Add(this.cashierPaymentMethodComboBox);
            this.cashierOrderControls.Add(this.cashierCartGrid);

            var cartTitleRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            cartTitleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98F));
            cartTitleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.cashierCartTitleLabel = new Label { Text = "Carrito", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(27, 67, 50), TextAlign = ContentAlignment.MiddleLeft };
            this.cashierCartTicketLabel = new Label { Text = string.Empty, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold), ForeColor = Color.FromArgb(245, 158, 11), TextAlign = ContentAlignment.MiddleLeft };
            cartTitleRow.Controls.Add(this.cashierCartTitleLabel, 0, 0);
            cartTitleRow.Controls.Add(this.cashierCartTicketLabel, 1, 0);

            this.cashierChargeButton = new Button
            {
                Text = "Cobrar venta",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(234, 88, 12),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Margin = new Padding(0, 2, 0, 2)
            };
            this.cashierChargeButton.FlatAppearance.BorderSize = 0;
            this.cashierChargeButton.Click += ChargeCashierSale;
            this.cashierOrderControls.Add(this.cashierChargeButton);

            this.cashierPendingButton = new Button
            {
                Text = "Guardar pendiente",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(8, 109, 156),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Margin = new Padding(0, 2, 0, 2)
            };
            this.cashierPendingButton.FlatAppearance.BorderSize = 0;
            this.cashierPendingButton.Click += SaveCurrentCartAsPending;
            this.cashierOrderControls.Add(this.cashierPendingButton);

            panel.Controls.Add(cartTitleRow, 0, 0);
            panel.Controls.Add(this.cashierCartGrid, 0, 1);
            panel.Controls.Add(this.cashierCurrentOrderLabel, 0, 2);
            panel.Controls.Add(BuildComboRow(string.Empty, this.cashierOrderKindComboBox, string.Empty, this.cashierPaymentMethodComboBox), 0, 3);
            panel.Controls.Add(this.cashierTotalLabel, 0, 4);
            panel.Controls.Add(this.cashierChargeButton, 0, 5);
            panel.Controls.Add(this.cashierPendingButton, 0, 6);
            return panel;
        }

        private Control BuildPendingTicketsPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 8, 8)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.pendingTicketsFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.White
            };
            this.pendingSummaryLabel = new Label { Dock = DockStyle.Bottom, Height = 20, ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft };

            panel.Controls.Add(new Label { Text = "Tickets pendientes", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(27, 67, 50) }, 0, 0);
            var listHost = new Panel { Dock = DockStyle.Fill };
            listHost.Controls.Add(this.pendingTicketsFlowPanel);
            listHost.Controls.Add(this.pendingSummaryLabel);
            panel.Controls.Add(listHost, 0, 1);

            RefreshPendingTicketsView();
            return panel;
        }

        private void RefreshPendingTicketsView()
        {
            if (this.pendingTicketsFlowPanel == null)
            {
                return;
            }

            this.pendingTicketsFlowPanel.Controls.Clear();
            foreach (var ticket in this.pendingTickets)
            {
                var isSelected = this.activeCashierPendingTicket == ticket;
                var cardPanel = new Panel
                {
                    Width = 174,
                    Height = 72,
                    Margin = new Padding(4, 4, 4, 4),
                    BackColor = isSelected ? Color.FromArgb(191, 219, 254) : Color.FromArgb(239, 246, 255)
                };

                var ticketButton = new Button
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = isSelected ? Color.FromArgb(191, 219, 254) : Color.FromArgb(239, 246, 255),
                    ForeColor = isSelected ? Color.FromArgb(30, 64, 175) : Color.FromArgb(23, 37, 84),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                    Padding = new Padding(6, 8, 32, 4),
                    Text = ticket.Name + Environment.NewLine + "$" + ticket.Total.ToString("N2") + "  |  " + ticket.OrderKind,
                    Tag = ticket
                };
                ticketButton.FlatAppearance.BorderColor = isSelected ? Color.FromArgb(37, 99, 235) : Color.FromArgb(191, 219, 254);
                ticketButton.FlatAppearance.BorderSize = isSelected ? 2 : 1;
                ticketButton.Click += OpenSelectedPendingTicket;

                Action<bool> applyHover = delegate(bool hovered)
                {
                    var selected = this.activeCashierPendingTicket == ticket;
                    var cardColor = selected
                        ? Color.FromArgb(191, 219, 254)
                        : (hovered ? Color.FromArgb(224, 242, 254) : Color.FromArgb(239, 246, 255));
                    var foreColor = selected ? Color.FromArgb(30, 64, 175) : Color.FromArgb(23, 37, 84);
                    var borderColor = selected
                        ? Color.FromArgb(37, 99, 235)
                        : (hovered ? Color.FromArgb(96, 165, 250) : Color.FromArgb(191, 219, 254));

                    cardPanel.BackColor = cardColor;
                    ticketButton.BackColor = cardColor;
                    ticketButton.ForeColor = foreColor;
                    ticketButton.FlatAppearance.BorderColor = borderColor;
                    ticketButton.FlatAppearance.BorderSize = selected ? 2 : 1;
                };

                ticketButton.MouseEnter += delegate { applyHover(true); };
                ticketButton.MouseLeave += delegate { applyHover(false); };
                cardPanel.MouseEnter += delegate { applyHover(true); };
                cardPanel.MouseLeave += delegate { applyHover(false); };

                var deleteButton = new Button
                {
                    Width = 28,
                    Height = 28,
                    Text = "x",
                    BackColor = Color.FromArgb(220, 80, 85),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Location = new Point(cardPanel.Width - 30, 2),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Tag = ticket,
                    TabStop = false
                };
                deleteButton.FlatAppearance.BorderSize = 0;
                deleteButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 28, 28);
                deleteButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(153, 27, 27);
                deleteButton.UseVisualStyleBackColor = false;
                deleteButton.Click += DeletePendingTicketRequested;

                cardPanel.Controls.Add(ticketButton);
                cardPanel.Controls.Add(deleteButton);
                deleteButton.BringToFront();
                this.pendingTicketsFlowPanel.Controls.Add(cardPanel);
            }

            this.pendingSummaryLabel.Text = this.pendingTickets.Count == 0
                ? "No hay tickets pendientes."
                : "Selecciona un ticket para cargarlo al carrito.";

            SavePendingTicketsToStorage();
        }

        private void OpenSelectedPendingTicket(object sender, EventArgs eventArgs)
        {
            var sourceButton = sender as Button;
            if (sourceButton == null || !(sourceButton.Tag is PendingTicketDraft))
            {
                return;
            }

            var pending = (PendingTicketDraft)sourceButton.Tag;
            LoadPendingTicketIntoCashier(pending);
            RefreshPendingTicketsView();
        }

        private void DeletePendingTicketRequested(object sender, EventArgs eventArgs)
        {
            var sourceButton = sender as Button;
            if (sourceButton == null || !(sourceButton.Tag is PendingTicketDraft))
            {
                return;
            }

            var pending = (PendingTicketDraft)sourceButton.Tag;
            var answer = MessageBox.Show(
                "¿Seguro que deseas eliminar el ticket pendiente '" + pending.Name + "'?",
                "Eliminar ticket pendiente",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            if (this.activeCashierPendingTicket == pending)
            {
                this.activeCashierPendingTicket = null;
                ResetCashierOrderState();
            }

            this.pendingTickets.Remove(pending);
            RefreshPendingTicketsView();
        }

        private void ResetCashierOrderState()
        {
            this.cashierCartLines.Clear();
            this.cashierOrderNote = string.Empty;
            this.cashierDigitalPlatformUnitPrice = null;
            this.cashierDigitalPlatformName = string.Empty;
            this.cashierDigitalPlatformPricingMode = string.Empty;
            SetCashierOrderUiEnabled(false);
            this.selectedCashierCategoryId = null;
            LoadCashierCategories();
            UpdateCurrentOrderLabel();
            UpdateCashierTotal();
        }

        private void InitializeCashierCatalog()
        {
            if (this.cashierPaymentMethodComboBox == null || this.cashierOrderKindComboBox == null)
            {
                return;
            }

            this.cashierPaymentMethodComboBox.DataSource = new[]
            {
                PaymentMethod.Efectivo,
                PaymentMethod.Tarjeta,
                PaymentMethod.Transferencia
            };

            this.cashierOrderKindComboBox.DataSource = new[]
            {
                OrderKind.Mostrador,
                OrderKind.ParaLlevar,
                OrderKind.Mesa,
                OrderKind.PlataformaDigital
            };
            this.cashierOrderKindComboBox.SelectedIndexChanged += CashierOrderKindChanged;

            this.cashierCategories = this.catalogApplicationService.GetCategories() ?? new List<CategoryViewModel>();
            this.selectedCashierCategoryId = null;
            LoadCashierCategories();

            UpdateCashierTotal();
            SetCashierOrderUiEnabled(false);
            UpdateCurrentOrderLabel();
        }

        private void SetCashierOrderUiEnabled(bool enabled)
        {
            this.cashierOrderStarted = enabled;
            foreach (var control in this.cashierOrderControls)
            {
                if (control != null)
                {
                    control.Enabled = enabled;
                }
            }
        }

        private void LoadCashierCategories()
        {
            if (this.cashierProductsPanel == null)
            {
                return;
            }

            var categories = this.cashierCategories ?? new List<CategoryViewModel>();

            this.cashierProductsPanel.Controls.Clear();
            foreach (var category in categories)
            {
                var count = this.catalogApplicationService.GetProductsByCategory(category.Id).Count;
                this.cashierProductsPanel.Controls.Add(CreateCashierCategoryButton(category, count));
            }
        }

        private void LoadCashierProducts(int categoryId)
        {
            if (this.cashierProductsPanel == null)
            {
                return;
            }

            var products = this.catalogApplicationService.GetProductsByCategory(categoryId);

            this.cashierProductsPanel.Controls.Clear();
            var backButton = new Button
            {
                Width = 170,
                Height = 58,
                Margin = new Padding(8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(247, 249, 251),
                ForeColor = Color.FromArgb(56, 84, 132),
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Text = "<- Categorías"
            };
            backButton.FlatAppearance.BorderColor = Color.FromArgb(209, 220, 232);
            backButton.Click += delegate
            {
                this.selectedCashierCategoryId = null;
                LoadCashierCategories();
            };
            this.cashierProductsPanel.Controls.Add(backButton);

            foreach (var product in products)
            {
                this.cashierProductsPanel.Controls.Add(CreateCashierProductButton(product));
            }
        }

        private Button CreateCashierCategoryButton(CategoryViewModel category, int productCount)
        {
            var button = new Button
            {
                Width = 210,
                Height = 72,
                Margin = new Padding(8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(247, 249, 251),
                ForeColor = Color.FromArgb(56, 84, 132),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = category.Name + Environment.NewLine + productCount + " productos"
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(209, 220, 232);
            button.Click += delegate
            {
                this.selectedCashierCategoryId = category.Id;
                LoadCashierProducts(category.Id);
            };
            return button;
        }

        private void CashierCartGridCellMouseClick(object sender, DataGridViewCellMouseEventArgs eventArgs)
        {
            if (eventArgs.RowIndex < 0 || this.cashierCartGrid == null || eventArgs.ColumnIndex < 0)
            {
                return;
            }

            var line = this.cashierCartGrid.Rows[eventArgs.RowIndex].DataBoundItem as CartLineView;
            if (line == null)
            {
                return;
            }

            var column = this.cashierCartGrid.Columns[eventArgs.ColumnIndex];
            if (column == null)
            {
                return;
            }

            if (column.Name != "QtyMinus" && column.Name != "QtyPlus")
            {
                return;
            }

            if (column.Name == "QtyMinus")
            {
                line.Quantity -= 1;
                if (line.Quantity <= 0)
                {
                    this.cashierCartLines.Remove(line);
                }
                else
                {
                    RefreshCashierCart();
                }

                UpdateCashierTotal();
                SyncActivePendingDraftFromCurrentCart(false);
                return;
            }

            if (column.Name == "QtyPlus")
            {
                line.Quantity += 1;
                RefreshCashierCart();
                UpdateCashierTotal();
                SyncActivePendingDraftFromCurrentCart(false);
            }
        }

        private void CashierCartGridCellFormatting(object sender, DataGridViewCellFormattingEventArgs eventArgs)
        {
            if (eventArgs.RowIndex < 0 || this.cashierCartGrid == null || eventArgs.ColumnIndex < 0)
            {
                return;
            }

            var column = this.cashierCartGrid.Columns[eventArgs.ColumnIndex];
            if (column == null)
            {
                return;
            }

            if (column.Name == "QtyPlus")
            {
                eventArgs.Value = "+";
                eventArgs.FormattingApplied = true;
                return;
            }

            if (column.Name == "Price" && IsCurrentDigitalPlatformOrder())
            {
                eventArgs.Value = "-";
                eventArgs.FormattingApplied = true;
                return;
            }

            if (column.Name != "QtyMinus")
            {
                return;
            }

            var row = this.cashierCartGrid.Rows[eventArgs.RowIndex];
            var line = row == null ? null : row.DataBoundItem as CartLineView;
            eventArgs.Value = line != null && line.Quantity <= 1m ? "x" : "-";
            eventArgs.FormattingApplied = true;
        }

        private Button CreateCashierProductButton(ProductViewModel product)
        {
            var hidePrice = IsCurrentDigitalPlatformOrder();
            var currentPrice = ResolveCurrentCashierUnitPrice(product.SalePrice);
            var button = new Button
            {
                Width = 190,
                Height = 82,
                Margin = new Padding(8),
                FlatStyle = FlatStyle.Flat,
                BackColor = product.UsesInventory && product.StockOnHand <= 0 ? Color.FromArgb(230, 230, 230) : Color.FromArgb(247, 249, 251),
                ForeColor = Color.FromArgb(56, 84, 132),
                Font = new Font("Segoe UI", 10.5F),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = hidePrice
                    ? product.Name + Environment.NewLine + "Precio oculto en plataforma"
                    : product.Name + Environment.NewLine + "$" + currentPrice.ToString("N2")
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(209, 220, 232);
            button.Click += delegate
            {
                if (product.UsesInventory && product.StockOnHand <= 0)
                {
                    MessageBox.Show("Sin inventario disponible para " + product.Name + ".", "Inventario", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                AddProductToCashierCart(product);
            };
            return button;
        }

        private void AddProductToCashierCart(ProductViewModel product)
        {
            if (!this.cashierOrderStarted)
            {
                MessageBox.Show("Primero selecciona Nueva orden para habilitar el menú.", "Punto de venta", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var unitPrice = ResolveCurrentCashierUnitPrice(product.SalePrice);
            this.cashierCartLines.Add(new CartLineView
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = 1,
                UnitPrice = unitPrice,
                EstimatedCost = product.EstimatedCost,
                UsesInventory = product.UsesInventory
            });

            RefreshCashierCart();

            UpdateCashierTotal();
            SyncActivePendingDraftFromCurrentCart(false);
        }

        private decimal ResolveCurrentCashierUnitPrice(decimal productSalePrice)
        {
            var isDigitalPlatformOrder = this.cashierOrderStarted
                && this.cashierOrderKindComboBox != null
                && this.cashierOrderKindComboBox.SelectedItem is OrderKind
                && (OrderKind)this.cashierOrderKindComboBox.SelectedItem == OrderKind.PlataformaDigital;
            if (!isDigitalPlatformOrder || !this.cashierDigitalPlatformUnitPrice.HasValue || this.cashierDigitalPlatformUnitPrice.Value <= 0m)
            {
                return productSalePrice;
            }

            return 0m;
        }

        private void CashierOrderKindChanged(object sender, EventArgs eventArgs)
        {
            UpdateCurrentOrderLabel();
        }

        private int PromptTableNumber()
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Seleccionar mesa";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(460, 360);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                layout.Controls.Add(new Label { Text = "Elige la mesa", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(27, 67, 50) }, 0, 0);

                var tablesPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(247, 249, 251), Padding = new Padding(6) };
                var occupiedTables = GetOccupiedTableNumbers();
                var tableCount = this.configuredDiningTableCount <= 0 ? 5 : this.configuredDiningTableCount;
                for (var tableNumber = 1; tableNumber <= tableCount; tableNumber++)
                {
                    var currentTable = tableNumber;
                    var isOccupied = occupiedTables.Contains(currentTable);
                    var button = new Button
                    {
                        Text = isOccupied ? "Mesa " + currentTable.ToString("00") + Environment.NewLine + "Ocupada" : "Mesa " + currentTable.ToString("00"),
                        Width = 110,
                        Height = 54,
                        Margin = new Padding(6),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = isOccupied ? Color.FromArgb(232, 234, 237) : Color.White,
                        ForeColor = Color.FromArgb(27, 67, 50)
                    };
                    button.Enabled = !isOccupied;
                    button.Click += delegate
                    {
                        dialog.Tag = currentTable;
                        dialog.DialogResult = DialogResult.OK;
                    };
                    tablesPanel.Controls.Add(button);
                }

                layout.Controls.Add(tablesPanel, 0, 1);
                dialog.Controls.Add(layout);
                return dialog.ShowDialog(this) == DialogResult.OK && dialog.Tag is int ? (int)dialog.Tag : 0;
            }
        }

        private void IncreaseSelectedCashierLine(object sender, EventArgs eventArgs)
        {
            var line = GetSelectedCashierLine();
            if (line == null)
            {
                return;
            }

            line.Quantity += 1;
            RefreshCashierCart();
            UpdateCashierTotal();
            SyncActivePendingDraftFromCurrentCart(false);
        }

        private void DecreaseSelectedCashierLine(object sender, EventArgs eventArgs)
        {
            var line = GetSelectedCashierLine();
            if (line == null)
            {
                return;
            }

            line.Quantity -= 1;
            if (line.Quantity <= 0)
            {
                this.cashierCartLines.Remove(line);
            }
            else
            {
                RefreshCashierCart();
            }

            UpdateCashierTotal();
            SyncActivePendingDraftFromCurrentCart(false);
        }

        private void RemoveSelectedCashierLine(object sender, EventArgs eventArgs)
        {
            var line = GetSelectedCashierLine();
            if (line == null)
            {
                return;
            }

            this.cashierCartLines.Remove(line);
            UpdateCashierTotal();
            SyncActivePendingDraftFromCurrentCart(false);
        }

        private void ClearCashierCart(object sender, EventArgs eventArgs)
        {
            this.cashierCartLines.Clear();
            this.cashierOrderKindComboBox.SelectedItem = OrderKind.Mostrador;
            this.cashierOrderNote = string.Empty;
            this.cashierDigitalPlatformUnitPrice = null;
            this.cashierDigitalPlatformName = string.Empty;
            this.cashierDigitalPlatformPricingMode = string.Empty;
            UpdateCurrentOrderLabel();
            UpdateCashierTotal();
            SyncActivePendingDraftFromCurrentCart(false);
        }

        private void UpdateCashierTotal()
        {
            if (this.cashierTotalLabel == null)
            {
                return;
            }

            var total = GetCurrentOrderTotalForCharge();
            this.cashierTotalLabel.Text = "Total: $" + total.ToString("N2");
        }

        private bool IsCurrentDigitalPlatformOrder()
        {
            return this.cashierOrderStarted
                && this.cashierOrderKindComboBox != null
                && this.cashierOrderKindComboBox.SelectedItem is OrderKind
                && (OrderKind)this.cashierOrderKindComboBox.SelectedItem == OrderKind.PlataformaDigital;
        }

        private decimal GetCurrentOrderTotalForCharge()
        {
            if (IsCurrentDigitalPlatformOrder() && this.cashierDigitalPlatformUnitPrice.HasValue && this.cashierDigitalPlatformUnitPrice.Value > 0m)
            {
                return this.cashierDigitalPlatformUnitPrice.Value;
            }

            return this.cashierCartLines.Sum(item => item.LineTotal);
        }

        private IList<SaleLineDraft> BuildSaleLinesForCharge()
        {
            var lines = this.cashierCartLines.Select(item => new SaleLineDraft
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                EstimatedCost = item.EstimatedCost,
                UsesInventory = item.UsesInventory
            }).ToList();

            if (!IsCurrentDigitalPlatformOrder() || !this.cashierDigitalPlatformUnitPrice.HasValue || this.cashierDigitalPlatformUnitPrice.Value <= 0m || lines.Count == 0)
            {
                return lines;
            }

            var fixedTotal = this.cashierDigitalPlatformUnitPrice.Value;
            var totalQuantity = lines.Sum(item => item.Quantity <= 0m ? 0m : item.Quantity);
            if (totalQuantity <= 0m)
            {
                return lines;
            }

            var distributedUnit = decimal.Round(fixedTotal / totalQuantity, 6, MidpointRounding.AwayFromZero);
            foreach (var line in lines)
            {
                line.UnitPrice = distributedUnit;
            }

            var computedTotal = lines.Sum(item => item.Quantity * item.UnitPrice);
            var diff = fixedTotal - computedTotal;
            if (diff != 0m)
            {
                var firstLine = lines[0];
                var firstQty = firstLine.Quantity <= 0m ? 1m : firstLine.Quantity;
                firstLine.UnitPrice += diff / firstQty;
            }

            return lines;
        }

        private void UpdateCurrentOrderLabel()
        {
            if (this.cashierCurrentOrderLabel == null || this.cashierOrderKindComboBox == null || this.cashierOrderKindComboBox.SelectedItem == null)
            {
                return;
            }

            if (!this.cashierOrderStarted)
            {
                this.cashierCurrentOrderLabel.Text = "Menú bloqueado. Presiona Nueva orden.";
                if (this.cashierCartTicketLabel != null)
                {
                    this.cashierCartTicketLabel.Text = string.Empty;
                }
                return;
            }

            var kindText = this.cashierOrderKindComboBox.SelectedItem.ToString();
            var platformPriceLabel = this.cashierDigitalPlatformUnitPrice.HasValue
                ? " | Precio plataforma: $" + this.cashierDigitalPlatformUnitPrice.Value.ToString("N2")
                : string.Empty;
            this.cashierCurrentOrderLabel.Text = string.IsNullOrWhiteSpace(this.cashierOrderNote)
                ? "Orden activa: " + kindText + platformPriceLabel
                : "Orden activa: " + kindText + " | " + this.cashierOrderNote + platformPriceLabel;

            if (this.cashierCartTicketLabel != null)
            {
                var ticketName = GetCurrentTicketDisplayName();
                this.cashierCartTicketLabel.Text = string.IsNullOrWhiteSpace(ticketName) ? string.Empty : ticketName;
            }
        }

        private CartLineView GetSelectedCashierLine()
        {
            if (this.cashierCartGrid == null || this.cashierCartGrid.CurrentRow == null)
            {
                return null;
            }

            return this.cashierCartGrid.CurrentRow.DataBoundItem as CartLineView;
        }

        private void RefreshCashierCart()
        {
            if (this.cashierCartGrid != null)
            {
                this.cashierCartGrid.Refresh();
            }
        }

        private void ChargeCashierSale(object sender, EventArgs eventArgs)
        {
            if (!this.cashierOrderStarted)
            {
                MessageBox.Show("Primero inicia una orden.", "Punto de venta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (this.cashierCartLines.Count == 0)
            {
                MessageBox.Show("Agrega productos antes de cobrar.", "Punto de venta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var activeShift = EnsureActiveShift();
            if (activeShift == null)
            {
                return;
            }

            var defaultPaymentMethod = this.cashierPaymentMethodComboBox.SelectedItem is PaymentMethod
                ? (PaymentMethod)this.cashierPaymentMethodComboBox.SelectedItem
                : PaymentMethod.Efectivo;
            var orderTotal = GetCurrentOrderTotalForCharge();
            var selectedPaymentMethod = PromptPaymentMethodForCharge(defaultPaymentMethod, orderTotal);
            if (!selectedPaymentMethod.HasValue)
            {
                return;
            }

            this.cashierPaymentMethodComboBox.SelectedItem = selectedPaymentMethod.Value;

            TryPrintCashierPrecheck();

            var request = new CreateSaleRequest
            {
                UserId = this.authenticatedUser.UserId,
                UserName = this.authenticatedUser.Username,
                CashShiftId = activeShift.ShiftId,
                OrderKind = (OrderKind)this.cashierOrderKindComboBox.SelectedItem,
                PaymentMethod = selectedPaymentMethod.Value,
                Note = this.cashierOrderNote,
                Items = BuildSaleLinesForCharge()
            };

            var result = this.salesApplicationService.RegisterSale(request);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Punto de venta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedPaymentMethod.Value == PaymentMethod.Efectivo)
            {
                ShowCashChangeHelper(result.Data == null ? orderTotal : result.Data.Total);
            }

            if (this.activeCashierPendingTicket != null)
            {
                this.pendingTickets.Remove(this.activeCashierPendingTicket);
                this.activeCashierPendingTicket = null;
                RefreshPendingTicketsView();
            }

            this.cashierCartLines.Clear();
            this.cashierOrderNote = string.Empty;
            this.cashierDigitalPlatformUnitPrice = null;
            this.cashierDigitalPlatformName = string.Empty;
            this.cashierDigitalPlatformPricingMode = string.Empty;
            this.cashierOrderKindComboBox.SelectedItem = OrderKind.Mostrador;
            SetCashierOrderUiEnabled(false);
            UpdateCurrentOrderLabel();
            UpdateCashierTotal();
        }

        private PaymentMethod? PromptPaymentMethodForCharge(PaymentMethod defaultMethod, decimal total)
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Confirmar cobro";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(460, 230);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(12) };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

                var paymentCombo = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DrawMode = DrawMode.OwnerDrawFixed,
                    ItemHeight = 36,
                    Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                    IntegralHeight = false,
                    MaxDropDownItems = 3
                };
                paymentCombo.DataSource = new[] { PaymentMethod.Efectivo, PaymentMethod.Tarjeta, PaymentMethod.Transferencia };
                paymentCombo.SelectedItem = defaultMethod;
                paymentCombo.DrawItem += delegate(object sender, DrawItemEventArgs eventArgs)
                {
                    eventArgs.DrawBackground();
                    if (eventArgs.Index < 0)
                    {
                        return;
                    }

                    var text = paymentCombo.Items[eventArgs.Index].ToString();
                    var foreColor = (eventArgs.State & DrawItemState.Selected) == DrawItemState.Selected
                        ? SystemColors.HighlightText
                        : Color.FromArgb(33, 37, 41);
                    TextRenderer.DrawText(
                        eventArgs.Graphics,
                        text,
                        paymentCombo.Font,
                        eventArgs.Bounds,
                        foreColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                    eventArgs.DrawFocusRectangle();
                };

                var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
                var acceptButton = new Button { Text = "Cobrar", Width = 100, Height = 30, DialogResult = DialogResult.OK };
                var cancelButton = new Button { Text = "Cancelar", Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
                buttons.Controls.Add(acceptButton);
                buttons.Controls.Add(cancelButton);

                layout.Controls.Add(new Label { Text = "¿Deseas cobrar esta venta?", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
                layout.Controls.Add(new Label { Text = "Total a cobrar: $" + total.ToString("N2"), Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
                layout.Controls.Add(new Label { Text = "Método de pago:", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
                layout.Controls.Add(paymentCombo, 0, 3);
                layout.Controls.Add(buttons, 0, 4);
                dialog.Controls.Add(layout);
                dialog.AcceptButton = acceptButton;
                dialog.CancelButton = cancelButton;

                return dialog.ShowDialog(this) == DialogResult.OK && paymentCombo.SelectedItem is PaymentMethod
                    ? (PaymentMethod)paymentCombo.SelectedItem
                    : (PaymentMethod?)null;
            }
        }

        private void ShowCashChangeHelper(decimal total)
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Cambio en efectivo";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(520, 680);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(236, 241, 245);

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(20) };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

                var paidValuePanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(0, 4, 0, 4)
                };
                var paidValueLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 23, 42),
                    TextAlign = ContentAlignment.MiddleRight,
                    Padding = new Padding(0, 0, 12, 0)
                };
                paidValuePanel.Controls.Add(paidValueLabel);

                var changePanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
                var changeLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                changePanel.Controls.Add(changeLabel);

                var typedWhole = 0L;
                var hasTyped = false;

                Action refresh = delegate
                {
                    paidValueLabel.Text = hasTyped ? "$" + typedWhole.ToString("N0") : string.Empty;

                    if (!hasTyped)
                    {
                        changeLabel.Text = "Ingresa el monto recibido";
                        changeLabel.ForeColor = Color.FromArgb(71, 85, 105);
                        changePanel.BackColor = Color.FromArgb(241, 245, 249);
                        return;
                    }

                    var change = typedWhole - total;
                    if (change < 0m)
                    {
                        changeLabel.Text = "Faltante: $" + Math.Abs(change).ToString("N2");
                        changeLabel.ForeColor = Color.FromArgb(153, 27, 27);
                        changePanel.BackColor = Color.FromArgb(254, 226, 226);
                        return;
                    }

                    changeLabel.Text = "Cambio a entregar: $" + change.ToString("N2");
                    changeLabel.ForeColor = Color.FromArgb(22, 101, 52);
                    changePanel.BackColor = Color.FromArgb(220, 252, 231);
                };

                Action<int> appendDigit = digit =>
                {
                    hasTyped = true;
                    const long maxWhole = 1000000L;
                    var next = (typedWhole * 10L) + digit;
                    typedWhole = next > maxWhole ? maxWhole : next;
                    refresh();
                };
                Action backspace = delegate
                {
                    if (!hasTyped)
                    {
                        return;
                    }

                    typedWhole /= 10L;
                    refresh();
                };
                Action clear = delegate
                {
                    typedWhole = 0L;
                    hasTyped = false;
                    refresh();
                };

                var keypadPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 4, Margin = new Padding(0, 6, 0, 6) };
                for (var column = 0; column < 3; column++)
                {
                    keypadPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
                }

                for (var row = 0; row < 4; row++)
                {
                    keypadPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
                }

                var darkText = Color.FromArgb(33, 37, 41);
                var keypadFont = new Font("Segoe UI Semibold", 22F, FontStyle.Bold);

                Func<string, Color, Color, Action, Button> makeKey = (text, backColor, foreColor, onClick) =>
                {
                    var key = new Button
                    {
                        Text = text,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(6),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = backColor,
                        ForeColor = foreColor,
                        Font = keypadFont
                    };
                    key.FlatAppearance.BorderColor = Color.FromArgb(208, 215, 222);
                    key.Click += delegate { onClick(); };
                    return key;
                };

                keypadPanel.Controls.Add(makeKey("7", Color.White, darkText, () => appendDigit(7)), 0, 0);
                keypadPanel.Controls.Add(makeKey("8", Color.White, darkText, () => appendDigit(8)), 1, 0);
                keypadPanel.Controls.Add(makeKey("9", Color.White, darkText, () => appendDigit(9)), 2, 0);
                keypadPanel.Controls.Add(makeKey("4", Color.White, darkText, () => appendDigit(4)), 0, 1);
                keypadPanel.Controls.Add(makeKey("5", Color.White, darkText, () => appendDigit(5)), 1, 1);
                keypadPanel.Controls.Add(makeKey("6", Color.White, darkText, () => appendDigit(6)), 2, 1);
                keypadPanel.Controls.Add(makeKey("1", Color.White, darkText, () => appendDigit(1)), 0, 2);
                keypadPanel.Controls.Add(makeKey("2", Color.White, darkText, () => appendDigit(2)), 1, 2);
                keypadPanel.Controls.Add(makeKey("3", Color.White, darkText, () => appendDigit(3)), 2, 2);
                keypadPanel.Controls.Add(makeKey("C", Color.FromArgb(185, 28, 28), Color.White, clear), 0, 3);
                keypadPanel.Controls.Add(makeKey("0", Color.White, darkText, () => appendDigit(0)), 1, 3);
                keypadPanel.Controls.Add(makeKey("⌫", Color.FromArgb(69, 123, 157), Color.White, backspace), 2, 3);

                var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
                var closeButton = new Button
                {
                    Text = "Cerrar",
                    Width = 130,
                    Height = 38,
                    DialogResult = DialogResult.OK,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(27, 67, 50),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
                };
                closeButton.FlatAppearance.BorderSize = 0;
                var skipButton = new Button
                {
                    Text = "Omitir",
                    Width = 130,
                    Height = 38,
                    DialogResult = DialogResult.Cancel,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    ForeColor = darkText,
                    Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
                };
                skipButton.FlatAppearance.BorderColor = Color.FromArgb(208, 215, 222);
                buttons.Controls.Add(closeButton);
                buttons.Controls.Add(skipButton);

                layout.Controls.Add(new Label { Text = "Venta cobrada en efectivo", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold), ForeColor = darkText, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
                layout.Controls.Add(new Label { Text = "Total: $" + total.ToString("N2"), Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold), ForeColor = darkText, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
                layout.Controls.Add(new Label { Text = "Pago con:", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), ForeColor = darkText, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
                layout.Controls.Add(paidValuePanel, 0, 3);
                layout.Controls.Add(changePanel, 0, 4);
                layout.Controls.Add(keypadPanel, 0, 5);
                layout.Controls.Add(buttons, 0, 6);

                dialog.Controls.Add(layout);
                dialog.AcceptButton = closeButton;
                dialog.CancelButton = skipButton;
                refresh();
                dialog.ShowDialog(this);
            }
        }

        private decimal? PromptActualCashForCloseShift(decimal expectedCash)
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Confirmar corte";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(390, 170);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12) };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

                var cashInput = new NumericUpDown
                {
                    Dock = DockStyle.Fill,
                    DecimalPlaces = 2,
                    Maximum = 1000000m,
                    Minimum = 0m,
                    ThousandsSeparator = true,
                    Value = expectedCash < 0m ? 0m : expectedCash
                };

                var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
                var confirmButton = new Button { Text = "Confirmar", Width = 100, Height = 30, DialogResult = DialogResult.OK };
                var cancelButton = new Button { Text = "Cancelar", Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
                buttons.Controls.Add(confirmButton);
                buttons.Controls.Add(cancelButton);

                layout.Controls.Add(new Label { Text = "Efectivo esperado: $" + expectedCash.ToString("N2"), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
                layout.Controls.Add(new Label { Text = "Efectivo contado:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
                layout.Controls.Add(cashInput, 0, 2);
                layout.Controls.Add(buttons, 0, 3);
                dialog.Controls.Add(layout);
                dialog.AcceptButton = confirmButton;
                dialog.CancelButton = cancelButton;
                TouchInputSupport.EnableFor(dialog);

                return dialog.ShowDialog(this) == DialogResult.OK ? cashInput.Value : (decimal?)null;
            }
        }

        private void TryPrintCashierPrecheck()
        {
            var hideProductPrice = IsCurrentDigitalPlatformOrder();
            var formattedItems = this.cashierCartLines.Select(item => (
                item.Quantity.ToString("0"),
                item.ProductName,
                hideProductPrice ? string.Empty : item.LineTotal.ToString("N2")
            )).ToList();

            var orderKindText = GetCurrentTicketDisplayName();
            var printableNote = GetPrintableOrderNote(orderKindText);
            var widthMm = this.startupState.TicketWidthMm <= 0 ? 80 : this.startupState.TicketWidthMm;
            var layout = string.IsNullOrWhiteSpace(this.startupState.TicketLayoutName) ? "Clásico compacto" : this.startupState.TicketLayoutName;
            var headerText = BuildTicketHeaderText();

            var total = GetCurrentOrderTotalForCharge();
            var lines = TicketFormatter.FormatReceipt(
                this.startupState.BusinessName ?? "MrAlbertoCompany",
                this.startupState.BusinessSlogan,
                "PRE-CUENTA",
                DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                this.authenticatedUser.DisplayName,
                orderKindText,
                printableNote,
                formattedItems,
                total.ToString("N2"),
                widthMm,
                layout,
                headerText,
                this.startupState.TicketFooterText,
                this.startupState.ShowSystemFooter,
                this.startupState.TicketSystemFooterText,
                this.startupState.TicketHorizontalOffset,
                this.startupState.TicketVerticalOffset,
                this.startupState.TicketCharactersPerLine);

            this.lastReceiptPrinterName = this.startupState.ActivePrinterName;
            this.lastReceiptTitle = "Pre-cuenta";
            this.lastReceiptLines = lines;

            if (string.IsNullOrWhiteSpace(this.startupState.ActivePrinterName))
            {
                return;
            }

            this.ticketPrinter.Print(new TicketPrintJob
            {
                PrinterName = this.startupState.ActivePrinterName,
                Title = "Pre-cuenta",
                TicketWidthMm = this.startupState.TicketWidthMm,
                UseFullPaperWidth = this.startupState.UseFullPaperWidth,
                Lines = lines,
                HeaderFontSize = this.startupState.TicketTitleFontSize,
                InfoFontSize = this.startupState.TicketInfoFontSize,
                ItemsFontSize = this.startupState.TicketItemsFontSize,
                TotalFontSize = this.startupState.TicketTotalFontSize,
                FooterFontSize = this.startupState.TicketFooterFontSize
            });
        }

        private void TryPrintCashierReceipt(CreateSaleResult sale)
        {
            var hideProductPrice = IsCurrentDigitalPlatformOrder();
            var formattedItems = this.cashierCartLines.Select(item => (
                item.Quantity.ToString("0"),
                item.ProductName,
                hideProductPrice ? string.Empty : item.LineTotal.ToString("N2")
            )).ToList();

            var orderKindText = GetCurrentTicketDisplayName();
            var printableNote = GetPrintableOrderNote(orderKindText);
            var widthMm = this.startupState.TicketWidthMm <= 0 ? 80 : this.startupState.TicketWidthMm;
            var layout = string.IsNullOrWhiteSpace(this.startupState.TicketLayoutName) ? "Clásico compacto" : this.startupState.TicketLayoutName;
            var headerText = BuildTicketHeaderText();

            var lines = TicketFormatter.FormatReceipt(
                this.startupState.BusinessName ?? "MrAlbertoCompany",
                this.startupState.BusinessSlogan,
                sale.Folio,
                sale.SoldUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                this.authenticatedUser.DisplayName,
                orderKindText,
                printableNote,
                formattedItems,
                sale.Total.ToString("N2"),
                widthMm,
                layout,
                headerText,
                this.startupState.TicketFooterText,
                this.startupState.ShowSystemFooter,
                this.startupState.TicketSystemFooterText,
                this.startupState.TicketHorizontalOffset,
                this.startupState.TicketVerticalOffset,
                this.startupState.TicketCharactersPerLine);

            this.lastReceiptPrinterName = this.startupState.ActivePrinterName;
            this.lastReceiptTitle = "Ticket de venta";
            this.lastReceiptLines = lines;

            if (string.IsNullOrWhiteSpace(this.startupState.ActivePrinterName))
            {
                return;
            }

            this.ticketPrinter.Print(new TicketPrintJob
            {
                PrinterName = this.startupState.ActivePrinterName,
                Title = "Ticket de venta",
                TicketWidthMm = this.startupState.TicketWidthMm,
                UseFullPaperWidth = this.startupState.UseFullPaperWidth,
                Lines = lines,
                HeaderFontSize = this.startupState.TicketTitleFontSize,
                InfoFontSize = this.startupState.TicketInfoFontSize,
                ItemsFontSize = this.startupState.TicketItemsFontSize,
                TotalFontSize = this.startupState.TicketTotalFontSize,
                FooterFontSize = this.startupState.TicketFooterFontSize
            });
        }

        private string BuildTicketHeaderText()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(this.startupState.BusinessAddress))
            {
                parts.Add(this.startupState.BusinessAddress.Trim());
            }

            if (!string.IsNullOrWhiteSpace(this.startupState.BusinessPhone))
            {
                parts.Add(this.startupState.BusinessPhone.Trim());
            }

            if (!string.IsNullOrWhiteSpace(this.startupState.TicketHeaderText))
            {
                parts.Add(this.startupState.TicketHeaderText.Trim());
            }

            return string.Join(Environment.NewLine, parts);
        }

        private void SaveCurrentCartAsPending(object sender, EventArgs eventArgs)
        {
            SaveCurrentCartAsPendingInternal(true);
        }

        private bool SaveCurrentCartAsPendingInternal(bool showValidationMessages)
        {
            if (!this.cashierOrderStarted)
            {
                if (showValidationMessages)
                {
                    MessageBox.Show("Primero inicia una orden.", "Órdenes pendientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                return false;
            }

            if (this.cashierCartLines.Count == 0)
            {
                if (showValidationMessages)
                {
                    MessageBox.Show("Agrega productos antes de guardar un pendiente.", "Órdenes pendientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                return false;
            }

            var orderKind = (OrderKind)this.cashierOrderKindComboBox.SelectedItem;
            var ticketName = this.activeCashierPendingTicket == null
                ? BuildAutomaticPendingTicketName(orderKind, this.cashierOrderNote)
                : this.activeCashierPendingTicket.Name;

            var pendingDraft = new PendingTicketDraft
            {
                Name = ticketName,
                OrderKind = orderKind,
                Note = this.cashierOrderNote,
                DigitalPlatformName = this.cashierDigitalPlatformName,
                DigitalPlatformPricingMode = this.cashierDigitalPlatformPricingMode,
                DigitalPlatformUnitPrice = this.cashierDigitalPlatformUnitPrice,
                Items = this.cashierCartLines.Select(item => new SaleLineDraft
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    EstimatedCost = item.EstimatedCost,
                    UsesInventory = item.UsesInventory
                }).ToList(),
                Total = GetCurrentOrderTotalForCharge()
            };

            if (this.activeCashierPendingTicket != null)
            {
                this.activeCashierPendingTicket.Name = pendingDraft.Name;
                this.activeCashierPendingTicket.OrderKind = pendingDraft.OrderKind;
                this.activeCashierPendingTicket.Note = pendingDraft.Note;
                this.activeCashierPendingTicket.DigitalPlatformName = pendingDraft.DigitalPlatformName;
                this.activeCashierPendingTicket.DigitalPlatformPricingMode = pendingDraft.DigitalPlatformPricingMode;
                this.activeCashierPendingTicket.DigitalPlatformUnitPrice = pendingDraft.DigitalPlatformUnitPrice;
                this.activeCashierPendingTicket.Items = pendingDraft.Items;
                this.activeCashierPendingTicket.Total = pendingDraft.Total;
            }
            else
            {
                var existingMesa = orderKind == OrderKind.Mesa ? FindPendingByTableFromText(ticketName, null) : null;
                if (existingMesa != null)
                {
                    existingMesa.Name = pendingDraft.Name;
                    existingMesa.OrderKind = pendingDraft.OrderKind;
                    existingMesa.Note = pendingDraft.Note;
                    existingMesa.DigitalPlatformName = pendingDraft.DigitalPlatformName;
                    existingMesa.DigitalPlatformPricingMode = pendingDraft.DigitalPlatformPricingMode;
                    existingMesa.DigitalPlatformUnitPrice = pendingDraft.DigitalPlatformUnitPrice;
                    existingMesa.Items = pendingDraft.Items;
                    existingMesa.Total = pendingDraft.Total;
                    this.activeCashierPendingTicket = existingMesa;
                }
                else
                {
                    this.pendingTickets.Add(pendingDraft);
                    this.activeCashierPendingTicket = pendingDraft;
                }
            }

            RefreshPendingTicketsView();
            this.activeCashierPendingTicket = null;
            ClearCashierCart(null, EventArgs.Empty);
            SetCashierOrderUiEnabled(false);
            UpdateCurrentOrderLabel();
            return true;
        }

        private string BuildMesaPendingTicketName()
        {
            var note = this.cashierOrderNote ?? string.Empty;
            if (note.StartsWith("Mesa ", StringComparison.OrdinalIgnoreCase))
            {
                return note.Trim();
            }

            return "Mesa " + DateTime.Now.ToString("HHmm");
        }

        private string BuildAutomaticPendingTicketName(OrderKind orderKind, string note)
        {
            if (orderKind == OrderKind.Mesa)
            {
                return BuildMesaPendingTicketName();
            }

            if (orderKind == OrderKind.ParaLlevar)
            {
                return "Para llevar #" + GetNextTakeAwayNumber().ToString(CultureInfo.InvariantCulture);
            }

            if (orderKind == OrderKind.PlataformaDigital)
            {
                if (!string.IsNullOrWhiteSpace(note) && note.StartsWith("Plataforma:", StringComparison.OrdinalIgnoreCase))
                {
                    var namePart = note.Split('|').FirstOrDefault();
                    return string.IsNullOrWhiteSpace(namePart) ? note.Trim() : namePart.Trim();
                }

                return "Plataforma digital " + DateTime.Now.ToString("HH:mm");
            }

            return "Mostrador " + DateTime.Now.ToString("HH:mm");
        }

        private void SyncActivePendingDraftFromCurrentCart(bool createIfMissing)
        {
            if (!this.cashierOrderStarted || this.cashierOrderKindComboBox == null || !(this.cashierOrderKindComboBox.SelectedItem is OrderKind))
            {
                return;
            }

            var orderKind = (OrderKind)this.cashierOrderKindComboBox.SelectedItem;
            string generatedName = null;
            if (this.activeCashierPendingTicket == null && createIfMissing)
            {
                generatedName = BuildAutomaticPendingTicketName(orderKind, this.cashierOrderNote);
                var existingMesa = orderKind == OrderKind.Mesa ? FindPendingByTableFromText(generatedName, null) : null;
                if (existingMesa != null)
                {
                    this.activeCashierPendingTicket = existingMesa;
                }
                else
                {
                    this.activeCashierPendingTicket = new PendingTicketDraft { Name = generatedName };
                    this.pendingTickets.Add(this.activeCashierPendingTicket);
                }
            }

            if (this.activeCashierPendingTicket == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(this.activeCashierPendingTicket.Name))
            {
                this.activeCashierPendingTicket.Name = generatedName ?? BuildAutomaticPendingTicketName(orderKind, this.cashierOrderNote);
            }

            if (orderKind == OrderKind.Mesa)
            {
                this.activeCashierPendingTicket.Name = BuildMesaPendingTicketName();
            }

            this.activeCashierPendingTicket.OrderKind = orderKind;
            this.activeCashierPendingTicket.Note = this.cashierOrderNote;
            this.activeCashierPendingTicket.DigitalPlatformName = this.cashierDigitalPlatformName;
            this.activeCashierPendingTicket.DigitalPlatformPricingMode = this.cashierDigitalPlatformPricingMode;
            this.activeCashierPendingTicket.DigitalPlatformUnitPrice = this.cashierDigitalPlatformUnitPrice;
            this.activeCashierPendingTicket.Items = this.cashierCartLines.Select(item => new SaleLineDraft
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                EstimatedCost = item.EstimatedCost,
                UsesInventory = item.UsesInventory
            }).ToList();
            this.activeCashierPendingTicket.Total = GetCurrentOrderTotalForCharge();
            RefreshPendingTicketsView();
        }

        private int GetNextTakeAwayNumber()
        {
            var usedNumbers = new HashSet<int>();
            foreach (var ticket in this.pendingTickets)
            {
                if (ticket == null || ticket.OrderKind != OrderKind.ParaLlevar)
                {
                    continue;
                }

                var number = TryExtractTakeAwayNumber(ticket.Name);
                if (number.HasValue && number.Value > 0)
                {
                    usedNumbers.Add(number.Value);
                }
            }

            var candidate = 1;
            while (usedNumbers.Contains(candidate))
            {
                candidate += 1;
            }

            this.nextTakeAwayNumber = candidate;
            return candidate;
        }

        private static int? TryExtractTakeAwayNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var text = value.Trim();
            if (!text.StartsWith("Para llevar #", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var numberText = text.Substring("Para llevar #".Length).Trim();
            if (int.TryParse(numberText, out var number) && number > 0)
            {
                return number;
            }

            return null;
        }

        private string PromptPendingTicketName()
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Guardar pendiente";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(420, 150);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(14) };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
                var input = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
                var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
                var saveButton = new Button { Text = "Guardar", Width = 100, Height = 30, DialogResult = DialogResult.OK };
                var cancelButton = new Button { Text = "Cancelar", Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
                buttons.Controls.Add(saveButton);
                buttons.Controls.Add(cancelButton);

                layout.Controls.Add(new Label { Text = "Nombre del ticket pendiente:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
                layout.Controls.Add(input, 0, 1);
                layout.Controls.Add(new Label { Text = string.Empty, Dock = DockStyle.Fill }, 0, 2);
                layout.Controls.Add(buttons, 0, 3);
                dialog.Controls.Add(layout);
                dialog.AcceptButton = saveButton;
                dialog.CancelButton = cancelButton;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return null;
                }

                var value = (input.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = "Pendiente " + DateTime.Now.ToString("HH:mm:ss");
                }

                return value;
            }
        }

        private void LoadPendingTicketIntoCashier(PendingTicketDraft pending)
        {
            SetCashierOrderUiEnabled(true);
            this.activeCashierPendingTicket = pending;
            this.cashierCartLines.Clear();
            foreach (var item in pending.Items ?? new List<SaleLineDraft>())
            {
                this.cashierCartLines.Add(new CartLineView
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    EstimatedCost = item.EstimatedCost,
                    UsesInventory = item.UsesInventory
                });
            }

            this.cashierOrderKindComboBox.SelectedItem = pending.OrderKind;
            this.cashierOrderNote = pending.Note ?? string.Empty;
            this.cashierDigitalPlatformName = pending.DigitalPlatformName ?? string.Empty;
            this.cashierDigitalPlatformPricingMode = pending.DigitalPlatformPricingMode ?? string.Empty;
            this.cashierDigitalPlatformUnitPrice = pending.DigitalPlatformUnitPrice;
            UpdateCurrentOrderLabel();
            UpdateCashierTotal();
        }

        private static Button CreateHeaderActionButton(string text, Color color, Action action)
        {
            var button = new Button
            {
                Text = text,
                Width = 194,
                Height = 36,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(4, 0, 4, 0),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += delegate { action(); };
            return button;
        }

        private static Control BuildComboRow(string leftLabel, Control leftControl, string rightLabel, Control rightControl)
        {
            var hideLabels = string.IsNullOrWhiteSpace(leftLabel) && string.IsNullOrWhiteSpace(rightLabel);
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = hideLabels ? 2 : 4,
                RowCount = 1
            };

            if (hideLabels)
            {
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                panel.Controls.Add(leftControl, 0, 0);
                panel.Controls.Add(rightControl, 1, 0);
                return panel;
            }

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panel.Controls.Add(new Label { Text = leftLabel, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            panel.Controls.Add(leftControl, 1, 0);
            panel.Controls.Add(new Label { Text = rightLabel, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
            panel.Controls.Add(rightControl, 3, 0);
            return panel;
        }

        private static Button CreateSmallButton(string text, EventHandler handler)
        {
            var button = new Button
            {
                Text = text,
                Width = 74,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 5, 0),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
            button.Click += handler;
            return button;
        }

        private void ShowOrderTypeDialog()
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Nueva orden";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(620, 400);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(242, 245, 248);
                dialog.Font = new Font("Segoe UI", 10F);

                var card = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding = new Padding(18),
                    Margin = new Padding(0)
                };

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1 };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));

                layout.Controls.Add(new Label
                {
                    Text = "Selecciona el tipo de orden",
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(27, 67, 50),
                    TextAlign = ContentAlignment.MiddleLeft
                }, 0, 0);
                layout.Controls.Add(new Label
                {
                    Text = "Elige una modalidad para iniciar el ticket",
                    Dock = DockStyle.Fill,
                    ForeColor = Color.DimGray,
                    TextAlign = ContentAlignment.MiddleLeft
                }, 0, 1);

                layout.Controls.Add(CreateDialogButton("Comer aquí", "Asigna mesa y abre orden en comedor", Color.FromArgb(27, 67, 50), delegate { dialog.Tag = OrderKind.Mesa; dialog.DialogResult = DialogResult.OK; }), 0, 2);
                layout.Controls.Add(CreateDialogButton("Para llevar", "Captura cliente opcional y despacha", Color.FromArgb(30, 136, 229), delegate { dialog.Tag = OrderKind.ParaLlevar; dialog.DialogResult = DialogResult.OK; }), 0, 3);
                layout.Controls.Add(CreateDialogButton("Plataformas digitales", "Selecciona plataforma y montos de referencia", Color.FromArgb(228, 108, 10), delegate { dialog.Tag = OrderKind.PlataformaDigital; dialog.DialogResult = DialogResult.OK; }), 0, 4);

                card.Controls.Add(layout);
                dialog.Controls.Add(card);

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if (!CanStartNewOrder())
                {
                    return;
                }

                var selectedKind = dialog.Tag is OrderKind ? (OrderKind)dialog.Tag : OrderKind.Mostrador;
                if (selectedKind == OrderKind.Mesa)
                {
                    var selectedTable = PromptTableNumber();
                    if (selectedTable <= 0)
                    {
                        return;
                    }

                    if (IsTableOccupied(selectedTable, null))
                    {
                        MessageBox.Show("La mesa seleccionada ya está ocupada. Reabre su ticket pendiente para continuar.", "Nueva orden", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    StartCashierOrder(OrderKind.Mesa, "Mesa " + selectedTable.ToString("00"));
                    return;
                }

                if (selectedKind == OrderKind.ParaLlevar)
                {
                    var customerName = PromptOptionalCustomerName();
                    if (customerName == null)
                    {
                        return;
                    }

                    var note = string.IsNullOrWhiteSpace(customerName) ? string.Empty : "Cliente: " + customerName.Trim();
                    StartCashierOrder(OrderKind.ParaLlevar, note);
                    return;
                }

                var platformSelection = PromptDigitalPlatformOrderSelection();
                if (platformSelection == null)
                {
                    return;
                }

                StartCashierOrder(OrderKind.PlataformaDigital, platformSelection.Note, platformSelection);
            }
        }

        private bool CanStartNewOrder()
        {
            if (!this.cashierOrderStarted || this.cashierCartLines.Count == 0)
            {
                return true;
            }

            var answer = MessageBox.Show(
                "La orden actual tiene productos sin guardar. ¿Deseas guardarla como pendiente antes de crear una nueva orden?",
                "Nueva orden",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (answer == DialogResult.Cancel)
            {
                return false;
            }

            if (answer == DialogResult.Yes)
            {
                return SaveCurrentCartAsPendingInternal(false);
            }

            return true;
        }

        private void StartCashierOrder(OrderKind orderKind, string note, DigitalPlatformOrderSelection platformSelection = null)
        {
            SetCashierOrderUiEnabled(true);
            this.activeCashierPendingTicket = null;
            this.cashierCartLines.Clear();
            this.cashierOrderKindComboBox.SelectedItem = orderKind;
            this.cashierOrderNote = note ?? string.Empty;
            this.cashierDigitalPlatformUnitPrice = null;
            this.cashierDigitalPlatformName = string.Empty;
            this.cashierDigitalPlatformPricingMode = string.Empty;
            if (orderKind == OrderKind.PlataformaDigital && platformSelection != null)
            {
                this.cashierDigitalPlatformUnitPrice = platformSelection.UnitPrice;
                this.cashierDigitalPlatformName = platformSelection.PlatformName ?? string.Empty;
                this.cashierDigitalPlatformPricingMode = platformSelection.PricingMode ?? string.Empty;
            }

            this.selectedCashierCategoryId = null;
            LoadCashierCategories();
            UpdateCurrentOrderLabel();
            UpdateCashierTotal();
            SyncActivePendingDraftFromCurrentCart(true);
        }

        private string PromptOptionalCustomerName()
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Para llevar";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(420, 160);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(14) };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
                var input = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
                var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
                var nextButton = new Button { Text = "Siguiente", Width = 100, Height = 30, DialogResult = DialogResult.OK };
                var cancelButton = new Button { Text = "Cancelar", Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
                buttons.Controls.Add(nextButton);
                buttons.Controls.Add(cancelButton);

                layout.Controls.Add(new Label { Text = "Nombre del cliente (opcional):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
                layout.Controls.Add(input, 0, 1);
                layout.Controls.Add(new Label { Text = string.Empty, Dock = DockStyle.Fill }, 0, 2);
                layout.Controls.Add(buttons, 0, 3);
                dialog.Controls.Add(layout);

                var result = dialog.ShowDialog(this);
                if (result == DialogResult.Cancel)
                {
                    return null;
                }

                return (input.Text ?? string.Empty).Trim();
            }
        }

        private DigitalPlatformOrderSelection PromptDigitalPlatformOrderSelection()
        {
            var platforms = this.configuredDigitalPlatforms == null
                ? new List<DigitalPlatformConfigurationView>()
                : this.configuredDigitalPlatforms.Where(item => item != null && item.IsActive).ToList();
            if (platforms.Count == 0)
            {
                MessageBox.Show("No hay plataformas digitales activas. Pide al administrador configurarlas.", "Plataformas digitales", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            using (var dialog = new Form())
            {
                dialog.Text = "Plataforma digital";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(640, 470);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(242, 245, 248);

                var card = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding = new Padding(18)
                };

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 12 };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

                var platformCombo = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                    Height = 40
                };
                platformCombo.DataSource = platforms;
                platformCombo.DisplayMember = "Name";

                var firstInputLabel = new Label
                {
                    Text = "Monto",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
                };
                var firstInput = new NumericUpDown
                {
                    Dock = DockStyle.Fill,
                    DecimalPlaces = 2,
                    Maximum = 1000000m,
                    Minimum = 0m,
                    ThousandsSeparator = true,
                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                    Height = 44,
                    TextAlign = HorizontalAlignment.Right
                };

                var secondInputLabel = new Label
                {
                    Text = "Monto",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
                };
                var secondInput = new NumericUpDown
                {
                    Dock = DockStyle.Fill,
                    DecimalPlaces = 2,
                    Maximum = 1000000m,
                    Minimum = 0m,
                    ThousandsSeparator = true,
                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                    Height = 44,
                    TextAlign = HorizontalAlignment.Right
                };

                Action refreshFieldLabels = delegate
                {
                    var selectedPlatform = platformCombo.SelectedItem as DigitalPlatformConfigurationView;
                    var mode = NormalizePricingMode(selectedPlatform == null ? string.Empty : selectedPlatform.PricingMode);
                    if (mode == "rappi")
                    {
                        firstInputLabel.Text = "Total sugerido a facturar:";
                        secondInputLabel.Text = "Subtotal restaurante:";
                        return;
                    }

                    if (mode == "didi")
                    {
                        firstInputLabel.Text = "Precio con descuento:";
                        secondInputLabel.Text = "Ingresos estimados:";
                        return;
                    }

                    firstInputLabel.Text = "Precio de plataforma:";
                    secondInputLabel.Text = "Ingreso estimado:";
                };

                platformCombo.SelectedIndexChanged += delegate
                {
                    refreshFieldLabels();
                    firstInput.Value = 0m;
                    secondInput.Value = 0m;
                    firstInput.Focus();
                    firstInput.Select(0, firstInput.Text.Length);
                    secondInput.Select(0, secondInput.Text.Length);
                };
                refreshFieldLabels();

                var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
                var acceptButton = new Button
                {
                    Text = "Aceptar",
                    Width = 130,
                    Height = 38,
                    DialogResult = DialogResult.OK,
                    BackColor = Color.FromArgb(27, 67, 50),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold)
                };
                acceptButton.FlatAppearance.BorderSize = 0;
                var cancelButton = new Button
                {
                    Text = "Cancelar",
                    Width = 130,
                    Height = 38,
                    DialogResult = DialogResult.Cancel,
                    BackColor = Color.FromArgb(233, 236, 239),
                    ForeColor = Color.FromArgb(33, 37, 41),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold)
                };
                cancelButton.FlatAppearance.BorderSize = 0;
                buttons.Controls.Add(acceptButton);
                buttons.Controls.Add(cancelButton);

                layout.Controls.Add(new Label
                {
                    Text = "Plataforma digital",
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(27, 67, 50),
                    TextAlign = ContentAlignment.MiddleLeft
                }, 0, 0);
                layout.Controls.Add(new Label
                {
                    Text = "Selecciona plataforma y captura montos de operación",
                    Dock = DockStyle.Fill,
                    ForeColor = Color.DimGray,
                    TextAlign = ContentAlignment.MiddleLeft
                }, 0, 1);
                layout.Controls.Add(new Label { Text = "Plataforma:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
                layout.Controls.Add(platformCombo, 0, 3);
                layout.Controls.Add(new Label { Text = string.Empty, Dock = DockStyle.Fill }, 0, 4);
                layout.Controls.Add(firstInputLabel, 0, 5);
                layout.Controls.Add(firstInput, 0, 6);
                layout.Controls.Add(new Label { Text = string.Empty, Dock = DockStyle.Fill }, 0, 7);
                layout.Controls.Add(secondInputLabel, 0, 8);
                layout.Controls.Add(secondInput, 0, 9);
                layout.Controls.Add(new Label { Text = "El primer monto se usa como precio fijo para todos los productos de esta orden.", Dock = DockStyle.Fill, ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft }, 0, 10);
                layout.Controls.Add(buttons, 0, 11);
                card.Controls.Add(layout);
                dialog.Controls.Add(card);
                dialog.Shown += delegate
                {
                    firstInput.Focus();
                    firstInput.Select(0, firstInput.Text.Length);
                    secondInput.Select(0, secondInput.Text.Length);
                };

                var result = dialog.ShowDialog(this);
                if (result != DialogResult.OK)
                {
                    return null;
                }

                var selected = platformCombo.SelectedItem as DigitalPlatformConfigurationView;
                if (selected == null)
                {
                    return null;
                }

                var modeValue = NormalizePricingMode(selected.PricingMode);
                var unitPrice = firstInput.Value;
                var auxAmount = secondInput.Value;
                var note = BuildDigitalPlatformNote(selected.Name, modeValue, unitPrice, auxAmount);

                return new DigitalPlatformOrderSelection
                {
                    PlatformName = selected.Name,
                    PricingMode = modeValue,
                    UnitPrice = unitPrice,
                    Note = note
                };
            }
        }

        private static string NormalizePricingMode(string pricingMode)
        {
            var value = (pricingMode ?? string.Empty).Trim().ToLowerInvariant();
            if (value == "rappi" || value == "didi")
            {
                return value;
            }

            return "manual";
        }

        private static string BuildDigitalPlatformNote(string platformName, string pricingMode, decimal mainAmount, decimal secondaryAmount)
        {
            var safeName = string.IsNullOrWhiteSpace(platformName) ? "Sin plataforma" : platformName.Trim();
            if (pricingMode == "rappi")
            {
                return "Plataforma: " + safeName
                    + " | Total sugerido a facturar: $" + mainAmount.ToString("N2", CultureInfo.InvariantCulture)
                    + " | Subtotal restaurante: $" + secondaryAmount.ToString("N2", CultureInfo.InvariantCulture);
            }

            if (pricingMode == "didi")
            {
                return "Plataforma: " + safeName
                    + " | Precio con descuento: $" + mainAmount.ToString("N2", CultureInfo.InvariantCulture)
                    + " | Ingresos estimados: $" + secondaryAmount.ToString("N2", CultureInfo.InvariantCulture);
            }

            return "Plataforma: " + safeName
                + " | Precio plataforma: $" + mainAmount.ToString("N2", CultureInfo.InvariantCulture)
                + " | Ingreso estimado: $" + secondaryAmount.ToString("N2", CultureInfo.InvariantCulture);
        }

        private void ShowTableSelectorDialog()
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Seleccionar mesa";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.Size = new Size(720, 520);
                dialog.MinimumSize = new Size(620, 440);
                dialog.BackColor = Color.White;
                dialog.Font = new Font("Segoe UI", 10F);

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(18) };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                layout.Controls.Add(new Label
                {
                    Text = "Elige la mesa",
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(27, 67, 50),
                    TextAlign = ContentAlignment.MiddleLeft
                }, 0, 0);

                var tablesPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(247, 249, 251), Padding = new Padding(10) };
                var occupiedTables = GetOccupiedTableNumbers();
                var tableCount = this.configuredDiningTableCount <= 0 ? 5 : this.configuredDiningTableCount;
                for (var tableNumber = 1; tableNumber <= tableCount; tableNumber++)
                {
                    var currentTable = tableNumber;
                    var isOccupied = occupiedTables.Contains(currentTable);
                    var button = new Button
                    {
                        Text = isOccupied ? "Mesa " + currentTable.ToString("00") + Environment.NewLine + "Ocupada" : "Mesa " + currentTable.ToString("00"),
                        Width = 118,
                        Height = 64,
                        Margin = new Padding(8),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = isOccupied ? Color.FromArgb(232, 234, 237) : Color.White,
                        ForeColor = Color.FromArgb(27, 67, 50),
                        Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
                    };
                    button.FlatAppearance.BorderColor = Color.FromArgb(210, 218, 224);
                    button.Enabled = !isOccupied;
                    button.Click += delegate
                    {
                        dialog.Tag = currentTable;
                        dialog.DialogResult = DialogResult.OK;
                    };
                    tablesPanel.Controls.Add(button);
                }

                layout.Controls.Add(tablesPanel, 0, 1);
                dialog.Controls.Add(layout);

                if (dialog.ShowDialog(this) == DialogResult.OK && dialog.Tag is int)
                {
                    var selectedTable = (int)dialog.Tag;
                    StartCashierOrder(OrderKind.Mesa, "Mesa " + selectedTable.ToString("00"));
                }
            }
        }

        private HashSet<int> GetOccupiedTableNumbers(PendingTicketDraft ignoreTicket = null)
        {
            var occupiedTables = new HashSet<int>();
            foreach (var ticket in this.pendingTickets)
            {
                if (ticket == null || ticket == ignoreTicket || ticket.OrderKind != OrderKind.Mesa)
                {
                    continue;
                }

                var tableNumber = TryExtractMesaNumber(ticket.Note) ?? TryExtractMesaNumber(ticket.Name);
                if (tableNumber.HasValue && tableNumber.Value > 0)
                {
                    occupiedTables.Add(tableNumber.Value);
                }
            }

            if (this.cashierOrderStarted
                && this.cashierOrderKindComboBox != null
                && this.cashierOrderKindComboBox.SelectedItem is OrderKind
                && (OrderKind)this.cashierOrderKindComboBox.SelectedItem == OrderKind.Mesa)
            {
                var activeTableNumber = TryExtractMesaNumber(this.cashierOrderNote);
                if (activeTableNumber.HasValue && activeTableNumber.Value > 0)
                {
                    occupiedTables.Add(activeTableNumber.Value);
                }
            }

            return occupiedTables;
        }

        private bool IsTableOccupied(int tableNumber, PendingTicketDraft ignoreTicket)
        {
            return tableNumber > 0 && GetOccupiedTableNumbers(ignoreTicket).Contains(tableNumber);
        }

        private PendingTicketDraft FindPendingByTableFromText(string tableText, PendingTicketDraft ignoreTicket)
        {
            var tableNumber = TryExtractMesaNumber(tableText);
            if (!tableNumber.HasValue || tableNumber.Value <= 0)
            {
                return null;
            }

            return this.pendingTickets.FirstOrDefault(ticket =>
                ticket != null
                && ticket != ignoreTicket
                && ticket.OrderKind == OrderKind.Mesa
                && ((TryExtractMesaNumber(ticket.Note) ?? TryExtractMesaNumber(ticket.Name)) == tableNumber));
        }

        private static int? TryExtractMesaNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var text = value.Trim();
            if (!text.StartsWith("Mesa ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var numberText = text.Substring(5).Trim();
            var firstToken = numberText.Split(new[] { ' ', '-', ':' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(firstToken))
            {
                return null;
            }

            if (int.TryParse(firstToken, out var mesa) && mesa > 0)
            {
                return mesa;
            }

            return null;
        }

        private void ReprintLastTicket()
        {
            var printerName = string.IsNullOrWhiteSpace(this.lastReceiptPrinterName) ? this.startupState.ActivePrinterName : this.lastReceiptPrinterName;
            if (this.lastReceiptLines == null || this.lastReceiptLines.Count == 0)
            {
                MessageBox.Show("Todavía no hay un ticket registrado para reimprimir en esta sesión.", "Reimprimir ticket", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(printerName))
            {
                MessageBox.Show("Configura una impresora antes de reimprimir.", "Reimprimir ticket", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = this.ticketPrinter.Print(new TicketPrintJob { PrinterName = printerName, Title = this.lastReceiptTitle ?? "Ticket de venta", Lines = this.lastReceiptLines });
            MessageBox.Show(result.Message, "Reimprimir ticket", MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private string GetPendingTicketsStorageFilePath()
        {
            var paths = StationPathsFactory.CreateDefault();
            StationPathsFactory.EnsureCreated(paths);
            return Path.Combine(paths.DataPath, "pending-tickets-" + (this.startupState.StationCode ?? "station") + "-" + this.authenticatedUser.UserId + ".json");
        }

        private void LoadPendingTicketsFromStorage()
        {
            this.pendingTickets.Clear();
            try
            {
                var filePath = GetPendingTicketsStorageFilePath();
                if (!File.Exists(filePath))
                {
                    return;
                }

                using (var stream = File.OpenRead(filePath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(PendingTicketsStorageModel));
                    var model = serializer.ReadObject(stream) as PendingTicketsStorageModel;
                    if (model == null)
                    {
                        return;
                    }

                    this.nextTakeAwayNumber = model.NextTakeAwayNumber <= 0 ? 1 : model.NextTakeAwayNumber;
                    var items = model.Tickets ?? new List<PendingTicketStorageItem>();
                    foreach (var item in items)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        this.pendingTickets.Add(new PendingTicketDraft
                        {
                            Name = item.Name,
                            OrderKind = (OrderKind)item.OrderKind,
                            Note = item.Note,
                            DigitalPlatformName = item.DigitalPlatformName,
                            DigitalPlatformPricingMode = item.DigitalPlatformPricingMode,
                            DigitalPlatformUnitPrice = item.DigitalPlatformUnitPrice,
                            Total = item.Total,
                            Items = (item.Items ?? new List<SaleLineDraftStorageItem>()).Select(line => new SaleLineDraft
                            {
                                ProductId = line.ProductId,
                                ProductName = line.ProductName,
                                Quantity = line.Quantity,
                                UnitPrice = line.UnitPrice,
                                EstimatedCost = line.EstimatedCost,
                                UsesInventory = line.UsesInventory
                            }).ToList()
                        });
                    }
                }
            }
            catch
            {
                this.pendingTickets.Clear();
            }
        }

        private void SavePendingTicketsToStorage()
        {
            try
            {
                var model = new PendingTicketsStorageModel
                {
                    NextTakeAwayNumber = this.nextTakeAwayNumber <= 0 ? 1 : this.nextTakeAwayNumber,
                    Tickets = this.pendingTickets.Select(item => new PendingTicketStorageItem
                    {
                        Name = item.Name,
                        OrderKind = (int)item.OrderKind,
                        Note = item.Note,
                        DigitalPlatformName = item.DigitalPlatformName,
                        DigitalPlatformPricingMode = item.DigitalPlatformPricingMode,
                        DigitalPlatformUnitPrice = item.DigitalPlatformUnitPrice,
                        Total = item.Total,
                        Items = (item.Items ?? new List<SaleLineDraft>()).Select(line => new SaleLineDraftStorageItem
                        {
                            ProductId = line.ProductId,
                            ProductName = line.ProductName,
                            Quantity = line.Quantity,
                            UnitPrice = line.UnitPrice,
                            EstimatedCost = line.EstimatedCost,
                            UsesInventory = line.UsesInventory
                        }).ToList()
                    }).ToList()
                };

                var filePath = GetPendingTicketsStorageFilePath();
                using (var stream = File.Create(filePath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(PendingTicketsStorageModel));
                    serializer.WriteObject(stream, model);
                }
            }
            catch
            {
            }
        }

        private string GetCurrentTicketDisplayName()
        {
            if (this.activeCashierPendingTicket != null && !string.IsNullOrWhiteSpace(this.activeCashierPendingTicket.Name))
            {
                return this.activeCashierPendingTicket.Name;
            }

            if (this.cashierOrderKindComboBox != null && this.cashierOrderKindComboBox.SelectedItem is OrderKind)
            {
                var kind = (OrderKind)this.cashierOrderKindComboBox.SelectedItem;
                if (kind == OrderKind.Mesa)
                {
                    return BuildMesaPendingTicketName();
                }

                if (kind == OrderKind.PlataformaDigital && !string.IsNullOrWhiteSpace(this.cashierDigitalPlatformName))
                {
                    return "Plataforma: " + this.cashierDigitalPlatformName;
                }
            }

            return this.cashierOrderKindComboBox != null && this.cashierOrderKindComboBox.SelectedItem != null
                ? this.cashierOrderKindComboBox.SelectedItem.ToString()
                : "Venta";
        }

        private string GetPrintableOrderNote(string ticketName)
        {
            var note = this.cashierOrderNote ?? string.Empty;
            if (string.IsNullOrWhiteSpace(note))
            {
                return string.Empty;
            }

            return string.Equals(note.Trim(), ticketName ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : note;
        }

        private static Control CreateCashierInfoPanel(string title, string text)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18), Margin = new Padding(0, 0, 12, 12) };
            panel.Controls.Add(new Label { Dock = DockStyle.Fill, Text = text, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 11F), TextAlign = ContentAlignment.TopLeft });
            panel.Controls.Add(new Label { Dock = DockStyle.Top, Height = 36, Text = title, Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold), ForeColor = Color.FromArgb(27, 67, 50) });
            return panel;
        }

        private static Button CreateCashierButton(string text, string subtitle, Color color, Action action)
        {
            return CreateActionButton(text, subtitle, color, action);
        }

        private static Button CreateDialogButton(string text, string subtitle, Color color, Action action)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                Text = text + Environment.NewLine + subtitle,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Margin = new Padding(0, 6, 0, 6),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 20, 0)
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += delegate { action(); };
            return button;
        }

        private static Control CreateTakeAwayPanel(TextBox customerNameTextBox)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(customerNameTextBox, 0, 0);
            panel.Controls.Add(CreateDialogButton("Plataformas digitales", "Configura y usa plataformas activas", Color.FromArgb(228, 108, 10), delegate { MessageBox.Show("Plataformas digitales está pendiente.", "Nueva orden", MessageBoxButtons.OK, MessageBoxIcon.Information); }), 0, 1);
            return panel;
        }

        private static void ShowPendingModule(string moduleName)
        {
            MessageBox.Show(
                moduleName + " quedará conectado en las fases siguientes del MVP.",
                "Módulo en construcción",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private Control CreateFooterButtons()
        {
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
            panel.Controls.Add(CreateFooterButton("Cerrar sesión", Color.FromArgb(130, 27, 52), delegate { this.LogoutRequested = true; Close(); }));
            panel.Controls.Add(CreateFooterButton("Salir", Color.FromArgb(90, 90, 90), delegate { this.LogoutRequested = false; Close(); }));
            return panel;
        }

        private static Control CreateInfoCard(string title, string value)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(247, 249, 251), Padding = new Padding(12), Margin = new Padding(0, 0, 0, 8) };
            panel.Controls.Add(new Label { Dock = DockStyle.Fill, Text = value, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
            panel.Controls.Add(new Label { Dock = DockStyle.Top, Height = 18, Text = title, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.5F), TextAlign = ContentAlignment.MiddleLeft });
            return panel;
        }

        private static Control CreateStatusCard(string title, Control content)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(247, 249, 251), Padding = new Padding(12), Margin = new Padding(0, 0, 0, 8) };
            content.Dock = DockStyle.Fill;
            panel.Controls.Add(content);
            panel.Controls.Add(new Label { Dock = DockStyle.Top, Height = 18, Text = title, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.5F), TextAlign = ContentAlignment.MiddleLeft });
            return panel;
        }

        private static Button CreateActionButton(string text, string subtitle, Color color, Action action)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                Text = text + Environment.NewLine + Environment.NewLine + subtitle,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                Margin = new Padding(12)
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += delegate { action(); };
            return button;
        }

        private static Button CreateFooterButton(string text, Color color, Action action)
        {
            var button = new Button
            {
                Text = text,
                Width = 140,
                Height = 40,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 10, 0)
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += delegate { action(); };
            return button;
        }

        private sealed class DigitalPlatformOrderSelection
        {
            public string PlatformName { get; set; }
            public string PricingMode { get; set; }
            public decimal UnitPrice { get; set; }
            public string Note { get; set; }
        }

        private sealed class PendingTicketDraft
        {
            public string Name { get; set; }
            public OrderKind OrderKind { get; set; }
            public string Note { get; set; }
            public string DigitalPlatformName { get; set; }
            public string DigitalPlatformPricingMode { get; set; }
            public decimal? DigitalPlatformUnitPrice { get; set; }
            public IList<SaleLineDraft> Items { get; set; }
            public decimal Total { get; set; }
        }

        [DataContract]
        private sealed class PendingTicketsStorageModel
        {
            [DataMember]
            public int NextTakeAwayNumber { get; set; }

            [DataMember]
            public List<PendingTicketStorageItem> Tickets { get; set; }
        }

        [DataContract]
        private sealed class PendingTicketStorageItem
        {
            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public int OrderKind { get; set; }

            [DataMember]
            public string Note { get; set; }

            [DataMember]
            public string DigitalPlatformName { get; set; }

            [DataMember]
            public string DigitalPlatformPricingMode { get; set; }

            [DataMember]
            public decimal? DigitalPlatformUnitPrice { get; set; }

            [DataMember]
            public decimal Total { get; set; }

            [DataMember]
            public List<SaleLineDraftStorageItem> Items { get; set; }
        }

        [DataContract]
        private sealed class SaleLineDraftStorageItem
        {
            [DataMember]
            public int ProductId { get; set; }

            [DataMember]
            public string ProductName { get; set; }

            [DataMember]
            public decimal Quantity { get; set; }

            [DataMember]
            public decimal UnitPrice { get; set; }

            [DataMember]
            public decimal EstimatedCost { get; set; }

            [DataMember]
            public bool UsesInventory { get; set; }
        }

        private sealed class SoldTicketSummary
        {
            public int SaleId { get; set; }
            public string Folio { get; set; }
            public OrderKind OrderKind { get; set; }
            public SaleStatus Status { get; set; }
            public DateTime SoldLocal { get; set; }
            public decimal Total { get; set; }
            public string Note { get; set; }
            public string CashierDisplayName { get; set; }
            public string OrderDisplayName { get; set; }
            public PaymentMethod PaymentMethod { get; set; }
            public decimal EstimatedCostTotal { get; set; }
            public decimal EstimatedProfitTotal { get; set; }
            public IList<SoldTicketLine> Items { get; private set; } = new List<SoldTicketLine>();

            public override string ToString()
            {
                return this.SoldLocal.ToString("HH:mm") + " | " + this.Folio + " | " + this.OrderDisplayName + " | $" + this.Total.ToString("N2") + " | " + this.Status;
            }
        }

        private sealed class SoldTicketLine
        {
            public string ProductName { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal LineTotal { get; set; }
        }

        private sealed class ShiftSelectionItem
        {
            public int ShiftId { get; set; }
            public string Folio { get; set; }
            public string Status { get; set; }
            public DateTime OpenedUtc { get; set; }

            public override string ToString()
            {
                var statusText = string.Equals(this.Status, "abierta", StringComparison.OrdinalIgnoreCase) ? "Activo" : "Cerrado";
                return this.Folio + " | " + this.OpenedUtc.ToLocalTime().ToString("dd/MM HH:mm") + " | " + statusText;
            }
        }

        private sealed class CartLineView
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal EstimatedCost { get; set; }
            public bool UsesInventory { get; set; }
            public decimal LineTotal
            {
                get { return this.Quantity * this.UnitPrice; }
            }
        }
    }
}
