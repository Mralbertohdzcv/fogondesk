using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;
using FogonDesk.Configuration;
using FogonDesk.Domain.Common;

namespace FogonDesk.Desktop
{
    public sealed class StatisticsReportsForm : Form
    {
        private readonly AppStartupState startupState;
        private readonly ICashShiftApplicationService cashShiftApplicationService;
        private readonly ISalesApplicationService salesApplicationService;
        private readonly AuthenticatedUserView authenticatedUser;
        private readonly ComboBox rangeComboBox;
        private readonly Label totalsLabel;
        private readonly Label statsLabel;
        private readonly ListBox ticketsList;
        private readonly ListBox detailsList;
        private readonly List<SaleTicketView> ticketsCache = new List<SaleTicketView>();

        public StatisticsReportsForm(
            AppStartupState startupState,
            ICashShiftApplicationService cashShiftApplicationService,
            ISalesApplicationService salesApplicationService,
            AuthenticatedUserView authenticatedUser)
        {
            this.startupState = startupState;
            this.cashShiftApplicationService = cashShiftApplicationService;
            this.salesApplicationService = salesApplicationService;
            this.authenticatedUser = authenticatedUser;

            Text = "Estadísticas y reportes";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1080, 700);
            MinimumSize = new Size(980, 620);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10F);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(14)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            root.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Ventas y desempeño operativo",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 67, 50),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            filters.Controls.Add(new Label { Text = "Rango:", AutoSize = true, Margin = new Padding(0, 8, 8, 0) });
            this.rangeComboBox = new ComboBox { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            this.rangeComboBox.Items.AddRange(new[] { "Hoy", "Últimos 7 días", "Todo el historial" });
            this.rangeComboBox.SelectedIndex = 0;
            this.rangeComboBox.SelectedIndexChanged += delegate { ReloadData(); };
            filters.Controls.Add(this.rangeComboBox);

            var refreshButton = new Button
            {
                Text = "Actualizar",
                Width = 110,
                Height = 32,
                BackColor = Color.FromArgb(27, 67, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(10, 0, 0, 0)
            };
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.Click += delegate { ReloadData(); };
            filters.Controls.Add(refreshButton);
            root.Controls.Add(filters, 0, 1);

            this.totalsLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold) };
            this.statsLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.DimGray };

            var summaryPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            summaryPanel.Controls.Add(this.totalsLabel, 0, 0);
            summaryPanel.Controls.Add(this.statsLabel, 0, 1);
            root.Controls.Add(summaryPanel, 0, 2);

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            this.ticketsList = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5F), HorizontalScrollbar = true };
            this.ticketsList.SelectedIndexChanged += TicketSelected;

            this.detailsList = new ListBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 9.5F), HorizontalScrollbar = true };

            var leftCard = CreateCard("Tickets", this.ticketsList);
            var rightCard = CreateCard("Detalle", this.detailsList);
            body.Controls.Add(leftCard, 0, 0);
            body.Controls.Add(rightCard, 1, 0);
            root.Controls.Add(body, 0, 3);

            var closeBar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            var closeButton = new Button
            {
                Text = "Cerrar",
                Width = 120,
                Height = 32,
                BackColor = Color.FromArgb(90, 90, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += delegate { Close(); };
            closeBar.Controls.Add(closeButton);
            root.Controls.Add(closeBar, 0, 4);

            Controls.Add(root);
            Load += delegate { ReloadData(); };
        }

        private void ReloadData()
        {
            this.ticketsCache.Clear();
            this.ticketsCache.AddRange(LoadTicketsFromDatabase());

            this.ticketsList.Items.Clear();
            foreach (var ticket in this.ticketsCache)
            {
                this.ticketsList.Items.Add(ticket);
            }

            var confirmed = this.ticketsCache.Where(item => item.Status == SaleStatus.Confirmada).ToList();
            var cancelled = this.ticketsCache.Where(item => item.Status == SaleStatus.Cancelada).ToList();
            var confirmedTotal = confirmed.Sum(item => item.Total);
            var cancelledTotal = cancelled.Sum(item => item.Total);
            var netTotal = confirmedTotal - cancelledTotal;
            var estimatedProfit = confirmed.Sum(item => item.EstimatedProfitTotal) - cancelled.Sum(item => item.EstimatedProfitTotal);

            this.totalsLabel.Text = "Confirmadas: $" + confirmedTotal.ToString("N2")
                + " | Canceladas: $" + cancelledTotal.ToString("N2")
                + " | Neto: $" + netTotal.ToString("N2")
                + " | Ganancia estimada: $" + estimatedProfit.ToString("N2");

            this.statsLabel.Text = "Tickets: " + this.ticketsCache.Count
                + " | Confirmadas: " + confirmed.Count
                + " | Canceladas: " + cancelled.Count
                + " | Usuario: " + this.authenticatedUser.DisplayName
                + " | Estación: " + (string.IsNullOrWhiteSpace(this.startupState.StationName) ? this.startupState.StationCode : this.startupState.StationName);

            this.detailsList.Items.Clear();
            this.detailsList.Items.Add("Selecciona un ticket para ver su detalle.");

            if (this.ticketsList.Items.Count > 0)
            {
                this.ticketsList.SelectedIndex = 0;
            }
        }

        private void TicketSelected(object sender, EventArgs eventArgs)
        {
            this.detailsList.Items.Clear();
            if (!(this.ticketsList.SelectedItem is SaleTicketView))
            {
                this.detailsList.Items.Add("Selecciona un ticket para ver su detalle.");
                return;
            }

            var ticket = (SaleTicketView)this.ticketsList.SelectedItem;
            this.detailsList.Items.Add("Folio: " + ticket.Folio);
            this.detailsList.Items.Add("Estado: " + ticket.Status);
            this.detailsList.Items.Add("Fecha: " + ticket.SoldLocal.ToString("dd/MM/yyyy HH:mm"));
            this.detailsList.Items.Add("Tipo: " + ticket.OrderKind);
            this.detailsList.Items.Add("Cajero: " + ticket.CashierName);
            this.detailsList.Items.Add("Nota: " + (string.IsNullOrWhiteSpace(ticket.Note) ? "(sin nota)" : ticket.Note));
            this.detailsList.Items.Add("--------------------------------------------");
            foreach (var item in ticket.Items)
            {
                this.detailsList.Items.Add(item.Quantity.ToString("0.##") + " x " + item.ProductName + "  $" + item.UnitPrice.ToString("N2") + "  = $" + item.LineTotal.ToString("N2"));
            }
            this.detailsList.Items.Add("--------------------------------------------");
            this.detailsList.Items.Add("Total ticket: $" + ticket.Total.ToString("N2"));
            this.detailsList.Items.Add("Ganancia estimada: $" + ticket.EstimatedProfitTotal.ToString("N2"));
        }

        private IList<SaleTicketView> LoadTicketsFromDatabase()
        {
            var result = new List<SaleTicketView>();
            var paths = StationPathsFactory.CreateDefault();
            StationPathsFactory.EnsureCreated(paths);
            if (!File.Exists(paths.DatabaseFilePath))
            {
                return result;
            }

            using (var connection = new SQLiteConnection("Data Source=" + paths.DatabaseFilePath + ";Version=3;Foreign Keys=True;"))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    var filter = BuildDateFilter();
                    command.CommandText = "SELECT s.id, s.folio, s.order_kind, s.status, s.sold_utc, s.total, s.note, s.estimated_profit_total, COALESCE(u.display_name, '')\n"
                        + "FROM sales s\n"
                        + "LEFT JOIN users u ON u.id = s.sold_by_user_id\n"
                        + filter + "\n"
                        + "ORDER BY s.sold_utc DESC\n"
                        + "LIMIT 800;";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new SaleTicketView
                            {
                                SaleId = Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                                Folio = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                OrderKind = reader.IsDBNull(2) ? OrderKind.Mostrador : (OrderKind)Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture),
                                Status = reader.IsDBNull(3) ? SaleStatus.Confirmada : (SaleStatus)Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
                                SoldLocal = reader.IsDBNull(4)
                                    ? DateTime.Now
                                    : DateTime.SpecifyKind(DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), DateTimeKind.Utc).ToLocalTime(),
                                Total = reader.IsDBNull(5) ? 0m : Convert.ToDecimal(reader.GetValue(5), CultureInfo.InvariantCulture),
                                Note = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                EstimatedProfitTotal = reader.IsDBNull(7) ? 0m : Convert.ToDecimal(reader.GetValue(7), CultureInfo.InvariantCulture),
                                CashierName = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                            };
                            result.Add(item);
                        }
                    }
                }

                foreach (var ticket in result)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"SELECT product_name_snapshot, quantity, unit_price, line_total
FROM sale_items
WHERE sale_id = @sale_id
ORDER BY id ASC;";
                        command.Parameters.AddWithValue("@sale_id", ticket.SaleId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ticket.Items.Add(new SaleTicketLine
                                {
                                    ProductName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                                    Quantity = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture),
                                    UnitPrice = reader.IsDBNull(2) ? 0m : Convert.ToDecimal(reader.GetValue(2), CultureInfo.InvariantCulture),
                                    LineTotal = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3), CultureInfo.InvariantCulture)
                                });
                            }
                        }
                    }
                }
            }

            return result;
        }

        private string BuildDateFilter()
        {
            if (this.rangeComboBox.SelectedIndex <= 0)
            {
                return "WHERE DATE(s.sold_utc) = DATE('now', 'localtime')";
            }

            if (this.rangeComboBox.SelectedIndex == 1)
            {
                return "WHERE DATETIME(s.sold_utc) >= DATETIME('now', '-7 days')";
            }

            return string.Empty;
        }

        private static Control CreateCard(string title, Control content)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.FromArgb(247, 249, 251),
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 10, 0)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = title,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            panel.Controls.Add(content, 0, 1);
            return panel;
        }

        private sealed class SaleTicketView
        {
            public int SaleId { get; set; }
            public string Folio { get; set; }
            public OrderKind OrderKind { get; set; }
            public SaleStatus Status { get; set; }
            public DateTime SoldLocal { get; set; }
            public decimal Total { get; set; }
            public decimal EstimatedProfitTotal { get; set; }
            public string CashierName { get; set; }
            public string Note { get; set; }
            public IList<SaleTicketLine> Items { get; private set; } = new List<SaleTicketLine>();

            public override string ToString()
            {
                return this.SoldLocal.ToString("dd/MM HH:mm") + " | " + this.Folio + " | " + this.OrderKind + " | $" + this.Total.ToString("N2") + " | " + this.Status;
            }
        }

        private sealed class SaleTicketLine
        {
            public string ProductName { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal LineTotal { get; set; }
        }
    }
}
