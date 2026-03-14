using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RetailPricing.Application.Common.Interfaces;
using RetailPricing.Application.Common.Models;
using RetailPricing.Application.Features.PricingRecords.Commands.UploadPricingFeed;
using RetailPricing.Domain.Entities;
using RetailPricing.Domain.Repositories;
using Xunit;

namespace RetailPricing.Application.Tests.Handlers;

public class UploadPricingFeedCommandHandlerTests
{
    private readonly IUploadBatchRepository  _batchRepository   = Substitute.For<IUploadBatchRepository>();
    private readonly IPricingRecordRepository _pricingRepository = Substitute.For<IPricingRecordRepository>();
    private readonly ICsvParserService        _csvParser         = Substitute.For<ICsvParserService>();
    private readonly IUnitOfWork              _unitOfWork        = Substitute.For<IUnitOfWork>();
    private readonly UploadPricingFeedCommandHandler _handler;

    public UploadPricingFeedCommandHandlerTests()
    {
        _handler = new UploadPricingFeedCommandHandler(
            _batchRepository,
            _pricingRepository,
            _csvParser,
            _unitOfWork,
            NullLogger<UploadPricingFeedCommandHandler>.Instance);
    }

    // ─────────────────────────────────────────────
    //  Empty CSV
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyCsv_ReturnsFail()
    {
        SetupParser(rows: Array.Empty<CsvPricingRow>());

        var result = await _handler.Handle(ValidCommand(), default);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EmptyCsv_ErrorMessageMentionsNoDataRows()
    {
        SetupParser(rows: Array.Empty<CsvPricingRow>());

        var result = await _handler.Handle(ValidCommand(), default);

        result.Errors.Should().ContainSingle(e => e.Message.Contains("no data rows"));
    }

    [Fact]
    public async Task Handle_EmptyCsv_SavesChangesOnce_ForBatchCreation()
    {
        SetupParser(rows: Array.Empty<CsvPricingRow>());

        await _handler.Handle(ValidCommand(), default);

        // SaveChanges called once to persist initial Pending batch + once to mark it Failed
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyCsv_DoesNotCallBulkUpsert()
    {
        SetupParser(rows: Array.Empty<CsvPricingRow>());

        await _handler.Handle(ValidCommand(), default);

        await _pricingRepository.DidNotReceive()
              .BulkUpsertAsync(Arg.Any<IEnumerable<PricingRecord>>(), Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────
    //  All rows valid
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_AllValidRows_ReturnsOkResult()
    {
        SetupParser(rows: ValidRows(5));

        var result = await _handler.Handle(ValidCommand(), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AllValidRows_ProcessedRowsMatchTotalRows()
    {
        SetupParser(rows: ValidRows(5));

        var result = await _handler.Handle(ValidCommand(), default);

        result.Value.TotalRows.Should().Be(5);
        result.Value.ProcessedRows.Should().Be(5);
        result.Value.FailedRows.Should().Be(0);
    }

    [Fact]
    public async Task Handle_AllValidRows_RowErrorsIsEmpty()
    {
        SetupParser(rows: ValidRows(3));

        var result = await _handler.Handle(ValidCommand(), default);

        result.Value.RowErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AllValidRows_CallsBulkUpsert()
    {
        SetupParser(rows: ValidRows(10));

        await _handler.Handle(ValidCommand(), default);

        await _pricingRepository.Received(1)
              .BulkUpsertAsync(Arg.Any<IEnumerable<PricingRecord>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllValidRows_CallsBulkUpsertWith_CorrectNumberOfRecords()
    {
        SetupParser(rows: ValidRows(7));
        IEnumerable<PricingRecord>? capturedRecords = null;

        await _pricingRepository
              .BulkUpsertAsync(
                  Arg.Do<IEnumerable<PricingRecord>>(r => capturedRecords = r),
                  Arg.Any<CancellationToken>());

        await _handler.Handle(ValidCommand(), default);

        capturedRecords.Should().NotBeNull();
        capturedRecords!.Count().Should().Be(7);
    }

    [Fact]
    public async Task Handle_AllValidRows_ReturnsBatchId_AsNonEmpty()
    {
        SetupParser(rows: ValidRows(2));

        var result = await _handler.Handle(ValidCommand(), default);

        result.Value.BatchId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_AllValidRows_StatusIsCompleted()
    {
        SetupParser(rows: ValidRows(2));

        var result = await _handler.Handle(ValidCommand(), default);

        result.Value.Status.Should().Be(nameof(UploadBatchStatus.Completed));
    }

    // ─────────────────────────────────────────────
    //  Mixed rows (some valid, some invalid)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_MixedRows_ReturnsOkResult()
    {
        SetupParser(rows: MixedRows(valid: 8, invalid: 2));

        var result = await _handler.Handle(ValidCommand(), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MixedRows_CountsMatchExpected()
    {
        SetupParser(rows: MixedRows(valid: 8, invalid: 2));

        var result = await _handler.Handle(ValidCommand(), default);

        result.Value.TotalRows.Should().Be(10);
        result.Value.ProcessedRows.Should().Be(8);
        result.Value.FailedRows.Should().Be(2);
    }

    [Fact]
    public async Task Handle_MixedRows_RowErrors_ContainsInvalidRowNumbers()
    {
        SetupParser(rows: MixedRows(valid: 3, invalid: 2));

        var result = await _handler.Handle(ValidCommand(), default);

        result.Value.RowErrors.Should().HaveCount(2);
        result.Value.RowErrors.Should().AllSatisfy(e => e.Errors.Should().NotBeEmpty());
    }

    [Fact]
    public async Task Handle_MixedRows_StatusIsCompletedWithErrors()
    {
        SetupParser(rows: MixedRows(valid: 5, invalid: 2));

        var result = await _handler.Handle(ValidCommand(), default);

        result.Value.Status.Should().Be(nameof(UploadBatchStatus.CompletedWithErrors));
    }

    [Fact]
    public async Task Handle_MixedRows_BulkUpsertCalledWithOnlyValidRecords()
    {
        SetupParser(rows: MixedRows(valid: 6, invalid: 4));
        IEnumerable<PricingRecord>? capturedRecords = null;

        await _pricingRepository
              .BulkUpsertAsync(
                  Arg.Do<IEnumerable<PricingRecord>>(r => capturedRecords = r),
                  Arg.Any<CancellationToken>());

        await _handler.Handle(ValidCommand(), default);

        capturedRecords!.Count().Should().Be(6);
    }

    // ─────────────────────────────────────────────
    //  All rows invalid
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_AllInvalidRows_ReturnsOkResult_WithCompletedWithErrors()
    {
        SetupParser(rows: MixedRows(valid: 0, invalid: 5));

        var result = await _handler.Handle(ValidCommand(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(UploadBatchStatus.CompletedWithErrors));
        result.Value.ProcessedRows.Should().Be(0);
        result.Value.FailedRows.Should().Be(5);
    }

    [Fact]
    public async Task Handle_AllInvalidRows_DoesNotCallBulkUpsert()
    {
        SetupParser(rows: MixedRows(valid: 0, invalid: 3));

        await _handler.Handle(ValidCommand(), default);

        await _pricingRepository.DidNotReceive()
              .BulkUpsertAsync(Arg.Any<IEnumerable<PricingRecord>>(), Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────
    //  Currency derivation
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData("AU-1001", "AUD")]
    [InlineData("GB-2001", "GBP")]
    [InlineData("US-3001", "USD")]
    [InlineData("NZ-4001", "NZD")]
    [InlineData("CA-5001", "CAD")]
    [InlineData("XX-9999", "USD")] // unknown prefix defaults to USD
    public async Task Handle_DerivesCurrencyFromStoreIdPrefix(string storeId, string expectedCurrency)
    {
        // Arrange one valid row for this store
        var row = ValidRow(rowNumber: 1, storeId: storeId);
        SetupParser(rows: new[] { row });

        IEnumerable<PricingRecord>? captured = null;
        await _pricingRepository
              .BulkUpsertAsync(
                  Arg.Do<IEnumerable<PricingRecord>>(r => captured = r),
                  Arg.Any<CancellationToken>());

        var cmd = new UploadPricingFeedCommand("prices.csv", Stream.Null, storeId, "user");

        // Act
        await _handler.Handle(cmd, default);

        // Assert
        captured.Should().NotBeNull();
        captured!.First().CurrencyCode.Should().Be(expectedCurrency);
    }

    // ─────────────────────────────────────────────
    //  Exception path
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_CsvParserThrows_ReturnsFail()
    {
        _csvParser
            .ParseAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<CsvPricingRow>>(_ => throw new InvalidOperationException("Disk error"));

        var result = await _handler.Handle(ValidCommand(), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("Upload processing failed"));
    }

    [Fact]
    public async Task Handle_CsvParserThrows_SavesChangesToMarkBatchFailed()
    {
        _csvParser
            .ParseAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<CsvPricingRow>>(_ => throw new InvalidOperationException("Disk error"));

        await _handler.Handle(ValidCommand(), default);

        // At minimum: 1 save for Pending batch creation + 1 save for Failed status
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private void SetupParser(IEnumerable<CsvPricingRow> rows)
    {
        var list = rows.ToList();
        _csvParser
            .ParseAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CsvPricingRow>>(list));
    }

    private static UploadPricingFeedCommand ValidCommand() =>
        new("prices.csv", Stream.Null, "AU-1001", "uploader@store.com");

    private static CsvPricingRow ValidRow(
        int    rowNumber = 1,
        string storeId   = "AU-1001",
        string sku       = "SKU-001",
        decimal price    = 3.99m)
        => new()
        {
            RowNumber     = rowNumber,
            StoreId       = storeId,
            Sku           = sku,
            ProductName   = "Test Product",
            Price         = price,
            EffectiveDate = new DateOnly(2024, 1, 15),
            IsValid       = true,
            Errors        = new List<string>()
        };

    private static List<CsvPricingRow> ValidRows(int count) =>
        Enumerable.Range(1, count)
                  .Select(i => ValidRow(i, sku: $"SKU-{i:000}", price: i * 1.00m))
                  .ToList();

    private static List<CsvPricingRow> MixedRows(int valid, int invalid)
    {
        var rows = Enumerable.Range(1, valid)
                             .Select(i => ValidRow(i, sku: $"SKU-V{i:000}"))
                             .ToList();

        rows.AddRange(Enumerable.Range(valid + 1, invalid).Select(i => new CsvPricingRow
        {
            RowNumber   = i,
            StoreId     = string.Empty,   // intentionally invalid
            Sku         = string.Empty,
            ProductName = string.Empty,
            PriceRaw    = "not-a-number",
            IsValid     = false,
            Errors      = new List<string> { "StoreId is required.", "Price is not a valid number." }
        }));

        return rows;
    }
}
