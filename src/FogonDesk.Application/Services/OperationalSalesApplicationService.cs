using System;
using System.Linq;
using System.Threading.Tasks;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;
using FogonDesk.Domain.Common;

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
                    var telegramService = this.telegramIntegrationService;
                    var notificationMessage = "❌ Ticket cancelado\n\n"
                        + "🆔 Ticket: " + request.SaleId.ToString() + "\n"
                        + "👤 Cancelado por: " + (request.UserName ?? string.Empty) + "\n"
                        + "🕒 " + this.clock.UtcNow.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
                    Task.Run(() =>
                    {
                        try
                        {
                            telegramService.SendAdminBroadcast(notificationMessage);
                        }
                        catch (Exception exception)
                        {
                            this.logger.Error("No fue posible enviar la notificacion de cancelacion a Telegram.", exception);
                        }
                    });
                }

                return OperationResult.Ok("Ticket cancelado correctamente.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible cancelar el ticket.", exception);
                return OperationResult.Fail(exception.Message);
            }
        }

        public OperationResult SaveReceiptText(int saleId, string receiptText)
        {
            try
            {
                this.repository.SaveReceiptText(saleId, receiptText);
                return OperationResult.Ok("Comprobante guardado.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible guardar el comprobante del ticket.", exception);
                return OperationResult.Fail(exception.Message);
            }
        }

        public OperationResult UpdatePaymentMethod(int saleId, PaymentMethod paymentMethod)
        {
            if (saleId <= 0)
            {
                return OperationResult.Fail("Debes indicar el ticket a actualizar.");
            }

            try
            {
                this.repository.UpdatePaymentMethod(saleId, paymentMethod);
                return OperationResult.Ok("Método de pago actualizado correctamente.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible actualizar el método de pago.", exception);
                return OperationResult.Fail(exception.Message);
            }
        }
    }
}
