namespace FogonDesk.Domain.Catalog
{
    public sealed class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Sku { get; set; }
        public string Name { get; set; }
        public decimal SalePrice { get; set; }
        public decimal EstimatedCost { get; set; }
        public bool UsesInventory { get; set; }
        public decimal StockOnHand { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class ModifierGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SelectionMode { get; set; }
        public int MinSelection { get; set; }
        public int MaxSelection { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class ModifierOption
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string Name { get; set; }
        public decimal ExtraPrice { get; set; }
        public decimal EstimatedCost { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class ComboDefinition
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal SalePrice { get; set; }
        public decimal EstimatedCost { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class ComboItem
    {
        public int Id { get; set; }
        public int ComboId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
    }

    public sealed class DigitalPlatform
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int NextSequence { get; set; }
    }

    public sealed class DiningTable
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int Seats { get; set; }
        public bool IsActive { get; set; }
    }
}
