# Modelo de dominio y datos

## Agregados principales

### Configuración

- `BusinessProfile`: nombre, tipo de negocio, slogan, dirección, teléfono.
- `VisualTheme`: colores principales y secundarios.
- `TicketProfile`: ancho de ticket, encabezado, pie, márgenes y bandera de cajón.
- `PrinterProfile`: impresora seleccionada, tipo de salida, comandos RAW opcionales.

### Seguridad

- `UserAccount`: usuario, hash, salt, activo, rol principal.
- `RoleDefinition`: administrador, supervisor, cajero.
- `PermissionGrant`: permisos por módulo y acción.

### Catálogo

- `Category`
- `Product`
- `ModifierGroup`
- `ModifierOption`
- `ComboDefinition`
- `ComboItem`
- `DigitalPlatform`
- `DiningTable`

### Operación

- `PendingOrder`
- `PendingOrderItem`
- `PendingOrderItemModifier`
- `Sale`
- `SaleItem`
- `SaleItemModifier`
- `PaymentRecord`
- `CashShift`
- `CashShiftSaleLink`
- `CancellationRequest`
- `InventoryMovement`
- `AuditEntry`
- `BackupRecord`

## Reglas de modelado de inventario

- `Product.UsaInventario` define si el producto descuenta existencia.
- `Product.ExistenciaActual` siempre tiene valor numérico.
- Productos sin control de inventario conservan `ExistenciaActual = 0`, pero nunca se interpreta como existencia real.
- La lógica de descuento consulta `UsaInventario`; no usa `NULL` ni convenciones ambiguas.

## Esquema relacional propuesto

### Configuración

- `schema_migrations`
- `business_profile`
- `visual_theme`
- `ticket_profile`
- `printer_profile`
- `station_profile`

### Seguridad

- `roles`
- `permissions`
- `role_permissions`
- `users`
- `user_sessions`

### Catálogo

- `categories`
- `products`
- `modifier_groups`
- `modifier_options`
- `product_modifier_groups`
- `combo_definitions`
- `combo_items`
- `digital_platforms`
- `dining_tables`

### Operación

- `pending_orders`
- `pending_order_items`
- `pending_order_item_modifiers`
- `sales`
- `sale_items`
- `sale_item_modifiers`
- `payments`
- `cash_shifts`
- `cash_shift_sales`
- `cancellation_requests`
- `inventory_movements`
- `audit_log`
- `backup_records`

## Portabilidad futura

La capa de repositorios expone contratos sin acoplar consultas a detalles de SQLite. Los tipos se mantienen compatibles con motores relacionales comunes para facilitar una migración futura a MariaDB/MySQL si se requiere operación multiestación.
