using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;
using System.Collections.Generic;
using System;

namespace FogonDesk.Application.Services
{
    public sealed class CatalogApplicationService : ICatalogApplicationService
    {
        private readonly ICatalogRepository repository;

        public CatalogApplicationService(ICatalogRepository repository)
        {
            this.repository = repository;
        }

        public IList<CategoryViewModel> GetCategories()
        {
            return this.repository.LoadCategories();
        }

        public IList<CategoryManagementView> GetCategoriesForManagement()
        {
            return this.repository.LoadCategoriesForManagement();
        }

        public IList<ProductViewModel> GetProductsByCategory(int categoryId)
        {
            return this.repository.LoadProductsByCategory(categoryId);
        }

        public IList<ProductManagementView> GetProductsForManagement()
        {
            return this.repository.LoadProductsForManagement();
        }

        public Common.OperationResult CreateCategory(CreateCategoryRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return Common.OperationResult.Fail("Debes capturar el nombre de la categoría.");
            }

            this.repository.CreateCategory(request, DateTime.UtcNow);
            return Common.OperationResult.Ok("Categoría agregada correctamente.");
        }

        public Common.OperationResult UpdateCategory(UpdateCategoryRequest request)
        {
            if (request == null || request.CategoryId <= 0)
            {
                return Common.OperationResult.Fail("Debes seleccionar una categoría válida.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Common.OperationResult.Fail("Debes capturar el nombre de la categoría.");
            }

            this.repository.UpdateCategory(request);
            return Common.OperationResult.Ok("Categoría actualizada correctamente.");
        }

        public Common.OperationResult DeleteCategory(int categoryId)
        {
            if (categoryId <= 0)
            {
                return Common.OperationResult.Fail("Debes seleccionar una categoría válida.");
            }

            this.repository.DeleteCategory(categoryId);
            return Common.OperationResult.Ok("Categoría eliminada correctamente.");
        }

        public Common.OperationResult CreateProduct(CreateProductRequest request)
        {
            if (request == null)
            {
                return Common.OperationResult.Fail("La captura del producto es obligatoria.");
            }

            if (request.CategoryId <= 0)
            {
                return Common.OperationResult.Fail("Debes seleccionar una categoría válida.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Common.OperationResult.Fail("Debes capturar el nombre del producto.");
            }

            if (request.SalePrice < 0m || request.EstimatedCost < 0m || request.StockOnHand < 0m)
            {
                return Common.OperationResult.Fail("Los importes y existencias no pueden ser negativos.");
            }

            this.repository.CreateProduct(request, DateTime.UtcNow);
            return Common.OperationResult.Ok("Producto agregado correctamente.");
        }

        public Common.OperationResult UpdateProduct(UpdateProductRequest request)
        {
            if (request == null || request.ProductId <= 0)
            {
                return Common.OperationResult.Fail("Debes seleccionar un producto válido.");
            }

            if (request.CategoryId <= 0)
            {
                return Common.OperationResult.Fail("Debes seleccionar una categoría válida.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Common.OperationResult.Fail("Debes capturar el nombre del producto.");
            }

            if (request.SalePrice < 0m || request.EstimatedCost < 0m || request.StockOnHand < 0m)
            {
                return Common.OperationResult.Fail("Los importes y existencias no pueden ser negativos.");
            }

            this.repository.UpdateProduct(request);
            return Common.OperationResult.Ok("Producto actualizado correctamente.");
        }

        public Common.OperationResult DeleteProduct(int productId)
        {
            if (productId <= 0)
            {
                return Common.OperationResult.Fail("Debes seleccionar un producto válido.");
            }

            this.repository.DeleteProduct(productId);
            return Common.OperationResult.Ok("Producto eliminado correctamente.");
        }
    }
}
