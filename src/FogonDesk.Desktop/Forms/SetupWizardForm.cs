using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Desktop
{
    public sealed class SetupWizardForm : Form
    {
        private readonly IInitialSetupService initialSetupService;
        private readonly ITicketPrinter ticketPrinter;
        private readonly IList<BusinessTemplateDefinition> templates;
        private readonly ComboBox templateCombo;
        private readonly ComboBox ticketWidthCombo;
        private readonly ComboBox printerCombo;
        private readonly TextBox businessNameTextBox;
        private readonly TextBox sloganTextBox;
        private readonly TextBox addressTextBox;
        private readonly TextBox phoneTextBox;
        private readonly TextBox primaryColorTextBox;
        private readonly TextBox accentColorTextBox;
        private readonly TextBox headerTextBox;
        private readonly TextBox footerTextBox;
        private readonly TextBox stationNameTextBox;
        private readonly TextBox stationCodeTextBox;
        private readonly TextBox adminUserTextBox;
        private readonly TextBox adminDisplayNameTextBox;
        private readonly TextBox adminPasswordTextBox;
        private readonly TextBox adminPasswordConfirmTextBox;
        private readonly TextBox manualBusinessTypeTextBox;
        private readonly TableLayoutPanel businessFieldsTable;
        private readonly BindingList<SeedCategoryDefinition> editableCategories;
        private readonly SplitContainer contentSplit;
        private readonly Panel catalogSectionPanel;
        private readonly DataGridView categoriesGrid;
        private readonly Label errorLabel;

        public SetupWizardForm(IInitialSetupService initialSetupService, ITicketPrinter ticketPrinter)
        {
            this.initialSetupService = initialSetupService;
            this.ticketPrinter = ticketPrinter;
            this.templates = this.initialSetupService.GetAvailableTemplates();
            this.editableCategories = new BindingList<SeedCategoryDefinition>();

            var workingArea = Screen.PrimaryScreen == null
                ? new Rectangle(0, 0, 1360, 860)
                : Screen.PrimaryScreen.WorkingArea;
            var initialWidth = Math.Min(workingArea.Width, Math.Min(1240, Math.Max(900, workingArea.Width - 12)));
            var initialHeight = Math.Min(workingArea.Height, Math.Min(720, Math.Max(620, workingArea.Height - 16)));

            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;
            Text = "Configuración inicial";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            Size = new Size(initialWidth, initialHeight);
            MinimumSize = new Size(Math.Min(workingArea.Width, 820), Math.Min(workingArea.Height, 620));
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.FromArgb(242, 245, 248);

            var titleLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point),
                Text = "Asistente inicial de MrAlbertoCompany",
                TextAlign = ContentAlignment.BottomLeft
            };

            var subtitleLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.DimGray,
                Text = "Configura lo esencial y guarda sin perder espacio en pantalla.",
                TextAlign = ContentAlignment.TopLeft
            };

            var headerTextLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            headerTextLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            headerTextLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            headerTextLayout.Controls.Add(titleLabel, 0, 0);
            headerTextLayout.Controls.Add(subtitleLabel, 0, 1);

            var headerActionsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Dock = DockStyle.Top,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerLayout.Controls.Add(headerTextLayout, 0, 0);
            headerLayout.Controls.Add(headerActionsPanel, 1, 0);

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = Color.White,
                Padding = new Padding(18, 10, 18, 10)
            };
            headerPanel.Controls.Add(headerLayout);

            this.contentSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterWidth = 14,
                BorderStyle = BorderStyle.None,
                BackColor = BackColor
            };
            this.contentSplit.Panel1.BackColor = BackColor;
            this.contentSplit.Panel2.BackColor = BackColor;

            this.businessNameTextBox = CreateTextBox();
            this.sloganTextBox = CreateTextBox();
            this.addressTextBox = CreateTextBox(true);
            this.phoneTextBox = CreateTextBox();
            this.primaryColorTextBox = CreateTextBox();
            this.primaryColorTextBox.Text = "#1B4332";
            this.accentColorTextBox = CreateTextBox();
            this.accentColorTextBox.Text = "#F4A261";
            this.headerTextBox = CreateTextBox();
            this.footerTextBox = CreateTextBox();
            this.stationNameTextBox = CreateTextBox();
            this.stationNameTextBox.Text = "Caja principal";
            this.stationCodeTextBox = CreateTextBox();
            this.stationCodeTextBox.Text = "CAJA01";
            this.adminUserTextBox = CreateTextBox();
            this.adminDisplayNameTextBox = CreateTextBox();
            this.adminDisplayNameTextBox.Text = "Administrador";
            this.adminPasswordTextBox = CreateTextBox();
            this.adminPasswordTextBox.UseSystemPasswordChar = true;
            this.adminPasswordConfirmTextBox = CreateTextBox();
            this.adminPasswordConfirmTextBox.UseSystemPasswordChar = true;
            this.manualBusinessTypeTextBox = CreateTextBox();

            this.templateCombo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Height = 40,
                DisplayMember = "DisplayName"
            };
            foreach (var template in this.templates)
            {
                this.templateCombo.Items.Add(template);
            }

            this.ticketWidthCombo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                IntegralHeight = false
            };
            this.ticketWidthCombo.Items.Add(80);
            this.ticketWidthCombo.SelectedIndex = 0;

            this.printerCombo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.printerCombo.Items.Add(string.Empty);
            this.printerCombo.SelectedIndex = 0;

            this.businessFieldsTable = CreateFieldTable();
            AddField(this.businessFieldsTable, 0, "Nombre del negocio", this.businessNameTextBox);
            AddField(this.businessFieldsTable, 1, "Tipo de negocio", this.templateCombo);
            AddField(this.businessFieldsTable, 2, "Tipo manual", this.manualBusinessTypeTextBox);
            AddField(this.businessFieldsTable, 3, "Slogan", this.sloganTextBox);
            AddField(this.businessFieldsTable, 4, "Teléfono", this.phoneTextBox);
            AddField(this.businessFieldsTable, 5, "Dirección", this.addressTextBox);
            SetTableRowVisibility(this.businessFieldsTable, 2, false);

            var accessFields = CreateFieldTable();
            AddField(accessFields, 0, "Nombre estación", this.stationNameTextBox);
            AddField(accessFields, 1, "Código estación", this.stationCodeTextBox);
            AddField(accessFields, 2, "Usuario admin", this.adminUserTextBox);
            AddField(accessFields, 3, "Nombre visible admin", this.adminDisplayNameTextBox);
            AddField(accessFields, 4, "Contraseña admin", this.adminPasswordTextBox);
            AddField(accessFields, 5, "Confirmar contraseña", this.adminPasswordConfirmTextBox);

            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1
            };
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.Controls.Add(CreateCardPanel("Datos del negocio", "Prioriza el nombre, tipo de negocio y datos generales del local.", this.businessFieldsTable), 0, 0);
            leftLayout.Controls.Add(CreateCardPanel("Acceso y estación", "Define la caja local y el acceso administrador antes de configurar lo demás.", accessFields), 0, 1);

            var leftScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20, 16, 10, 16),
                BackColor = BackColor
            };
            leftScrollPanel.Controls.Add(leftLayout);
            this.contentSplit.Panel1.Controls.Add(leftScrollPanel);

            this.categoriesGrid = CreateCategoriesGrid();
            this.categoriesGrid.DataSource = this.editableCategories;
            this.templateCombo.SelectedIndexChanged += delegate
            {
                UpdateBusinessTypeMode();
                RefreshTemplatePreview();
            };
            if (this.templateCombo.Items.Count > 0)
            {
                this.templateCombo.SelectedIndex = 0;
            }

            this.errorLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.Firebrick,
                Font = new Font("Segoe UI", 9F),
                MaximumSize = new Size(520, 0),
                Margin = new Padding(0, 12, 0, 0)
            };

            var previewHelpLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8.5F),
                MaximumSize = new Size(480, 0),
                Text = "Catálogo inicial opcional. Si no lo necesitas ahora, déjalo como base y ajústalo después desde administración.",
                Margin = new Padding(0)
            };

            var categoriesLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            categoriesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            categoriesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            categoriesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            categoriesLayout.Controls.Add(this.categoriesGrid, 0, 0);
            categoriesLayout.Controls.Add(CreateGridActionsPanel("Agregar categoría", AddCategoryRow, "Quitar categoría", RemoveSelectedCategoryRow), 0, 1);

            this.catalogSectionPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 236,
                Margin = new Padding(0)
            };
            this.catalogSectionPanel.Controls.Add(categoriesLayout);

            var saveButton = CreatePrimaryButton("Guardar y continuar");
            saveButton.Click += SaveButtonClick;
            var cancelButton = CreateSecondaryButton("Cancelar");
            cancelButton.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            headerActionsPanel.Controls.Add(saveButton);
            headerActionsPanel.Controls.Add(cancelButton);

            var reviewLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1
            };
            reviewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            reviewLayout.RowCount = 3;
            reviewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 236F));
            reviewLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            reviewLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            reviewLayout.Controls.Add(this.catalogSectionPanel, 0, 0);
            reviewLayout.Controls.Add(previewHelpLabel, 0, 1);
            reviewLayout.Controls.Add(this.errorLabel, 0, 2);

            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1
            };
            rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightLayout.Controls.Add(CreateCardPanel("Catálogo inicial opcional", "Aparece disponible, pero queda en segundo plano frente a negocio y acceso.", reviewLayout), 0, 0);

            var rightScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10, 16, 20, 16),
                BackColor = BackColor
            };
            rightScrollPanel.Controls.Add(rightLayout);
            this.contentSplit.Panel2.Controls.Add(rightScrollPanel);

            Controls.Add(this.contentSplit);
            Controls.Add(headerPanel);

            AcceptButton = saveButton;
            CancelButton = cancelButton;
            Resize += delegate { UpdateResponsiveLayout(); };
            Shown += delegate { UpdateResponsiveLayout(); };
            UpdateBusinessTypeMode();
            RefreshTemplatePreview();
            UpdateResponsiveLayout();
        }

        private void SaveButtonClick(object sender, EventArgs e)
        {
            this.categoriesGrid.EndEdit();

            if (!string.Equals(this.adminPasswordTextBox.Text, this.adminPasswordConfirmTextBox.Text, StringComparison.Ordinal))
            {
                this.errorLabel.Text = "La contraseña de administrador y su confirmación deben coincidir.";
                return;
            }

            var request = new InitialSetupRequest
            {
                BusinessName = this.businessNameTextBox.Text,
                BusinessTypeCode = this.templateCombo.SelectedItem == null ? string.Empty : ((BusinessTemplateDefinition)this.templateCombo.SelectedItem).Code,
                ManualBusinessTypeName = this.manualBusinessTypeTextBox.Text,
                Slogan = this.sloganTextBox.Text,
                Address = this.addressTextBox.Text,
                Phone = this.phoneTextBox.Text,
                PrimaryColorHex = string.Empty,
                AccentColorHex = string.Empty,
                HeaderText = string.Empty,
                FooterText = string.Empty,
                TicketWidthMm = 80,
                PrinterName = string.Empty,
                StationName = this.stationNameTextBox.Text,
                StationCode = this.stationCodeTextBox.Text,
                AdminUsername = this.adminUserTextBox.Text,
                AdminDisplayName = this.adminDisplayNameTextBox.Text,
                AdminPassword = this.adminPasswordTextBox.Text,
                Categories = this.editableCategories
                    .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Name))
                    .Select((item, index) => new SeedCategoryDefinition
                    {
                        Name = item.Name.Trim(),
                        SortOrder = item.SortOrder > 0 ? item.SortOrder : index + 1
                    })
                    .ToList(),
                Products = new List<SeedProductDefinition>()
            };

            var result = this.initialSetupService.Execute(request);
            if (!result.Success)
            {
                this.errorLabel.Text = result.Message;
                return;
            }

            MessageBox.Show(
                "La configuración inicial se guardó correctamente.",
                    "MrAlbertoCompany",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void RefreshTemplatePreview()
        {
            if (this.templateCombo == null || this.editableCategories == null)
            {
                return;
            }

            var template = this.templateCombo.SelectedItem as BusinessTemplateDefinition;
            this.editableCategories.Clear();

            if (template == null)
            {
                return;
            }

            foreach (var category in template.SuggestedCategories.OrderBy(item => item.SortOrder))
            {
                this.editableCategories.Add(CloneCategory(category));
            }
        }

        private void UpdateBusinessTypeMode()
        {
            if (this.templateCombo == null || this.businessFieldsTable == null || this.manualBusinessTypeTextBox == null)
            {
                return;
            }

            var template = this.templateCombo.SelectedItem as BusinessTemplateDefinition;
            var requiresManualType = template != null && string.Equals(template.Code, "otro", StringComparison.OrdinalIgnoreCase);
            SetTableRowVisibility(this.businessFieldsTable, 2, requiresManualType);

            if (!requiresManualType)
            {
                this.manualBusinessTypeTextBox.Text = string.Empty;
            }
        }

        private void AddCategoryRow()
        {
            var nextSortOrder = this.editableCategories.Count == 0
                ? 1
                : this.editableCategories.Max(item => item.SortOrder) + 1;

            this.editableCategories.Add(new SeedCategoryDefinition
            {
                Name = "Nueva categoría",
                SortOrder = nextSortOrder
            });
        }

        private void RemoveSelectedCategoryRow()
        {
            if (this.categoriesGrid.CurrentRow == null || this.categoriesGrid.CurrentRow.Index < 0 || this.categoriesGrid.CurrentRow.Index >= this.editableCategories.Count)
            {
                return;
            }

            var category = this.editableCategories[this.categoriesGrid.CurrentRow.Index];
            if (category == null)
            {
                return;
            }

            var categoryName = category.Name == null ? string.Empty : category.Name.Trim();
            this.editableCategories.RemoveAt(this.categoriesGrid.CurrentRow.Index);
        }

        private void UpdateResponsiveLayout()
        {
            if (this.contentSplit == null || this.contentSplit.IsDisposed || this.contentSplit.Width <= 0 || this.contentSplit.Height <= 0)
            {
                return;
            }

            var stackedLayout = this.contentSplit.Width < 1120;
            this.contentSplit.Orientation = stackedLayout ? Orientation.Horizontal : Orientation.Vertical;

            if (stackedLayout)
            {
                this.contentSplit.Panel1MinSize = 250;
                this.contentSplit.Panel2MinSize = 320;

                var maxTop = Math.Max(this.contentSplit.Panel1MinSize, this.contentSplit.Height - this.contentSplit.Panel2MinSize - this.contentSplit.SplitterWidth);
                var desiredTop = (int)(this.contentSplit.Height * 0.42F);
                this.contentSplit.SplitterDistance = Math.Max(this.contentSplit.Panel1MinSize, Math.Min(maxTop, desiredTop));

                UpdateCatalogHeight();
                return;
            }

            this.contentSplit.Panel1MinSize = 430;
            this.contentSplit.Panel2MinSize = 360;

            var maxLeft = Math.Max(this.contentSplit.Panel1MinSize, this.contentSplit.Width - this.contentSplit.Panel2MinSize - this.contentSplit.SplitterWidth);
            var desiredLeft = (int)(this.contentSplit.Width * 0.55F);
            this.contentSplit.SplitterDistance = Math.Max(this.contentSplit.Panel1MinSize, Math.Min(maxLeft, desiredLeft));

            UpdateCatalogHeight();
        }

        private void UpdateCatalogHeight()
        {
            if (this.catalogSectionPanel == null || this.catalogSectionPanel.IsDisposed)
            {
                return;
            }

            var availableHeight = this.contentSplit.Panel2.ClientSize.Height;
            if (availableHeight <= 0)
            {
                return;
            }

            var targetHeight = this.contentSplit.Orientation == Orientation.Horizontal
                ? availableHeight - 130
                : availableHeight - 150;

            this.catalogSectionPanel.Height = Math.Max(210, Math.Min(320, targetHeight));
        }

        private static SeedCategoryDefinition CloneCategory(SeedCategoryDefinition source)
        {
            return new SeedCategoryDefinition
            {
                Name = source.Name,
                SortOrder = source.SortOrder
            };
        }

        private static DataGridView CreateCategoriesGrid()
        {
            var grid = CreateBaseGrid();
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Name",
                HeaderText = "Nombre de categoría",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "SortOrder",
                HeaderText = "Orden",
                Width = 70
            });
            return grid;
        }

        private static DataGridView CreateBaseGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(224, 228, 232),
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                EditMode = DataGridViewEditMode.EditOnEnter,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 32,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                Margin = new Padding(0, 6, 0, 4),
                MinimumSize = new Size(0, 110)
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(27, 67, 50);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(227, 108, 10);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Padding = new Padding(3, 1, 3, 1);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            return grid;
        }

        private static FlowLayoutPanel CreateGridActionsPanel(string addText, Action addAction, string removeText, Action removeAction)
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 4, 0, 0),
                Padding = new Padding(0, 2, 0, 0)
            };

            var addButton = CreateSecondaryButton(addText, 150);
            addButton.Click += delegate { addAction(); };
            var removeButton = CreateSecondaryButton(removeText, 150);
            removeButton.Click += delegate { removeAction(); };

            panel.Controls.Add(addButton);
            panel.Controls.Add(removeButton);
            return panel;
        }

        private static TableLayoutPanel CreateFieldTable()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return table;
        }

        private static void SetTableRowVisibility(TableLayoutPanel table, int rowIndex, bool visible)
        {
            if (table == null || rowIndex < 0 || rowIndex >= table.RowStyles.Count)
            {
                return;
            }

            table.RowStyles[rowIndex].Height = visible ? 44F : 0F;
            table.RowStyles[rowIndex].SizeType = SizeType.Absolute;

            foreach (Control control in table.Controls)
            {
                var targetRow = table.GetRow(control);
                if (targetRow == rowIndex)
                {
                    control.Visible = visible;
                }
            }
        }

        private static Panel CreateCardPanel(string title, string subtitle, Control content)
        {
            var card = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(14, 12, 14, 14),
                Margin = new Padding(0, 0, 0, 8)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                Margin = new Padding(0)
            };

            var subtitleLabel = new Label
            {
                Text = subtitle,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 2, 0, 0)
            };

            content.Dock = DockStyle.Top;
            content.Margin = new Padding(0, 10, 0, 0);

            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(subtitleLabel, 0, 1);
            layout.Controls.Add(content, 0, 2);
            card.Controls.Add(layout);
            return card;
        }

        private static TextBox CreateTextBox(bool multiline = false)
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = multiline,
                Height = multiline ? 66 : 34,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 2, 0, 5),
                ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
            };
        }

        private static void AddField(TableLayoutPanel panel, int rowIndex, string label, Control control)
        {
            if (panel.RowCount <= rowIndex)
            {
                panel.RowCount = rowIndex + 1;
            }

            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, control.Height + 10));

            var fieldLabel = new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Margin = new Padding(0, 2, 8, 0)
            };

            panel.Controls.Add(fieldLabel, 0, rowIndex);
            panel.Controls.Add(control, 1, rowIndex);
        }

        private static Label CreateSectionLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            };
        }

        private static Button CreatePrimaryButton(string text)
        {
            return new Button
            {
                Text = text,
                Width = 176,
                Height = 40,
                BackColor = Color.FromArgb(27, 67, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(8, 0, 0, 0)
            };
        }

        private static Button CreateSecondaryButton(string text, int width = 120)
        {
            return new Button
            {
                Text = text,
                Width = width,
                Height = 36,
                BackColor = Color.Gainsboro,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(8, 0, 0, 0)
            };
        }
    }
}
