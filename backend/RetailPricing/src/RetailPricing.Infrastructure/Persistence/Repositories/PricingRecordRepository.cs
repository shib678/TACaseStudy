using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using RetailPricing.Domain.Entities;
using RetailPricing.Domain.Repositories;

namespace RetailPricing.Infrastructure.Persistence.Repositories;

public class PricingRecordRepository : IPricingRecordRepository
{
    private readonly ApplicationDbContext _context;

    public PricingRecordRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PricingRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PricingRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<PricingRecord> Items, int TotalCount)> SearchAsync(
        string? storeId,
        string? sku,
        string? productName,
        decimal? minPrice,
        decimal? maxPrice,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PricingRecords.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(storeId))
            query = query.Where(r => r.StoreId == storeId);

        if (!string.IsNullOrWhiteSpace(sku))
            query = query.Where(r => r.Sku == sku);

        if (!string.IsNullOrWhiteSpace(productName))
            query = query.Where(r => EF.Functions.Like(r.ProductName, $"%{productName}%"));

        if (minPrice.HasValue)
            query = query.Where(r => r.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(r => r.Price <= maxPrice.Value);

        if (dateFrom.HasValue)
            query = query.Where(r => r.EffectiveDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(r => r.EffectiveDate <= dateTo.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.EffectiveDate)
            .ThenBy(r => r.StoreId)
            .ThenBy(r => r.Sku)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> ExistsAsync(
        string storeId, string sku, DateOnly effectiveDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.PricingRecords
            .AnyAsync(r => r.StoreId == storeId &&
                           r.Sku == sku &&
                           r.EffectiveDate == effectiveDate, cancellationToken);
    }

    public async Task BulkUpsertAsync(
        IEnumerable<PricingRecord> records,
        CancellationToken cancellationToken = default)
    {
        var recordList = records.ToList();

        var bulkConfig = new BulkConfig
        {
            UpdateByProperties = new List<string>
            {
                nameof(PricingRecord.StoreId),
                nameof(PricingRecord.Sku),
                nameof(PricingRecord.EffectiveDate)
            },
            PropertiesToExcludeOnUpdate = new List<string>
            {
                nameof(PricingRecord.CreatedAt),
                nameof(PricingRecord.CreatedBy),
                nameof(PricingRecord.Id)
            }
        };

        await _context.BulkInsertOrUpdateAsync(recordList, bulkConfig, cancellationToken: cancellationToken);
    }

    public void Update(PricingRecord record)
    {
        _context.PricingRecords.Update(record);
    }
}
