using System;
using System.Collections.Generic;
using System.Linq;
using FogonDesk.Application.Models;

namespace FogonDesk.Application.Catalogs
{
    public static class BusinessTemplateCatalog
    {
        private static readonly List<BusinessTemplateDefinition> Templates = new List<BusinessTemplateDefinition>
        {
            new BusinessTemplateDefinition
            {
                Code = "tacos_arabes",
                DisplayName = "Tacos árabes",
                SuggestedCategories = new List<SeedCategoryDefinition>
                {
                    new SeedCategoryDefinition { Name = "Tacos árabes", SortOrder = 1 },
                    new SeedCategoryDefinition { Name = "Quesadillas", SortOrder = 2 },
                    new SeedCategoryDefinition { Name = "Bebidas", SortOrder = 3 }
                },
                SuggestedProducts = new List<SeedProductDefinition>
                {
                    new SeedProductDefinition { CategoryName = "Tacos árabes", Name = "Taco árabe clásico", SalePrice = 38m, EstimatedCost = 16m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Tacos árabes", Name = "Taco árabe con queso", SalePrice = 45m, EstimatedCost = 19m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Quesadillas", Name = "Quesadilla especial", SalePrice = 55m, EstimatedCost = 24m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Bebidas", Name = "Agua de sabor", SalePrice = 24m, EstimatedCost = 8m, UsesInventory = true, StockOnHand = 30m }
                }
            },
            new BusinessTemplateDefinition
            {
                Code = "taqueria_mexicana",
                DisplayName = "Taquería mexicana",
                SuggestedCategories = new List<SeedCategoryDefinition>
                {
                    new SeedCategoryDefinition { Name = "Tacos", SortOrder = 1 },
                    new SeedCategoryDefinition { Name = "Tortas", SortOrder = 2 },
                    new SeedCategoryDefinition { Name = "Bebidas", SortOrder = 3 }
                },
                SuggestedProducts = new List<SeedProductDefinition>
                {
                    new SeedProductDefinition { CategoryName = "Tacos", Name = "Taco al pastor", SalePrice = 22m, EstimatedCost = 9m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Tacos", Name = "Taco de suadero", SalePrice = 25m, EstimatedCost = 10m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Tortas", Name = "Torta cubana", SalePrice = 78m, EstimatedCost = 31m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Bebidas", Name = "Refresco 600 ml", SalePrice = 28m, EstimatedCost = 14m, UsesInventory = true, StockOnHand = 24m }
                }
            },
            new BusinessTemplateDefinition
            {
                Code = "pizzeria",
                DisplayName = "Pizzería",
                SuggestedCategories = new List<SeedCategoryDefinition>
                {
                    new SeedCategoryDefinition { Name = "Pizzas", SortOrder = 1 },
                    new SeedCategoryDefinition { Name = "Complementos", SortOrder = 2 },
                    new SeedCategoryDefinition { Name = "Bebidas", SortOrder = 3 }
                },
                SuggestedProducts = new List<SeedProductDefinition>
                {
                    new SeedProductDefinition { CategoryName = "Pizzas", Name = "Pizza pepperoni mediana", SalePrice = 169m, EstimatedCost = 74m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Pizzas", Name = "Pizza hawaiana grande", SalePrice = 219m, EstimatedCost = 98m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Complementos", Name = "Papas gajo", SalePrice = 69m, EstimatedCost = 24m, UsesInventory = true, StockOnHand = 18m },
                    new SeedProductDefinition { CategoryName = "Bebidas", Name = "Refresco 2 litros", SalePrice = 44m, EstimatedCost = 22m, UsesInventory = true, StockOnHand = 10m }
                }
            },
            new BusinessTemplateDefinition
            {
                Code = "hamburguesas",
                DisplayName = "Hamburguesas",
                SuggestedCategories = new List<SeedCategoryDefinition>
                {
                    new SeedCategoryDefinition { Name = "Hamburguesas", SortOrder = 1 },
                    new SeedCategoryDefinition { Name = "Papas", SortOrder = 2 },
                    new SeedCategoryDefinition { Name = "Bebidas", SortOrder = 3 }
                },
                SuggestedProducts = new List<SeedProductDefinition>
                {
                    new SeedProductDefinition { CategoryName = "Hamburguesas", Name = "Hamburguesa clásica", SalePrice = 82m, EstimatedCost = 34m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Hamburguesas", Name = "Hamburguesa doble", SalePrice = 109m, EstimatedCost = 46m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Papas", Name = "Papas a la francesa", SalePrice = 42m, EstimatedCost = 15m, UsesInventory = true, StockOnHand = 25m },
                    new SeedProductDefinition { CategoryName = "Bebidas", Name = "Malteada", SalePrice = 58m, EstimatedCost = 22m, UsesInventory = false, StockOnHand = 0m }
                }
            },
            new BusinessTemplateDefinition
            {
                Code = "cafeteria",
                DisplayName = "Cafetería",
                SuggestedCategories = new List<SeedCategoryDefinition>
                {
                    new SeedCategoryDefinition { Name = "Cafés", SortOrder = 1 },
                    new SeedCategoryDefinition { Name = "Panadería", SortOrder = 2 },
                    new SeedCategoryDefinition { Name = "Fríos", SortOrder = 3 }
                },
                SuggestedProducts = new List<SeedProductDefinition>
                {
                    new SeedProductDefinition { CategoryName = "Cafés", Name = "Americano", SalePrice = 36m, EstimatedCost = 10m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Cafés", Name = "Capuchino", SalePrice = 52m, EstimatedCost = 18m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Panadería", Name = "Cuernito de mantequilla", SalePrice = 24m, EstimatedCost = 9m, UsesInventory = true, StockOnHand = 20m },
                    new SeedProductDefinition { CategoryName = "Fríos", Name = "Frappé moka", SalePrice = 68m, EstimatedCost = 25m, UsesInventory = false, StockOnHand = 0m }
                }
            },
            new BusinessTemplateDefinition
            {
                Code = "otro",
                DisplayName = "Otro",
                SuggestedCategories = new List<SeedCategoryDefinition>
                {
                    new SeedCategoryDefinition { Name = "Platillos", SortOrder = 1 },
                    new SeedCategoryDefinition { Name = "Extras", SortOrder = 2 },
                    new SeedCategoryDefinition { Name = "Bebidas", SortOrder = 3 }
                },
                SuggestedProducts = new List<SeedProductDefinition>
                {
                    new SeedProductDefinition { CategoryName = "Platillos", Name = "Producto demo 1", SalePrice = 50m, EstimatedCost = 20m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Extras", Name = "Extra demo", SalePrice = 15m, EstimatedCost = 6m, UsesInventory = false, StockOnHand = 0m },
                    new SeedProductDefinition { CategoryName = "Bebidas", Name = "Bebida demo", SalePrice = 20m, EstimatedCost = 8m, UsesInventory = true, StockOnHand = 15m }
                }
            }
        };

        public static IList<BusinessTemplateDefinition> GetAvailableTemplates()
        {
            return Templates.Select(Clone).ToList();
        }

        public static BusinessTemplateDefinition GetTemplate(string code)
        {
            var template = Templates.FirstOrDefault(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
            return Clone(template ?? Templates.Last());
        }

        private static BusinessTemplateDefinition Clone(BusinessTemplateDefinition source)
        {
            return new BusinessTemplateDefinition
            {
                Code = source.Code,
                DisplayName = source.DisplayName,
                SuggestedCategories = source.SuggestedCategories
                    .Select(item => new SeedCategoryDefinition
                    {
                        Name = item.Name,
                        SortOrder = item.SortOrder
                    })
                    .ToList(),
                SuggestedProducts = source.SuggestedProducts
                    .Select(item => new SeedProductDefinition
                    {
                        CategoryName = item.CategoryName,
                        Name = item.Name,
                        SalePrice = item.SalePrice,
                        EstimatedCost = item.EstimatedCost,
                        UsesInventory = item.UsesInventory,
                        StockOnHand = item.StockOnHand
                    })
                    .ToList()
            };
        }
    }
}
