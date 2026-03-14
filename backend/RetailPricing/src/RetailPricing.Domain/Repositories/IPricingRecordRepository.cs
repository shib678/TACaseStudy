using RetailPricing.Domain.Entities;

namespace RetailPricing.Domain.Repositories;

public interface IPricingRecordRepository
{
    Task<PricingRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PricingRecord> Items, int TotalCount)> SearchAsync(
        string? storeId,
        string? sku,
        string? productName,
        decimal? minPrice,
        decimal? maxPrice,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string storeId, string sku, DateOnly effectiveDate, CancellationToken cancellationToken = default);

    Task BulkUpsertAsync(IEnumerable<PricingRecord> records, CancellationToken cancellationToken = default);

    void Update(PricingRecord record);
}
