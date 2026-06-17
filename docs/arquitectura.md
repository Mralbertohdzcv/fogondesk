# Arquitectura propuesta

## Objetivo técnico

FogonDesk POS se diseña como una aplicación de escritorio nativa para Windows 8.1 de 32 bits, enfocada en rapidez operativa, tolerancia a fallos locales y mantenimiento a largo plazo.

## Compatibilidad con Windows 8.1 x86

- Runtime objetivo: .NET Framework 4.8.
- Arquitectura de compilación: `x86` y `Prefer32Bit` activado.
- UI nativa: WinForms, sin navegador incrustado y sin Electron.
- Base de datos local: SQLite en archivo local.
- Dependencias ligeras: sin servicios web obligatorios, sin PHP, sin Apache.
- Impresión encapsulada con APIs nativas de Windows y `PrintDocument`.

## Capas

### Dominio

Contiene entidades, enumeraciones y reglas núcleo del negocio: productos, ventas, órdenes pendientes, inventario, cancelaciones, caja, configuración y auditoría.

### Aplicación

Orquesta casos de uso. Define DTOs, servicios de aplicación y contratos para persistencia, autenticación, impresión, respaldo y logging.

### Infraestructura

Implementa acceso a datos SQLite, migraciones, semillas, hashing, logging local, respaldo y repositorios.

### Configuración

Resuelve rutas locales, archivos de configuración técnica y opciones por estación.

### Impresión

Aísla la generación de tickets y la salida a impresora estándar o RAW ESC/POS.

### UI Desktop

WinForms táctil en español. Solo contiene presentación, navegación y binding básico; no contiene lógica de negocio.

## Decisiones clave

- Transacciones explícitas en ventas, cancelaciones, cortes y setup inicial.
- Inventario modelado con semántica explícita: `UsaInventario` y `ExistenciaActual`.
- Órdenes pendientes normalizadas en cabecera, detalle y modificadores, evitando blobs JSON para la operación principal.
- La venta no se revierte por error de impresión; la impresión queda como evento reintentable.
- El sistema arranca con verificación de esquema y semillas base.
- La capa de persistencia se diseña con interfaces para permitir migración futura de SQLite a MariaDB/MySQL.

## Solución de proyectos

- `FogonDesk.Domain`
- `FogonDesk.Application`
- `FogonDesk.Infrastructure`
- `FogonDesk.Configuration`
- `FogonDesk.Printing`
- `FogonDesk.Desktop`

## Estrategia de despliegue

- Instalador con Inno Setup para Windows 8.1 x86.
- Archivos operativos en directorio local administrado por la aplicación.
- Backup y restauración desde UI, con validación de integridad SQLite.
