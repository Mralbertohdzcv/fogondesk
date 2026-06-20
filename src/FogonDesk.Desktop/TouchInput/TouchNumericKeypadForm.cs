using System;
using System.Drawing;
using System.Windows.Forms;

namespace FogonDesk.Desktop.TouchInput
{
    internal sealed class TouchNumericKeypadForm : Form
    {
        private static TouchNumericKeypadForm activeKeypad;
        private NumericUpDown boundInput;
        private long centValue;

        private TouchNumericKeypadForm()
        {
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(236, 241, 245);
            Padding = new Padding(10);
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            KeyPreview = true;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 4,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            for (var column = 0; column < 4; column++)
            {
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }

            for (var row = 0; row < 4; row++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            }

            AddKeyButton(layout, "7", 0, 0, digit => AppendDigit(7));
            AddKeyButton(layout, "8", 1, 0, digit => AppendDigit(8));
            AddKeyButton(layout, "9", 2, 0, digit => AppendDigit(9));
            AddKeyButton(layout, "⌫", 3, 0, _ => BackspaceDigit(), Color.FromArgb(69, 123, 157));

            AddKeyButton(layout, "4", 0, 1, digit => AppendDigit(4));
            AddKeyButton(layout, "5", 1, 1, digit => AppendDigit(5));
            AddKeyButton(layout, "6", 2, 1, digit => AppendDigit(6));
            AddKeyButton(layout, "C", 3, 1, _ => ClearValue(), Color.FromArgb(185, 28, 28));

            AddKeyButton(layout, "1", 0, 2, digit => AppendDigit(1));
            AddKeyButton(layout, "2", 1, 2, digit => AppendDigit(2));
            AddKeyButton(layout, "3", 2, 2, digit => AppendDigit(3));
            AddKeyButton(layout, "OK", 3, 2, _ => HideForCurrentInput(), Color.FromArgb(27, 67, 50), 2);

            AddKeyButton(layout, "0", 0, 3, _ => AppendDigit(0), columnSpan: 3);

            Controls.Add(layout);
            Deactivate += TouchNumericKeypadFormDeactivate;
        }

        public static void ShowFor(NumericUpDown input, Form owner)
        {
            if (input == null)
            {
                return;
            }

            if (activeKeypad == null || activeKeypad.IsDisposed)
            {
                activeKeypad = new TouchNumericKeypadForm();
            }

            activeKeypad.Bind(input, owner);
            activeKeypad.Show();
            activeKeypad.BringToFront();
        }

        public static void HideFor(NumericUpDown input)
        {
            if (activeKeypad == null || activeKeypad.IsDisposed)
            {
                return;
            }

            if (activeKeypad.boundInput == input)
            {
                activeKeypad.HideForCurrentInput();
            }
        }

        public static void HideAll()
        {
            if (activeKeypad == null || activeKeypad.IsDisposed)
            {
                return;
            }

            activeKeypad.HideForCurrentInput();
        }

        private void Bind(NumericUpDown input, Form owner)
        {
            boundInput = input;
            centValue = ToCentValue(input.Value, input.DecimalPlaces);
            ClientSize = new Size(360, 300);
            PositionNearOwner(owner ?? input.FindForm());
            UpdateInputFromCentValue();
        }

        private void PositionNearOwner(Form owner)
        {
            if (owner == null)
            {
                StartPosition = FormStartPosition.CenterScreen;
                return;
            }

            var workingArea = Screen.FromControl(owner).WorkingArea;
            var top = owner.Top + ((owner.Height - Height) / 2);

            if (top < workingArea.Top)
            {
                top = workingArea.Top + 12;
            }

            if (top + Height > workingArea.Bottom)
            {
                top = workingArea.Bottom - Height - 12;
            }

            var left = owner.Right + 12;

            if (left + Width > workingArea.Right)
            {
                left = owner.Left - Width - 12;
            }

            if (left < workingArea.Left)
            {
                left = workingArea.Right - Width - 12;
            }

            Location = new Point(left, top);
        }

        private void HideForCurrentInput()
        {
            boundInput = null;
            Hide();
        }

        private void TouchNumericKeypadFormDeactivate(object sender, EventArgs e)
        {
            if (boundInput != null && boundInput.Focused)
            {
                return;
            }

            HideForCurrentInput();
        }

        private void AppendDigit(int digit)
        {
            if (boundInput == null)
            {
                return;
            }

            var maxCents = ToCentValue(boundInput.Maximum, boundInput.DecimalPlaces);
            var nextValue = (centValue * 10L) + digit;
            centValue = nextValue > maxCents ? maxCents : nextValue;
            UpdateInputFromCentValue();
        }

        private void BackspaceDigit()
        {
            centValue /= 10L;
            UpdateInputFromCentValue();
        }

        private void ClearValue()
        {
            centValue = 0L;
            UpdateInputFromCentValue();
        }

        private void UpdateInputFromCentValue()
        {
            if (boundInput == null)
            {
                return;
            }

            var factor = Pow10(boundInput.DecimalPlaces);
            var nextValue = centValue / (decimal)factor;
            if (nextValue < boundInput.Minimum)
            {
                nextValue = boundInput.Minimum;
                centValue = ToCentValue(nextValue, boundInput.DecimalPlaces);
            }

            if (nextValue > boundInput.Maximum)
            {
                nextValue = boundInput.Maximum;
                centValue = ToCentValue(nextValue, boundInput.DecimalPlaces);
            }

            boundInput.Value = nextValue;
            PlaceCaretAtEnd(boundInput);
        }

        private static long ToCentValue(decimal value, int decimalPlaces)
        {
            var factor = Pow10(decimalPlaces);
            return (long)Math.Round(value * factor, MidpointRounding.AwayFromZero);
        }

        private static int Pow10(int decimalPlaces)
        {
            var factor = 1;
            for (var index = 0; index < decimalPlaces; index++)
            {
                factor *= 10;
            }

            return factor;
        }

        private static void PlaceCaretAtEnd(NumericUpDown input)
        {
            var textBox = GetInnerTextBox(input);
            if (textBox == null)
            {
                return;
            }

            textBox.SelectionStart = textBox.TextLength;
            textBox.SelectionLength = 0;
        }

        private static TextBox GetInnerTextBox(NumericUpDown input)
        {
            foreach (Control control in input.Controls)
            {
                if (control is TextBox textBox)
                {
                    return textBox;
                }
            }

            return null;
        }

        private void AddKeyButton(
            TableLayoutPanel layout,
            string text,
            int column,
            int row,
            Action<Button> onClick,
            Color? backColor = null,
            int rowSpan = 1,
            int columnSpan = 1)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor ?? Color.White,
                ForeColor = backColor.HasValue ? Color.White : Color.FromArgb(33, 37, 41),
                Font = Font
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(208, 215, 222);
            button.Click += delegate { onClick(button); };

            layout.Controls.Add(button, column, row);
            if (columnSpan > 1 || rowSpan > 1)
            {
                layout.SetColumnSpan(button, columnSpan);
                layout.SetRowSpan(button, rowSpan);
            }
        }
    }
}
