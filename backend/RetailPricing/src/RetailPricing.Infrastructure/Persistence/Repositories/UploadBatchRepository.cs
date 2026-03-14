using Microsoft.EntityFrameworkCore;
using RetailPricing.Domain.Entities;
using RetailPricing.Domain.Repositories;

namespace RetailPricing.Infrastructure.Persistence.Repositories;

public class UploadBatchRepository : IUploadBatchRepository
{
    private readonly ApplicationDbContext _context;

    public UploadBatchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UploadBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UploadBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<UploadBatch>> GetByStoreIdAsync(
        string storeId, int take = 20,
        CancellationToken cancellationToken = default)
    {
        return await _context.UploadBatches
            .AsNoTracking()
            .Where(b => b.StoreId == storeId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UploadBatch batch, CancellationToken cancellationToken = default)
    {
        await _context.UploadBatches.AddAsync(batch, cancellationToken);
    }

    public void Update(UploadBatch batch)
    {
        _context.UploadBatches.Update(batch);
    }
}
