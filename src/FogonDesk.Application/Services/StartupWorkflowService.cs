using System;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Application.Services
{
    public sealed class StartupWorkflowService : IStartupWorkflowService
    {
        private readonly IDatabaseBootstrapper databaseBootstrapper;
        private readonly ISetupRepository setupRepository;
        private readonly IAppLogger logger;

        public StartupWorkflowService(
            IDatabaseBootstrapper databaseBootstrapper,
            ISetupRepository setupRepository,
            IAppLogger logger)
        {
            this.databaseBootstrapper = databaseBootstrapper;
            this.setupRepository = setupRepository;
            this.logger = logger;
        }

        public OperationResult<AppStartupState> Initialize()
        {
            try
            {
                this.databaseBootstrapper.EnsureDatabaseReady();
                var state = this.setupRepository.LoadStartupState();
                return OperationResult<AppStartupState>.Ok(state);
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible inicializar la aplicación.", exception);
                return OperationResult<AppStartupState>.Fail("No fue posible inicializar la aplicación.");
            }
        }
    }
}
