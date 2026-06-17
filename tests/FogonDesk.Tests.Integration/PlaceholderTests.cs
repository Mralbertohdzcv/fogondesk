using System;
using System.IO;
using System.Linq;
using FogonDesk.Application.Services;
using FogonDesk.Application.Models;
using FogonDesk.Configuration;
using FogonDesk.Infrastructure.Data;
using FogonDesk.Infrastructure.Platform;
using FogonDesk.Infrastructure.Security;
using Xunit;

namespace FogonDesk.Tests.Integration
{
    public sealed class PlaceholderTests
    {
        [Fact]
        public void BootstrapYSetupInicialDebenCrearSistemaOperativoLocal()
        {
            var rootPath = Path.Combine(Path.GetTempPath(), "FogonDeskTests", Guid.NewGuid().ToString("N"));
            var paths = new StationPaths(rootPath);
            StationPathsFactory.EnsureCreated(paths);

            try
            {
                var clock = new SystemClock();
                var logger = new FileLogger(paths);
                var connectionFactory = new SqliteConnectionFactory(paths);
                var bootstrapper = new SqliteDatabaseBootstrapper(connectionFactory, paths, clock, logger);
                var setupRepository = new SqliteSetupRepository(connectionFactory, clock);
                var userRepository = new SqliteUserRepository(connectionFactory);
                var catalogRepository = new SqliteCatalogRepository(connectionFactory);
                var salesRepository = new SqliteSalesRepository(connectionFactory);
                var passwordHasher = new Pbkdf2PasswordHasher();
                var setupService = new InitialSetupService(setupRepository, passwordHasher, clock, logger);
                var authService = new AuthenticationService(userRepository, passwordHasher, clock, logger);
                var catalogService = new CatalogApplicationService(catalogRepository);
                var salesService = new OperationalSalesApplicationService(salesRepository, clock, logger);

                bootstrapper.EnsureDatabaseReady();

                var setupResult = setupService.Execute(new InitialSetupRequest
                {
                    BusinessName = "Cocina Demo",
                    BusinessTypeCode = "cafeteria",
                    Slogan = "Pruebas locales",
                    Address = "Calle 1",
                    Phone = "5550000000",
                    PrimaryColorHex = "#1B4332",
                    AccentColorHex = "#F4A261",
                    TicketWidthMm = 80,
                    HeaderText = "Cocina Demo",
                    FooterText = "Gracias",
                    StationName = "Caja principal",
                    StationCode = "CAJA01",
                    AdminUsername = "admin",
                    AdminDisplayName = "Administrador",
                    AdminPassword = "Cambio123",
                    Categories = new[]
                    {
                        new SeedCategoryDefinition { Name = "Bebidas especiales", SortOrder = 1 },
                        new SeedCategoryDefinition { Name = "Postres caseros", SortOrder = 2 }
                    },
                    Products = new[]
                    {
                        new SeedProductDefinition
                        {
                            CategoryName = "Bebidas especiales",
                            Name = "Limonada mineral",
                            SalePrice = 32m,
                            EstimatedCost = 11m,
                            UsesInventory = true,
                            StockOnHand = 18m
                        },
                        new SeedProductDefinition
                        {
                            CategoryName = "Postres caseros",
                            Name = "Pay de queso",
                            SalePrice = 48m,
                            EstimatedCost = 19m,
                            UsesInventory = false,
                            StockOnHand = 0m
                        }
                    }
                });

                Assert.True(setupResult.Success);

                var startupState = setupRepository.LoadStartupState();
                Assert.True(startupState.IsConfigured);
                Assert.Equal("Cocina Demo", startupState.BusinessName);

                var authResult = authService.Authenticate(new AuthenticationRequest
                {
                    Username = "admin",
                    Password = "Cambio123"
                });

                Assert.True(authResult.Success);
                Assert.Equal("administrador", authResult.Data.RoleCode);

                var categories = catalogService.GetCategories();
                Assert.NotEmpty(categories);
                Assert.Contains(categories, item => item.Name == "Bebidas especiales");
                Assert.Contains(categories, item => item.Name == "Postres caseros");

                var stockedCategory = categories.First(item => item.Name == "Bebidas especiales");
                var stockedProduct = catalogService.GetProductsByCategory(stockedCategory.Id).FirstOrDefault(item => item.Name == "Limonada mineral");

                Assert.NotNull(stockedProduct);
                var previousStock = stockedProduct.StockOnHand;

                var saleResult = salesService.RegisterSale(new CreateSaleRequest
                {
                    UserId = authResult.Data.UserId,
                    UserName = authResult.Data.Username,
                    OrderKind = FogonDesk.Domain.Common.OrderKind.Mostrador,
                    PaymentMethod = FogonDesk.Domain.Common.PaymentMethod.Efectivo,
                    Note = string.Empty,
                    Items = new[]
                    {
                        new SaleLineDraft
                        {
                            ProductId = stockedProduct.Id,
                            ProductName = stockedProduct.Name,
                            Quantity = 2,
                            UnitPrice = stockedProduct.SalePrice,
                            EstimatedCost = stockedProduct.EstimatedCost,
                            UsesInventory = true
                        }
                    }
                });

                Assert.True(saleResult.Success);
                Assert.StartsWith("VTA-", saleResult.Data.Folio);

                var refreshedProduct = catalogService.GetProductsByCategory(stockedCategory.Id).First(item => item.Id == stockedProduct.Id);
                Assert.Equal(previousStock - 2m, refreshedProduct.StockOnHand);
            }
            finally
            {
                System.Data.SQLite.SQLiteConnection.ClearAllPools();
                if (Directory.Exists(rootPath))
                {
                    Directory.Delete(rootPath, true);
                }
            }
        }
    }
}
