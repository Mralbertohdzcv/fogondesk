using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Desktop
{
    public sealed class OperationSettingsForm : Form
    {
        private readonly AppStartupState startupState;
        private readonly IOperationSettingsApplicationService operationSettingsApplicationService;
        private readonly NumericUpDown diningTableCountInput;
        private readonly DataGridView platformsGrid;
        private readonly BindingList<PlatformRow> platformRows = new BindingList<PlatformRow>();

        public OperationSettingsForm(AppStartupState startupState, IOperationSettingsApplicationService operationSettingsApplicationService)
        {
            this.startupState = startupState;
            this.operationSettingsApplicationService = operationSettingsApplicationService;

            Text = "Plataformas y mesas";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(840, 620);
            MinimumSize = new Size(760, 560);
            BackColor = Color.FromArgb(242, 245, 248);
            Font = new Font("Segoe UI", 10F);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(16)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            root.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Configuración de operación",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 67, 50),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            var tableCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            tableCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            tableCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            tableCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            this.diningTableCountInput = new NumericUpDown
            {
                Dock = DockStyle.Left,
                Width = 120,
                DecimalPlaces = 0,
                Minimum = 1,
                Maximum = 60,
                Value = 5
            };

            tableCard.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Mesas activas para comedor:",
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            tableCard.Controls.Add(this.diningTableCountInput, 0, 1);
            tableCard.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Se mostrarán desde Mesa 01 hasta la cantidad indicada.",
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, 1);
            root.Controls.Add(tableCard, 0, 1);

            var platformsCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            platformsCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            platformsCard.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            var addButton = CreateButton("Agregar plataforma", Color.FromArgb(27, 67, 50));
            addButton.Click += AddPlatform;
            var removeButton = CreateButton("Eliminar seleccionada", Color.FromArgb(166, 58, 40));
            removeButton.Click += RemoveSelectedPlatform;
            toolbar.Controls.Add(addButton);
            toolbar.Controls.Add(removeButton);

            this.platformsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                DataSource = this.platformRows,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            this.platformsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Plataforma", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            this.platformsGrid.Columns.Add(new DataGridViewComboBoxColumn
            {
                DataPropertyName = "PricingMode",
                HeaderText = "Tipo de precio",
                Width = 180,
                DataSource = new[] { "rappi", "didi", "manual" }
            });
            this.platformsGrid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "Activa", Width = 70 });

            platformsCard.Controls.Add(toolbar, 0, 0);
            platformsCard.Controls.Add(this.platformsGrid, 0, 1);
            root.Controls.Add(platformsCard, 0, 2);

            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            var saveButton = CreateButton("Guardar", Color.FromArgb(27, 67, 50));
            saveButton.Click += SaveSettings;
            var closeButton = CreateButton("Cerrar", Color.FromArgb(90, 90, 90));
            closeButton.Click += delegate { Close(); };
            footer.Controls.Add(saveButton);
            footer.Controls.Add(closeButton);
            root.Controls.Add(footer, 0, 3);

            Controls.Add(root);
            Load += OnLoad;
        }

        private void OnLoad(object sender, EventArgs eventArgs)
        {
            var settings = this.operationSettingsApplicationService.GetSettings();
            this.platformRows.Clear();

            if (settings != null)
            {
                var tableCount = settings.DiningTableCount <= 0 ? 5 : settings.DiningTableCount;
                this.diningTableCountInput.Value = tableCount;
                foreach (var item in settings.DigitalPlatforms ?? Array.Empty<DigitalPlatformConfigurationView>())
                {
                    this.platformRows.Add(new PlatformRow
                    {
                        PlatformId = item.PlatformId,
                        Name = item.Name,
                        PricingMode = string.IsNullOrWhiteSpace(item.PricingMode) ? "manual" : item.PricingMode,
                        IsActive = item.IsActive
                    });
                }
            }
            else
            {
                this.diningTableCountInput.Value = 5;
            }

            EnsureDefaultPlatforms();
        }

        private void EnsureDefaultPlatforms()
        {
            if (!this.platformRows.Any(item => string.Equals(item.Name, "Rappi", StringComparison.OrdinalIgnoreCase)))
            {
                this.platformRows.Add(new PlatformRow { Name = "Rappi", PricingMode = "rappi", IsActive = true });
            }

            if (!this.platformRows.Any(item => string.Equals(item.Name, "Didi", StringComparison.OrdinalIgnoreCase)))
            {
                this.platformRows.Add(new PlatformRow { Name = "Didi", PricingMode = "didi", IsActive = true });
            }
        }

        private void AddPlatform(object sender, EventArgs eventArgs)
        {
            this.platformRows.Add(new PlatformRow
            {
                Name = "Nueva plataforma",
                PricingMode = "manual",
                IsActive = true
            });
        }

        private void RemoveSelectedPlatform(object sender, EventArgs eventArgs)
        {
            if (this.platformsGrid.CurrentRow == null || !(this.platformsGrid.CurrentRow.DataBoundItem is PlatformRow))
            {
                return;
            }

            this.platformRows.Remove((PlatformRow)this.platformsGrid.CurrentRow.DataBoundItem);
        }

        private void SaveSettings(object sender, EventArgs eventArgs)
        {
            var request = new SaveOperationSettingsRequest
            {
                DiningTableCount = Convert.ToInt32(this.diningTableCountInput.Value),
                DigitalPlatforms = this.platformRows.Select(item => new DigitalPlatformConfigurationEditView
                {
                    PlatformId = item.PlatformId <= 0 ? (int?)null : item.PlatformId,
                    Name = item.Name,
                    PricingMode = item.PricingMode,
                    IsActive = item.IsActive
                }).ToList()
            };

            var result = this.operationSettingsApplicationService.SaveSettings(request);
            MessageBox.Show(result.Message, "Plataformas y mesas", MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (!result.Success)
            {
                return;
            }

            this.startupState.DiningTableCount = request.DiningTableCount;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Button CreateButton(string text, Color color)
        {
            var button = new Button
            {
                Text = text,
                Width = 160,
                Height = 34,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(6, 0, 6, 0)
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private sealed class PlatformRow
        {
            public int PlatformId { get; set; }
            public string Name { get; set; }
            public string PricingMode { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
