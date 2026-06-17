# Reglas de negocio críticas

1. No puede existir más de un corte de caja abierto por estación.
2. Toda venta confirmada debe persistirse en una sola transacción.
3. Una falla de impresión no revierte la venta.
4. Solo usuarios con permiso pueden aprobar cancelaciones.
5. La cancelación aprobada revierte inventario únicamente para productos con `UsaInventario = true`.
6. Los folios de venta, orden pendiente y corte deben ser únicos, legibles y consecutivos por tipo.
7. Una orden pendiente puede reabrirse mientras no se convierta en venta o no se cancele.
8. Un producto con `UsaInventario = false` no participa en validaciones de existencia.
9. Todo cambio crítico genera auditoría: login, setup, venta, cancelación, corte, respaldo y restauración.
10. El sistema debe arrancar incluso si la impresora configurada no está disponible.
11. La restauración obliga a generar un respaldo previo y validar integridad del archivo origen.
12. Los permisos se validan en capa de aplicación, no solo en UI.
