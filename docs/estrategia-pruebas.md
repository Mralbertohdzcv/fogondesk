# Estrategia de pruebas

## Unitarias

- Reglas de inventario.
- Cálculo de totales y ganancias.
- Generación de folios.
- Validación de permisos.
- Hashing y autenticación.

## Integración

- Migraciones sobre SQLite vacía.
- Setup inicial completo.
- Venta con transacción y rollback.
- Cancelación con reversa de inventario.
- Apertura y cierre de corte.
- Respaldo y restauración con validación.

## UI operativa

- Flujo de primer arranque.
- Login y cierre de sesión.
- Venta rápida con mouse.
- Venta rápida con pantalla táctil.
- Reimpresión.

## Pruebas de estabilidad

- Jornadas largas con múltiples ventas.
- Cierres inesperados durante operaciones no confirmadas.
- Reinicio con órdenes pendientes y corte activo.
