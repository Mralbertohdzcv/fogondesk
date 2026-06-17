using System;
using System.Collections.Generic;
using System.Data.SQLite;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Infrastructure.Data
{
    public sealed class SqliteCatalogRepository : ICatalogRepository
    {
        private readonly SqliteConnectionFactory connectionFactory;

        public SqliteCatalogRepository(SqliteConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public IList<CategoryViewModel> LoadCategories()
        {
            var categories = new List<CategoryViewModel>();
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT id, name
FROM categories
WHERE is_active = 1
ORDER BY sort_order ASC, name ASC;";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new CategoryViewModel
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }
            }

            return categories;
        }

        public IList<CategoryManagementView> LoadCategoriesForManagement()
        {
            var categories = new List<CategoryManagementView>();
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT id, name, sort_order, is_active
FROM categories
WHERE is_active = 1
ORDER BY sort_order ASC, name ASC;";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new CategoryManagementView
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            SortOrder = reader.GetInt32(2),
                            IsActive = reader.GetInt32(3) == 1
                        });
                    }
                }
            }

            return categories;
        }

        public IList<ProductViewModel> LoadProductsByCategory(int categoryId)
        {
            var products = new List<ProductViewModel>();
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT id, category_id, name, sale_price, estimated_cost, uses_inventory, stock_on_hand
FROM products
WHERE category_id = @category_id AND is_active = 1
ORDER BY name ASC;";
                command.Parameters.AddWithValue("@category_id", categoryId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new ProductViewModel
                        {
                            Id = reader.GetInt32(0),
                            CategoryId = reader.GetInt32(1),
                            Name = reader.GetString(2),
                            SalePrice = Convert.ToDecimal(reader.GetValue(3)),
                            EstimatedCost = Convert.ToDecimal(reader.GetValue(4)),
                            UsesInventory = Convert.ToInt32(reader.GetValue(5)) == 1,
                            StockOnHand = Convert.ToDecimal(reader.GetValue(6))
                        });
                    }
                }
            }

            return products;
        }

        public IList<ProductManagementView> LoadProductsForManagement()
        {
            var products = new List<ProductManagementView>();
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT p.id, p.category_id, c.name, p.name, p.sale_price, p.estimated_cost, p.uses_inventory, p.stock_on_hand, p.is_active
FROM products p
INNER JOIN categories c ON c.id = p.category_id
WHERE p.is_active = 1 AND c.is_active = 1
ORDER BY c.sort_order ASC, c.name ASC, p.name ASC;";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new ProductManagementView
                        {
                            Id = reader.GetInt32(0),
                            CategoryId = reader.GetInt32(1),
                            CategoryName = reader.GetString(2),
                            Name = reader.GetString(3),
                            SalePrice = Convert.ToDecimal(reader.GetValue(4)),
                            EstimatedCost = Convert.ToDecimal(reader.GetValue(5)),
                            UsesInventory = Convert.ToInt32(reader.GetValue(6)) == 1,
                            StockOnHand = Convert.ToDecimal(reader.GetValue(7)),
                            IsActive = Convert.ToInt32(reader.GetValue(8)) == 1
                        });
                    }
                }
            }

            return products;
        }

        public void CreateCategory(CreateCategoryRequest request, DateTime createdUtc)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"INSERT INTO categories (name, sort_order, is_active, created_utc)
VALUES (@name, @sort_order, 1, @created_utc);";
                command.Parameters.AddWithValue("@name", request.Name.Trim());
                command.Parameters.AddWithValue("@sort_order", request.SortOrder <= 0 ? 1 : request.SortOrder);
                command.Parameters.AddWithValue("@created_utc", createdUtc.ToString("o"));
                command.ExecuteNonQuery();
            }
        }

        public void UpdateCategory(UpdateCategoryRequest request)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE categories
SET name = @name,
    sort_order = @sort_order,
    is_active = @is_active
WHERE id = @id;";
                command.Parameters.AddWithValue("@id", request.CategoryId);
                command.Parameters.AddWithValue("@name", request.Name.Trim());
                command.Parameters.AddWithValue("@sort_order", request.SortOrder <= 0 ? 1 : request.SortOrder);
                command.Parameters.AddWithValue("@is_active", request.IsActive ? 1 : 0);
                command.ExecuteNonQuery();
            }
        }

        public void DeleteCategory(int categoryId)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                ExecuteNonQuery(
                    connection,
                    transaction,
                    "UPDATE categories SET is_active = 0 WHERE id = @id;",
                    new SQLiteParameter("@id", categoryId));

                ExecuteNonQuery(
                    connection,
                    transaction,
                    "UPDATE products SET is_active = 0 WHERE category_id = @category_id;",
                    new SQLiteParameter("@category_id", categoryId));

                transaction.Commit();
            }
        }

        public void CreateProduct(CreateProductRequest request, DateTime createdUtc)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"INSERT INTO products (category_id, sku, name, sale_price, estimated_cost, uses_inventory, stock_on_hand, is_active, created_utc)
VALUES (@category_id, '', @name, @sale_price, @estimated_cost, @uses_inventory, @stock_on_hand, 1, @created_utc);";
                command.Parameters.AddWithValue("@category_id", request.CategoryId);
                command.Parameters.AddWithValue("@name", request.Name.Trim());
                command.Parameters.AddWithValue("@sale_price", request.SalePrice);
                command.Parameters.AddWithValue("@estimated_cost", request.EstimatedCost);
                command.Parameters.AddWithValue("@uses_inventory", request.UsesInventory ? 1 : 0);
                command.Parameters.AddWithValue("@stock_on_hand", request.StockOnHand);
                command.Parameters.AddWithValue("@created_utc", createdUtc.ToString("o"));
                command.ExecuteNonQuery();
            }
        }

        public void UpdateProduct(UpdateProductRequest request)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE products
SET category_id = @category_id,
    name = @name,
    sale_price = @sale_price,
    estimated_cost = @estimated_cost,
    uses_inventory = @uses_inventory,
    stock_on_hand = @stock_on_hand,
    is_active = @is_active
WHERE id = @id;";
                command.Parameters.AddWithValue("@id", request.ProductId);
                command.Parameters.AddWithValue("@category_id", request.CategoryId);
                command.Parameters.AddWithValue("@name", request.Name.Trim());
                command.Parameters.AddWithValue("@sale_price", request.SalePrice);
                command.Parameters.AddWithValue("@estimated_cost", request.EstimatedCost);
                command.Parameters.AddWithValue("@uses_inventory", request.UsesInventory ? 1 : 0);
                command.Parameters.AddWithValue("@stock_on_hand", request.StockOnHand);
                command.Parameters.AddWithValue("@is_active", request.IsActive ? 1 : 0);
                command.ExecuteNonQuery();
            }
        }

        public void DeleteProduct(int productId)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE products
SET is_active = 0
WHERE id = @id;";
                command.Parameters.AddWithValue("@id", productId);
                command.ExecuteNonQuery();
            }
        }

        private static void ExecuteNonQuery(SQLiteConnection connection, SQLiteTransaction transaction, string sql, params SQLiteParameter[] parameters)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = sql;
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }

                command.ExecuteNonQuery();
            }
        }
    }
}
