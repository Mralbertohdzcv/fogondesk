using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;
using FogonDesk.Domain.Common;

namespace FogonDesk.Desktop
{
    public sealed class AdministrationForm : Form
    {
        private readonly ICatalogApplicationService catalogApplicationService;
        private readonly IUserAdministrationService userAdministrationService;
        private readonly DataGridView categoriesGrid;
        private readonly DataGridView productsGrid;
        private readonly DataGridView usersGrid;
        private readonly ComboBox productCategoryCombo;
        private readonly ComboBox productCategoryFilterCombo;
        private readonly ComboBox userRoleCombo;
        private readonly TextBox productNameTextBox;
        private readonly NumericUpDown productSalePriceInput;
        private readonly NumericUpDown productCostInput;
        private readonly NumericUpDown productStockInput;
        private readonly CheckBox productUsesInventoryCheckBox;
        private readonly CheckBox productIsActiveCheckBox;
        private readonly Button productSaveButton;
        private readonly Button productDeleteButton;
        private readonly Button productClearButton;
        private readonly Label productTitleLabel;
        private readonly TextBox userNameTextBox;
        private readonly TextBox userDisplayNameTextBox;
        private readonly TextBox userPasswordTextBox;
        private readonly CheckBox userIsActiveCheckBox;
        private readonly Button userSaveButton;
        private readonly Button userDeleteButton;
        private readonly Button userClearButton;
        private readonly Label userTitleLabel;
        private readonly Label userPasswordHintLabel;
        private List<CategoryManagementView> categoriesCache;
        private List<ProductManagementView> productsCache;
        private List<UserManagementView> usersCache;
        private int? selectedProductId;
        private int? selectedProductCategoryId;
        private int? selectedUserId;
        private int selectedProductCategoryFilterId;
        private bool refreshingData;

        public AdministrationForm(ICatalogApplicationService catalogApplicationService, IUserAdministrationService userAdministrationService)
        {
            this.catalogApplicationService = catalogApplicationService;
            this.userAdministrationService = userAdministrationService;

            Text = "Administración";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1200, 780);
            BackColor = Color.FromArgb(242, 245, 248);
            Font = new Font("Segoe UI", 10F);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.White,
                Padding = new Padding(24, 16, 24, 16)
            };
            header.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Administración de catálogo y usuarios",
                Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });

            this.categoriesGrid = CreateReadOnlyGrid();
            this.productsGrid = CreateReadOnlyGrid();
            this.usersGrid = CreateReadOnlyGrid();
            ConfigureCategoriesGrid();
            ConfigureProductsGrid();
            ConfigureUsersGrid();

            this.productCategoryCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 10, 2) };
            this.productNameTextBox = CreateTextBox();
            this.productSalePriceInput = CreateNumericInput(0, 1000000, 2);
            this.productCostInput = CreateNumericInput(0, 1000000, 2);
            this.productStockInput = CreateNumericInput(0, 1000000, 2);
            this.productUsesInventoryCheckBox = new CheckBox { Text = "Usa inventario", AutoSize = true, Checked = false, Margin = new Padding(0, 6, 0, 0) };
            this.productIsActiveCheckBox = new CheckBox { Text = "Activo", AutoSize = true, Checked = true, Margin = new Padding(0, 6, 0, 0) };
            this.productSaveButton = CreatePrimaryButton("Agregar producto");
            this.productSaveButton.Click += SaveProductClick;
            this.productDeleteButton = CreateDangerButton("Eliminar");
            this.productDeleteButton.Click += DeleteProductClick;
            this.productClearButton = CreateSecondaryButton("Nuevo");
            this.productClearButton.Click += ClearProductClick;
            this.productTitleLabel = CreateEditorTitle("Producto");

            this.productCategoryFilterCombo = new ComboBox { Dock = DockStyle.Left, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(8, 6, 0, 0) };
            this.productCategoryFilterCombo.DisplayMember = "Name";
            this.productCategoryFilterCombo.ValueMember = "CategoryId";
            this.productCategoryFilterCombo.SelectedIndexChanged += ProductCategoryFilterChanged;

            this.userNameTextBox = CreateTextBox();
            this.userDisplayNameTextBox = CreateTextBox();
            this.userPasswordTextBox = CreateTextBox();
            this.userPasswordTextBox.UseSystemPasswordChar = true;
            this.userPasswordHintLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Deja la contraseña en blanco al editar para conservar la actual.",
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.userRoleCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 10, 2) };
            this.userRoleCombo.Items.AddRange(SystemRoles.All.Cast<object>().ToArray());
            if (this.userRoleCombo.Items.Count > 0)
            {
                this.userRoleCombo.SelectedItem = SystemRoles.Cashier;
            }
            this.userIsActiveCheckBox = new CheckBox { Text = "Activo", AutoSize = true, Checked = true, Margin = new Padding(0, 6, 0, 0) };
            this.userSaveButton = CreatePrimaryButton("Agregar usuario");
            this.userSaveButton.Click += SaveUserClick;
            this.userDeleteButton = CreateDangerButton("Eliminar");
            this.userDeleteButton.Click += DeleteUserClick;
            this.userClearButton = CreateSecondaryButton("Nuevo");
            this.userClearButton.Click += ClearUserClick;
            this.userTitleLabel = CreateEditorTitle("Usuario");

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(14, 6)
            };
            tabs.TabPages.Add(CreateCategoriesTab());
            tabs.TabPages.Add(CreateProductsTab());
            tabs.TabPages.Add(CreateUsersTab());

            Controls.Add(tabs);
            Controls.Add(header);
            Load += delegate { ReloadData(); };
        }

        private TabPage CreateCategoriesTab()
        {
            var page = new TabPage("Categorías") { BackColor = BackColor };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(18)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(CreateGridCard("Categorías registradas", this.categoriesGrid), 0, 0);
            layout.Controls.Add(CreateActionBar("Nuevo", "Editar", "Eliminar", CreateCategoryClick, EditCategoryClick, DeleteCategoryClick), 0, 1);
            page.Controls.Add(layout);
            return page;
        }

        private TabPage CreateProductsTab()
        {
            var page = new TabPage("Productos") { BackColor = BackColor };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(18)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(CreateProductsBrowser(), 0, 0);
            layout.Controls.Add(CreateGridCard("Productos registrados", this.productsGrid), 0, 1);
            layout.Controls.Add(CreateActionBar("Nuevo", "Editar", "Eliminar", CreateProductClick, EditProductClick, DeleteProductClick), 0, 2);
            page.Controls.Add(layout);
            return page;
        }

        private TabPage CreateUsersTab()
        {
            var page = new TabPage("Usuarios") { BackColor = BackColor };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(18)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(CreateGridCard("Usuarios del sistema", this.usersGrid), 0, 0);
            layout.Controls.Add(CreateActionBar("Nuevo", "Editar", "Eliminar", CreateUserClick, EditUserClick, DeleteUserClick), 0, 1);
            page.Controls.Add(layout);
            return page;
        }

        private Control CreateProductsBrowser()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16) };

            var toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 10)
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            toolbar.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Filtrar categorías",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Padding = new Padding(0, 2, 12, 0)
            }, 0, 0);
            toolbar.Controls.Add(this.productCategoryFilterCombo, 1, 0);
            toolbar.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Los productos se muestran ordenados por categoría y puedes editarlos desde la fila seleccionada.",
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Padding = new Padding(0, 6, 0, 0)
            }, 0, 1);
            toolbar.SetColumnSpan(toolbar.Controls[2], 2);

            var gridCard = CreateGridCard("Productos registrados", this.productsGrid);
            gridCard.Dock = DockStyle.Fill;

            panel.Controls.Add(gridCard);
            panel.Controls.Add(toolbar);
            return panel;
        }

        private static Panel CreateGridCard(string title, Control content)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Controls.Add(new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            content.Dock = DockStyle.Fill;
            layout.Controls.Add(content, 0, 1);
            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildProductEditor()
        {
            var layout = CreateEditorGrid(4, 4);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));

            layout.Controls.Add(CreateFieldLabel("Categoría"), 0, 0);
            layout.Controls.Add(CreateFieldLabel("Producto"), 1, 0);
            layout.Controls.Add(CreateFieldLabel("Venta"), 2, 0);
            layout.Controls.Add(CreateFieldLabel("Costo"), 3, 0);
            layout.Controls.Add(this.productCategoryCombo, 0, 1);
            layout.SetColumnSpan(this.productCategoryCombo, 1);
            layout.Controls.Add(this.productNameTextBox, 1, 1);
            layout.Controls.Add(this.productSalePriceInput, 2, 1);
            layout.Controls.Add(this.productCostInput, 3, 1);

            layout.Controls.Add(CreateFieldLabel("Existencia"), 0, 2);
            layout.Controls.Add(CreateFieldLabel("Inventario"), 1, 2);
            layout.Controls.Add(CreateFieldLabel("Estado"), 2, 2);
            layout.Controls.Add(this.productStockInput, 0, 3);
            layout.Controls.Add(this.productUsesInventoryCheckBox, 1, 3);
            layout.Controls.Add(this.productIsActiveCheckBox, 2, 3);

            var buttons = CreateButtonRow();
            buttons.Controls.Add(this.productSaveButton);
            buttons.Controls.Add(this.productDeleteButton);
            buttons.Controls.Add(this.productClearButton);
            layout.Controls.Add(buttons, 0, 4);
            layout.SetColumnSpan(buttons, 4);
            return layout;
        }

        private Control BuildUserEditor()
        {
            var layout = CreateEditorGrid(4, 4);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));

            layout.Controls.Add(CreateFieldLabel("Usuario"), 0, 0);
            layout.Controls.Add(CreateFieldLabel("Nombre visible"), 1, 0);
            layout.Controls.Add(CreateFieldLabel("Rol"), 2, 0);
            layout.Controls.Add(CreateFieldLabel("Estado"), 3, 0);
            layout.Controls.Add(this.userNameTextBox, 0, 1);
            layout.Controls.Add(this.userDisplayNameTextBox, 1, 1);
            layout.Controls.Add(this.userRoleCombo, 2, 1);
            layout.Controls.Add(this.userIsActiveCheckBox, 3, 1);

            layout.Controls.Add(CreateFieldLabel("Contraseña"), 0, 2);
            layout.Controls.Add(this.userPasswordHintLabel, 1, 2);
            layout.SetColumnSpan(this.userPasswordHintLabel, 3);
            layout.Controls.Add(this.userPasswordTextBox, 0, 3);
            layout.SetColumnSpan(this.userPasswordTextBox, 2);

            var buttons = CreateButtonRow();
            buttons.Controls.Add(this.userSaveButton);
            buttons.Controls.Add(this.userDeleteButton);
            buttons.Controls.Add(this.userClearButton);
            layout.Controls.Add(buttons, 0, 4);
            layout.SetColumnSpan(buttons, 4);
            return layout;
        }

        private static TableLayoutPanel CreateEditorGrid(int columns, int rows)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = columns,
                RowCount = rows,
                Margin = new Padding(0)
            };

            return layout;
        }

        private static FlowLayoutPanel CreateButtonRow()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 8, 0, 0)
            };
        }

        private static FlowLayoutPanel CreateActionBar(string newText, string editText, string deleteText, EventHandler newHandler, EventHandler editHandler, EventHandler deleteHandler)
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 12, 0, 0),
                Padding = new Padding(0)
            };

            var newButton = CreatePrimaryButton(newText);
            newButton.Click += newHandler;
            var editButton = CreateSecondaryButton(editText);
            editButton.Click += editHandler;
            var deleteButton = CreateDangerButton(deleteText);
            deleteButton.Click += deleteHandler;

            bar.Controls.Add(newButton);
            bar.Controls.Add(editButton);
            bar.Controls.Add(deleteButton);
            return bar;
        }

        private void SaveProductClick(object sender, EventArgs eventArgs)
        {
            var category = this.productCategoryCombo.SelectedItem as CategoryManagementView;
            if (category == null)
            {
                ShowResult(OperationResult.Fail("Debes seleccionar una categoría válida."), "Productos");
                return;
            }

            OperationResult result;
            if (this.selectedProductId.HasValue)
            {
                result = this.catalogApplicationService.UpdateProduct(new UpdateProductRequest
                {
                    ProductId = this.selectedProductId.Value,
                    CategoryId = category.Id,
                    Name = this.productNameTextBox.Text,
                    SalePrice = this.productSalePriceInput.Value,
                    EstimatedCost = this.productCostInput.Value,
                    UsesInventory = this.productUsesInventoryCheckBox.Checked,
                    StockOnHand = this.productStockInput.Value,
                    IsActive = this.productIsActiveCheckBox.Checked
                });
            }
            else
            {
                result = this.catalogApplicationService.CreateProduct(new CreateProductRequest
                {
                    CategoryId = category.Id,
                    Name = this.productNameTextBox.Text,
                    SalePrice = this.productSalePriceInput.Value,
                    EstimatedCost = this.productCostInput.Value,
                    UsesInventory = this.productUsesInventoryCheckBox.Checked,
                    StockOnHand = this.productStockInput.Value
                });
            }

            ShowResult(result, "Productos");
            if (result.Success)
            {
                ClearProductEditor();
                ReloadData();
            }
        }

        private void DeleteProductActionClick(object sender, EventArgs eventArgs)
        {
            if (!this.selectedProductId.HasValue)
            {
                return;
            }

            var result = this.catalogApplicationService.DeleteProduct(this.selectedProductId.Value);
            ShowResult(result, "Productos");
            if (result.Success)
            {
                ClearProductEditor();
                ReloadData();
            }
        }

        private void ClearProductClick(object sender, EventArgs eventArgs)
        {
            ClearProductEditor();
        }

        private void SaveUserClick(object sender, EventArgs eventArgs)
        {
            var roleCode = this.userRoleCombo.SelectedItem == null ? SystemRoles.Cashier : this.userRoleCombo.SelectedItem.ToString();
            OperationResult result;
            if (this.selectedUserId.HasValue)
            {
                result = this.userAdministrationService.UpdateUser(new UpdateUserRequest
                {
                    UserId = this.selectedUserId.Value,
                    Username = this.userNameTextBox.Text,
                    DisplayName = this.userDisplayNameTextBox.Text,
                    RoleCode = roleCode,
                    Password = this.userPasswordTextBox.Text,
                    IsActive = this.userIsActiveCheckBox.Checked
                });
            }
            else
            {
                result = this.userAdministrationService.CreateUser(new CreateUserRequest
                {
                    Username = this.userNameTextBox.Text,
                    DisplayName = this.userDisplayNameTextBox.Text,
                    RoleCode = roleCode,
                    Password = this.userPasswordTextBox.Text,
                    IsActive = this.userIsActiveCheckBox.Checked
                });
            }

            ShowResult(result, "Usuarios");
            if (result.Success)
            {
                ClearUserEditor();
                ReloadData();
            }
        }

        private void DeleteUserActionClick(object sender, EventArgs eventArgs)
        {
            if (!this.selectedUserId.HasValue)
            {
                return;
            }

            var result = this.userAdministrationService.DeleteUser(this.selectedUserId.Value);
            ShowResult(result, "Usuarios");
            if (result.Success)
            {
                ClearUserEditor();
                ReloadData();
            }
        }

        private void ClearUserClick(object sender, EventArgs eventArgs)
        {
            ClearUserEditor();
        }

        private void ProductCategoryFilterChanged(object sender, EventArgs eventArgs)
        {
            if (this.refreshingData)
            {
                return;
            }

            var selectedValue = this.productCategoryFilterCombo.SelectedValue;
            this.selectedProductCategoryFilterId = selectedValue == null ? 0 : Convert.ToInt32(selectedValue);
            RefreshProductsGrid();
        }

        private void ReloadData()
        {
            this.refreshingData = true;
            try
            {
                this.categoriesCache = this.catalogApplicationService.GetCategoriesForManagement().ToList();
                this.productsCache = this.catalogApplicationService.GetProductsForManagement().ToList();
                this.usersCache = this.userAdministrationService.GetUsers().ToList();

                BindCategoryData();
                BindProductFilterData();
                RefreshProductsGrid();
                BindUserData();

                ClearProductEditor();
                ClearUserEditor();
            }
            finally
            {
                this.refreshingData = false;
            }
        }

        private void BindCategoryData()
        {
            this.categoriesGrid.DataSource = null;
            this.categoriesGrid.DataSource = this.categoriesCache;
        }

        private void BindProductFilterData()
        {
            var items = new List<CategoryFilterItem>
            {
                new CategoryFilterItem { CategoryId = 0, Name = "Todas las categorías" }
            };

            foreach (var category in this.categoriesCache)
            {
                items.Add(new CategoryFilterItem { CategoryId = category.Id, Name = category.Name });
            }

            this.productCategoryFilterCombo.DataSource = null;
            this.productCategoryFilterCombo.DataSource = items;
            this.productCategoryFilterCombo.DisplayMember = "Name";
            this.productCategoryFilterCombo.ValueMember = "CategoryId";

            if (this.selectedProductCategoryFilterId > 0)
            {
                this.productCategoryFilterCombo.SelectedValue = this.selectedProductCategoryFilterId;
            }
            else
            {
                this.productCategoryFilterCombo.SelectedValue = 0;
            }

            this.productCategoryCombo.DataSource = null;
            this.productCategoryCombo.DataSource = this.categoriesCache;
            this.productCategoryCombo.DisplayMember = "Name";
            this.productCategoryCombo.ValueMember = "Id";

            if (this.selectedProductCategoryId.HasValue)
            {
                this.productCategoryCombo.SelectedValue = this.selectedProductCategoryId.Value;
            }
            else if (this.categoriesCache.Count > 0)
            {
                this.selectedProductCategoryId = this.categoriesCache[0].Id;
                this.productCategoryCombo.SelectedValue = this.selectedProductCategoryId.Value;
            }
        }

        private void BindUserData()
        {
            this.usersGrid.DataSource = null;
            this.usersGrid.DataSource = this.usersCache;
            UpdateUserEditorState();
        }

        private void RefreshProductsGrid()
        {
            IEnumerable<ProductManagementView> items = this.productsCache;
            if (this.selectedProductCategoryFilterId > 0)
            {
                items = items.Where(item => item.CategoryId == this.selectedProductCategoryFilterId);
            }

            this.productsGrid.DataSource = null;
            this.productsGrid.DataSource = items.ToList();
            UpdateProductEditorState();
        }

        private void ConfigureCategoriesGrid()
        {
            this.categoriesGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Nombre", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            this.categoriesGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SortOrder", HeaderText = "Orden", Width = 70 });
            this.categoriesGrid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "Activo", Width = 70 });
            this.categoriesGrid.CellDoubleClick += CategoriesGridCellDoubleClick;
        }

        private void ConfigureProductsGrid()
        {
            this.productsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CategoryName", HeaderText = "Categoría", Width = 150 });
            this.productsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Producto", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            this.productsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SalePrice", HeaderText = "Venta", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            this.productsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EstimatedCost", HeaderText = "Costo", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            this.productsGrid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "UsesInventory", HeaderText = "Inventario", Width = 80 });
            this.productsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StockOnHand", HeaderText = "Existencia", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            this.productsGrid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "Activo", Width = 70 });
            this.productsGrid.CellDoubleClick += ProductsGridCellDoubleClick;
        }

        private void ConfigureUsersGrid()
        {
            this.usersGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Username", HeaderText = "Usuario", Width = 140 });
            this.usersGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DisplayName", HeaderText = "Nombre visible", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            this.usersGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RoleCode", HeaderText = "Rol", Width = 120 });
            this.usersGrid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "Activo", Width = 70 });
            this.usersGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LastLoginUtc", HeaderText = "Último acceso", Width = 160, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
            this.usersGrid.CellDoubleClick += UsersGridCellDoubleClick;
        }

        private void CategoriesGridCellDoubleClick(object sender, DataGridViewCellEventArgs eventArgs)
        {
            if (eventArgs.RowIndex < 0)
            {
                return;
            }

            EditCategoryClick(sender, EventArgs.Empty);
        }

        private void ProductsGridCellDoubleClick(object sender, DataGridViewCellEventArgs eventArgs)
        {
            if (eventArgs.RowIndex < 0)
            {
                return;
            }

            EditProductClick(sender, EventArgs.Empty);
        }

        private void UsersGridCellDoubleClick(object sender, DataGridViewCellEventArgs eventArgs)
        {
            if (eventArgs.RowIndex < 0)
            {
                return;
            }

            EditUserClick(sender, EventArgs.Empty);
        }

        private void CreateCategoryClick(object sender, EventArgs eventArgs)
        {
            var data = ShowCategoryDialog(null);
            if (data == null)
            {
                return;
            }

            var result = this.catalogApplicationService.CreateCategory(new CreateCategoryRequest
            {
                Name = data.Name,
                SortOrder = data.SortOrder
            });
            ShowResult(result, "Categorías");
            if (result.Success)
            {
                ReloadData();
            }
        }

        private void EditCategoryClick(object sender, EventArgs eventArgs)
        {
            var selected = GetSelectedRowData<CategoryManagementView>(this.categoriesGrid);
            if (selected == null)
            {
                MessageBox.Show("Selecciona una categoría de la lista.", "Categorías", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var data = ShowCategoryDialog(selected);
            if (data == null)
            {
                return;
            }

            var result = this.catalogApplicationService.UpdateCategory(new UpdateCategoryRequest
            {
                CategoryId = selected.Id,
                Name = data.Name,
                SortOrder = data.SortOrder,
                IsActive = data.IsActive
            });
            ShowResult(result, "Categorías");
            if (result.Success)
            {
                ReloadData();
            }
        }

        private void DeleteCategoryClick(object sender, EventArgs eventArgs)
        {
            var selected = GetSelectedRowData<CategoryManagementView>(this.categoriesGrid);
            if (selected == null)
            {
                MessageBox.Show("Selecciona una categoría de la lista.", "Categorías", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("¿Eliminar la categoría seleccionada?\nLos productos de esa categoría también se desactivarán.", "Categorías", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            var result = this.catalogApplicationService.DeleteCategory(selected.Id);
            ShowResult(result, "Categorías");
            if (result.Success)
            {
                ReloadData();
            }
        }

        private void CreateProductClick(object sender, EventArgs eventArgs)
        {
            var data = ShowProductDialog(null);
            if (data == null)
            {
                return;
            }

            var result = this.catalogApplicationService.CreateProduct(new CreateProductRequest
            {
                CategoryId = data.CategoryId,
                Name = data.Name,
                SalePrice = data.SalePrice,
                EstimatedCost = data.EstimatedCost,
                UsesInventory = data.UsesInventory,
                StockOnHand = data.StockOnHand
            });
            ShowResult(result, "Productos");
            if (result.Success)
            {
                this.selectedProductCategoryId = data.CategoryId;
                ReloadData();
            }
        }

        private void EditProductClick(object sender, EventArgs eventArgs)
        {
            var selected = GetSelectedRowData<ProductManagementView>(this.productsGrid);
            if (selected == null)
            {
                MessageBox.Show("Selecciona un producto de la lista.", "Productos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var data = ShowProductDialog(selected);
            if (data == null)
            {
                return;
            }

            var result = this.catalogApplicationService.UpdateProduct(new UpdateProductRequest
            {
                ProductId = selected.Id,
                CategoryId = data.CategoryId,
                Name = data.Name,
                SalePrice = data.SalePrice,
                EstimatedCost = data.EstimatedCost,
                UsesInventory = data.UsesInventory,
                StockOnHand = data.StockOnHand,
                IsActive = data.IsActive
            });
            ShowResult(result, "Productos");
            if (result.Success)
            {
                this.selectedProductCategoryId = data.CategoryId;
                this.selectedProductCategoryFilterId = data.CategoryId;
                ReloadData();
            }
        }

        private void DeleteProductClick(object sender, EventArgs eventArgs)
        {
            var selected = GetSelectedRowData<ProductManagementView>(this.productsGrid);
            if (selected == null)
            {
                MessageBox.Show("Selecciona un producto de la lista.", "Productos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("¿Eliminar el producto seleccionado?", "Productos", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            var result = this.catalogApplicationService.DeleteProduct(selected.Id);
            ShowResult(result, "Productos");
            if (result.Success)
            {
                ReloadData();
            }
        }

        private void CreateUserClick(object sender, EventArgs eventArgs)
        {
            var data = ShowUserDialog(null);
            if (data == null)
            {
                return;
            }

            var result = this.userAdministrationService.CreateUser(new CreateUserRequest
            {
                Username = data.Username,
                DisplayName = data.DisplayName,
                RoleCode = data.RoleCode,
                Password = data.Password,
                IsActive = data.IsActive
            });
            ShowResult(result, "Usuarios");
            if (result.Success)
            {
                ReloadData();
            }
        }

        private void EditUserClick(object sender, EventArgs eventArgs)
        {
            var selected = GetSelectedRowData<UserManagementView>(this.usersGrid);
            if (selected == null)
            {
                MessageBox.Show("Selecciona un usuario de la lista.", "Usuarios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var data = ShowUserDialog(selected);
            if (data == null)
            {
                return;
            }

            var result = this.userAdministrationService.UpdateUser(new UpdateUserRequest
            {
                UserId = selected.UserId,
                Username = data.Username,
                DisplayName = data.DisplayName,
                RoleCode = data.RoleCode,
                Password = data.Password,
                IsActive = data.IsActive
            });
            ShowResult(result, "Usuarios");
            if (result.Success)
            {
                ReloadData();
            }
        }

        private void DeleteUserClick(object sender, EventArgs eventArgs)
        {
            var selected = GetSelectedRowData<UserManagementView>(this.usersGrid);
            if (selected == null)
            {
                MessageBox.Show("Selecciona un usuario de la lista.", "Usuarios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("¿Eliminar el usuario seleccionado?", "Usuarios", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            var result = this.userAdministrationService.DeleteUser(selected.UserId);
            ShowResult(result, "Usuarios");
            if (result.Success)
            {
                ReloadData();
            }
        }

        private void ProductsGridSelectionChanged(object sender, EventArgs eventArgs)
        {
            if (this.refreshingData)
            {
                return;
            }

            var selected = GetSelectedRowData<ProductManagementView>(this.productsGrid);
            if (selected == null)
            {
                ClearProductEditor();
                return;
            }

            this.selectedProductId = selected.Id;
            this.productNameTextBox.Text = selected.Name;
            this.productSalePriceInput.Value = selected.SalePrice;
            this.productCostInput.Value = selected.EstimatedCost;
            this.productStockInput.Value = selected.StockOnHand;
            this.productUsesInventoryCheckBox.Checked = selected.UsesInventory;
            this.productIsActiveCheckBox.Checked = selected.IsActive;

            var category = this.categoriesCache != null ? this.categoriesCache.FirstOrDefault(item => item.Id == selected.CategoryId) : null;
            if (category != null)
            {
                this.productCategoryCombo.SelectedItem = category;
            }

            UpdateProductEditorState();
        }

        private void UsersGridSelectionChanged(object sender, EventArgs eventArgs)
        {
            if (this.refreshingData)
            {
                return;
            }

            var selected = GetSelectedRowData<UserManagementView>(this.usersGrid);
            if (selected == null)
            {
                ClearUserEditor();
                return;
            }

            this.selectedUserId = selected.UserId;
            this.userNameTextBox.Text = selected.Username;
            this.userDisplayNameTextBox.Text = selected.DisplayName;
            this.userPasswordTextBox.Clear();
            this.userIsActiveCheckBox.Checked = selected.IsActive;
            if (this.userRoleCombo.Items.Contains(selected.RoleCode))
            {
                this.userRoleCombo.SelectedItem = selected.RoleCode;
            }

            UpdateUserEditorState();
        }

        private void ClearProductEditor(int? categoryId = null)
        {
            this.selectedProductId = null;
            this.productTitleLabel.Text = "Producto";
            this.productNameTextBox.Clear();
            this.productSalePriceInput.Value = 0;
            this.productCostInput.Value = 0;
            this.productStockInput.Value = 0;
            this.productUsesInventoryCheckBox.Checked = false;
            this.productIsActiveCheckBox.Checked = true;

            if (categoryId.HasValue)
            {
                this.selectedProductCategoryId = categoryId;
            }

            if (!this.selectedProductCategoryId.HasValue && this.categoriesCache != null && this.categoriesCache.Count > 0)
            {
                this.selectedProductCategoryId = this.categoriesCache[0].Id;
            }

            if (this.selectedProductCategoryId.HasValue)
            {
                this.productCategoryCombo.SelectedValue = this.selectedProductCategoryId.Value;
            }

            UpdateProductEditorState();
        }

        private void ClearUserEditor()
        {
            this.selectedUserId = null;
            this.userTitleLabel.Text = "Usuario";
            this.userNameTextBox.Clear();
            this.userDisplayNameTextBox.Clear();
            this.userPasswordTextBox.Clear();
            this.userIsActiveCheckBox.Checked = true;
            if (this.userRoleCombo.Items.Count > 0)
            {
                this.userRoleCombo.SelectedItem = SystemRoles.Cashier;
            }

            UpdateUserEditorState();
        }

        private void UpdateProductEditorState()
        {
            var editing = this.selectedProductId.HasValue;
            this.productTitleLabel.Text = "Producto";
            this.productSaveButton.Text = editing ? "Guardar cambios" : "Agregar producto";
            this.productDeleteButton.Enabled = editing;
        }

        private void UpdateUserEditorState()
        {
            var editing = this.selectedUserId.HasValue;
            this.userTitleLabel.Text = "Usuario";
            this.userSaveButton.Text = editing ? "Guardar cambios" : "Agregar usuario";
            this.userDeleteButton.Enabled = editing;
        }

        private static T GetSelectedRowData<T>(DataGridView grid) where T : class
        {
            if (grid.CurrentRow == null)
            {
                return null;
            }

            return grid.CurrentRow.DataBoundItem as T;
        }

        private CategoryEditorData ShowCategoryDialog(CategoryManagementView category)
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Categoría";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ClientSize = new Size(420, 220);
                dialog.BackColor = Color.White;
                dialog.Font = new Font("Segoe UI", 10F);

                var nameTextBox = new TextBox { Dock = DockStyle.Fill, Text = category?.Name ?? string.Empty };
                var orderInput = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 1000, DecimalPlaces = 0, Value = category == null ? 1 : Math.Max(1, category.SortOrder) };
                var activeCheckBox = new CheckBox { Text = "Activa", AutoSize = true, Checked = category == null || category.IsActive };

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 4,
                    Padding = new Padding(16)
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
                layout.Controls.Add(CreateFieldLabel("Nombre"), 0, 0);
                layout.Controls.Add(nameTextBox, 1, 0);
                layout.Controls.Add(CreateFieldLabel("Orden"), 0, 1);
                layout.Controls.Add(orderInput, 1, 1);
                layout.Controls.Add(CreateFieldLabel("Estado"), 0, 2);
                layout.Controls.Add(activeCheckBox, 1, 2);

                var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
                var saveButton = CreatePrimaryButton(category == null ? "Agregar" : "Guardar cambios");
                var cancelButton = CreateSecondaryButton("Cancelar");
                buttons.Controls.Add(saveButton);
                buttons.Controls.Add(cancelButton);
                layout.Controls.Add(buttons, 0, 3);
                layout.SetColumnSpan(buttons, 2);
                dialog.Controls.Add(layout);
                dialog.AcceptButton = saveButton;
                dialog.CancelButton = cancelButton;

                saveButton.Click += delegate
                {
                    if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                    {
                        MessageBox.Show(dialog, "Debes capturar el nombre.", "Categorías", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    dialog.Tag = new CategoryEditorData
                    {
                        Name = nameTextBox.Text.Trim(),
                        SortOrder = Convert.ToInt32(orderInput.Value),
                        IsActive = activeCheckBox.Checked
                    };
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                };

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return null;
                }

                return dialog.Tag as CategoryEditorData;
            }
        }

        private ProductEditorData ShowProductDialog(ProductManagementView product)
        {
            if (this.categoriesCache == null || this.categoriesCache.Count == 0)
            {
                MessageBox.Show("Primero crea una categoría.", "Productos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            using (var dialog = new Form())
            {
                dialog.Text = "Producto";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ClientSize = new Size(540, 320);
                dialog.BackColor = Color.White;
                dialog.Font = new Font("Segoe UI", 10F);

                var categoryCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
                categoryCombo.DataSource = this.categoriesCache.ToList();
                categoryCombo.DisplayMember = "Name";
                categoryCombo.ValueMember = "Id";
                if (product != null)
                {
                    categoryCombo.SelectedValue = product.CategoryId;
                }
                else if (this.selectedProductCategoryId.HasValue)
                {
                    categoryCombo.SelectedValue = this.selectedProductCategoryId.Value;
                }

                var nameTextBox = new TextBox { Dock = DockStyle.Fill, Text = product?.Name ?? string.Empty };
                var salePriceInput = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 1000000, DecimalPlaces = 2, Value = product == null ? 0 : product.SalePrice };
                var costInput = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 1000000, DecimalPlaces = 2, Value = product == null ? 0 : product.EstimatedCost };
                var stockInput = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 1000000, DecimalPlaces = 2, Value = product == null ? 0 : product.StockOnHand };
                var inventoryCheckBox = new CheckBox { Text = "Usa inventario", AutoSize = true, Checked = product != null && product.UsesInventory };
                var activeCheckBox = new CheckBox { Text = "Activo", AutoSize = true, Checked = product == null || product.IsActive };

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 4,
                    RowCount = 5,
                    Padding = new Padding(16)
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
                layout.Controls.Add(CreateFieldLabel("Categoría"), 0, 0);
                layout.Controls.Add(categoryCombo, 1, 0);
                layout.SetColumnSpan(categoryCombo, 3);
                layout.Controls.Add(CreateFieldLabel("Producto"), 0, 1);
                layout.Controls.Add(nameTextBox, 1, 1);
                layout.SetColumnSpan(nameTextBox, 3);
                layout.Controls.Add(CreateFieldLabel("Venta"), 0, 2);
                layout.Controls.Add(salePriceInput, 1, 2);
                layout.Controls.Add(CreateFieldLabel("Costo"), 2, 2);
                layout.Controls.Add(costInput, 3, 2);
                layout.Controls.Add(CreateFieldLabel("Existencia"), 0, 3);
                layout.Controls.Add(CreateFieldLabel("Inventario"), 2, 3);
                layout.Controls.Add(CreateFieldLabel("Estado"), 3, 3);
                layout.Controls.Add(stockInput, 0, 4);
                layout.Controls.Add(inventoryCheckBox, 2, 4);
                layout.Controls.Add(activeCheckBox, 3, 4);
                layout.SetColumnSpan(stockInput, 2);

                var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
                var saveButton = CreatePrimaryButton(product == null ? "Agregar" : "Guardar cambios");
                var cancelButton = CreateSecondaryButton("Cancelar");
                buttons.Controls.Add(saveButton);
                buttons.Controls.Add(cancelButton);

                dialog.Controls.Add(layout);
                dialog.Controls.Add(buttons);
                dialog.AcceptButton = saveButton;
                dialog.CancelButton = cancelButton;

                saveButton.Click += delegate
                {
                    var selectedCategory = categoryCombo.SelectedItem as CategoryManagementView;
                    if (selectedCategory == null)
                    {
                        MessageBox.Show(dialog, "Debes seleccionar una categoría.", "Productos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                    {
                        MessageBox.Show(dialog, "Debes capturar el nombre del producto.", "Productos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    dialog.Tag = new ProductEditorData
                    {
                        CategoryId = selectedCategory.Id,
                        Name = nameTextBox.Text.Trim(),
                        SalePrice = salePriceInput.Value,
                        EstimatedCost = costInput.Value,
                        StockOnHand = stockInput.Value,
                        UsesInventory = inventoryCheckBox.Checked,
                        IsActive = activeCheckBox.Checked
                    };
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                };

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return null;
                }

                return dialog.Tag as ProductEditorData;
            }
        }

        private UserEditorData ShowUserDialog(UserManagementView user)
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Usuario";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ClientSize = new Size(560, 300);
                dialog.BackColor = Color.White;
                dialog.Font = new Font("Segoe UI", 10F);

                var usernameTextBox = new TextBox { Dock = DockStyle.Fill, Text = user?.Username ?? string.Empty };
                var displayNameTextBox = new TextBox { Dock = DockStyle.Fill, Text = user?.DisplayName ?? string.Empty };
                var roleCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
                roleCombo.Items.AddRange(SystemRoles.All.Cast<object>().ToArray());
                if (user != null && roleCombo.Items.Contains(user.RoleCode))
                {
                    roleCombo.SelectedItem = user.RoleCode;
                }
                else if (roleCombo.Items.Count > 0)
                {
                    roleCombo.SelectedItem = SystemRoles.Cashier;
                }

                var passwordTextBox = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
                var activeCheckBox = new CheckBox { Text = "Activo", AutoSize = true, Checked = user == null || user.IsActive };
                var noteLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = user == null ? "La contraseña es obligatoria." : "Deja la contraseña en blanco para conservar la actual.",
                    ForeColor = Color.DimGray,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 4,
                    RowCount = 5,
                    Padding = new Padding(16)
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
                layout.Controls.Add(CreateFieldLabel("Usuario"), 0, 0);
                layout.Controls.Add(usernameTextBox, 1, 0);
                layout.SetColumnSpan(usernameTextBox, 3);
                layout.Controls.Add(CreateFieldLabel("Nombre visible"), 0, 1);
                layout.Controls.Add(displayNameTextBox, 1, 1);
                layout.SetColumnSpan(displayNameTextBox, 3);
                layout.Controls.Add(CreateFieldLabel("Rol"), 0, 2);
                layout.Controls.Add(roleCombo, 1, 2);
                layout.Controls.Add(CreateFieldLabel("Estado"), 2, 2);
                layout.Controls.Add(activeCheckBox, 3, 2);
                layout.Controls.Add(CreateFieldLabel("Contraseña"), 0, 3);
                layout.Controls.Add(passwordTextBox, 1, 3);
                layout.SetColumnSpan(passwordTextBox, 3);
                layout.Controls.Add(noteLabel, 0, 4);
                layout.SetColumnSpan(noteLabel, 4);

                var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
                var saveButton = CreatePrimaryButton(user == null ? "Agregar" : "Guardar cambios");
                var cancelButton = CreateSecondaryButton("Cancelar");
                buttons.Controls.Add(saveButton);
                buttons.Controls.Add(cancelButton);

                dialog.Controls.Add(layout);
                dialog.Controls.Add(buttons);
                dialog.AcceptButton = saveButton;
                dialog.CancelButton = cancelButton;

                saveButton.Click += delegate
                {
                    if (string.IsNullOrWhiteSpace(usernameTextBox.Text))
                    {
                        MessageBox.Show(dialog, "Debes capturar el usuario.", "Usuarios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(displayNameTextBox.Text))
                    {
                        MessageBox.Show(dialog, "Debes capturar el nombre visible.", "Usuarios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    dialog.Tag = new UserEditorData
                    {
                        UserId = user?.UserId,
                        Username = usernameTextBox.Text.Trim(),
                        DisplayName = displayNameTextBox.Text.Trim(),
                        RoleCode = roleCombo.SelectedItem == null ? SystemRoles.Cashier : roleCombo.SelectedItem.ToString(),
                        Password = passwordTextBox.Text,
                        IsActive = activeCheckBox.Checked
                    };
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                };

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return null;
                }

                return dialog.Tag as UserEditorData;
            }
        }

        private static DataGridView CreateReadOnlyGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 34,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(27, 67, 50),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
                }
            };
        }

        private static Label CreateEditorTitle(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 67, 50),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 6)
            };
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Margin = new Padding(0, 0, 10, 0)
            };
        }

        private static TextBox CreateTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 2, 10, 2)
            };
        }

        private static NumericUpDown CreateNumericInput(decimal minimum, decimal maximum, int decimals)
        {
            return new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = minimum,
                Maximum = maximum,
                DecimalPlaces = decimals,
                ThousandsSeparator = true,
                Margin = new Padding(0, 2, 10, 2)
            };
        }

        private static Button CreatePrimaryButton(string text)
        {
            return CreateButton(text, Color.FromArgb(27, 67, 50), Color.White);
        }

        private static Button CreateDangerButton(string text)
        {
            return CreateButton(text, Color.FromArgb(130, 27, 52), Color.White);
        }

        private static Button CreateSecondaryButton(string text)
        {
            return CreateButton(text, Color.FromArgb(90, 90, 90), Color.White);
        }

        private static Button CreateButton(string text, Color backColor, Color foreColor)
        {
            var button = new Button
            {
                Text = text,
                Width = 138,
                Height = 38,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 10, 0)
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private static void ShowResult(OperationResult result, string title)
        {
            MessageBox.Show(result.Message, title, MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private sealed class CategoryEditorData
        {
            public string Name { get; set; }
            public int SortOrder { get; set; }
            public bool IsActive { get; set; }
        }

        private sealed class ProductEditorData
        {
            public int CategoryId { get; set; }
            public string Name { get; set; }
            public decimal SalePrice { get; set; }
            public decimal EstimatedCost { get; set; }
            public decimal StockOnHand { get; set; }
            public bool UsesInventory { get; set; }
            public bool IsActive { get; set; }
        }

        private sealed class UserEditorData
        {
            public int? UserId { get; set; }
            public string Username { get; set; }
            public string DisplayName { get; set; }
            public string RoleCode { get; set; }
            public string Password { get; set; }
            public bool IsActive { get; set; }
        }

        private sealed class CategoryFilterItem
        {
            public int CategoryId { get; set; }
            public string Name { get; set; }
        }
    }
}
