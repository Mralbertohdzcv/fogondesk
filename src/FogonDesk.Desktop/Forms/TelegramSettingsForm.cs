using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Desktop
{
    public sealed class TelegramSettingsForm : Form
    {
        private readonly ITelegramIntegrationService telegramIntegrationService;
        private readonly TextBox botTokenTextBox;
        private readonly Label statusLabel;
        private readonly Label generatedCodeLabel;
        private readonly DataGridView chatsGrid;
        private readonly Timer autoSyncTimer;

        public TelegramSettingsForm(ITelegramIntegrationService telegramIntegrationService)
        {
            this.telegramIntegrationService = telegramIntegrationService;

            Text = "Configuracion Telegram";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(860, 560);
            MinimumSize = new Size(760, 500);
            BackColor = Color.FromArgb(242, 245, 248);
            Font = new Font("Segoe UI", 10F);

            this.botTokenTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };

            this.statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            this.generatedCodeLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(27, 67, 50),
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Codigo pendiente: ninguno"
            };

            this.chatsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.chatsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Chat ID", DataPropertyName = "ChatId", Width = 180 });
            this.chatsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Usuario", DataPropertyName = "Username", Width = 180 });
            this.chatsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nombre", DataPropertyName = "DisplayName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            this.chatsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Vinculado", DataPropertyName = "LinkedUtc", Width = 150, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });

            this.autoSyncTimer = new Timer { Interval = 8000 };
            this.autoSyncTimer.Tick += AutoSyncTick;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(16)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

            root.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Telegram para cancelaciones y cierres",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 67, 50),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            var tokenRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            tokenRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            tokenRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tokenRow.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Bot token:", TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            tokenRow.Controls.Add(this.botTokenTextBox, 1, 0);
            root.Controls.Add(tokenRow, 0, 1);

            var actionsRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            actionsRow.Controls.Add(CreateActionButton("Guardar token", Color.FromArgb(27, 67, 50), SaveToken));
            actionsRow.Controls.Add(CreateActionButton("Generar codigo", Color.FromArgb(33, 158, 188), GenerateCode));
            actionsRow.Controls.Add(CreateActionButton("Sincronizar vinculos", Color.FromArgb(52, 73, 94), SyncLinks));
            root.Controls.Add(actionsRow, 0, 2);

            this.generatedCodeLabel.Text = "Codigo: pendiente";
            root.Controls.Add(this.generatedCodeLabel, 0, 3);

            var gridCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            gridCard.Controls.Add(this.chatsGrid);
            gridCard.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Text = "Chats vinculados",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41)
            });
            root.Controls.Add(gridCard, 0, 4);

            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            footer.Controls.Add(this.statusLabel, 0, 0);
            var closeButton = new Button { Text = "Cerrar", Dock = DockStyle.Fill, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(90, 90, 90), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            closeButton.FlatAppearance.BorderSize = 0;
            footer.Controls.Add(closeButton, 1, 0);
            root.Controls.Add(footer, 0, 5);

            Controls.Add(root);
            AcceptButton = closeButton;

            Load += delegate
            {
                ReloadSettings();
                this.autoSyncTimer.Start();
            };
            FormClosed += delegate { this.autoSyncTimer.Stop(); };
        }

        private void ReloadSettings()
        {
            var settings = this.telegramIntegrationService.GetSettings();
            this.botTokenTextBox.Text = settings.BotToken ?? string.Empty;
            this.chatsGrid.DataSource = (settings.LinkedChats ?? new TelegramLinkedChatView[0]).ToList();
            this.statusLabel.Text = "Ultimo update: " + settings.LastUpdateId.ToString();
        }

        private void SaveToken(object sender, EventArgs eventArgs)
        {
            var result = this.telegramIntegrationService.SaveBotToken(this.botTokenTextBox.Text);
            MessageBox.Show(result.Message, "Telegram", MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (result.Success)
            {
                ReloadSettings();
            }
        }

        private void GenerateCode(object sender, EventArgs eventArgs)
        {
            var result = this.telegramIntegrationService.GenerateLinkCode(10);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(result.Message, "Telegram", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.generatedCodeLabel.Text = "Codigo: " + result.Data.Code + " (vence " + result.Data.ExpiresUtc.ToLocalTime().ToString("HH:mm") + ")";
            this.statusLabel.Text = "Comparte el codigo: en Telegram busca el bot y envia /start " + result.Data.Code;
        }

        private void SyncLinks(object sender, EventArgs eventArgs)
        {
            var result = this.telegramIntegrationService.SyncLinkRequests();
            MessageBox.Show(result.Message, "Telegram", MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (result.Success)
            {
                ReloadSettings();
            }
        }

        private void AutoSyncTick(object sender, EventArgs eventArgs)
        {
            var svc = this.telegramIntegrationService;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var result = svc.SyncLinkRequests();
                    if (result.Success && result.Data > 0)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            if (!this.IsDisposed)
                            {
                                ReloadSettings();
                                this.statusLabel.Text = "Se vincularon " + result.Data.ToString() + " chat(s) automaticamente.";
                            }
                        }));
                    }
                }
                catch
                {
                }
            });
        }

        private static Button CreateActionButton(string text, Color backColor, EventHandler handler)
        {
            var button = new Button
            {
                Text = text,
                Width = 140,
                Height = 34,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 8, 0)
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += handler;
            return button;
        }
    }
}
