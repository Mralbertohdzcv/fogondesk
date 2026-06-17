using System;
using System.Collections.Generic;
using FogonDesk.Domain.Configuration;
using FogonDesk.Domain.Common;

namespace FogonDesk.Application.Models
{
    public sealed class AppStartupState
    {
        public bool IsConfigured { get; set; }
        public string BusinessName { get; set; }
        public string BusinessSlogan { get; set; }
        public string BusinessAddress { get; set; }
        public string BusinessPhone { get; set; }
        public string TicketHeaderText { get; set; }
        public string TicketFooterText { get; set; }
        public string TicketSystemFooterText { get; set; }
        public string BusinessTypeCode { get; set; }
        public string StationName { get; set; }
        public string StationCode { get; set; }
        public string ActivePrinterName { get; set; }
        public int TicketWidthMm { get; set; }
        public bool UseFullPaperWidth { get; set; }
        public int TicketCharactersPerLine { get; set; }
        public int DiningTableCount { get; set; }
        public int TicketTitleFontSize { get; set; }
        public int TicketBodyFontSize { get; set; }
        public int TicketInfoFontSize { get; set; }
        public int TicketItemsFontSize { get; set; }
        public int TicketTotalFontSize { get; set; }
        public int TicketFooterFontSize { get; set; }
        public int TicketHorizontalOffset { get; set; }
        public int TicketVerticalOffset { get; set; }
        public bool PrintKitchenTicket { get; set; }
        public bool ShowSystemFooter { get; set; }
        public string TicketLayoutName { get; set; }
    }

    public sealed class InitialSetupRequest
    {
        public string BusinessName { get; set; }
        public string BusinessTypeCode { get; set; }
        public string ManualBusinessTypeName { get; set; }
        public string Slogan { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string PrimaryColorHex { get; set; }
        public string AccentColorHex { get; set; }
        public int TicketWidthMm { get; set; }
        public string HeaderText { get; set; }
        public string FooterText { get; set; }
        public string AdminUsername { get; set; }
        public string AdminDisplayName { get; set; }
        public string AdminPassword { get; set; }
        public string StationName { get; set; }
        public string StationCode { get; set; }
        public string PrinterName { get; set; }
        public IList<SeedCategoryDefinition> Categories { get; set; }
        public IList<SeedProductDefinition> Products { get; set; }
    }

    public sealed class InitialSetupPersistenceModel
    {
        public BusinessProfile BusinessProfile { get; set; }
        public VisualTheme VisualTheme { get; set; }
        public TicketProfile TicketProfile { get; set; }
        public PrinterProfile PrinterProfile { get; set; }
        public StationProfile StationProfile { get; set; }
        public UserAccountSeed AdminUser { get; set; }
        public IList<SeedCategoryDefinition> Categories { get; set; }
        public IList<SeedProductDefinition> Products { get; set; }
    }

    public sealed class UserAccountSeed
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string PasswordHashBase64 { get; set; }
        public string PasswordSaltBase64 { get; set; }
        public string RoleCode { get; set; }
    }

    public sealed class SeedCategoryDefinition
    {
        public string Name { get; set; }
        public int SortOrder { get; set; }
    }

    public sealed class SeedProductDefinition
    {
        public string CategoryName { get; set; }
        public string Name { get; set; }
        public decimal SalePrice { get; set; }
        public decimal EstimatedCost { get; set; }
        public bool UsesInventory { get; set; }
        public decimal StockOnHand { get; set; }
    }

    public sealed class BusinessTemplateDefinition
    {
        public string Code { get; set; }
        public string DisplayName { get; set; }
        public IList<SeedCategoryDefinition> SuggestedCategories { get; set; }
        public IList<SeedProductDefinition> SuggestedProducts { get; set; }
    }

    public sealed class AuthenticationRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public sealed class AuthenticatedUserView
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string RoleCode { get; set; }
        public DateTime SignedInUtc { get; set; }
    }

    public sealed class UserAccountRecord
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string PasswordHashBase64 { get; set; }
        public string PasswordSaltBase64 { get; set; }
        public string RoleCode { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginUtc { get; set; }
    }

    public sealed class UserManagementView
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string RoleCode { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginUtc { get; set; }
    }

    public sealed class CreateUserRequest
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string RoleCode { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class UpdateUserRequest
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string RoleCode { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class PasswordHashResult
    {
        public string HashBase64 { get; set; }
        public string SaltBase64 { get; set; }
    }

    public sealed class TicketPrintJob
    {
        public string PrinterName { get; set; }
        public string Title { get; set; }
        public IList<string> Lines { get; set; }
        public int TicketWidthMm { get; set; }
        public bool UseFullPaperWidth { get; set; }
        public int HeaderFontSize { get; set; }
        public int InfoFontSize { get; set; }
        public int ItemsFontSize { get; set; }
        public int TotalFontSize { get; set; }
        public int FooterFontSize { get; set; }
    }

    public sealed class TicketPrintSettingsView
    {
        public string PrinterName { get; set; }
        public int TicketWidthMm { get; set; }
        public bool UseFullPaperWidth { get; set; }
        public int TicketCharactersPerLine { get; set; }
        public int DiningTableCount { get; set; }
        public int TicketTitleFontSize { get; set; }
        public int TicketBodyFontSize { get; set; }
        public int TicketInfoFontSize { get; set; }
        public int TicketItemsFontSize { get; set; }
        public int TicketTotalFontSize { get; set; }
        public int TicketFooterFontSize { get; set; }
        public int TicketHorizontalOffset { get; set; }
        public int TicketVerticalOffset { get; set; }
        public bool PrintKitchenTicket { get; set; }
        public bool ShowSystemFooter { get; set; }
        public string TicketLayoutName { get; set; }
        public string BusinessName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string SystemFooterText { get; set; }
        public string HeaderText { get; set; }
        public string FooterText { get; set; }
        public string Slogan { get; set; }
    }

    public sealed class OperationSettingsView
    {
        public int DiningTableCount { get; set; }
        public IList<DigitalPlatformConfigurationView> DigitalPlatforms { get; set; }
    }

    public sealed class DigitalPlatformConfigurationView
    {
        public int PlatformId { get; set; }
        public string Name { get; set; }
        public string PricingMode { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class SaveOperationSettingsRequest
    {
        public int DiningTableCount { get; set; }
        public IList<DigitalPlatformConfigurationEditView> DigitalPlatforms { get; set; }
    }

    public sealed class DigitalPlatformConfigurationEditView
    {
        public int? PlatformId { get; set; }
        public string Name { get; set; }
        public string PricingMode { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class BackupSnapshot
    {
        public string FilePath { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public sealed class TelegramSettingsView
    {
        public string BotToken { get; set; }
        public long LastUpdateId { get; set; }
        public IList<TelegramLinkedChatView> LinkedChats { get; set; }
    }

    public sealed class TelegramLinkedChatView
    {
        public long ChatId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public DateTime LinkedUtc { get; set; }
    }

    public sealed class TelegramLinkCodeResult
    {
        public string Code { get; set; }
        public DateTime ExpiresUtc { get; set; }
    }

    public sealed class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public sealed class CategoryManagementView
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class ProductViewModel
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public decimal SalePrice { get; set; }
        public decimal EstimatedCost { get; set; }
        public bool UsesInventory { get; set; }
        public decimal StockOnHand { get; set; }
    }

    public sealed class ProductManagementView
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Name { get; set; }
        public decimal SalePrice { get; set; }
        public decimal EstimatedCost { get; set; }
        public bool UsesInventory { get; set; }
        public decimal StockOnHand { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class CreateCategoryRequest
    {
        public string Name { get; set; }
        public int SortOrder { get; set; }
    }

    public sealed class UpdateCategoryRequest
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class CreateProductRequest
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public decimal SalePrice { get; set; }
        public decimal EstimatedCost { get; set; }
        public bool UsesInventory { get; set; }
        public decimal StockOnHand { get; set; }
    }

    public sealed class UpdateProductRequest
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public decimal SalePrice { get; set; }
        public decimal EstimatedCost { get; set; }
        public bool UsesInventory { get; set; }
        public decimal StockOnHand { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class SaleLineDraft
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal Quantity { get; set; }
        public bool UsesInventory { get; set; }
    }

    public sealed class CreateSaleRequest
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int? CashShiftId { get; set; }
        public OrderKind OrderKind { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Note { get; set; }
        public IList<SaleLineDraft> Items { get; set; }
    }

    public sealed class CreateSaleResult
    {
        public int SaleId { get; set; }
        public string Folio { get; set; }
        public decimal Total { get; set; }
        public DateTime SoldUtc { get; set; }
    }

    public sealed class CancelSaleRequest
    {
        public int SaleId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int? CashShiftId { get; set; }
    }

    public sealed class CashShiftSummaryView
    {
        public int ShiftId { get; set; }
        public string Folio { get; set; }
        public string StationCode { get; set; }
        public string OpenedByDisplayName { get; set; }
        public DateTime OpenedUtc { get; set; }
        public DateTime? ClosedUtc { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal? ActualCash { get; set; }
        public decimal SalesTotal { get; set; }
        public decimal EstimatedProfitTotal { get; set; }
        public decimal? DifferenceTotal { get; set; }
        public string Status { get; set; }
    }

    public sealed class OpenCashShiftRequest
    {
        public string StationCode { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public decimal OpeningCash { get; set; }
    }

    public sealed class CloseCashShiftRequest
    {
        public int ShiftId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public decimal ActualCash { get; set; }
        public string TelegramSummaryMessage { get; set; }
    }
}
