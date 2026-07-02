using System;
using System.Collections.Generic;
using FogonDesk.Application.Common;
using FogonDesk.Application.Models;
using FogonDesk.Domain.Common;

namespace FogonDesk.Application.Contracts
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public interface IAppLogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception exception = null);
    }

    public interface IPasswordHasher
    {
        PasswordHashResult HashPassword(string plainTextPassword);
        bool Verify(string plainTextPassword, string saltBase64, string hashBase64);
    }

    public interface IDatabaseBootstrapper
    {
        void EnsureDatabaseReady();
    }

    public interface ISetupRepository
    {
        bool IsSystemConfigured();
        AppStartupState LoadStartupState();
        void ExecuteInitialSetup(InitialSetupPersistenceModel model);
    }

    public interface IUserRepository
    {
        UserAccountRecord FindByUsername(string username);
        UserAccountRecord FindById(int userId);
        IList<UserManagementView> LoadUsers();
        void CreateUser(UserAccountSeed user, bool isActive, DateTime createdUtc);
        void UpdateUser(UpdateUserRequest request, string passwordHashBase64, string passwordSaltBase64, DateTime updatedUtc);
        void DeleteUser(int userId, DateTime updatedUtc);
        void UpdateLastLogin(int userId, DateTime lastLoginUtc);
    }

    public interface IInitialSetupService
    {
        IList<BusinessTemplateDefinition> GetAvailableTemplates();
        OperationResult Execute(InitialSetupRequest request);
    }

    public interface IAuthenticationService
    {
        OperationResult<AuthenticatedUserView> Authenticate(AuthenticationRequest request);
    }

    public interface IStartupWorkflowService
    {
        OperationResult<AppStartupState> Initialize();
    }

    public interface ITicketPrinter
    {
        IList<string> GetInstalledPrinters();
        OperationResult Print(TicketPrintJob job);
    }

    public interface IBackupService
    {
        OperationResult<BackupSnapshot> CreateBackup(string requestedByUser);
        OperationResult RestoreBackup(string requestedByUser, string sourceFilePath);
    }

    public interface ICatalogApplicationService
    {
        IList<CategoryViewModel> GetCategories();
        IList<CategoryManagementView> GetCategoriesForManagement();
        IList<ProductViewModel> GetProductsByCategory(int categoryId);
        IList<ProductManagementView> GetProductsForManagement();
        OperationResult CreateCategory(CreateCategoryRequest request);
        OperationResult UpdateCategory(UpdateCategoryRequest request);
        OperationResult DeleteCategory(int categoryId);
        OperationResult CreateProduct(CreateProductRequest request);
        OperationResult UpdateProduct(UpdateProductRequest request);
        OperationResult DeleteProduct(int productId);
    }

    public interface IUserAdministrationService
    {
        IList<UserManagementView> GetUsers();
        OperationResult CreateUser(CreateUserRequest request);
        OperationResult UpdateUser(UpdateUserRequest request);
        OperationResult DeleteUser(int userId);
    }

    public interface ISalesApplicationService
    {
        OperationResult<CreateSaleResult> RegisterSale(CreateSaleRequest request);
        OperationResult CancelSale(CancelSaleRequest request);
        OperationResult SaveReceiptText(int saleId, string receiptText);
        OperationResult UpdatePaymentMethod(int saleId, PaymentMethod paymentMethod);
    }

    public interface ITicketPrintSettingsApplicationService
    {
        TicketPrintSettingsView GetSettings();
        OperationResult SaveSettings(TicketPrintSettingsView settings);
    }

    public interface IOperationSettingsApplicationService
    {
        OperationSettingsView GetSettings();
        OperationResult SaveSettings(SaveOperationSettingsRequest request);
    }

    public interface ITelegramIntegrationService
    {
        TelegramSettingsView GetSettings();
        OperationResult SaveBotToken(string botToken);
        OperationResult<TelegramLinkCodeResult> GenerateLinkCode(int expiresInMinutes);
        OperationResult<int> SyncLinkRequests();
        OperationResult SendAdminBroadcast(string message);
    }

    public interface IPendingOrderApplicationService
    {
    }

    public interface IInventoryApplicationService
    {
    }

    public interface ICashShiftApplicationService
    {
        OperationResult<CashShiftSummaryView> GetActiveShift(string stationCode);
        OperationResult<CashShiftSummaryView> OpenShift(OpenCashShiftRequest request);
        OperationResult<CashShiftSummaryView> CloseShift(CloseCashShiftRequest request);
        IList<CashShiftSummaryView> GetRecentShifts(string stationCode, int maxCount);
        int CountParaLlevarSalesInActiveShift(string stationCode);
    }

    public interface IDataResetService
    {
        OperationResult ResetSalesData();
    }

    public interface ICancellationApplicationService
    {
    }

    public interface IReportingApplicationService
    {
    }

    public interface ICatalogRepository
    {
        IList<CategoryViewModel> LoadCategories();
        IList<CategoryManagementView> LoadCategoriesForManagement();
        IList<ProductViewModel> LoadProductsByCategory(int categoryId);
        IList<ProductManagementView> LoadProductsForManagement();
        void CreateCategory(CreateCategoryRequest request, DateTime createdUtc);
        void UpdateCategory(UpdateCategoryRequest request);
        void DeleteCategory(int categoryId);
        void CreateProduct(CreateProductRequest request, DateTime createdUtc);
        void UpdateProduct(UpdateProductRequest request);
        void DeleteProduct(int productId);
    }

    public interface ICashShiftRepository
    {
        CashShiftSummaryView FindActiveShift(string stationCode);
        CashShiftSummaryView OpenShift(OpenCashShiftRequest request, DateTime openedUtc);
        CashShiftSummaryView CloseShift(CloseCashShiftRequest request, DateTime closedUtc);
        IList<CashShiftSummaryView> LoadRecentShifts(string stationCode, int maxCount);
        int CountConfirmedSalesByOrderKind(int shiftId, int orderKind);
    }

    public interface ISalesRepository
    {
        CreateSaleResult PersistSale(CreateSaleRequest request, DateTime soldUtc);
        void CancelSale(CancelSaleRequest request, DateTime cancelledUtc);
        void SaveReceiptText(int saleId, string receiptText);
        void UpdatePaymentMethod(int saleId, PaymentMethod paymentMethod);
    }

    public interface ITicketPrintSettingsRepository
    {
        TicketPrintSettingsView LoadSettings();
        void SaveSettings(TicketPrintSettingsView settings, DateTime updatedUtc);
    }

    public interface IOperationSettingsRepository
    {
        OperationSettingsView LoadSettings();
        void SaveSettings(SaveOperationSettingsRequest request, DateTime updatedUtc);
    }

    public interface ICounterRepository
    {
        int GetNextValue(string counterName);
        int PeekCurrentValue(string counterName);
        void ResetValue(string counterName, int value);
    }
}
