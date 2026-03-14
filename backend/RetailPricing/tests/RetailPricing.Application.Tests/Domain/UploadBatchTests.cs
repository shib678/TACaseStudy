using FluentAssertions;
using RetailPricing.Domain.Entities;
using Xunit;

namespace RetailPricing.Application.Tests.Domain;

public class UploadBatchTests
{
    // ─────────────────────────────────────────────
    //  Create factory
    // ─────────────────────────────────────────────

    [Fact]
    public void Create_SetsFileName()
    {
        var batch = UploadBatch.Create("prices.csv", "AU-1001", "uploader@store.com");

        batch.FileName.Should().Be("prices.csv");
    }

    [Fact]
    public void Create_SetsStoreId()
    {
        var batch = UploadBatch.Create("prices.csv", "AU-1001", "uploader@store.com");

        batch.StoreId.Should().Be("AU-1001");
    }

    [Fact]
    public void Create_SetsCreatedBy()
    {
        var batch = UploadBatch.Create("prices.csv", "AU-1001", "uploader@store.com");

        batch.CreatedBy.Should().Be("uploader@store.com");
    }

    [Fact]
    public void Create_SetsStatusToPending()
    {
        var batch = UploadBatch.Create("prices.csv", "AU-1001", "uploader@store.com");

        batch.Status.Should().Be(UploadBatchStatus.Pending);
    }

    [Fact]
    public void Create_SetsCreatedAt_ToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var batch  = UploadBatch.Create("prices.csv", "AU-1001", "uploader@store.com");

        batch.CreatedAt.Should().BeAfter(before);
        batch.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Create_AssignsUniqueIds()
    {
        var b1 = UploadBatch.Create("a.csv", "AU-1001", "u");
        var b2 = UploadBatch.Create("b.csv", "AU-1001", "u");

        b1.Id.Should().NotBe(Guid.Empty);
        b2.Id.Should().NotBe(b1.Id);
    }

    [Fact]
    public void Create_InitialisesWithZeroRowCounts()
    {
        var batch = UploadBatch.Create("prices.csv", "AU-1001", "u");

        batch.TotalRows.Should().Be(0);
        batch.ProcessedRows.Should().Be(0);
        batch.FailedRows.Should().Be(0);
    }

    // ─────────────────────────────────────────────
    //  StartProcessing
    // ─────────────────────────────────────────────

    [Fact]
    public void StartProcessing_SetsStatusToProcessing()
    {
        var batch = UploadBatch.Create("prices.csv", "AU-1001", "u");

        batch.StartProcessing();

        batch.Status.Should().Be(UploadBatchStatus.Processing);
    }

    // ─────────────────────────────────────────────
    //  Complete — no errors
    // ─────────────────────────────────────────────

    [Fact]
    public void Complete_WithNoFailedRows_SetsStatusToCompleted()
    {
        var batch = BuildProcessingBatch();

        batch.Complete(totalRows: 100, processedRows: 100, failedRows: 0);

        batch.Status.Should().Be(UploadBatchStatus.Completed);
    }

    [Fact]
    public void Complete_WithNoFailedRows_SetsRowCounts()
    {
        var batch = BuildProcessingBatch();

        batch.Complete(totalRows: 100, processedRows: 100, failedRows: 0);

        batch.TotalRows.Should().Be(100);
        batch.ProcessedRows.Should().Be(100);
        batch.FailedRows.Should().Be(0);
    }

    [Fact]
    public void Complete_WithNoFailedRows_LeavesErrorSummaryNull()
    {
        var batch = BuildProcessingBatch();

        batch.Complete(100, 100, 0);

        batch.ErrorSummary.Should().BeNull();
    }

    // ─────────────────────────────────────────────
    //  Complete — with errors
    // ─────────────────────────────────────────────

    [Fact]
    public void Complete_WithFailedRows_SetsStatusToCompletedWithErrors()
    {
        var batch = BuildProcessingBatch();

        batch.Complete(totalRows: 100, processedRows: 90, failedRows: 10, "10 rows failed validation.");

        batch.Status.Should().Be(UploadBatchStatus.CompletedWithErrors);
    }

    [Fact]
    public void Complete_WithFailedRows_SetsErrorSummary()
    {
        var batch = BuildProcessingBatch();
        const string summary = "10 rows failed validation.";

        batch.Complete(100, 90, 10, summary);

        batch.ErrorSummary.Should().Be(summary);
    }

    [Fact]
    public void Complete_SetsLastModifiedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var batch  = BuildProcessingBatch();

        batch.Complete(50, 50, 0);

        batch.LastModifiedAt.Should().NotBeNull();
        batch.LastModifiedAt.Should().BeAfter(before);
    }

    // ─────────────────────────────────────────────
    //  Fail
    // ─────────────────────────────────────────────

    [Fact]
    public void Fail_SetsStatusToFailed()
    {
        var batch = BuildProcessingBatch();

        batch.Fail("Unexpected exception.");

        batch.Status.Should().Be(UploadBatchStatus.Failed);
    }

    [Fact]
    public void Fail_SetsErrorSummary()
    {
        var batch = BuildProcessingBatch();

        batch.Fail("Disk full.");

        batch.ErrorSummary.Should().Be("Disk full.");
    }

    [Fact]
    public void Fail_SetsLastModifiedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var batch  = BuildProcessingBatch();

        batch.Fail("Error.");

        batch.LastModifiedAt.Should().BeAfter(before);
    }

    // ─────────────────────────────────────────────
    //  SetBlobPath
    // ─────────────────────────────────────────────

    [Fact]
    public void SetBlobPath_StoresBlobStoragePath()
    {
        var batch = UploadBatch.Create("prices.csv", "AU-1001", "u");

        batch.SetBlobPath("https://storage.blob.core.windows.net/uploads/prices.csv");

        batch.BlobStoragePath.Should().Be("https://storage.blob.core.windows.net/uploads/prices.csv");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private static UploadBatch BuildProcessingBatch()
    {
        var batch = UploadBatch.Create("prices.csv", "AU-1001", "uploader@store.com");
        batch.StartProcessing();
        return batch;
    }
}
