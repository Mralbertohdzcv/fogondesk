using System;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Application.Services
{
    public sealed class TicketPrintSettingsApplicationService : ITicketPrintSettingsApplicationService
    {
        private readonly ITicketPrintSettingsRepository repository;
        private readonly IClock clock;
        private readonly IAppLogger logger;

        public TicketPrintSettingsApplicationService(ITicketPrintSettingsRepository repository, IClock clock, IAppLogger logger)
        {
            this.repository = repository;
            this.clock = clock;
            this.logger = logger;
        }

        public TicketPrintSettingsView GetSettings()
        {
            return this.repository.LoadSettings();
        }

        public OperationResult SaveSettings(TicketPrintSettingsView settings)
        {
            if (settings == null)
            {
                return OperationResult.Fail("La configuración de ticket es obligatoria.");
            }

            if (settings.TicketWidthMm != 58 && settings.TicketWidthMm != 80)
            {
                return OperationResult.Fail("El ancho del ticket debe ser 58 u 80 mm.");
            }

            if (settings.TicketCharactersPerLine < 24 || settings.TicketCharactersPerLine > 48)
            {
                return OperationResult.Fail("Los caracteres por línea deben estar entre 24 y 48.");
            }

            if (string.IsNullOrWhiteSpace(settings.BusinessName))
            {
                return OperationResult.Fail("El nombre del negocio en el ticket es obligatorio.");
            }

            if (settings.TicketTitleFontSize < 7 || settings.TicketTitleFontSize > 24 || settings.TicketBodyFontSize < 7 || settings.TicketBodyFontSize > 18 || settings.TicketInfoFontSize < 7 || settings.TicketInfoFontSize > 18 || settings.TicketItemsFontSize < 7 || settings.TicketItemsFontSize > 18 || settings.TicketTotalFontSize < 7 || settings.TicketTotalFontSize > 20 || settings.TicketFooterFontSize < 7 || settings.TicketFooterFontSize > 18)
            {
                return OperationResult.Fail("Los tamaños de fuente están fuera del rango permitido.");
            }

            if (settings.TicketHorizontalOffset < -20 || settings.TicketHorizontalOffset > 20 || settings.TicketVerticalOffset < -20 || settings.TicketVerticalOffset > 20)
            {
                return OperationResult.Fail("Los ajustes de margen deben estar entre -20 y 20.");
            }

            try
            {
                this.repository.SaveSettings(settings, this.clock.UtcNow);
                return OperationResult.Ok("Configuración de ticket e impresión guardada correctamente.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible guardar la configuración de ticket e impresión.", exception);
                return OperationResult.Fail("No fue posible guardar la configuración de ticket e impresión.");
            }
        }
    }
}