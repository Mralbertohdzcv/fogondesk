# Plan de implementación

## Fase 1. Base técnica, configuración e instalación

- Solución multicapa `net48` / `x86`.
- Arranque con verificación de esquema.
- Configuración local, logging y directorios de trabajo.
- Wizard inicial y script de instalador base.

## Fase 2. Autenticación y roles

- Login local seguro.
- Hashing PBKDF2.
- Roles mínimos: administrador, supervisor, cajero.
- Permisos por rol y trazabilidad de sesión.

## Fase 3. Catálogo, categorías y usuarios

- CRUD de categorías.
- CRUD de productos.
- CRUD de usuarios.
- Modificadores, extras, combos y plataformas.

## Fase 4. Cajero y ventas

- Pantalla de venta rápida táctil.
- Carrito, notas, modificadores y cobro.
- Persistencia transaccional de venta y detalle.
- Impresión y reimpresión desacopladas.

## Fase 5. Órdenes pendientes, mesas y plataformas

- Mesas y mapa básico.
- Órdenes pendientes reabribles.
- Flujo para llevar y plataformas digitales.
- Estados claros y folios legibles.

## Fase 6. Inventario, cancelaciones y cortes

- Descuento y reversa de inventario.
- Solicitud y aprobación de cancelaciones.
- Apertura y cierre de caja.
- Auditoría operativa.

## Fase 7. Reportes, impresión y respaldos

- Reportes operativos.
- Configuración fina de tickets y cajón.
- Respaldos y restauración guiada.
- Exportaciones simples.
