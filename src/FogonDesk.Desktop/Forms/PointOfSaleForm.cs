using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;
using FogonDesk.Domain.Common;
using FogonDesk.Printing;

namespace FogonDesk.Desktop
{
    public sealed class PointOfSaleForm : Form
    {
        private readonly AppStartupState startupState;
        private readonly AuthenticatedUserView currentUser;
        private readonly CashShiftSummaryView activeShift;
        private readonly ICatalogApplicationService catalogApplicationService;
        private readonly ISalesApplicationService salesApplicationService;
        private readonly ITicketPrinter ticketPrinter;
        private readonly OrderKind? initialOrderKind;
        private readonly string initialNote;
        private readonly IList<SaleLineDraft> initialItems;
        private readonly ListBox categoriesListBox;
        private readonly FlowLayoutPanel productsPanel;
        private readonly DataGridView cartGrid;
        private readonly BindingList<CartLineView> cartLines;
        private ComboBox paymentMethodComboBox;
        private ComboBox orderKindComboBox;
        private Label totalLabel;

        public bool PendingTicketSaved { get; private set; }
        public string PendingTicketName { get; private set; }
        public IList<SaleLineDraft> PendingTicketItems { get; private set; }
        public decimal PendingTicketTotal { get; private set; }
        public OrderKind PendingTicketOrderKind { get; private set; }
        public string PendingTicketNote { get; private set; }

        public PointOfSaleForm(
            AppStartupState startupState,
            AuthenticatedUserView currentUser,
            CashShiftSummaryView activeShift,
            ICatalogApplicationService catalogApplicationService,
            ISalesApplicationService salesApplicationService,
            ITicketPrinter ticketPrinter)
            : this(startupState, currentUser, activeShift, catalogApplicationService, salesApplicationService, ticketPrinter, null, string.Empty, null)
        {
        }

        public PointOfSaleForm(
            AppStartupState startupState,
            AuthenticatedUserView currentUser,
            CashShiftSummaryView activeShift,
            ICatalogApplicationService catalogApplicationService,
            ISalesApplicationService salesApplicationService,
            ITicketPrinter ticketPrinter,
            OrderKind? initialOrderKind,
            string initialNote,
            IList<SaleLineDraft> initialItems)
        {
            this.startupState = startupState;
            this.currentUser = currentUser;
            this.activeShift = activeShift;
            this.catalogApplicationService = catalogApplicationService;
            this.salesApplicationService = salesApplicationService;
            this.ticketPrinter = ticketPrinter;
            this.initialOrderKind = initialOrderKind;
            this.initialNote = initialNote ?? string.Empty;
            this.initialItems = initialItems;
            this.cartLines = new BindingList<CartLineView>();

            Text = "Punto de venta";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1200, 760);
            BackColor = Color.WhiteSmoke;
            Font = new Font("Segoe UI", 10F);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(16)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

            this.categoriesListBox = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold) };
            this.categoriesListBox.SelectedIndexChanged += CategoriesChanged;

            this.productsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            this.cartGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                DataSource = this.cartLines,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            this.cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Producto", DataPropertyName = "ProductName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            this.cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cant.", DataPropertyName = "Quantity", Width = 70 });
            this.cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unit.", DataPropertyName = "UnitPrice", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            this.cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Total", DataPropertyName = "LineTotal", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });

            root.Controls.Add(BuildCategoriesPanel(), 0, 0);
            root.Controls.Add(BuildProductsPanel(), 1, 0);
            root.Controls.Add(BuildCartPanel(), 2, 0);
            Controls.Add(root);

            Load += OnLoad;
        }

        public string LastReceiptPrinterName { get; private set; }
        public string LastReceiptTitle { get; private set; }
        public IList<string> LastReceiptLines { get; private set; }

        private Control BuildCategoriesPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
            panel.Controls.Add(this.categoriesListBox);
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Text = "Categorías",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 67, 50)
            });
            return panel;
        }

        private Control BuildProductsPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
            panel.Controls.Add(this.productsPanel);
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Text = "Productos",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 67, 50)
            });
            return panel;
        }

        private Control BuildCartPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 7,
                ColumnCount = 1,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

            this.paymentMethodComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            this.orderKindComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            this.totalLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold), Text = "$0.00" };

            var adjustmentsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            adjustmentsPanel.Controls.Add(CreateSmallButton("+1", IncreaseSelectedLine));
            adjustmentsPanel.Controls.Add(CreateSmallButton("-1", DecreaseSelectedLine));
            adjustmentsPanel.Controls.Add(CreateSmallButton("Quitar", RemoveSelectedLine));
            adjustmentsPanel.Controls.Add(CreateSmallButton("Limpiar", ClearCart));

            var chargeButton = new Button
            {
                Text = "Cobrar venta",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(228, 108, 10),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold)
            };
            chargeButton.Click += ChargeSale;

            var pendingButton = new Button
            {
                Text = "Guardar pendiente",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(69, 123, 157),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold)
            };
            pendingButton.FlatAppearance.BorderSize = 0;
            pendingButton.Click += SavePendingTicket;

            panel.Controls.Add(new Label { Text = "Carrito", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold), ForeColor = Color.FromArgb(27, 67, 50) }, 0, 0);
            panel.Controls.Add(this.cartGrid, 0, 1);
            panel.Controls.Add(BuildComboRow("Tipo orden", this.orderKindComboBox, "Pago", this.paymentMethodComboBox), 0, 2);
            panel.Controls.Add(adjustmentsPanel, 0, 3);
            panel.Controls.Add(this.totalLabel, 0, 4);
            panel.Controls.Add(chargeButton, 0, 5);
            panel.Controls.Add(pendingButton, 0, 6);
            return panel;
        }

        private void OnLoad(object sender, EventArgs eventArgs)
        {
            this.paymentMethodComboBox.DataSource = new[]
            {
                PaymentMethod.Efectivo,
                PaymentMethod.Tarjeta,
                PaymentMethod.Transferencia
            };

            this.orderKindComboBox.DataSource = new[]
            {
                OrderKind.Mostrador,
                OrderKind.ParaLlevar,
                OrderKind.Mesa,
                OrderKind.PlataformaDigital
            };
            if (this.initialOrderKind.HasValue)
            {
                this.orderKindComboBox.SelectedItem = this.initialOrderKind.Value;
            }

            if (this.initialItems != null)
            {
                foreach (var item in this.initialItems)
                {
                    this.cartLines.Add(new CartLineView
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        EstimatedCost = item.EstimatedCost,
                        UsesInventory = item.UsesInventory
                    });
                }
                UpdateTotal();
            }

            var categories = this.catalogApplicationService.GetCategories();
            this.categoriesListBox.DisplayMember = "Name";
            this.categoriesListBox.ValueMember = "Id";
            this.categoriesListBox.DataSource = categories;
        }

        private void SavePendingTicket(object sender, EventArgs eventArgs)
        {
            if (this.cartLines.Count == 0)
            {
                MessageBox.Show("Agrega productos antes de guardar un pendiente.", "Órdenes pendientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ticketName = PromptPendingTicketName();
            if (ticketName == null)
            {
                return;
            }

            this.PendingTicketSaved = true;
            this.PendingTicketName = ticketName;
            this.PendingTicketOrderKind = (OrderKind)this.orderKindComboBox.SelectedItem;
            this.PendingTicketNote = this.initialNote;
            this.PendingTicketItems = this.cartLines.Select(item => new SaleLineDraft
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                EstimatedCost = item.EstimatedCost,
                UsesInventory = item.UsesInventory
            }).ToList();
            this.PendingTicketTotal = this.PendingTicketItems.Sum(item => item.Quantity * item.UnitPrice);

            Close();
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
                layout.Controls.Add(new Label { Text = "", Dock = DockStyle.Fill }, 0, 2);
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

        private void CategoriesChanged(object sender, EventArgs eventArgs)
        {
            var selectedCategory = this.categoriesListBox.SelectedItem as CategoryViewModel;
            this.productsPanel.Controls.Clear();
            if (selectedCategory == null)
            {
                return;
            }

            foreach (var product in this.catalogApplicationService.GetProductsByCategory(selectedCategory.Id))
            {
                this.productsPanel.Controls.Add(CreateProductButton(product));
            }
        }

        private Button CreateProductButton(ProductViewModel product)
        {
            var button = new Button
            {
                Width = 180,
                Height = 92,
                Margin = new Padding(8),
                FlatStyle = FlatStyle.Flat,
                BackColor = product.UsesInventory && product.StockOnHand <= 0 ? Color.FromArgb(230, 230, 230) : Color.White,
                ForeColor = Color.FromArgb(27, 67, 50),
                Text = product.Name + Environment.NewLine + "$" + product.SalePrice.ToString("N2")
            };
            button.Click += delegate
            {
                if (product.UsesInventory && product.StockOnHand <= 0)
                {
                    MessageBox.Show("Sin inventario disponible para " + product.Name + ".", "Inventario", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                AddProduct(product);
            };
            return button;
        }

        private void AddProduct(ProductViewModel product)
        {
            var line = this.cartLines.FirstOrDefault(item => item.ProductId == product.Id);
            if (line == null)
            {
                this.cartLines.Add(new CartLineView
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = 1,
                    UnitPrice = product.SalePrice,
                    EstimatedCost = product.EstimatedCost,
                    UsesInventory = product.UsesInventory
                });
            }
            else
            {
                line.Quantity += 1;
                RefreshCart();
            }

            UpdateTotal();
        }

        private void IncreaseSelectedLine(object sender, EventArgs eventArgs)
        {
            var line = GetSelectedLine();
            if (line == null)
            {
                return;
            }

            line.Quantity += 1;
            RefreshCart();
            UpdateTotal();
        }

        private void DecreaseSelectedLine(object sender, EventArgs eventArgs)
        {
            var line = GetSelectedLine();
            if (line == null)
            {
                return;
            }

            line.Quantity -= 1;
            if (line.Quantity <= 0)
            {
                this.cartLines.Remove(line);
            }
            else
            {
                RefreshCart();
            }

            UpdateTotal();
        }

        private void RemoveSelectedLine(object sender, EventArgs eventArgs)
        {
            var line = GetSelectedLine();
            if (line == null)
            {
                return;
            }

            this.cartLines.Remove(line);
            UpdateTotal();
        }

        private void ClearCart(object sender, EventArgs eventArgs)
        {
            this.cartLines.Clear();
            UpdateTotal();
        }

        private void ChargeSale(object sender, EventArgs eventArgs)
        {
            if (this.cartLines.Count == 0)
            {
                MessageBox.Show("Agrega productos antes de cobrar.", "Punto de venta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var request = new CreateSaleRequest
            {
                UserId = this.currentUser.UserId,
                UserName = this.currentUser.Username,
                CashShiftId = this.activeShift == null ? (int?)null : this.activeShift.ShiftId,
                OrderKind = (OrderKind)this.orderKindComboBox.SelectedItem,
                PaymentMethod = (PaymentMethod)this.paymentMethodComboBox.SelectedItem,
                Note = this.initialNote,
                Items = this.cartLines.Select(item => new SaleLineDraft
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    EstimatedCost = item.EstimatedCost,
                    UsesInventory = item.UsesInventory
                }).ToList()
            };

            var result = this.salesApplicationService.RegisterSale(request);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Punto de venta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            TryPrintReceipt(result.Data);
            MessageBox.Show("Venta registrada con folio " + result.Data.Folio + ".", "Punto de venta", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.cartLines.Clear();
            UpdateTotal();
            CategoriesChanged(null, EventArgs.Empty);
        }

        private void TryPrintReceipt(CreateSaleResult sale)
        {
            var formattedItems = this.cartLines.Select(item => (
                item.Quantity.ToString("0"),
                item.ProductName,
                item.LineTotal.ToString("N2")
            )).ToList();

            string orderKindText = this.orderKindComboBox.SelectedItem?.ToString() ?? "Venta";

            int widthMm = this.startupState.TicketWidthMm <= 0 ? 80 : this.startupState.TicketWidthMm;
            string layout = string.IsNullOrWhiteSpace(this.startupState.TicketLayoutName) ? "Clásico compacto" : this.startupState.TicketLayoutName;
            string headerText = BuildTicketHeaderText();

            var lines = TicketFormatter.FormatReceipt(
                this.startupState.BusinessName ?? "MrAlbertoCompany",
                this.startupState.BusinessSlogan,
                sale.Folio,
                sale.SoldUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                this.currentUser.DisplayName,
                orderKindText,
                this.initialNote,
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
                this.startupState.TicketCharactersPerLine
            );

            this.LastReceiptPrinterName = this.startupState.ActivePrinterName;
            this.LastReceiptTitle = "Ticket de venta";
            this.LastReceiptLines = lines;

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

        private CartLineView GetSelectedLine()
        {
            if (this.cartGrid.CurrentRow == null)
            {
                return null;
            }

            return this.cartGrid.CurrentRow.DataBoundItem as CartLineView;
        }

        private void RefreshCart()
        {
            this.cartGrid.Refresh();
        }

        private void UpdateTotal()
        {
            var total = this.cartLines.Sum(item => item.LineTotal);
            this.totalLabel.Text = "Total: $" + total.ToString("N2");
        }

        private static Control BuildComboRow(string leftLabel, Control leftControl, string rightLabel, Control rightControl)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1
            };
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
                Width = 88,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(6, 0, 6, 0)
            };
            button.Click += handler;
            return button;
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
