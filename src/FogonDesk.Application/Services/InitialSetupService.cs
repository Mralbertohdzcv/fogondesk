using System;
using System.Collections.Generic;
using System.Linq;
using FogonDesk.Application.Catalogs;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;
using FogonDesk.Domain.Common;
using FogonDesk.Domain.Configuration;

namespace FogonDesk.Application.Services
{
    public sealed class InitialSetupService : IInitialSetupService
    {
        private readonly ISetupRepository setupRepository;
        private readonly IPasswordHasher passwordHasher;
        private readonly IClock clock;
        private readonly IAppLogger logger;

        public InitialSetupService(
            ISetupRepository setupRepository,
            IPasswordHasher passwordHasher,
            IClock clock,
            IAppLogger logger)
        {
            this.setupRepository = setupRepository;
            this.passwordHasher = passwordHasher;
            this.clock = clock;
            this.logger = logger;
        }

        public IList<BusinessTemplateDefinition> GetAvailableTemplates()
        {
            return BusinessTemplateCatalog.GetAvailableTemplates();
        }

        public OperationResult Execute(InitialSetupRequest request)
        {
            if (request == null)
            {
                return OperationResult.Fail("La configuración inicial es obligatoria.");
            }

            if (this.setupRepository.IsSystemConfigured())
            {
                return OperationResult.Fail("El sistema ya fue configurado previamente.");
            }

            var validationMessage = Validate(request);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return OperationResult.Fail(validationMessage);
            }

            try
            {
                var templateCode = ResolveTemplateCode(request.BusinessTypeCode);
                var template = BusinessTemplateCatalog.GetTemplate(templateCode);
                var categories = BuildCategories(request, template);
                var products = BuildProducts(request, template, categories);
                var passwordHash = this.passwordHasher.HashPassword(request.AdminPassword.Trim());
                var now = this.clock.UtcNow;

                var persistenceModel = new InitialSetupPersistenceModel
                {
                    BusinessProfile = new BusinessProfile
                    {
                        Id = 1,
                        TradeName = request.BusinessName.Trim(),
                        BusinessTypeCode = BuildStoredBusinessTypeCode(request, template),
                        Slogan = request.Slogan == null ? string.Empty : request.Slogan.Trim(),
                        Address = request.Address == null ? string.Empty : request.Address.Trim(),
                        Phone = request.Phone == null ? string.Empty : request.Phone.Trim(),
                        HeaderText = string.IsNullOrWhiteSpace(request.HeaderText) ? request.BusinessName.Trim() : request.HeaderText.Trim(),
                        FooterText = request.FooterText == null ? string.Empty : request.FooterText.Trim()
                    },
                    VisualTheme = new VisualTheme
                    {
                        Id = 1,
                        PrimaryColorHex = NormalizeColor(request.PrimaryColorHex, "#1B4332"),
                        AccentColorHex = NormalizeColor(request.AccentColorHex, "#F4A261")
                    },
                    TicketProfile = new TicketProfile
                    {
                        Id = 1,
                        TicketWidthMm = request.TicketWidthMm == 58 ? 58 : 80,
                        MarginLeft = 2,
                        MarginRight = 2,
                        AutoOpenDrawer = false
                    },
                    PrinterProfile = new PrinterProfile
                    {
                        Id = 1,
                        PrinterName = request.PrinterName == null ? string.Empty : request.PrinterName.Trim(),
                        OutputMode = PrinterOutputMode.WindowsDriver,
                        DrawerCommandHex = string.Empty,
                        DrawerPulseOnMilliseconds = 25,
                        DrawerPulseOffMilliseconds = 250
                    },
                    StationProfile = new StationProfile
                    {
                        Id = 1,
                        StationName = string.IsNullOrWhiteSpace(request.StationName) ? "Caja principal" : request.StationName.Trim(),
                        StationCode = string.IsNullOrWhiteSpace(request.StationCode) ? "CAJA01" : request.StationCode.Trim().ToUpperInvariant()
                    },
                    AdminUser = new UserAccountSeed
                    {
                        Username = request.AdminUsername.Trim().ToLowerInvariant(),
                        DisplayName = string.IsNullOrWhiteSpace(request.AdminDisplayName) ? "Administrador" : request.AdminDisplayName.Trim(),
                        PasswordHashBase64 = passwordHash.HashBase64,
                        PasswordSaltBase64 = passwordHash.SaltBase64,
                        RoleCode = SystemRoles.Administrator
                    },
                    Categories = categories,
                    Products = products
                };

                this.setupRepository.ExecuteInitialSetup(persistenceModel);
                this.logger.Info("Configuración inicial completada correctamente.");
                return OperationResult.Ok("Configuración inicial completada.");
            }
            catch (Exception exception)
            {
                this.logger.Error("Fallo en la configuración inicial.", exception);
                return OperationResult.Fail("No fue posible completar la configuración inicial.");
            }
        }

        private static string NormalizeColor(string colorHex, string fallback)
        {
            return string.IsNullOrWhiteSpace(colorHex) ? fallback : colorHex.Trim().ToUpperInvariant();
        }

        private static string Validate(InitialSetupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.BusinessName))
            {
                return "Debes capturar el nombre del negocio.";
            }

            var templateCode = ResolveTemplateCode(request.BusinessTypeCode);
            if (string.IsNullOrWhiteSpace(templateCode))
            {
                return "Debes seleccionar el tipo de negocio.";
            }

            if (string.Equals(templateCode, "otro", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(request.ManualBusinessTypeName))
            {
                return "Si seleccionas Otro, debes capturar el tipo de negocio manual.";
            }

            if (string.IsNullOrWhiteSpace(request.AdminUsername))
            {
                return "Debes capturar el usuario administrador inicial.";
            }

            if (string.IsNullOrWhiteSpace(request.AdminPassword) || request.AdminPassword.Trim().Length < 6)
            {
                return "La contraseña del administrador debe tener al menos 6 caracteres.";
            }

            if (request.TicketWidthMm != 0 && request.TicketWidthMm != 58 && request.TicketWidthMm != 80)
            {
                return "El ancho del ticket debe ser 58 u 80 mm.";
            }

            var categories = request.Categories == null
                ? new List<SeedCategoryDefinition>()
                : request.Categories.Where(item => item != null && !string.IsNullOrWhiteSpace(item.Name)).ToList();

            if (request.Categories != null && request.Categories.Count > 0 && categories.Count == 0)
            {
                return "Debes capturar al menos una categoría inicial válida.";
            }

            var duplicateCategory = categories
                .GroupBy(item => item.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateCategory != null)
            {
                return "Las categorías iniciales no pueden repetirse.";
            }

            var categoryNames = new HashSet<string>(categories.Select(item => item.Name.Trim()), StringComparer.OrdinalIgnoreCase);
            if (request.Products != null)
            {
                foreach (var product in request.Products.Where(item => item != null && (!string.IsNullOrWhiteSpace(item.Name) || !string.IsNullOrWhiteSpace(item.CategoryName))))
                {
                    if (string.IsNullOrWhiteSpace(product.Name))
                    {
                        return "Cada producto demo debe tener nombre.";
                    }

                    if (string.IsNullOrWhiteSpace(product.CategoryName))
                    {
                        return "Cada producto demo debe indicar su categoría.";
                    }

                    if (categoryNames.Count > 0 && !categoryNames.Contains(product.CategoryName.Trim()))
                    {
                        return "Todos los productos demo deben pertenecer a una categoría existente.";
                    }

                    if (product.SalePrice < 0m || product.EstimatedCost < 0m || product.StockOnHand < 0m)
                    {
                        return "Los importes y existencias iniciales no pueden ser negativos.";
                    }
                }
            }

            return string.Empty;
        }

        private static string ResolveTemplateCode(string businessTypeCode)
        {
            if (string.IsNullOrWhiteSpace(businessTypeCode))
            {
                return string.Empty;
            }

            var separatorIndex = businessTypeCode.IndexOf('|');
            return separatorIndex >= 0 ? businessTypeCode.Substring(0, separatorIndex) : businessTypeCode;
        }

        private static string BuildStoredBusinessTypeCode(InitialSetupRequest request, BusinessTemplateDefinition template)
        {
            if (!string.Equals(template.Code, "otro", StringComparison.OrdinalIgnoreCase))
            {
                return template.Code;
            }

            var manualName = request.ManualBusinessTypeName == null ? string.Empty : request.ManualBusinessTypeName.Trim();
            return string.IsNullOrWhiteSpace(manualName) ? template.Code : template.Code + "|" + manualName;
        }

        private static IList<SeedCategoryDefinition> BuildCategories(InitialSetupRequest request, BusinessTemplateDefinition template)
        {
            var source = request.Categories != null && request.Categories.Count > 0
                ? request.Categories
                : template.SuggestedCategories;

            return source
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Name))
                .Select((item, index) => new SeedCategoryDefinition
                {
                    Name = item.Name.Trim(),
                    SortOrder = item.SortOrder > 0 ? item.SortOrder : index + 1
                })
                .ToList();
        }

        private static IList<SeedProductDefinition> BuildProducts(InitialSetupRequest request, BusinessTemplateDefinition template, IList<SeedCategoryDefinition> categories)
        {
            var validCategories = new HashSet<string>(categories.Select(item => item.Name), StringComparer.OrdinalIgnoreCase);
            var source = request.Products != null && request.Products.Count > 0
                ? request.Products
                : template.SuggestedProducts;

            return source
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(item.CategoryName))
                .Select(item => new SeedProductDefinition
                {
                    CategoryName = item.CategoryName.Trim(),
                    Name = item.Name.Trim(),
                    SalePrice = item.SalePrice,
                    EstimatedCost = item.EstimatedCost,
                    UsesInventory = item.UsesInventory,
                    StockOnHand = item.StockOnHand
                })
                .Where(item => validCategories.Count == 0 || validCategories.Contains(item.CategoryName))
                .ToList();
        }
    }
}
