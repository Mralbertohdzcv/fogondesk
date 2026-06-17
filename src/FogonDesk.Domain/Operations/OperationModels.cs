using System;
using FogonDesk.Domain.Common;

namespace FogonDesk.Domain.Operations
{
    public sealed class PendingOrder
    {
        public int Id { get; set; }
        public string Folio { get; set; }
        public OrderKind OrderKind { get; set; }
        public PendingOrderStatus Status { get; set; }
        public int? TableId { get; set; }
        public string CustomerName { get; set; }
        public int? DigitalPlatformId { get; set; }
        public string PlatformReference { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime OpenedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public string Note { get; set; }
    }

    public sealed class PendingOrderItem
    {
        public int Id { get; set; }
        public int PendingOrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductNameSnapshot { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Note { get; set; }
    }

    public sealed class PendingOrderItemModifier
    {
        public int Id { get; set; }
        public int PendingOrderItemId { get; set; }
        public string ModifierNameSnapshot { get; set; }
        public decimal ExtraPrice { get; set; }
        public decimal Quantity { get; set; }
    }

    public sealed class Sale
    {
        public int Id { get; set; }
        public string Folio { get; set; }
        public OrderKind OrderKind { get; set; }
        public SaleStatus Status { get; set; }
        public int? PendingOrderId { get; set; }
        public int? CashShiftId { get; set; }
        public int SoldByUserId { get; set; }
        public DateTime SoldUtc { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal Total { get; set; }
        public decimal EstimatedCostTotal { get; set; }
        public decimal EstimatedProfitTotal { get; set; }
        public string PaymentSummary { get; set; }
        public string Note { get; set; }
        public string PrintStatus { get; set; }
    }

    public sealed class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int? ProductId { get; set; }
        public int? ComboId { get; set; }
        public string ProductNameSnapshot { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public decimal EstimatedCostTotal { get; set; }
        public bool UsesInventory { get; set; }
    }

    public sealed class SaleItemModifier
    {
        public int Id { get; set; }
        public int SaleItemId { get; set; }
        public string ModifierNameSnapshot { get; set; }
        public decimal ExtraPrice { get; set; }
        public decimal Quantity { get; set; }
    }

    public sealed class PaymentRecord
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public string ReferenceText { get; set; }
    }

    public sealed class CashShift
    {
        public int Id { get; set; }
        public string Folio { get; set; }
        public string StationCode { get; set; }
        public int OpenedByUserId { get; set; }
        public int? ClosedByUserId { get; set; }
        public DateTime OpenedUtc { get; set; }
        public DateTime? ClosedUtc { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal? ActualCash { get; set; }
        public decimal SalesTotal { get; set; }
        public decimal EstimatedCostTotal { get; set; }
        public decimal EstimatedProfitTotal { get; set; }
        public decimal? DifferenceTotal { get; set; }
        public string Status { get; set; }
    }

    public sealed class CancellationRequest
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int RequestedByUserId { get; set; }
        public int? ApprovedByUserId { get; set; }
        public DateTime RequestedUtc { get; set; }
        public DateTime? ResolvedUtc { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
    }

    public sealed class InventoryMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string MovementType { get; set; }
        public decimal Quantity { get; set; }
        public string SourceDocument { get; set; }
        public int ReferenceId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string Note { get; set; }
    }

    public sealed class AuditEntry
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string UserName { get; set; }
        public string Details { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public sealed class BackupRecord
    {
        public int Id { get; set; }
        public string FilePath { get; set; }
        public string OperationType { get; set; }
        public string ResultStatus { get; set; }
        public string CreatedByUserName { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string Notes { get; set; }
    }
}
