using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;
using FogonDesk.Printing;

namespace FogonDesk.Desktop
{
    public sealed class TicketPrintSettingsForm : Form
    {
        private readonly AppStartupState startupState;
        private readonly AuthenticatedUserView currentUser;
        private readonly ITicketPrinter ticketPrinter;
        private readonly ITicketPrintSettingsApplicationService settingsApplicationService;

        private readonly ComboBox printerComboBox;
        private readonly ComboBox ticketWidthComboBox;
        private readonly ComboBox layoutComboBox;
        private readonly NumericUpDown charactersPerLineInput;
        private readonly NumericUpDown titleFontSizeInput;
        private readonly NumericUpDown bodyFontSizeInput;
        private readonly NumericUpDown infoFontSizeInput;
        private readonly NumericUpDown itemsFontSizeInput;
        private readonly NumericUpDown totalFontSizeInput;
        private readonly NumericUpDown footerFontSizeInput;
        private readonly NumericUpDown horizontalOffsetInput;
        private readonly NumericUpDown verticalOffsetInput;
        private readonly CheckBox fullPaperWidthCheckBox;
        private readonly CheckBox showSystemFooterCheckBox;
        private readonly TextBox businessNameTextBox;
        private readonly TextBox sloganTextBox;
        private readonly TextBox addressTextBox;
        private readonly TextBox phoneTextBox;
        private readonly TextBox headerTextBox;
        private readonly TextBox footerTextBox;
        private readonly TextBox systemFooterTextBox;
        private readonly RichTextBox previewTextBox;

        private readonly Panel contentContainer;
        private readonly Panel printerPanel;
        private readonly Panel textsPanel;
        private readonly Panel designPanel;

        private readonly Button printerMenuBtn;
        private readonly Button textsMenuBtn;
        private readonly Button designMenuBtn;

        public TicketPrintSettingsForm(
            AppStartupState startupState, 
            AuthenticatedUserView currentUser, 
            ITicketPrinter ticketPrinter, 
            ITicketPrintSettingsApplicationService settingsApplicationService)
        {
            this.startupState = startupState;
            this.currentUser = currentUser;
            this.ticketPrinter = ticketPrinter;
            this.settingsApplicationService = settingsApplicationService;

            Text = "Configuración de ticket e impresión";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1120, 750);
            MinimumSize = new Size(960, 640);
            BackColor = Color.FromArgb(242, 245, 248);
            Font = new Font("Segoe UI", 10F);

            this.printerComboBox = CreateComboBox();
            this.ticketWidthComboBox = CreateComboBox();
            this.layoutComboBox = CreateComboBox();
            this.charactersPerLineInput = CreateNumberInput(24, 48, GetCharactersPerLine());
            this.titleFontSizeInput = CreateNumberInput(7, 24, GetTitleFontSize());
            this.bodyFontSizeInput = CreateNumberInput(7, 18, GetBodyFontSize());
            this.infoFontSizeInput = CreateNumberInput(7, 18, GetInfoFontSize());
            this.itemsFontSizeInput = CreateNumberInput(7, 18, GetItemsFontSize());
            this.totalFontSizeInput = CreateNumberInput(7, 20, GetTotalFontSize());
            this.footerFontSizeInput = CreateNumberInput(7, 18, GetFooterFontSize());
            this.horizontalOffsetInput = CreateNumberInput(-20, 20, GetHorizontalOffset());
            this.verticalOffsetInput = CreateNumberInput(-20, 20, GetVerticalOffset());
            this.fullPaperWidthCheckBox = CreateCheckBox("Usar ancho completo del papel (sin márgenes de software)", this.startupState.UseFullPaperWidth);
            this.showSystemFooterCheckBox = CreateCheckBox("Mostrar pie de sistema", this.startupState.ShowSystemFooter);
            this.businessNameTextBox = CreateTextBox(false);
            this.sloganTextBox = CreateTextBox(false);
            this.addressTextBox = CreateTextBox(false);
            this.phoneTextBox = CreateTextBox(false);
            this.headerTextBox = CreateTextBox(true);
            this.footerTextBox = CreateTextBox(true);
            this.systemFooterTextBox = CreateTextBox(true);
            this.previewTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Both,
                WordWrap = false,
                BackColor = Color.White,
                Font = new Font("Consolas", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                DetectUrls = false
            };

            this.ticketWidthComboBox.Items.Add("58 mm");
            this.ticketWidthComboBox.Items.Add("80 mm");
            this.ticketWidthComboBox.SelectedItem = this.startupState.TicketWidthMm == 58 ? "58 mm" : "80 mm";
            this.layoutComboBox.Items.Add("Clásico compacto");
            this.layoutComboBox.Items.Add("Detallado con separadores");
            this.layoutComboBox.SelectedItem = string.IsNullOrWhiteSpace(this.startupState.TicketLayoutName) ? "Clásico compacto" : this.startupState.TicketLayoutName;
            if (this.layoutComboBox.SelectedIndex < 0)
            {
                this.layoutComboBox.SelectedIndex = 0;
            }

            var savedSettings = this.settingsApplicationService.GetSettings();
            ApplySavedSettings(savedSettings);

            LoadPrinters();

            // Wire Preview events
            WirePreview(this.printerComboBox);
            WirePreview(this.ticketWidthComboBox);
            WirePreview(this.layoutComboBox);
            this.ticketWidthComboBox.SelectedIndexChanged += TicketWidthChanged;
            WirePreview(this.charactersPerLineInput);
            WirePreview(this.titleFontSizeInput);
            WirePreview(this.bodyFontSizeInput);
            WirePreview(this.infoFontSizeInput);
            WirePreview(this.itemsFontSizeInput);
            WirePreview(this.totalFontSizeInput);
            WirePreview(this.footerFontSizeInput);
            WirePreview(this.horizontalOffsetInput);
            WirePreview(this.verticalOffsetInput);
            WirePreview(this.fullPaperWidthCheckBox);
            WirePreview(this.showSystemFooterCheckBox);
            WirePreview(this.businessNameTextBox);
            WirePreview(this.sloganTextBox);
            WirePreview(this.addressTextBox);
            WirePreview(this.phoneTextBox);
            WirePreview(this.headerTextBox);
            WirePreview(this.footerTextBox);
            WirePreview(this.systemFooterTextBox);

            // Create Sub panels for different configuration sections
            this.printerPanel = BuildPrinterPanel();
            this.textsPanel = BuildTextsPanel();
            this.designPanel = BuildDesignPanel();

            // Content container panel
            this.contentContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };

            // Sidebar Menu buttons
            this.printerMenuBtn = CreateMenuButton("Impresora y Conexión", () => ShowSection(this.printerPanel, this.printerMenuBtn));
            this.textsMenuBtn = CreateMenuButton("Textos del Ticket", () => ShowSection(this.textsPanel, this.textsMenuBtn));
            this.designMenuBtn = CreateMenuButton("Diseño del Ticket", () => ShowSection(this.designPanel, this.designMenuBtn));

            BuildLayout();
            
            // Default active section
            ShowSection(this.printerPanel, this.printerMenuBtn);
            UpdatePreview(null, EventArgs.Empty);
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(12)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // Left Layout with menu on top/side and Content area + Actions below
            var leftSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };
            leftSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            leftSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftSplit.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));

            // Sidebar navigation panel
            var sidebar = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(242, 245, 248), Padding = new Padding(0, 0, 8, 0) };
            this.designMenuBtn.Dock = DockStyle.Top;
            this.textsMenuBtn.Dock = DockStyle.Top;
            this.printerMenuBtn.Dock = DockStyle.Top;

            sidebar.Controls.Add(this.designMenuBtn);
            sidebar.Controls.Add(this.textsMenuBtn);
            sidebar.Controls.Add(this.printerMenuBtn);

            leftSplit.Controls.Add(sidebar, 0, 0);
            leftSplit.Controls.Add(this.contentContainer, 1, 0);

            // Actions panel goes on Row 1 (span both columns or right column)
            var actions = BuildActions();
            leftSplit.Controls.Add(actions, 1, 1);

            // Right Preview setup
            var previewPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Margin = new Padding(12, 0, 0, 0)
            };
            previewPanel.Controls.Add(this.previewTextBox);
            previewPanel.Controls.Add(CreateSectionTitle("Vista previa del diseño"));

            root.Controls.Add(leftSplit, 0, 0);
            root.Controls.Add(previewPanel, 1, 0);
            Controls.Add(root);
        }

        private void ShowSection(Panel sectionPanel, Button menuButton)
        {
            this.contentContainer.Controls.Clear();
            sectionPanel.Dock = DockStyle.Fill;
            this.contentContainer.Controls.Add(sectionPanel);

            // Update active menu colors
            var activeBg = Color.FromArgb(27, 67, 50);
            var inactiveBg = Color.FromArgb(235, 239, 242);

            this.printerMenuBtn.BackColor = inactiveBg;
            this.printerMenuBtn.ForeColor = Color.FromArgb(33, 37, 41);
            this.textsMenuBtn.BackColor = inactiveBg;
            this.textsMenuBtn.ForeColor = Color.FromArgb(33, 37, 41);
            this.designMenuBtn.BackColor = inactiveBg;
            this.designMenuBtn.ForeColor = Color.FromArgb(33, 37, 41);

            menuButton.BackColor = activeBg;
            menuButton.ForeColor = Color.White;
        }

        private Panel BuildPrinterPanel()
        {
            var panel = CreateSectionPanel("Impresora y Conexión", "Selecciona la impresora de tickets instalada en Windows y el formato físico de impresión.", 4);

            var table = CreateFieldTable(4);
            AddField(table, 0, "Impresora", this.printerComboBox);
            AddField(table, 1, "Ancho", this.ticketWidthComboBox);
            AddField(table, 2, "Formato", this.layoutComboBox);
            AddField(table, 3, "Ajuste", this.fullPaperWidthCheckBox);
            panel.Controls.Add(table, 0, 2);

            return panel;
        }

        private Panel BuildTextsPanel()
        {
            var panel = CreateSectionPanel("Textos del Ticket", "Configura todos los textos que aparecen en la vista previa y en el ticket impreso.", 4);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 8, 0, 0)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var businessFields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 8,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 12, 0)
            };
            businessFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (var row = 0; row < 8; row++)
            {
                businessFields.RowStyles.Add(new RowStyle(SizeType.Absolute, row % 2 == 0 ? 26F : 34F));
            }

            businessFields.Controls.Add(CreateFieldLabel("Nombre en el ticket:"), 0, 0);
            this.businessNameTextBox.Dock = DockStyle.Fill;
            businessFields.Controls.Add(this.businessNameTextBox, 0, 1);

            businessFields.Controls.Add(CreateFieldLabel("Eslogan:"), 0, 2);
            this.sloganTextBox.Dock = DockStyle.Fill;
            businessFields.Controls.Add(this.sloganTextBox, 0, 3);

            businessFields.Controls.Add(CreateFieldLabel("Dirección:"), 0, 4);
            this.addressTextBox.Dock = DockStyle.Fill;
            businessFields.Controls.Add(this.addressTextBox, 0, 5);

            businessFields.Controls.Add(CreateFieldLabel("Teléfono / contacto:"), 0, 6);
            this.phoneTextBox.Dock = DockStyle.Fill;
            businessFields.Controls.Add(this.phoneTextBox, 0, 7);

            var ticketFields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Margin = new Padding(12, 0, 0, 0)
            };
            ticketFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ticketFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            ticketFields.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            ticketFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            ticketFields.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            ticketFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            ticketFields.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));

            ticketFields.Controls.Add(CreateFieldLabel("Texto adicional de encabezado:"), 0, 0);
            this.headerTextBox.Dock = DockStyle.Fill;
            ticketFields.Controls.Add(this.headerTextBox, 0, 1);

            ticketFields.Controls.Add(CreateFieldLabel("Pie del cliente:"), 0, 2);
            this.footerTextBox.Dock = DockStyle.Fill;
            ticketFields.Controls.Add(this.footerTextBox, 0, 3);

            ticketFields.Controls.Add(CreateFieldLabel("Pie de sistema:"), 0, 4);
            this.systemFooterTextBox.Dock = DockStyle.Fill;
            ticketFields.Controls.Add(this.systemFooterTextBox, 0, 5);

            table.Controls.Add(businessFields, 0, 0);
            table.Controls.Add(ticketFields, 1, 0);

            panel.Controls.Add(table, 0, 2);
            panel.SetRowSpan(table, 2);
            return panel;
        }

        private Panel BuildDesignPanel()
        {
            var panel = CreateSectionPanel("Diseño del Ticket", "Define las fuentes y los elementos opcionales que se reflejan en la vista previa.", 4);

            var table = CreateFieldTable(10);
            AddField(table, 0, "Fuente título (pt)", this.titleFontSizeInput);
            AddField(table, 1, "Fuente cuerpo (pt)", this.bodyFontSizeInput);
            AddField(table, 2, "Fuente datos venta", this.infoFontSizeInput);
            AddField(table, 3, "Fuente productos", this.itemsFontSizeInput);
            AddField(table, 4, "Fuente total", this.totalFontSizeInput);
            AddField(table, 5, "Fuente pie", this.footerFontSizeInput);
            AddField(table, 6, "Caracteres por línea", this.charactersPerLineInput);
            AddField(table, 7, "Margen horizontal", this.horizontalOffsetInput);
            AddField(table, 8, "Margen vertical", this.verticalOffsetInput);
            AddField(table, 9, "Pie de sistema", this.showSystemFooterCheckBox);
            panel.Controls.Add(table, 0, 2);

            return panel;
        }

        private Control BuildActions()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            panel.Controls.Add(CreateActionButton("Guardar", Color.FromArgb(27, 67, 50), SaveSettings));
            panel.Controls.Add(CreateActionButton("Imprimir prueba", Color.FromArgb(38, 70, 83), PrintTestTicket));
            panel.Controls.Add(CreateActionButton("Cancelar", Color.FromArgb(90, 90, 90), delegate { Close(); }));
            return panel;
        }

        private void SaveSettings(object sender, EventArgs eventArgs)
        {
            var settings = new TicketPrintSettingsView
            {
                PrinterName = Convert.ToString(this.printerComboBox.SelectedItem) ?? string.Empty,
                TicketWidthMm = Convert.ToString(this.ticketWidthComboBox.SelectedItem) == "58 mm" ? 58 : 80,
                UseFullPaperWidth = this.fullPaperWidthCheckBox.Checked,
                TicketCharactersPerLine = Convert.ToInt32(this.charactersPerLineInput.Value),
                DiningTableCount = this.startupState.DiningTableCount <= 0 ? 5 : this.startupState.DiningTableCount,
                TicketTitleFontSize = Convert.ToInt32(this.titleFontSizeInput.Value),
                TicketBodyFontSize = Convert.ToInt32(this.bodyFontSizeInput.Value),
                TicketInfoFontSize = Convert.ToInt32(this.infoFontSizeInput.Value),
                TicketItemsFontSize = Convert.ToInt32(this.itemsFontSizeInput.Value),
                TicketTotalFontSize = Convert.ToInt32(this.totalFontSizeInput.Value),
                TicketFooterFontSize = Convert.ToInt32(this.footerFontSizeInput.Value),
                TicketHorizontalOffset = Convert.ToInt32(this.horizontalOffsetInput.Value),
                TicketVerticalOffset = Convert.ToInt32(this.verticalOffsetInput.Value),
                PrintKitchenTicket = this.startupState.PrintKitchenTicket,
                ShowSystemFooter = this.showSystemFooterCheckBox.Checked,
                TicketLayoutName = Convert.ToString(this.layoutComboBox.SelectedItem) ?? "Clásico compacto",
                BusinessName = this.businessNameTextBox.Text,
                Address = this.addressTextBox.Text,
                Phone = this.phoneTextBox.Text,
                SystemFooterText = this.systemFooterTextBox.Text,
                HeaderText = this.headerTextBox.Text,
                FooterText = this.footerTextBox.Text,
                Slogan = this.sloganTextBox.Text
            };

            var result = this.settingsApplicationService.SaveSettings(settings);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ApplySavedSettings(settings);
            MessageBox.Show(result.Message, "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ApplySavedSettings(TicketPrintSettingsView settings)
        {
            if (settings == null)
            {
                return;
            }

            this.startupState.ActivePrinterName = settings.PrinterName ?? string.Empty;
            this.startupState.TicketWidthMm = settings.TicketWidthMm == 58 ? 58 : 80;
            this.startupState.UseFullPaperWidth = settings.UseFullPaperWidth;
            this.startupState.TicketCharactersPerLine = settings.TicketCharactersPerLine <= 0 ? GetDefaultCharactersPerLine(this.startupState.TicketWidthMm) : settings.TicketCharactersPerLine;
            this.startupState.DiningTableCount = settings.DiningTableCount <= 0 ? 5 : settings.DiningTableCount;
            this.startupState.TicketTitleFontSize = settings.TicketTitleFontSize <= 0 ? 12 : settings.TicketTitleFontSize;
            this.startupState.TicketBodyFontSize = settings.TicketBodyFontSize <= 0 ? 9 : settings.TicketBodyFontSize;
            this.startupState.TicketInfoFontSize = settings.TicketInfoFontSize <= 0 ? 9 : settings.TicketInfoFontSize;
            this.startupState.TicketItemsFontSize = settings.TicketItemsFontSize <= 0 ? 9 : settings.TicketItemsFontSize;
            this.startupState.TicketTotalFontSize = settings.TicketTotalFontSize <= 0 ? 9 : settings.TicketTotalFontSize;
            this.startupState.TicketFooterFontSize = settings.TicketFooterFontSize <= 0 ? 9 : settings.TicketFooterFontSize;
            this.startupState.TicketHorizontalOffset = settings.TicketHorizontalOffset;
            this.startupState.TicketVerticalOffset = settings.TicketVerticalOffset;
            this.startupState.PrintKitchenTicket = settings.PrintKitchenTicket;
            this.startupState.ShowSystemFooter = settings.ShowSystemFooter;
            this.startupState.TicketLayoutName = string.IsNullOrWhiteSpace(settings.TicketLayoutName) ? "Clásico compacto" : settings.TicketLayoutName;
            this.startupState.BusinessName = string.IsNullOrWhiteSpace(settings.BusinessName) ? this.startupState.BusinessName : settings.BusinessName;
            this.startupState.BusinessSlogan = settings.Slogan ?? string.Empty;
            this.startupState.BusinessAddress = settings.Address ?? string.Empty;
            this.startupState.BusinessPhone = settings.Phone ?? string.Empty;
            this.startupState.TicketHeaderText = settings.HeaderText ?? string.Empty;
            this.startupState.TicketFooterText = settings.FooterText ?? string.Empty;
            this.startupState.TicketSystemFooterText = settings.SystemFooterText ?? string.Empty;
            this.charactersPerLineInput.Value = ClampToRange(this.startupState.TicketCharactersPerLine, this.charactersPerLineInput.Minimum, this.charactersPerLineInput.Maximum);
            this.fullPaperWidthCheckBox.Checked = settings.UseFullPaperWidth;
            this.infoFontSizeInput.Value = ClampToRange(this.startupState.TicketInfoFontSize, this.infoFontSizeInput.Minimum, this.infoFontSizeInput.Maximum);
            this.itemsFontSizeInput.Value = ClampToRange(this.startupState.TicketItemsFontSize, this.itemsFontSizeInput.Minimum, this.itemsFontSizeInput.Maximum);
            this.totalFontSizeInput.Value = ClampToRange(this.startupState.TicketTotalFontSize, this.totalFontSizeInput.Minimum, this.totalFontSizeInput.Maximum);
            this.footerFontSizeInput.Value = ClampToRange(this.startupState.TicketFooterFontSize, this.footerFontSizeInput.Minimum, this.footerFontSizeInput.Maximum);
            this.horizontalOffsetInput.Value = ClampToRange(settings.TicketHorizontalOffset, this.horizontalOffsetInput.Minimum, this.horizontalOffsetInput.Maximum);
            this.verticalOffsetInput.Value = ClampToRange(settings.TicketVerticalOffset, this.verticalOffsetInput.Minimum, this.verticalOffsetInput.Maximum);
            this.showSystemFooterCheckBox.Checked = settings.ShowSystemFooter;
            this.businessNameTextBox.Text = settings.BusinessName ?? this.startupState.BusinessName ?? string.Empty;
            this.sloganTextBox.Text = settings.Slogan ?? string.Empty;
            this.addressTextBox.Text = settings.Address ?? string.Empty;
            this.phoneTextBox.Text = settings.Phone ?? string.Empty;
            this.headerTextBox.Text = settings.HeaderText ?? string.Empty;
            this.footerTextBox.Text = settings.FooterText ?? string.Empty;
            this.systemFooterTextBox.Text = settings.SystemFooterText ?? string.Empty;
        }

        private void PrintTestTicket(object sender, EventArgs eventArgs)
        {
            var printerName = Convert.ToString(this.printerComboBox.SelectedItem);
            if (string.IsNullOrWhiteSpace(printerName))
            {
                MessageBox.Show("Selecciona una impresora antes de imprimir la prueba.", "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = this.ticketPrinter.Print(new TicketPrintJob
            {
                PrinterName = printerName,
                Title = "Prueba de ticket",
                TicketWidthMm = Convert.ToString(this.ticketWidthComboBox.SelectedItem) == "58 mm" ? 58 : 80,
                UseFullPaperWidth = this.fullPaperWidthCheckBox.Checked,
                Lines = BuildPreviewLines(),
                HeaderFontSize = Convert.ToInt32(this.titleFontSizeInput.Value),
                InfoFontSize = Convert.ToInt32(this.infoFontSizeInput.Value),
                ItemsFontSize = Convert.ToInt32(this.itemsFontSizeInput.Value),
                TotalFontSize = Convert.ToInt32(this.totalFontSizeInput.Value),
                FooterFontSize = Convert.ToInt32(this.footerFontSizeInput.Value)
            });
            MessageBox.Show(result.Message, "Impresión", MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void UpdatePreview(object sender, EventArgs eventArgs)
        {
            var lines = BuildPreviewLines().ToArray();
            this.previewTextBox.SuspendLayout();
            this.previewTextBox.Clear();
            this.previewTextBox.Font = new Font("Consolas", Convert.ToSingle(this.bodyFontSizeInput.Value));
            this.previewTextBox.Text = string.Join(Environment.NewLine, lines);
            this.previewTextBox.SelectAll();
            this.previewTextBox.SelectionFont = new Font("Consolas", Convert.ToSingle(this.bodyFontSizeInput.Value), FontStyle.Regular);
            ApplySectionPreviewFonts(lines);
            ApplyTitlePreviewFont();
            this.previewTextBox.SelectionStart = 0;
            this.previewTextBox.SelectionLength = 0;
            this.previewTextBox.ResumeLayout();
        }

        private IList<string> BuildPreviewLines()
        {
            var testItems = new List<(string Qty, string Name, string Total)>
            {
                ("2", "Taco árabe clásico", "76.00"),
                ("1", "Agua de sabor", "24.00")
            };

            int widthMm = Convert.ToString(this.ticketWidthComboBox.SelectedItem) == "58 mm" ? 58 : 80;
            string layout = Convert.ToString(this.layoutComboBox.SelectedItem) ?? "Clásico compacto";

            return TicketFormatter.FormatReceipt(
                string.IsNullOrWhiteSpace(this.businessNameTextBox.Text) ? "NOMBRE DEL NEGOCIO" : this.businessNameTextBox.Text,
                this.sloganTextBox.Text,
                "F-00123",
                DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                this.currentUser.DisplayName,
                "Mesa",
                "Mesa 3",
                testItems,
                "100.00",
                widthMm,
                layout,
                BuildHeaderText(),
                this.footerTextBox.Text,
                this.showSystemFooterCheckBox.Checked,
                this.systemFooterTextBox.Text,
                Convert.ToInt32(this.horizontalOffsetInput.Value),
                Convert.ToInt32(this.verticalOffsetInput.Value),
                Convert.ToInt32(this.charactersPerLineInput.Value)
            );
        }

        private void ApplySectionPreviewFonts(string[] lines)
        {
            var currentIndex = 0;
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex] ?? string.Empty;
                var fontSize = GetPreviewFontSizeForLine(line);
                this.previewTextBox.Select(currentIndex, line.Length);
                this.previewTextBox.SelectionFont = new Font("Consolas", fontSize, FontStyle.Regular);
                currentIndex += line.Length + Environment.NewLine.Length;
            }
        }

        private float GetPreviewFontSizeForLine(string line)
        {
            var value = (line ?? string.Empty).Trim();
            if (value.StartsWith("Folio:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("Atendió:", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "VENTA", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToSingle(this.infoFontSizeInput.Value);
            }

            if (value.StartsWith("CANT", StringComparison.OrdinalIgnoreCase) || value.Contains(" x ") || string.Equals(value, "PRODUCTOS", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToSingle(this.itemsFontSizeInput.Value);
            }

            if (value.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToSingle(this.totalFontSizeInput.Value);
            }

            if (value.Length > 0 && !value.Any(char.IsLetterOrDigit))
            {
                return Convert.ToSingle(this.bodyFontSizeInput.Value);
            }

            return Convert.ToSingle(this.footerFontSizeInput.Value);
        }

        private string BuildHeaderText()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(this.addressTextBox.Text))
            {
                parts.Add(this.addressTextBox.Text.Trim());
            }

            if (!string.IsNullOrWhiteSpace(this.phoneTextBox.Text))
            {
                parts.Add(this.phoneTextBox.Text.Trim());
            }

            if (!string.IsNullOrWhiteSpace(this.headerTextBox.Text))
            {
                parts.Add(this.headerTextBox.Text.Trim());
            }

            return string.Join(Environment.NewLine, parts);
        }

        private void ApplyTitlePreviewFont()
        {
            if (this.previewTextBox.TextLength == 0)
            {
                return;
            }

            var titleText = string.IsNullOrWhiteSpace(this.businessNameTextBox.Text) ? "NOMBRE DEL NEGOCIO" : this.businessNameTextBox.Text.Trim().ToUpperInvariant();
            var titleStart = this.previewTextBox.Text.IndexOf(titleText, StringComparison.OrdinalIgnoreCase);
            if (titleStart < 0)
            {
                return;
            }

            this.previewTextBox.Select(titleStart, titleText.Length);
            this.previewTextBox.SelectionFont = new Font("Consolas", Convert.ToSingle(this.titleFontSizeInput.Value), FontStyle.Bold);
        }

        private void LoadPrinters()
        {
            this.printerComboBox.Items.Clear();
            foreach (var printerName in this.ticketPrinter.GetInstalledPrinters())
            {
                this.printerComboBox.Items.Add(printerName);
            }

            if (!string.IsNullOrWhiteSpace(this.startupState.ActivePrinterName) && this.printerComboBox.Items.Contains(this.startupState.ActivePrinterName))
            {
                this.printerComboBox.SelectedItem = this.startupState.ActivePrinterName;
            }
            else if (this.printerComboBox.Items.Count > 0)
            {
                this.printerComboBox.SelectedIndex = 0;
            }
        }

        private int GetTitleFontSize()
        {
            return this.startupState.TicketTitleFontSize <= 0 ? 12 : this.startupState.TicketTitleFontSize;
        }

        private int GetBodyFontSize()
        {
            return this.startupState.TicketBodyFontSize <= 0 ? 9 : this.startupState.TicketBodyFontSize;
        }

        private int GetCharactersPerLine()
        {
            return this.startupState.TicketCharactersPerLine <= 0 ? GetDefaultCharactersPerLine(this.startupState.TicketWidthMm) : Math.Max(24, Math.Min(48, this.startupState.TicketCharactersPerLine));
        }

        private static int GetDefaultCharactersPerLine(int ticketWidthMm)
        {
            return ticketWidthMm == 58 ? 30 : 42;
        }

        private void TicketWidthChanged(object sender, EventArgs eventArgs)
        {
            if (Convert.ToString(this.ticketWidthComboBox.SelectedItem) == "58 mm" && this.charactersPerLineInput.Value > 30)
            {
                this.charactersPerLineInput.Value = 30;
            }
        }

        private int GetInfoFontSize()
        {
            return this.startupState.TicketInfoFontSize <= 0 ? GetBodyFontSize() : this.startupState.TicketInfoFontSize;
        }

        private int GetItemsFontSize()
        {
            return this.startupState.TicketItemsFontSize <= 0 ? GetBodyFontSize() : this.startupState.TicketItemsFontSize;
        }

        private int GetTotalFontSize()
        {
            return this.startupState.TicketTotalFontSize <= 0 ? GetBodyFontSize() : this.startupState.TicketTotalFontSize;
        }

        private int GetFooterFontSize()
        {
            return this.startupState.TicketFooterFontSize <= 0 ? GetBodyFontSize() : this.startupState.TicketFooterFontSize;
        }

        private int GetHorizontalOffset()
        {
            return Math.Max(-20, Math.Min(20, this.startupState.TicketHorizontalOffset));
        }

        private int GetVerticalOffset()
        {
            return Math.Max(-20, Math.Min(20, this.startupState.TicketVerticalOffset));
        }

        private static decimal ClampToRange(int value, decimal minimum, decimal maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static Label CreateSectionTitle(string text)
        {
            return new Label
            {
                Dock = DockStyle.Top,
                Height = 42,
                Text = text,
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 67, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static TableLayoutPanel CreateSectionPanel(string title, string description, int rowCount)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = rowCount,
                Padding = new Padding(18, 18, 18, 10),
                BackColor = Color.White
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(CreateSectionTitle(title), 0, 0);
            panel.Controls.Add(CreateSectionDescription(description), 0, 1);
            return panel;
        }

        private static Label CreateSectionDescription(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.TopLeft
            };
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold) };
        }

        private Button CreateMenuButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 48,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(235, 239, 242),
                ForeColor = Color.FromArgb(33, 37, 41),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 4)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += delegate { onClick(); };
            return btn;
        }

        private static TableLayoutPanel CreateFieldTable(int rows)
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = rows,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 8, 0, 0),
                Margin = new Padding(0, 0, 0, 0)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (var row = 0; row < rows; row++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            }

            return table;
        }

        private static void AddField(TableLayoutPanel table, int row, string label, Control control)
        {
            table.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold) }, 0, row);
            control.Dock = DockStyle.Fill;
            table.Controls.Add(control, 1, row);
        }

        private static ComboBox CreateComboBox()
        {
            return new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, IntegralHeight = false };
        }

        private static NumericUpDown CreateNumberInput(int minimum, int maximum, int value)
        {
            return new NumericUpDown { Minimum = minimum, Maximum = maximum, Value = value };
        }

        private static CheckBox CreateCheckBox(string text, bool isChecked)
        {
            return new CheckBox { Text = text, Checked = isChecked, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold) };
        }

        private static TextBox CreateTextBox(bool multiline = false)
        {
            var tb = new TextBox { BorderStyle = BorderStyle.FixedSingle, Multiline = multiline };
            if (multiline)
            {
                tb.ScrollBars = ScrollBars.Vertical;
            }
            return tb;
        }

        private static Button CreateActionButton(string text, Color color, EventHandler handler)
        {
            var button = new Button
            {
                Text = text,
                Width = 142,
                Height = 40,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(8, 6, 0, 0)
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += handler;
            return button;
        }

        private void WirePreview(ComboBox control)
        {
            control.SelectedIndexChanged += UpdatePreview;
        }

        private void WirePreview(NumericUpDown control)
        {
            control.ValueChanged += UpdatePreview;
        }

        private void WirePreview(CheckBox control)
        {
            control.CheckedChanged += UpdatePreview;
        }

        private void WirePreview(TextBox control)
        {
            control.TextChanged += UpdatePreview;
        }
    }
}