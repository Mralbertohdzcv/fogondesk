using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Printing
{
    public sealed class WindowsTicketPrinter : ITicketPrinter
    {
        private readonly IAppLogger logger;

        public WindowsTicketPrinter(IAppLogger logger)
        {
            this.logger = logger;
        }

        public IList<string> GetInstalledPrinters()
        {
            var printers = new List<string>();
            foreach (string printerName in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printerName);
            }

            return printers;
        }

        public OperationResult Print(TicketPrintJob job)
        {
            if (job == null || string.IsNullOrWhiteSpace(job.PrinterName))
            {
                return OperationResult.Fail("Debes seleccionar una impresora válida.");
            }

            try
            {
                using (var document = new PrintDocument())
                using (var titleFont = new Font("Consolas", GetFontSize(job.HeaderFontSize, 12), FontStyle.Bold))
                using (var infoFont = new Font("Consolas", GetFontSize(job.InfoFontSize, 9), FontStyle.Regular))
                using (var itemsFont = new Font("Consolas", GetFontSize(job.ItemsFontSize, 9), FontStyle.Regular))
                using (var totalFont = new Font("Consolas", GetFontSize(job.TotalFontSize, 9), FontStyle.Bold))
                using (var footerFont = new Font("Consolas", GetFontSize(job.FooterFontSize, 9), FontStyle.Regular))
                using (var bodyFont = new Font("Consolas", 9f, FontStyle.Regular))
                {
                    document.PrinterSettings.PrinterName = job.PrinterName;
                    if (!document.PrinterSettings.IsValid)
                    {
                        return OperationResult.Fail("La impresora seleccionada no está disponible.");
                    }

                    document.OriginAtMargins = false;
                    document.DefaultPageSettings.Margins = job.UseFullPaperWidth ? new Margins(0, 0, 0, 0) : new Margins(5, 5, 5, 5);
                    if (job.TicketWidthMm == 58 || job.TicketWidthMm == 80)
                    {
                        var paperWidth = job.TicketWidthMm == 58 ? 228 : 315;
                        document.DefaultPageSettings.PaperSize = new PaperSize("Ticket", paperWidth, 2000);
                    }
                    document.PrintController = new StandardPrintController();
                    document.DocumentName = string.IsNullOrWhiteSpace(job.Title) ? "Ticket MrAlbertoCompany" : job.Title;

                    document.PrintPage += delegate(object sender, PrintPageEventArgs args)
                    {
                        if (job.UseFullPaperWidth)
                        {
                            args.Graphics.TranslateTransform(-args.PageSettings.HardMarginX, -args.PageSettings.HardMarginY);
                        }

                        var startX = job.UseFullPaperWidth ? 0 : args.MarginBounds.Left;
                        var startY = job.UseFullPaperWidth ? 0 : args.MarginBounds.Top;
                        var currentY = args.MarginBounds.Top;
                        currentY = startY;

                        if (job.Lines != null)
                        {
                            foreach (var line in job.Lines)
                            {
                                var font = SelectLineFont(line, job, titleFont, infoFont, itemsFont, totalFont, footerFont, bodyFont);
                                args.Graphics.DrawString(line ?? string.Empty, font, Brushes.Black, startX, currentY);
                                currentY += (int)font.GetHeight(args.Graphics) + 2;
                            }
                        }

                        args.HasMorePages = false;
                    };

                    document.Print();
                }

                return OperationResult.Ok("Impresión enviada correctamente.");
            }
            catch (Exception exception)
            {
                this.logger.Error("Fallo el envío del ticket a la impresora.", exception);
                return OperationResult.Fail("No fue posible imprimir el ticket.");
            }
        }

        private static float GetFontSize(int configuredSize, int defaultSize)
        {
            return configuredSize <= 0 ? defaultSize : configuredSize;
        }

        private static Font SelectLineFont(string line, TicketPrintJob job, Font titleFont, Font infoFont, Font itemsFont, Font totalFont, Font footerFont, Font bodyFont)
        {
            var value = (line ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(job.Title) && !string.IsNullOrWhiteSpace(value) && value.Equals(value.ToUpperInvariant()) && value.Any(char.IsLetter) && !value.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("CANT", StringComparison.OrdinalIgnoreCase) && !value.Contains(" x "))
            {
                return titleFont;
            }

            if (value.StartsWith("Folio:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("Atendió:", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "VENTA", StringComparison.OrdinalIgnoreCase))
            {
                return infoFont;
            }

            if (value.StartsWith("CANT", StringComparison.OrdinalIgnoreCase) || value.Contains(" x ") || string.Equals(value, "PRODUCTOS", StringComparison.OrdinalIgnoreCase))
            {
                return itemsFont;
            }

            if (value.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase))
            {
                return totalFont;
            }

            if (value.Length > 0 && !value.Any(char.IsLetterOrDigit))
            {
                return bodyFont;
            }

            return footerFont;
        }
    }
}
