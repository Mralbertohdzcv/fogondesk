using FogonDesk.Application.Contracts;
using FogonDesk.Application.Services;
using FogonDesk.Configuration;
using FogonDesk.Infrastructure.Data;
using FogonDesk.Infrastructure.Platform;
using FogonDesk.Infrastructure.Security;
using FogonDesk.Infrastructure.Services;
using FogonDesk.Printing;

namespace FogonDesk.Desktop
{
    public sealed class DesktopCompositionRoot
    {
        public DesktopCompositionRoot()
        {
            this.Paths = StationPathsFactory.CreateDefault();
            StationPathsFactory.EnsureCreated(this.Paths);

            this.Clock = new SystemClock();
            this.Logger = new FileLogger(this.Paths);
            this.ConnectionFactory = new SqliteConnectionFactory(this.Paths);
            this.PasswordHasher = new Pbkdf2PasswordHasher();
            this.DatabaseBootstrapper = new SqliteDatabaseBootstrapper(this.ConnectionFactory, this.Paths, this.Clock, this.Logger);
            this.SetupRepository = new SqliteSetupRepository(this.ConnectionFactory, this.Clock);
            this.UserRepository = new SqliteUserRepository(this.ConnectionFactory);
            this.CatalogRepository = new SqliteCatalogRepository(this.ConnectionFactory);
            this.CashShiftRepository = new SqliteCashShiftRepository(this.ConnectionFactory);
            this.SalesRepository = new SqliteSalesRepository(this.ConnectionFactory);
            this.TicketPrintSettingsRepository = new SqliteTicketPrintSettingsRepository(this.ConnectionFactory);
            this.OperationSettingsRepository = new SqliteOperationSettingsRepository(this.ConnectionFactory);
            this.CounterRepository = new SqliteCounterRepository(this.ConnectionFactory);
            this.DataResetService = new SqliteDataResetService(this.ConnectionFactory);
            this.TicketPrinter = new WindowsTicketPrinter(this.Logger);
            this.TelegramIntegrationService = new TelegramIntegrationService(this.ConnectionFactory, this.Clock, this.Logger);
            this.BackupService = new SqliteBackupService(this.ConnectionFactory, this.Paths, this.Clock, this.Logger);
            this.StartupWorkflowService = new StartupWorkflowService(this.DatabaseBootstrapper, this.SetupRepository, this.Logger);
            this.InitialSetupService = new InitialSetupService(this.SetupRepository, this.PasswordHasher, this.Clock, this.Logger);
            this.AuthenticationService = new AuthenticationService(this.UserRepository, this.PasswordHasher, this.Clock, this.Logger);
            this.CatalogApplicationService = new CatalogApplicationService(this.CatalogRepository);
            this.UserAdministrationService = new UserAdministrationService(this.UserRepository, this.PasswordHasher, this.Clock);
            this.CashShiftApplicationService = new CashShiftApplicationService(this.CashShiftRepository, this.Clock, this.Logger, this.TelegramIntegrationService);
            this.SalesApplicationService = new OperationalSalesApplicationService(this.SalesRepository, this.Clock, this.Logger, this.TelegramIntegrationService);
            this.TicketPrintSettingsApplicationService = new TicketPrintSettingsApplicationService(this.TicketPrintSettingsRepository, this.Clock, this.Logger);
            this.OperationSettingsApplicationService = new OperationSettingsApplicationService(this.OperationSettingsRepository, this.Clock, this.Logger);
        }

        public StationPaths Paths { get; private set; }
        public IClock Clock { get; private set; }
        public IAppLogger Logger { get; private set; }
        public SqliteConnectionFactory ConnectionFactory { get; private set; }
        public IPasswordHasher PasswordHasher { get; private set; }
        public IDatabaseBootstrapper DatabaseBootstrapper { get; private set; }
        public ISetupRepository SetupRepository { get; private set; }
        public IUserRepository UserRepository { get; private set; }
        public ICatalogRepository CatalogRepository { get; private set; }
        public ICashShiftRepository CashShiftRepository { get; private set; }
        public ISalesRepository SalesRepository { get; private set; }
        public ITicketPrintSettingsRepository TicketPrintSettingsRepository { get; private set; }
        public IOperationSettingsRepository OperationSettingsRepository { get; private set; }
        public ICounterRepository CounterRepository { get; private set; }
        public IDataResetService DataResetService { get; private set; }
        public ITicketPrinter TicketPrinter { get; private set; }
        public ITelegramIntegrationService TelegramIntegrationService { get; private set; }
        public IBackupService BackupService { get; private set; }
        public IStartupWorkflowService StartupWorkflowService { get; private set; }
        public IInitialSetupService InitialSetupService { get; private set; }
        public IAuthenticationService AuthenticationService { get; private set; }
        public ICatalogApplicationService CatalogApplicationService { get; private set; }
        public IUserAdministrationService UserAdministrationService { get; private set; }
        public ICashShiftApplicationService CashShiftApplicationService { get; private set; }
        public ISalesApplicationService SalesApplicationService { get; private set; }
        public ITicketPrintSettingsApplicationService TicketPrintSettingsApplicationService { get; private set; }
        public IOperationSettingsApplicationService OperationSettingsApplicationService { get; private set; }
    }
}
