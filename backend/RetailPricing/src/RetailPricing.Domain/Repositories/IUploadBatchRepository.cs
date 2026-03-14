using RetailPricing.Domain.Entities;

namespace RetailPricing.Domain.Repositories;

public interface IUploadBatchRepository
{
    Task<UploadBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UploadBatch>> GetByStoreIdAsync(string storeId, int take = 20, CancellationToken cancellationToken = default);
    Task AddAsync(UploadBatch batch, CancellationToken cancellationToken = default);
    void Update(UploadBatch batch);
}
