using System.Collections.Generic;

namespace FogonDesk.Domain.Common
{
    public enum OrderKind
    {
        Mostrador = 1,
        Mesa = 2,
        ParaLlevar = 3,
        PlataformaDigital = 4
    }

    public enum PendingOrderStatus
    {
        Abierta = 1,
        EnProceso = 2,
        Lista = 3,
        Cobrada = 4,
        Cancelada = 5
    }

    public enum SaleStatus
    {
        Confirmada = 1,
        Cancelada = 2
    }

    public enum PaymentMethod
    {
        Efectivo = 1,
        Tarjeta = 2,
        Transferencia = 3,
        Mixto = 4,
        Plataforma = 5
    }

    public enum PrinterOutputMode
    {
        WindowsDriver = 1,
        RawEscPos = 2
    }

    public static class SystemRoles
    {
        public const string Administrator = "administrador";
        public const string Supervisor = "supervisor";
        public const string Cashier = "cajero";

        public static IReadOnlyCollection<string> All { get; } = new[]
        {
            Administrator,
            Supervisor,
            Cashier
        };
    }

    public static class PermissionCodes
    {
        public const string SetupSystem = "sistema.configurar";
        public const string ManageCatalog = "catalogo.gestionar";
        public const string ManageUsers = "usuarios.gestionar";
        public const string UsePointOfSale = "ventas.usar";
        public const string ManagePendingOrders = "ordenes.gestionar";
        public const string ManageInventory = "inventario.gestionar";
        public const string ApproveCancellation = "cancelaciones.aprobar";
        public const string ManageCashShift = "caja.gestionar";
        public const string ViewReports = "reportes.ver";
        public const string ManagePrinting = "impresion.gestionar";
        public const string ManageBackup = "respaldo.gestionar";

        public static IReadOnlyCollection<string> All { get; } = new[]
        {
            SetupSystem,
            ManageCatalog,
            ManageUsers,
            UsePointOfSale,
            ManagePendingOrders,
            ManageInventory,
            ApproveCancellation,
            ManageCashShift,
            ViewReports,
            ManagePrinting,
            ManageBackup
        };
    }
}
