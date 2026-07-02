using System;
using System.Drawing;
using System.Windows.Forms;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Desktop
{
    public sealed class LoginForm : Form
    {
        private readonly IAuthenticationService authenticationService;
        private readonly TextBox userTextBox;
        private readonly TextBox passwordTextBox;
        private readonly Label errorLabel;

        public LoginForm(string businessName, IAuthenticationService authenticationService)
        {
            this.authenticationService = authenticationService;

            Text = "Acceso al sistema";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(560, 420);
            MinimumSize = new Size(560, 420);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.FromArgb(236, 241, 245);

            var root = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22)
            };

            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(26, 24, 26, 22)
            };
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var titleLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                Text = businessName,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var subtitleLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                Text = "Inicia sesión para operar la estación local.",
                TextAlign = ContentAlignment.MiddleLeft
            };

            this.userTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 6, 0, 0)
            };
            this.passwordTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true,
                Margin = new Padding(0, 6, 0, 0)
            };

            this.errorLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.Firebrick,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var loginButton = new Button
            {
                Text = "Entrar",
                Width = 170,
                Height = 42,
                BackColor = Color.FromArgb(27, 67, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0)
            };
            loginButton.Click += LoginButtonClick;
            loginButton.FlatAppearance.BorderSize = 0;

            var cancelButton = new Button
            {
                Text = "Salir",
                Width = 120,
                Height = 42,
                BackColor = Color.FromArgb(232, 235, 238),
                ForeColor = Color.FromArgb(33, 37, 41),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(10, 0, 0, 0)
            };
            cancelButton.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            cancelButton.FlatAppearance.BorderSize = 0;

            var actionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = new Padding(0, 8, 0, 0)
            };
            actionsPanel.Controls.Add(cancelButton);
            actionsPanel.Controls.Add(loginButton);

            card.Controls.Add(titleLabel, 0, 0);
            card.Controls.Add(subtitleLabel, 0, 1);
            card.Controls.Add(CreateFieldPanel("Usuario", this.userTextBox), 0, 2);
            card.Controls.Add(CreateFieldPanel("Contraseña", this.passwordTextBox), 0, 3);
            card.Controls.Add(this.errorLabel, 0, 4);
            card.Controls.Add(actionsPanel, 0, 5);

            root.Controls.Add(card);
            Controls.Add(root);
            AcceptButton = loginButton;
            CancelButton = cancelButton;

            Shown += delegate
            {
                var workingArea = Screen.FromControl(this).WorkingArea;
                Location = new Point(workingArea.Left + ((workingArea.Width - Width) / 2), workingArea.Top + 10);
            };
        }

        public AuthenticatedUserView AuthenticatedUser { get; private set; }

        private void LoginButtonClick(object sender, EventArgs e)
        {
            var result = this.authenticationService.Authenticate(
                new AuthenticationRequest
                {
                    Username = this.userTextBox.Text,
                    Password = this.passwordTextBox.Text
                });

            if (!result.Success)
            {
                this.errorLabel.Text = result.Message;
                return;
            }

            this.AuthenticatedUser = result.Data;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Control CreateFieldPanel(string labelText, Control control)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0),
                Height = 78
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

            panel.Controls.Add(new Label
            {
                Text = labelText,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            panel.Controls.Add(control, 0, 1);
            return panel;
        }
    }
}
