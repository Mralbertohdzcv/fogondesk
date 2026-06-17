using System;
using System.Collections.Generic;
using System.Linq;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Application.Services
{
    public sealed class OperationSettingsApplicationService : IOperationSettingsApplicationService
    {
        private static readonly string[] AllowedPricingModes = { "rappi", "didi", "manual" };
        private readonly IOperationSettingsRepository repository;
        private readonly IClock clock;
        private readonly IAppLogger logger;

        public OperationSettingsApplicationService(IOperationSettingsRepository repository, IClock clock, IAppLogger logger)
        {
            this.repository = repository;
            this.clock = clock;
            this.logger = logger;
        }

        public OperationSettingsView GetSettings()
        {
            return this.repository.LoadSettings();
        }

        public OperationResult SaveSettings(SaveOperationSettingsRequest request)
        {
            if (request == null)
            {
                return OperationResult.Fail("La configuración de operación es obligatoria.");
            }

            if (request.DiningTableCount < 1 || request.DiningTableCount > 60)
            {
                return OperationResult.Fail("La cantidad de mesas debe estar entre 1 y 60.");
            }

            var platforms = request.DigitalPlatforms ?? new List<DigitalPlatformConfigurationEditView>();
            if (platforms.Count == 0)
            {
                return OperationResult.Fail("Configura al menos una plataforma digital.");
            }

            foreach (var platform in platforms)
            {
                if (platform == null)
                {
                    return OperationResult.Fail("Existe una plataforma inválida en la lista.");
                }

                platform.Name = (platform.Name ?? string.Empty).Trim();
                platform.PricingMode = (platform.PricingMode ?? string.Empty).Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(platform.Name))
                {
                    return OperationResult.Fail("El nombre de la plataforma es obligatorio.");
                }

                if (!AllowedPricingModes.Contains(platform.PricingMode))
                {
                    return OperationResult.Fail("El tipo de precio de la plataforma no es válido.");
                }
            }

            var duplicateNames = platforms
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (duplicateNames.Count > 0)
            {
                return OperationResult.Fail("No se permiten plataformas con nombres repetidos.");
            }

            try
            {
                this.repository.SaveSettings(request, this.clock.UtcNow);
                return OperationResult.Ok("Configuración de plataformas y mesas guardada correctamente.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible guardar la configuración de operación.", exception);
                return OperationResult.Fail("No fue posible guardar la configuración de operación.");
            }
        }
    }
}
