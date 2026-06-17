using System;
using System.Linq;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Application.Services
{
    public sealed class OperationalSalesApplicationService : ISalesApplicationService
    {
        private readonly ISalesRepository repository;
        private readonly IClock clock;
        private readonly IAppLogger logger;
        private readonly ITelegramIntegrationService telegramIntegrationService;

        public OperationalSalesApplicationService(ISalesRepository repository, IClock clock, IAppLogger logger)
            : this(repository, clock, logger, null)
        {
        }

        public OperationalSalesApplicationService(ISalesRepository repository, IClock clock, IAppLogger logger, ITelegramIntegrationService telegramIntegrationService)
        {
            this.repository = repository;
            this.clock = clock;
            this.logger = logger;
            this.telegramIntegrationService = telegramIntegrationService;
        }

        public OperationResult<CreateSaleResult> RegisterSale(CreateSaleRequest request)
        {
            if (request == null)
            {
                return OperationResult<CreateSaleResult>.Fail("La venta es obligatoria.");
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                return OperationResult<CreateSaleResult>.Fail("Debes agregar al menos un producto a la venta.");
            }

            if (request.Items.Any(item => item.Quantity <= 0))
            {
                return OperationResult<CreateSaleResult>.Fail("Todas las cantidades deben ser mayores a cero.");
            }

            try
            {
                var result = this.repository.PersistSale(request, this.clock.UtcNow);
                return OperationResult<CreateSaleResult>.Ok(result, "Venta registrada correctamente.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible registrar la venta.", exception);
                return OperationResult<CreateSaleResult>.Fail(exception.Message);
            }
        }

        public OperationResult CancelSale(CancelSaleRequest request)
        {
            if (request == null || request.SaleId <= 0)
            {
                return OperationResult.Fail("Debes indicar el ticket a cancelar.");
            }

            try
            {
                this.repository.CancelSale(request, this.clock.UtcNow);
                if (this.telegramIntegrationService != null)
                {
                    this.telegramIntegrationService.SendAdminBroadcast(
                        "[CANCELACION] Ticket " + request.SaleId.ToString() + " cancelado por " + (request.UserName ?? string.Empty) + ".");
                }

                return OperationResult.Ok("Ticket cancelado correctamente.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible cancelar el ticket.", exception);
                return OperationResult.Fail(exception.Message);
            }
        }
    }
}
