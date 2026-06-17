using System;
using System.Collections.Generic;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Application.Services
{
    public sealed class CashShiftApplicationService : ICashShiftApplicationService
    {
        private readonly ICashShiftRepository repository;
        private readonly IClock clock;
        private readonly IAppLogger logger;
        private readonly ITelegramIntegrationService telegramIntegrationService;

        public CashShiftApplicationService(ICashShiftRepository repository, IClock clock, IAppLogger logger)
            : this(repository, clock, logger, null)
        {
        }

        public CashShiftApplicationService(ICashShiftRepository repository, IClock clock, IAppLogger logger, ITelegramIntegrationService telegramIntegrationService)
        {
            this.repository = repository;
            this.clock = clock;
            this.logger = logger;
            this.telegramIntegrationService = telegramIntegrationService;
        }

        public OperationResult<CashShiftSummaryView> GetActiveShift(string stationCode)
        {
            if (string.IsNullOrWhiteSpace(stationCode))
            {
                return OperationResult<CashShiftSummaryView>.Fail("No se encontró la estación activa.");
            }

            var shift = this.repository.FindActiveShift(stationCode.Trim());
            if (shift == null)
            {
                return OperationResult<CashShiftSummaryView>.Fail("No hay un corte abierto para esta estación.");
            }

            return OperationResult<CashShiftSummaryView>.Ok(shift, "Corte activo cargado.");
        }

        public OperationResult<CashShiftSummaryView> OpenShift(OpenCashShiftRequest request)
        {
            if (request == null)
            {
                return OperationResult<CashShiftSummaryView>.Fail("La apertura de caja es obligatoria.");
            }

            if (string.IsNullOrWhiteSpace(request.StationCode))
            {
                return OperationResult<CashShiftSummaryView>.Fail("No se encontró la estación activa.");
            }

            if (request.OpeningCash < 0m)
            {
                return OperationResult<CashShiftSummaryView>.Fail("El fondo inicial no puede ser negativo.");
            }

            if (this.repository.FindActiveShift(request.StationCode.Trim()) != null)
            {
                return OperationResult<CashShiftSummaryView>.Fail("Ya existe un corte abierto en esta estación.");
            }

            try
            {
                var shift = this.repository.OpenShift(request, this.clock.UtcNow);
                return OperationResult<CashShiftSummaryView>.Ok(shift, "Caja abierta correctamente.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible abrir la caja.", exception);
                return OperationResult<CashShiftSummaryView>.Fail("No fue posible abrir la caja.");
            }
        }

        public OperationResult<CashShiftSummaryView> CloseShift(CloseCashShiftRequest request)
        {
            if (request == null || request.ShiftId <= 0)
            {
                return OperationResult<CashShiftSummaryView>.Fail("Debes indicar el corte a cerrar.");
            }

            if (request.ActualCash < 0m)
            {
                return OperationResult<CashShiftSummaryView>.Fail("El efectivo contado no puede ser negativo.");
            }

            try
            {
                var shift = this.repository.CloseShift(request, this.clock.UtcNow);
                if (this.telegramIntegrationService != null && shift != null)
                {
                    var telegramMessage = (request.TelegramSummaryMessage ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(telegramMessage))
                    {
                        telegramMessage =
                            "[CORTE] " + shift.Folio + " cerrado por " + (request.UserName ?? string.Empty)
                            + ". Ventas: $" + shift.SalesTotal.ToString("N2")
                            + " | Esperado: $" + shift.ExpectedCash.ToString("N2")
                            + " | Real: $" + (shift.ActualCash ?? 0m).ToString("N2") + ".";
                    }

                    this.telegramIntegrationService.SendAdminBroadcast(
                        telegramMessage);
                }

                return OperationResult<CashShiftSummaryView>.Ok(shift, "Caja cerrada correctamente.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible cerrar la caja.", exception);
                return OperationResult<CashShiftSummaryView>.Fail(exception.Message);
            }
        }

        public IList<CashShiftSummaryView> GetRecentShifts(string stationCode, int maxCount)
        {
            return this.repository.LoadRecentShifts(stationCode, maxCount <= 0 ? 10 : maxCount);
        }
    }
}