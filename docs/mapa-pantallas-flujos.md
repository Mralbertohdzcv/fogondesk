# Mapa de pantallas y flujos

## Pantallas principales

1. Splash y validación inicial.
2. Wizard de configuración inicial.
3. Login.
4. Menú principal.
5. Punto de venta.
6. Órdenes pendientes.
7. Mesas.
8. Administración.
9. Reportes.
10. Caja.
11. Configuración e impresión.
12. Respaldo y restauración.

## Flujo de primer arranque

1. La aplicación crea rutas locales y base de datos si no existen.
2. Ejecuta migraciones y semillas base.
3. Si no existe configuración operativa, abre wizard inicial.
4. El wizard crea negocio, tema, ticket y administrador inicial.
5. Al finalizar, redirige a login.

## Flujo de venta rápida

1. Cajero inicia sesión.
2. Selecciona tipo de orden.
3. Agrega productos y modificadores.
4. Ajusta cantidades y notas.
5. Selecciona método de pago o guarda orden pendiente.
6. El sistema confirma la venta en transacción.
7. El sistema intenta imprimir sin comprometer la persistencia de la venta.

## Flujo de cancelación

1. Cajero solicita cancelación con motivo.
2. Supervisor o administrador autentica la aprobación.
3. El sistema revierte saldos e inventario si corresponde.
4. Se registra auditoría y estado final.

## Flujo de corte de caja

1. Usuario autorizado abre turno.
2. Las ventas se vinculan al corte activo.
3. Usuario autorizado cierra turno con conteo real.
4. El sistema calcula diferencia y genera resumen auditable.
