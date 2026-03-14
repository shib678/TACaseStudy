using RetailPricing.Domain.Common;

namespace RetailPricing.Domain.Entities;

public class PricingRecord : AuditableEntity
{
    public string StoreId { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public string CurrencyCode { get; private set; } = "AUD";
    public Guid UploadBatchId { get; private set; }

    // EF Core requires parameterless constructor
    private PricingRecord() { }

    public static PricingRecord Create(
        string storeId,
        string sku,
        string productName,
        decimal price,
        DateOnly effectiveDate,
        string currencyCode,
        Guid uploadBatchId)
    {
        return new PricingRecord
        {
            StoreId = storeId,
            Sku = sku,
            ProductName = productName,
            Price = price,
            EffectiveDate = effectiveDate,
            CurrencyCode = currencyCode,
            UploadBatchId = uploadBatchId
        };
    }

    public void UpdatePrice(decimal newPrice, string productName, string modifiedBy)
    {
        Price = newPrice;
        ProductName = productName;
        LastModifiedBy = modifiedBy;
        LastModifiedAt = DateTime.UtcNow;
    }

    // Navigation property
    public UploadBatch? UploadBatch { get; private set; }
}
