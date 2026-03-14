namespace RetailPricing.Application.Common.Models;

public class CsvPricingRow
{
    public int RowNumber { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string PriceRaw { get; set; } = string.Empty;
    public string DateRaw { get; set; } = string.Empty;

    // Parsed values
    public decimal? Price { get; set; }
    public DateOnly? EffectiveDate { get; set; }

    // Validation
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
}
