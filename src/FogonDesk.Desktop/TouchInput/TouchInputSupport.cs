using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FogonDesk.Desktop.TouchInput
{
    internal static class TouchInputSupport
    {
        private static readonly HashSet<Form> RegisteredForms = new HashSet<Form>();
        private static readonly HashSet<Control> HookedControls = new HashSet<Control>();
        private static bool initialized;

        public static void InitializeApplication()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            System.Windows.Forms.Application.Idle += ApplicationIdle;
            System.Windows.Forms.Application.ApplicationExit += delegate { TouchNumericKeypadForm.HideAll(); };
        }

        public static void EnableFor(Control root)
        {
            if (root == null)
            {
                return;
            }

            HookControlTree(root);
            root.ControlAdded += RootControlAdded;
        }

        private static void ApplicationIdle(object sender, EventArgs e)
        {
            foreach (Form form in System.Windows.Forms.Application.OpenForms)
            {
                RegisterFormIfNeeded(form);
            }
        }

        private static void RegisterFormIfNeeded(Form form)
        {
            if (form == null || !RegisteredForms.Add(form))
            {
                return;
            }

            form.FormClosed += delegate
            {
                RegisteredForms.Remove(form);
                TouchNumericKeypadForm.HideAll();
            };

            EnableFor(form);
        }

        private static void RootControlAdded(object sender, ControlEventArgs e)
        {
            HookControlTree(e.Control);
        }

        private static void HookControlTree(Control control)
        {
            if (control == null)
            {
                return;
            }

            HookControlIfNeeded(control);
            foreach (Control child in control.Controls)
            {
                HookControlTree(child);
            }
        }

        private static void HookControlIfNeeded(Control control)
        {
            if (control == null || !HookedControls.Add(control))
            {
                return;
            }

            if (control is NumericUpDown numericInput)
            {
                HookNumericInput(numericInput);
                return;
            }

            if (control is TextBox textBox && !textBox.ReadOnly && textBox.Enabled)
            {
                HookTextInput(textBox);
            }
        }

        private static void HookNumericInput(NumericUpDown input)
        {
            var innerTextBox = GetInnerTextBox(input);
            if (innerTextBox != null)
            {
                innerTextBox.ReadOnly = true;
                innerTextBox.TabStop = true;
                innerTextBox.GotFocus += NumericInnerTextBoxGotFocus;
                innerTextBox.MouseUp += NumericInnerTextBoxMouseUp;
            }

            input.Enter += NumericInputEnter;
            input.Click += NumericInputClick;
            input.Leave += NumericInputLeave;
        }

        private static void HookTextInput(TextBox textBox)
        {
            textBox.Enter += TextInputEnter;
            textBox.Click += TextInputClick;
        }

        private static void NumericInputEnter(object sender, EventArgs e)
        {
            var input = (NumericUpDown)sender;
            BeginPreserveCaret(input);
            TouchNumericKeypadForm.ShowFor(input, input.FindForm());
        }

        private static void NumericInputClick(object sender, EventArgs e)
        {
            var input = (NumericUpDown)sender;
            BeginPreserveCaret(input);
            TouchNumericKeypadForm.ShowFor(input, input.FindForm());
        }

        private static void NumericInputLeave(object sender, EventArgs e)
        {
            TouchNumericKeypadForm.HideFor((NumericUpDown)sender);
        }

        private static void NumericInnerTextBoxGotFocus(object sender, EventArgs e)
        {
            var textBox = (TextBox)sender;
            var input = textBox.Parent as NumericUpDown;
            if (input == null)
            {
                return;
            }

            BeginPreserveCaret(input);
            TouchNumericKeypadForm.ShowFor(input, input.FindForm());
        }

        private static void NumericInnerTextBoxMouseUp(object sender, MouseEventArgs e)
        {
            var textBox = (TextBox)sender;
            var input = textBox.Parent as NumericUpDown;
            if (input == null)
            {
                return;
            }

            BeginPreserveCaret(input);
        }

        private static void TextInputEnter(object sender, EventArgs e)
        {
            var textBox = (TextBox)sender;
            PreserveTextSelection(textBox);
            TouchKeyboardLauncher.Show(textBox, textBox.FindForm());
        }

        private static void TextInputClick(object sender, EventArgs e)
        {
            var textBox = (TextBox)sender;
            PreserveTextSelection(textBox);
            TouchKeyboardLauncher.Show(textBox, textBox.FindForm());
        }

        private static void BeginPreserveCaret(NumericUpDown input)
        {
            if (input == null)
            {
                return;
            }

            input.BeginInvoke(new Action(() => PreserveNumericSelection(input)));
        }

        private static void PreserveNumericSelection(NumericUpDown input)
        {
            var textBox = GetInnerTextBox(input);
            if (textBox == null)
            {
                return;
            }

            textBox.SelectionStart = textBox.TextLength;
            textBox.SelectionLength = 0;
        }

        private static void PreserveTextSelection(TextBox textBox)
        {
            if (textBox == null)
            {
                return;
            }

            textBox.BeginInvoke(new Action(() =>
            {
                textBox.SelectionStart = textBox.TextLength;
                textBox.SelectionLength = 0;
            }));
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
    }
}
