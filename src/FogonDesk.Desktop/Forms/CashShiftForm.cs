using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Desktop
{
    public sealed class CashShiftForm : Form
    {
        private readonly AppStartupState startupState;
        private readonly AuthenticatedUserView authenticatedUser;
        private readonly ICashShiftApplicationService cashShiftApplicationService;
        private readonly Label activeShiftLabel;
        private readonly Label activeTotalsLabel;
        private readonly NumericUpDown openingCashInput;
        private readonly NumericUpDown actualCashInput;
        private readonly DataGridView recentShiftsGrid;
        private readonly Button openShiftButton;
        private readonly Button closeShiftButton;
        private CashShiftSummaryView activeShift;

        public CashShiftForm(AppStartupState startupState, AuthenticatedUserView authenticatedUser, ICashShiftApplicationService cashShiftApplicationService)
        {
            this.startupState = startupState;
            this.authenticatedUser = authenticatedUser;
            this.cashShiftApplicationService = cashShiftApplicationService;

            Text = "Caja y turnos";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(980, 640);
            MinimumSize = new Size(900, 580);
            BackColor = Color.FromArgb(242, 245, 248);
            Font = new Font("Segoe UI", 10F);

            var header = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Color.White, Padding = new Padding(24, 16, 24, 16) };
            header.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Caja y turnos", Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });

            this.activeShiftLabel = new Label { Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(27, 67, 50) };
            this.activeTotalsLabel = new Label { Dock = DockStyle.Top, Height = 44, ForeColor = Color.DimGray };
            this.openingCashInput = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 1000000, ThousandsSeparator = true };
            this.actualCashInput = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 1000000, ThousandsSeparator = true };

            this.openShiftButton = CreatePrimaryButton("Abrir caja");
            this.closeShiftButton = CreateSecondaryButton("Cerrar caja");
            this.openShiftButton.Click += OpenShiftClick;
            this.closeShiftButton.Click += CloseShiftClick;

            var summaryCard = new Panel { Dock = DockStyle.Top, Height = 186, BackColor = Color.White, Padding = new Padding(18), Margin = new Padding(0, 0, 0, 12) };
            var summaryLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4 };
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            summaryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            summaryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            summaryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            summaryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            summaryLayout.Controls.Add(this.activeShiftLabel, 0, 0);
            summaryLayout.SetColumnSpan(this.activeShiftLabel, 4);
            summaryLayout.Controls.Add(this.activeTotalsLabel, 0, 1);
            summaryLayout.SetColumnSpan(this.activeTotalsLabel, 4);
            summaryLayout.Controls.Add(CreateFieldLabel("Fondo inicial"), 0, 2);
            summaryLayout.Controls.Add(this.openingCashInput, 1, 3);
            summaryLayout.Controls.Add(CreateFieldLabel("Conteo final"), 2, 2);
            summaryLayout.Controls.Add(this.actualCashInput, 3, 3);
            summaryCard.Controls.Add(summaryLayout);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 0, 0, 12) };
            actions.Controls.Add(this.openShiftButton);
            actions.Controls.Add(this.closeShiftButton);

            this.recentShiftsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 34,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(27, 67, 50),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
                }
            };
            this.recentShiftsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Folio", HeaderText = "Folio", Width = 140 });
            this.recentShiftsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OpenedByDisplayName", HeaderText = "Abierto por", Width = 180 });
            this.recentShiftsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OpenedUtc", HeaderText = "Apertura", Width = 150, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
            this.recentShiftsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SalesTotal", HeaderText = "Ventas", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            this.recentShiftsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ExpectedCash", HeaderText = "Esperado", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            this.recentShiftsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ActualCash", HeaderText = "Real", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            this.recentShiftsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DifferenceTotal", HeaderText = "Diferencia", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            this.recentShiftsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Estado", Width = 100 });

            var gridCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16) };
            gridCard.Controls.Add(this.recentShiftsGrid);
            gridCard.Controls.Add(new Label { Dock = DockStyle.Top, Height = 28, Text = "Turnos recientes", Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(33, 37, 41) });

            var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };
            root.Controls.Add(gridCard);
            root.Controls.Add(actions);
            root.Controls.Add(summaryCard);

            Controls.Add(root);
            Controls.Add(header);
            Load += delegate { RefreshState(); };
        }

        public CashShiftSummaryView ActiveShift
        {
            get { return this.activeShift; }
        }

        private void OpenShiftClick(object sender, EventArgs eventArgs)
        {
            var result = this.cashShiftApplicationService.OpenShift(new OpenCashShiftRequest
            {
                StationCode = this.startupState.StationCode,
                UserId = this.authenticatedUser.UserId,
                UserName = this.authenticatedUser.Username,
                OpeningCash = this.openingCashInput.Value
            });

            ShowResult(result, "Caja");
            if (result.Success)
            {
                RefreshState();
            }
        }

        private void CloseShiftClick(object sender, EventArgs eventArgs)
        {
            if (this.activeShift == null)
            {
                MessageBox.Show("No hay un turno abierto para cerrar.", "Caja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = this.cashShiftApplicationService.CloseShift(new CloseCashShiftRequest
            {
                ShiftId = this.activeShift.ShiftId,
                UserId = this.authenticatedUser.UserId,
                UserName = this.authenticatedUser.Username,
                ActualCash = this.actualCashInput.Value
            });

            ShowResult(result, "Caja");
            if (result.Success)
            {
                RefreshState();
            }
        }

        private void RefreshState()
        {
            var activeResult = this.cashShiftApplicationService.GetActiveShift(this.startupState.StationCode);
            this.activeShift = activeResult.Success ? activeResult.Data : null;

            if (this.activeShift == null)
            {
                this.activeShiftLabel.Text = "No hay caja abierta en esta estación.";
                this.activeTotalsLabel.Text = "Abre un turno para vincular las ventas del cajero a un corte activo.";
                this.openShiftButton.Enabled = true;
                this.closeShiftButton.Enabled = false;
                this.actualCashInput.Enabled = false;
            }
            else
            {
                this.activeShiftLabel.Text = "Turno activo: " + this.activeShift.Folio + " | Estación: " + this.activeShift.StationCode;
                this.activeTotalsLabel.Text = "Apertura: " + this.activeShift.OpenedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                    + " | Ventas: $" + this.activeShift.SalesTotal.ToString("N2")
                    + " | Efectivo esperado: $" + this.activeShift.ExpectedCash.ToString("N2");
                this.openShiftButton.Enabled = false;
                this.closeShiftButton.Enabled = true;
                this.actualCashInput.Enabled = true;
                this.openingCashInput.Value = this.activeShift.OpeningCash;
                this.actualCashInput.Value = this.activeShift.ActualCash ?? this.activeShift.ExpectedCash;
            }

            this.recentShiftsGrid.DataSource = this.cashShiftApplicationService.GetRecentShifts(this.startupState.StationCode, 10).ToList();
        }

        private static void ShowResult(OperationResult<CashShiftSummaryView> result, string title)
        {
            MessageBox.Show(result.Message, title, MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold) };
        }

        private static Button CreatePrimaryButton(string text)
        {
            var button = new Button { Text = text, Width = 150, Height = 40, BackColor = Color.FromArgb(27, 67, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 10, 0) };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private static Button CreateSecondaryButton(string text)
        {
            var button = new Button { Text = text, Width = 150, Height = 40, BackColor = Color.FromArgb(230, 233, 236), ForeColor = Color.FromArgb(33, 37, 41), FlatStyle = FlatStyle.Flat, Margin = new Padding(0) };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }
    }
}