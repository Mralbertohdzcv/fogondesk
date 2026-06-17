using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace FogonDesk.Printing
{
    public static class TicketFormatter
    {
        public static List<string> FormatReceipt(
            string businessName,
            string slogan,
            string folio,
            string dateStr,
            string cashierName,
            string orderKind,
            string note,
            List<(string Qty, string Name, string Total)> items,
            string totalAmount,
            int ticketWidthMm = 80,
            string layoutName = "Clásico compacto",
            string headerText = "",
            string footerText = "",
            bool showSystemFooter = true,
            string systemFooterText = "",
            int horizontalOffset = 0,
            int verticalOffset = 0,
            int charactersPerLine = 0)
        {
            var lines = new List<string>();
            int width = charactersPerLine > 0 ? Math.Max(24, Math.Min(48, charactersPerLine)) : ticketWidthMm == 58 ? 30 : 42;
            bool detailedLayout = string.Equals(layoutName, "Detallado con separadores", StringComparison.OrdinalIgnoreCase);

            lines.Add(new string('=', width));
            lines.Add(CenterText((businessName ?? string.Empty).ToUpper(), width));
            if (!string.IsNullOrWhiteSpace(slogan))
            {
                lines.Add(CenterText(slogan, width));
            }

            AppendWrappedCenteredBlock(lines, headerText, width);
            lines.Add(new string('-', width));

            if (detailedLayout)
            {
                lines.Add(CenterText("VENTA", width));
            }

            lines.Add(FormatTwoColumns("Folio: " + folio, "Fecha: " + dateStr, width));
            lines.Add(FormatTwoColumns("Atendió: " + cashierName, "Orden: " + orderKind, width));
            if (!string.IsNullOrWhiteSpace(note))
            {
                AppendWrappedCenteredBlock(lines, "▶ " + note.ToUpper() + " ◀", width);
            }
            lines.Add(new string('=', width));

            if (detailedLayout)
            {
                lines.Add(CenterText("PRODUCTOS", width));
                lines.Add(new string('-', width));
            }

            lines.Add(FormatThreeColumns("CANT", "PRODUCTO", "TOTAL", width));
            lines.Add(new string('-', width));

            foreach (var item in items)
            {
                lines.AddRange(FormatItemRows(item.Qty, item.Name, item.Total, width));
                if (detailedLayout)
                {
                    lines.Add(new string('.', width));
                }
            }

            lines.Add(new string('=', width));

            lines.Add(FormatTwoColumns("TOTAL NETO:", "$" + NormalizeAmount(totalAmount), width));
            lines.Add(new string('=', width));

            if (!string.IsNullOrWhiteSpace(footerText))
            {
                AppendWrappedCenteredBlock(lines, footerText, width);
            }
            
            if (showSystemFooter && !string.IsNullOrWhiteSpace(systemFooterText))
            {
                lines.Add(new string('-', width));
                AppendWrappedCenteredBlock(lines, systemFooterText, width);
            }

            lines.Add(new string('=', width));

            return ApplyOffsets(lines, horizontalOffset, verticalOffset);
        }

        public static List<string> ApplyOffsets(IEnumerable<string> sourceLines, int horizontalOffset, int verticalOffset)
        {
            var lines = (sourceLines ?? Enumerable.Empty<string>()).Select(line => line ?? string.Empty).ToList();

            if (verticalOffset < 0)
            {
                for (var index = 0; index < Math.Abs(verticalOffset); index++)
                {
                    lines.Insert(0, string.Empty);
                }
            }
            else if (verticalOffset > 0)
            {
                lines = lines.Skip(Math.Min(verticalOffset, lines.Count)).ToList();
            }

            if (horizontalOffset > 0)
            {
                var padding = new string(' ', horizontalOffset);
                lines = lines.Select(line => padding + line).ToList();
            }
            else if (horizontalOffset < 0)
            {
                var trimCount = Math.Abs(horizontalOffset);
                lines = lines.Select(line => line.Length <= trimCount ? string.Empty : line.Substring(trimCount)).ToList();
            }

            return lines;
        }

        public static string CenterText(string text, int width)
        {
            if (string.IsNullOrEmpty(text)) return new string(' ', width);
            text = text.Trim();
            if (text.Length >= width) return text.Substring(0, width);
            int leftPadding = (width - text.Length) / 2;
            int rightPadding = width - text.Length - leftPadding;
            return new string(' ', leftPadding) + text + new string(' ', rightPadding);
        }

        public static string FormatTwoColumns(string left, string right, int width)
        {
            left = left ?? "";
            right = right ?? "";
            if (width <= 0)
            {
                return string.Empty;
            }

            int availableSpace = width - left.Length - right.Length;
            if (availableSpace <= 0)
            {
                int safeWidth = Math.Max(1, width - 1);
                int leftWidth = Math.Min(left.Length, safeWidth / 2);
                int rightWidth = Math.Min(right.Length, safeWidth - leftWidth);

                if (leftWidth > 0)
                {
                    left = left.Substring(0, leftWidth);
                }
                else
                {
                    left = string.Empty;
                }

                if (rightWidth > 0)
                {
                    right = right.Substring(0, rightWidth);
                }
                else
                {
                    right = string.Empty;
                }

                availableSpace = width - left.Length - right.Length;
            }
            return left + new string(' ', availableSpace) + right;
        }

        public static string FormatThreeColumns(string col1, string col2, string col3, int width)
        {
            col1 = col1 ?? "";
            col2 = col2 ?? "";
            col3 = col3 ?? "";

            int col1Width = 5;
            int col3Width = 9;
            int col2Width = Math.Max(1, width - col1Width - col3Width);

            string c1 = col1.PadRight(col1Width);
            string c3 = col3.PadLeft(col3Width);
            string c2 = col2;
            if (c2.Length > col2Width)
            {
                c2 = c2.Substring(0, col2Width);
            }
            else
            {
                c2 = c2.PadRight(col2Width);
            }

            return c1 + c2 + c3;
        }

        public static string FormatItemRow(string qty, string name, string price, int width)
        {
            string qtyStr = (qty + " x").PadRight(5);
            int priceWidth = 9;
            int nameWidth = Math.Max(1, width - qtyStr.Length - priceWidth);

            string pStr = ("$" + NormalizeAmount(price)).PadLeft(priceWidth);
            string nStr = name ?? "";
            if (nStr.Length > nameWidth)
            {
                nStr = nStr.Substring(0, nameWidth);
            }
            else
            {
                nStr = nStr.PadRight(nameWidth);
            }

            return qtyStr + nStr + pStr;
        }

        private static void AppendWrappedCenteredBlock(ICollection<string> lines, string text, int width)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            foreach (var segment in SplitIntoWrappedLines(text, Math.Max(1, width - 2)))
            {
                lines.Add(CenterText(segment, width));
            }
        }

        private static IEnumerable<string> FormatItemRows(string qty, string name, string price, int width)
        {
            var rows = new List<string>();
            string qtyStr = (qty + " x").PadRight(5);
            int priceWidth = 9;
            int nameWidth = Math.Max(1, width - qtyStr.Length - priceWidth);
            string safeName = name ?? string.Empty;

            var wrappedName = SplitIntoWrappedLines(safeName, nameWidth).ToList();
            if (wrappedName.Count == 0)
            {
                wrappedName.Add(string.Empty);
            }

            var normalizedPrice = "$" + NormalizeAmount(price);
            rows.Add(qtyStr + wrappedName[0].PadRight(nameWidth) + normalizedPrice.PadLeft(priceWidth));

            for (var index = 1; index < wrappedName.Count; index++)
            {
                rows.Add(new string(' ', qtyStr.Length) + wrappedName[index].PadRight(nameWidth) + new string(' ', priceWidth));
            }

            return rows;
        }

        private static string NormalizeAmount(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var candidate = value.Trim().Replace("$", string.Empty).Replace(",", string.Empty);
            decimal amount;
            if (!decimal.TryParse(candidate, NumberStyles.Number | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out amount)
                && !decimal.TryParse(candidate, NumberStyles.Number | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount))
            {
                return value.Trim();
            }

            return amount % 1m == 0m
                ? amount.ToString("0", CultureInfo.InvariantCulture)
                : amount.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static IEnumerable<string> SplitIntoWrappedLines(string text, int width)
        {
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return lines;
            }

            if (width <= 0)
            {
                lines.Add(text.Trim());
                return lines;
            }

            foreach (var rawLine in text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                {
                    lines.Add(string.Empty);
                    continue;
                }

                var current = string.Empty;
                foreach (var word in line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (current.Length == 0)
                    {
                        current = word.Length > width ? word.Substring(0, width) : word;
                        continue;
                    }

                    if (current.Length + 1 + word.Length <= width)
                    {
                        current += " " + word;
                    }
                    else
                    {
                        lines.Add(current);
                        current = word.Length > width ? word.Substring(0, width) : word;
                    }
                }

                if (current.Length > 0)
                {
                    lines.Add(current);
                }
            }

            return lines;
        }
    }
}
