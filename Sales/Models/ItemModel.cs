namespace Sales.Models
{
    public class ItemModel
    {
        private static readonly decimal SALES_TAX_RATE_PERCENTAGE = 10;
        private static readonly decimal IMPORT_TAX_RATE_PERCENTAGE = 5;
        private static readonly decimal ROUND_DECIMAL = 0.05M;

        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? Name { get; set; }
        public bool Imported { get; set; }
        public bool Exempted { get; set; }

        public decimal SalesTax => (Exempted) ? 0 : Price * (SALES_TAX_RATE_PERCENTAGE / 100);

        public decimal RoundedSalesTax => Math.Ceiling(SalesTax / ROUND_DECIMAL) * ROUND_DECIMAL;

        public decimal ImportedTax => (Imported) ? Price * (IMPORT_TAX_RATE_PERCENTAGE / 100) : 0;

        public decimal RoundedImportedTax => Math.Ceiling(ImportedTax / ROUND_DECIMAL) * ROUND_DECIMAL;

        public decimal FinalPrice => Price + RoundedSalesTax + RoundedImportedTax;

        public decimal Total => Quantity * FinalPrice;
    }
}
