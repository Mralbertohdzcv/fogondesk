using System.Collections.Generic;

namespace FogonDesk.Infrastructure.Data
{
    public sealed class SqliteMigrationScript
    {
        public SqliteMigrationScript(string name, string sql)
        {
            this.Name = name;
            this.Sql = sql;
        }

        public string Name { get; private set; }
        public string Sql { get; private set; }
    }

    public static class SqliteSchemaScripts
    {
        public static IReadOnlyList<SqliteMigrationScript> All { get; } = new List<SqliteMigrationScript>
        {
            new SqliteMigrationScript(
                "001_initial_schema",
                @"
CREATE TABLE IF NOT EXISTS business_profile (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    trade_name TEXT NOT NULL,
    business_type_code TEXT NOT NULL,
    slogan TEXT NULL,
    address TEXT NULL,
    phone TEXT NULL,
    header_text TEXT NULL,
    footer_text TEXT NULL,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS visual_theme (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    primary_color_hex TEXT NOT NULL,
    accent_color_hex TEXT NOT NULL,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ticket_profile (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    ticket_width_mm INTEGER NOT NULL,
    margin_left INTEGER NOT NULL DEFAULT 2,
    margin_right INTEGER NOT NULL DEFAULT 2,
    auto_open_drawer INTEGER NOT NULL DEFAULT 0,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS printer_profile (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    printer_name TEXT NULL,
    printer_output_mode INTEGER NOT NULL DEFAULT 1,
    drawer_command_hex TEXT NULL,
    drawer_pulse_on_ms INTEGER NOT NULL DEFAULT 25,
    drawer_pulse_off_ms INTEGER NOT NULL DEFAULT 250,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS station_profile (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    station_name TEXT NOT NULL,
    station_code TEXT NOT NULL,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS roles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS permissions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS role_permissions (
    role_code TEXT NOT NULL,
    permission_code TEXT NOT NULL,
    PRIMARY KEY (role_code, permission_code)
);

CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL UNIQUE,
    display_name TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    password_salt TEXT NOT NULL,
    role_code TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL,
    last_login_utc TEXT NULL
);

CREATE TABLE IF NOT EXISTS user_sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    started_utc TEXT NOT NULL,
    ended_utc TEXT NULL,
    machine_name TEXT NOT NULL,
    status TEXT NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users (id)
);

CREATE TABLE IF NOT EXISTS categories (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    category_id INTEGER NOT NULL,
    sku TEXT NULL,
    name TEXT NOT NULL,
    sale_price NUMERIC NOT NULL,
    estimated_cost NUMERIC NOT NULL DEFAULT 0,
    uses_inventory INTEGER NOT NULL DEFAULT 0,
    stock_on_hand NUMERIC NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_utc TEXT NOT NULL,
    FOREIGN KEY (category_id) REFERENCES categories (id)
);

CREATE TABLE IF NOT EXISTS modifier_groups (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    selection_mode TEXT NOT NULL,
    min_selection INTEGER NOT NULL DEFAULT 0,
    max_selection INTEGER NOT NULL DEFAULT 1,
    is_active INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS modifier_options (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    group_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    extra_price NUMERIC NOT NULL DEFAULT 0,
    estimated_cost NUMERIC NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (group_id) REFERENCES modifier_groups (id)
);

CREATE TABLE IF NOT EXISTS product_modifier_groups (
    product_id INTEGER NOT NULL,
    group_id INTEGER NOT NULL,
    PRIMARY KEY (product_id, group_id)
);

CREATE TABLE IF NOT EXISTS combo_definitions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    sale_price NUMERIC NOT NULL,
    estimated_cost NUMERIC NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS combo_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    combo_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity NUMERIC NOT NULL DEFAULT 1,
    FOREIGN KEY (combo_id) REFERENCES combo_definitions (id),
    FOREIGN KEY (product_id) REFERENCES products (id)
);

CREATE TABLE IF NOT EXISTS digital_platforms (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    next_sequence INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS dining_tables (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    seats INTEGER NOT NULL DEFAULT 4,
    is_active INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS pending_orders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    folio TEXT NOT NULL UNIQUE,
    order_kind INTEGER NOT NULL,
    status INTEGER NOT NULL,
    table_id INTEGER NULL,
    customer_name TEXT NULL,
    digital_platform_id INTEGER NULL,
    platform_reference TEXT NULL,
    created_by_user_id INTEGER NOT NULL,
    opened_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL,
    note TEXT NULL,
    FOREIGN KEY (table_id) REFERENCES dining_tables (id),
    FOREIGN KEY (digital_platform_id) REFERENCES digital_platforms (id),
    FOREIGN KEY (created_by_user_id) REFERENCES users (id)
);

CREATE TABLE IF NOT EXISTS pending_order_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    pending_order_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    product_name_snapshot TEXT NOT NULL,
    quantity NUMERIC NOT NULL,
    unit_price NUMERIC NOT NULL,
    note TEXT NULL,
    FOREIGN KEY (pending_order_id) REFERENCES pending_orders (id),
    FOREIGN KEY (product_id) REFERENCES products (id)
);

CREATE TABLE IF NOT EXISTS pending_order_item_modifiers (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    pending_order_item_id INTEGER NOT NULL,
    modifier_name_snapshot TEXT NOT NULL,
    extra_price NUMERIC NOT NULL DEFAULT 0,
    quantity NUMERIC NOT NULL DEFAULT 1,
    FOREIGN KEY (pending_order_item_id) REFERENCES pending_order_items (id)
);

CREATE TABLE IF NOT EXISTS cash_shifts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    folio TEXT NOT NULL UNIQUE,
    station_code TEXT NOT NULL,
    opened_by_user_id INTEGER NOT NULL,
    closed_by_user_id INTEGER NULL,
    opened_utc TEXT NOT NULL,
    closed_utc TEXT NULL,
    opening_cash NUMERIC NOT NULL DEFAULT 0,
    expected_cash NUMERIC NOT NULL DEFAULT 0,
    actual_cash NUMERIC NULL,
    sales_total NUMERIC NOT NULL DEFAULT 0,
    estimated_cost_total NUMERIC NOT NULL DEFAULT 0,
    estimated_profit_total NUMERIC NOT NULL DEFAULT 0,
    difference_total NUMERIC NULL,
    status TEXT NOT NULL,
    FOREIGN KEY (opened_by_user_id) REFERENCES users (id),
    FOREIGN KEY (closed_by_user_id) REFERENCES users (id)
);

CREATE TABLE IF NOT EXISTS sales (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    folio TEXT NOT NULL UNIQUE,
    order_kind INTEGER NOT NULL,
    status INTEGER NOT NULL,
    pending_order_id INTEGER NULL,
    cash_shift_id INTEGER NULL,
    sold_by_user_id INTEGER NOT NULL,
    sold_utc TEXT NOT NULL,
    subtotal NUMERIC NOT NULL,
    discount_total NUMERIC NOT NULL DEFAULT 0,
    tax_total NUMERIC NOT NULL DEFAULT 0,
    total NUMERIC NOT NULL,
    estimated_cost_total NUMERIC NOT NULL DEFAULT 0,
    estimated_profit_total NUMERIC NOT NULL DEFAULT 0,
    payment_summary TEXT NOT NULL,
    note TEXT NULL,
    print_status TEXT NOT NULL DEFAULT 'pendiente',
    FOREIGN KEY (pending_order_id) REFERENCES pending_orders (id),
    FOREIGN KEY (cash_shift_id) REFERENCES cash_shifts (id),
    FOREIGN KEY (sold_by_user_id) REFERENCES users (id)
);

CREATE TABLE IF NOT EXISTS sale_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    sale_id INTEGER NOT NULL,
    product_id INTEGER NULL,
    combo_id INTEGER NULL,
    product_name_snapshot TEXT NOT NULL,
    quantity NUMERIC NOT NULL,
    unit_price NUMERIC NOT NULL,
    line_total NUMERIC NOT NULL,
    estimated_cost_total NUMERIC NOT NULL DEFAULT 0,
    uses_inventory INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (sale_id) REFERENCES sales (id),
    FOREIGN KEY (product_id) REFERENCES products (id),
    FOREIGN KEY (combo_id) REFERENCES combo_definitions (id)
);

CREATE TABLE IF NOT EXISTS sale_item_modifiers (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    sale_item_id INTEGER NOT NULL,
    modifier_name_snapshot TEXT NOT NULL,
    extra_price NUMERIC NOT NULL DEFAULT 0,
    quantity NUMERIC NOT NULL DEFAULT 1,
    FOREIGN KEY (sale_item_id) REFERENCES sale_items (id)
);

CREATE TABLE IF NOT EXISTS payments (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    sale_id INTEGER NOT NULL,
    payment_method INTEGER NOT NULL,
    amount NUMERIC NOT NULL,
    reference_text TEXT NULL,
    FOREIGN KEY (sale_id) REFERENCES sales (id)
);

CREATE TABLE IF NOT EXISTS cash_shift_sales (
    cash_shift_id INTEGER NOT NULL,
    sale_id INTEGER NOT NULL,
    PRIMARY KEY (cash_shift_id, sale_id),
    FOREIGN KEY (cash_shift_id) REFERENCES cash_shifts (id),
    FOREIGN KEY (sale_id) REFERENCES sales (id)
);

CREATE TABLE IF NOT EXISTS cancellation_requests (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    sale_id INTEGER NOT NULL,
    requested_by_user_id INTEGER NOT NULL,
    approved_by_user_id INTEGER NULL,
    requested_utc TEXT NOT NULL,
    resolved_utc TEXT NULL,
    reason TEXT NOT NULL,
    status TEXT NOT NULL,
    FOREIGN KEY (sale_id) REFERENCES sales (id),
    FOREIGN KEY (requested_by_user_id) REFERENCES users (id),
    FOREIGN KEY (approved_by_user_id) REFERENCES users (id)
);

CREATE TABLE IF NOT EXISTS inventory_movements (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    product_id INTEGER NOT NULL,
    movement_type TEXT NOT NULL,
    quantity NUMERIC NOT NULL,
    source_document TEXT NOT NULL,
    reference_id INTEGER NOT NULL,
    created_by_user_id INTEGER NOT NULL,
    created_utc TEXT NOT NULL,
    note TEXT NULL,
    FOREIGN KEY (product_id) REFERENCES products (id),
    FOREIGN KEY (created_by_user_id) REFERENCES users (id)
);

CREATE TABLE IF NOT EXISTS audit_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    event_type TEXT NOT NULL,
    entity_name TEXT NOT NULL,
    entity_id TEXT NULL,
    user_name TEXT NULL,
    details TEXT NULL,
    created_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS backup_records (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    file_path TEXT NOT NULL,
    operation_type TEXT NOT NULL,
    result_status TEXT NOT NULL,
    created_by_user_name TEXT NULL,
    created_utc TEXT NOT NULL,
    notes TEXT NULL
);

CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_sales_sold_utc ON sales(sold_utc);
CREATE INDEX IF NOT EXISTS idx_pending_orders_status ON pending_orders(status);
CREATE INDEX IF NOT EXISTS idx_products_category ON products(category_id);
CREATE INDEX IF NOT EXISTS idx_inventory_movements_product ON inventory_movements(product_id);
"),
            new SqliteMigrationScript(
                "002_seed_base_catalogs",
                @"
INSERT OR IGNORE INTO roles (code, name) VALUES ('administrador', 'Administrador');
INSERT OR IGNORE INTO roles (code, name) VALUES ('supervisor', 'Supervisor');
INSERT OR IGNORE INTO roles (code, name) VALUES ('cajero', 'Cajero');

INSERT OR IGNORE INTO permissions (code, name) VALUES ('sistema.configurar', 'Configurar sistema');
INSERT OR IGNORE INTO permissions (code, name) VALUES ('catalogo.gestionar', 'Gestionar catálogo');
INSERT OR IGNORE INTO permissions (code, name) VALUES ('usuarios.gestionar', 'Gestionar usuarios');
INSERT OR IGNORE INTO permissions (code, name) VALUES ('ventas.usar', 'Usar punto de venta');
INSERT OR IGNORE INTO permissions (code, name) VALUES ('ordenes.gestionar', 'Gestionar órdenes pendientes');
INSERT OR IGNORE INTO permissions (code, name) VALUES ('inventario.gestionar', 'Gestionar inventario');
INSERT OR IGNORE INTO permissions (code, name) VALUES ('cancelaciones.aprobar', 'Aprobar cancelaciones');
INSERT OR IGNORE INTO permissions (code, name) VALUES ('caja.gestionar', 'Gestionar caja');
INSERT OR IGNORE INTO permissions (code, name) VALUES ('reportes.ver', 'Ver reportes');
INSERT OR IGNORE INTO permissions (code, name) VALUES ('impresion.gestionar', 'Gestionar impresión');
INSERT OR IGNORE INTO permissions (code, name) VALUES ('respaldo.gestionar', 'Gestionar respaldo');

INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('administrador', 'sistema.configurar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('administrador', 'catalogo.gestionar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('administrador', 'usuarios.gestionar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('administrador', 'ventas.usar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('administrador', 'ordenes.gestionar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('administrador', 'inventario.gestionar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('administrador', 'cancelaciones.aprobar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('administrador', 'caja.gestionar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('administrador', 'reportes.ver');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('administrador', 'impresion.gestionar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('administrador', 'respaldo.gestionar');

INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('supervisor', 'catalogo.gestionar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('supervisor', 'ventas.usar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('supervisor', 'ordenes.gestionar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('supervisor', 'inventario.gestionar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('supervisor', 'cancelaciones.aprobar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('supervisor', 'caja.gestionar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('supervisor', 'reportes.ver');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('supervisor', 'impresion.gestionar');

INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('cajero', 'ventas.usar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('cajero', 'ordenes.gestionar');
INSERT OR IGNORE INTO role_permissions (role_code, permission_code) VALUES ('cajero', 'impresion.gestionar');

INSERT OR IGNORE INTO digital_platforms (code, name, is_active, next_sequence) VALUES ('rappi', 'Rappi', 1, 1);
INSERT OR IGNORE INTO digital_platforms (code, name, is_active, next_sequence) VALUES ('didi', 'Didi', 1, 1);

INSERT OR IGNORE INTO dining_tables (code, name, seats, is_active) VALUES ('M01', 'Mesa 1', 4, 1);
INSERT OR IGNORE INTO dining_tables (code, name, seats, is_active) VALUES ('M02', 'Mesa 2', 4, 1);
INSERT OR IGNORE INTO dining_tables (code, name, seats, is_active) VALUES ('M03', 'Mesa 3', 4, 1);
INSERT OR IGNORE INTO dining_tables (code, name, seats, is_active) VALUES ('M04', 'Mesa 4', 4, 1);
INSERT OR IGNORE INTO dining_tables (code, name, seats, is_active) VALUES ('M05', 'Mesa 5', 4, 1);
UPDATE dining_tables
SET is_active = CASE WHEN code IN ('M01', 'M02', 'M03', 'M04', 'M05') THEN 1 ELSE 0 END;
"),
            new SqliteMigrationScript(
                "003_ticket_print_settings",
                @"
ALTER TABLE ticket_profile ADD COLUMN title_font_size INTEGER NOT NULL DEFAULT 12;
ALTER TABLE ticket_profile ADD COLUMN body_font_size INTEGER NOT NULL DEFAULT 9;
ALTER TABLE ticket_profile ADD COLUMN print_kitchen_ticket INTEGER NOT NULL DEFAULT 0;
ALTER TABLE ticket_profile ADD COLUMN layout_name TEXT NOT NULL DEFAULT 'Clásico compacto';
"),
            new SqliteMigrationScript(
                "004_ticket_system_footer",
                @"
ALTER TABLE ticket_profile ADD COLUMN show_system_footer INTEGER NOT NULL DEFAULT 0;
"),
            new SqliteMigrationScript(
                "005_ticket_system_footer_text",
                @"
ALTER TABLE ticket_profile ADD COLUMN system_footer_text TEXT NOT NULL DEFAULT '';
"),
            new SqliteMigrationScript(
                "006_ticket_offsets",
                @"
ALTER TABLE ticket_profile ADD COLUMN horizontal_offset INTEGER NOT NULL DEFAULT 0;
ALTER TABLE ticket_profile ADD COLUMN vertical_offset INTEGER NOT NULL DEFAULT 0;
"),
            new SqliteMigrationScript(
                "007_ticket_width_and_section_fonts",
                @"
ALTER TABLE ticket_profile ADD COLUMN characters_per_line INTEGER NOT NULL DEFAULT 30;
ALTER TABLE ticket_profile ADD COLUMN info_font_size INTEGER NOT NULL DEFAULT 9;
ALTER TABLE ticket_profile ADD COLUMN items_font_size INTEGER NOT NULL DEFAULT 9;
ALTER TABLE ticket_profile ADD COLUMN total_font_size INTEGER NOT NULL DEFAULT 9;
ALTER TABLE ticket_profile ADD COLUMN footer_font_size INTEGER NOT NULL DEFAULT 9;
"),
            new SqliteMigrationScript(
                "008_ticket_full_paper_width",
                @"
ALTER TABLE ticket_profile ADD COLUMN use_full_paper_width INTEGER NOT NULL DEFAULT 1;
"),
            new SqliteMigrationScript(
                "009_telegram_integration",
                @"
CREATE TABLE IF NOT EXISTS telegram_settings (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    bot_token TEXT NOT NULL DEFAULT '',
    last_update_id INTEGER NOT NULL DEFAULT 0,
    updated_utc TEXT NOT NULL
);

INSERT OR IGNORE INTO telegram_settings (id, bot_token, last_update_id, updated_utc)
VALUES (1, '', 0, '1970-01-01T00:00:00.0000000Z');

CREATE TABLE IF NOT EXISTS telegram_link_codes (
    code TEXT PRIMARY KEY,
    created_utc TEXT NOT NULL,
    expires_utc TEXT NOT NULL,
    consumed_utc TEXT NULL,
    consumed_chat_id INTEGER NULL
);

CREATE TABLE IF NOT EXISTS telegram_linked_chats (
    chat_id INTEGER PRIMARY KEY,
    username TEXT NULL,
    display_name TEXT NULL,
    linked_utc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_telegram_link_codes_expires ON telegram_link_codes(expires_utc);

DELETE FROM telegram_link_codes
WHERE consumed_utc IS NOT NULL;
"),
            new SqliteMigrationScript(
                "010_operation_settings_defaults",
                @"
ALTER TABLE digital_platforms ADD COLUMN pricing_mode TEXT NOT NULL DEFAULT 'manual';

INSERT OR IGNORE INTO digital_platforms (code, name, pricing_mode, is_active, next_sequence)
VALUES ('rappi', 'Rappi', 'rappi', 1, 1);
INSERT OR IGNORE INTO digital_platforms (code, name, pricing_mode, is_active, next_sequence)
VALUES ('didi', 'Didi', 'didi', 1, 1);

UPDATE digital_platforms
SET is_active = 1,
    pricing_mode = 'rappi',
    name = 'Rappi'
WHERE lower(code) = 'rappi';

UPDATE digital_platforms
SET is_active = 1,
    pricing_mode = 'didi',
    name = 'Didi'
WHERE lower(code) = 'didi';

UPDATE digital_platforms
SET is_active = 0
WHERE lower(code) NOT IN ('rappi', 'didi');

INSERT OR IGNORE INTO dining_tables (code, name, seats, is_active) VALUES ('M04', 'Mesa 4', 4, 1);
INSERT OR IGNORE INTO dining_tables (code, name, seats, is_active) VALUES ('M05', 'Mesa 5', 4, 1);
UPDATE dining_tables
SET is_active = CASE WHEN code IN ('M01', 'M02', 'M03', 'M04', 'M05') THEN 1 ELSE 0 END;
"),
            new SqliteMigrationScript(
                "011_app_counters",
                @"
CREATE TABLE IF NOT EXISTS app_counters (
    name TEXT PRIMARY KEY,
    next_value INTEGER NOT NULL DEFAULT 1
);

INSERT OR IGNORE INTO app_counters (name, next_value) VALUES ('precheck_folio', 1);
"),
            new SqliteMigrationScript(
                "012_sales_receipt_text",
                @"
ALTER TABLE sales ADD COLUMN receipt_text TEXT NOT NULL DEFAULT '';
"),
        };
    }
}
