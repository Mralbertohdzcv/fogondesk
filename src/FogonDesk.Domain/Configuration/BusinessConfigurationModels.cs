using FogonDesk.Domain.Common;

namespace FogonDesk.Domain.Configuration
{
    public sealed class BusinessProfile
    {
        public int Id { get; set; }
        public string TradeName { get; set; }
        public string BusinessTypeCode { get; set; }
        public string Slogan { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string HeaderText { get; set; }
        public string FooterText { get; set; }
    }

    public sealed class VisualTheme
    {
        public int Id { get; set; }
        public string PrimaryColorHex { get; set; }
        public string AccentColorHex { get; set; }
    }

    public sealed class TicketProfile
    {
        public int Id { get; set; }
        public int TicketWidthMm { get; set; }
        public int MarginLeft { get; set; }
        public int MarginRight { get; set; }
        public bool AutoOpenDrawer { get; set; }
    }

    public sealed class PrinterProfile
    {
        public int Id { get; set; }
        public string PrinterName { get; set; }
        public PrinterOutputMode OutputMode { get; set; }
        public string DrawerCommandHex { get; set; }
        public int DrawerPulseOnMilliseconds { get; set; }
        public int DrawerPulseOffMilliseconds { get; set; }
    }

    public sealed class StationProfile
    {
        public int Id { get; set; }
        public string StationName { get; set; }
        public string StationCode { get; set; }
    }
}
