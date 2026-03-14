using RetailPricing.Domain.Common;

namespace RetailPricing.Domain.Entities;

public enum UploadBatchStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    CompletedWithErrors = 3,
    Failed = 4
}

public class UploadBatch : AuditableEntity
{
    public string FileName { get; private set; } = string.Empty;
    public string StoreId { get; private set; } = string.Empty;
    public int TotalRows { get; private set; }
    public int ProcessedRows { get; private set; }
    public int FailedRows { get; private set; }
    public UploadBatchStatus Status { get; private set; }
    public string? ErrorSummary { get; private set; }
    public string? BlobStoragePath { get; private set; }

    private readonly List<PricingRecord> _pricingRecords = new();
    public IReadOnlyCollection<PricingRecord> PricingRecords => _pricingRecords.AsReadOnly();

    private UploadBatch() { }

    public static UploadBatch Create(string fileName, string storeId, string uploadedBy)
    {
        return new UploadBatch
        {
            FileName = fileName,
            StoreId = storeId,
            Status = UploadBatchStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = uploadedBy
        };
    }

    public void StartProcessing()
    {
        Status = UploadBatchStatus.Processing;
    }

    public void Complete(int totalRows, int processedRows, int failedRows, string? errorSummary = null)
    {
        TotalRows = totalRows;
        ProcessedRows = processedRows;
        FailedRows = failedRows;
        ErrorSummary = errorSummary;
        Status = failedRows > 0
            ? UploadBatchStatus.CompletedWithErrors
            : UploadBatchStatus.Completed;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = UploadBatchStatus.Failed;
        ErrorSummary = errorMessage;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void SetBlobPath(string path) => BlobStoragePath = path;
}
