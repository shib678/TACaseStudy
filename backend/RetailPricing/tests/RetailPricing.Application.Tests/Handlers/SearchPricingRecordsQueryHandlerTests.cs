using FluentAssertions;
using NSubstitute;
using RetailPricing.Application.Features.PricingRecords.Queries.SearchPricingRecords;
using RetailPricing.Domain.Entities;
using RetailPricing.Domain.Repositories;
using Xunit;

namespace RetailPricing.Application.Tests.Handlers;

public class SearchPricingRecordsQueryHandlerTests
{
    private readonly IPricingRecordRepository         _repository = Substitute.For<IPricingRecordRepository>();
    private readonly SearchPricingRecordsQueryHandler _handler;

    public SearchPricingRecordsQueryHandlerTests()
    {
        _handler = new SearchPricingRecordsQueryHandler(_repository);
    }

    // ─────────────────────────────────────────────
    //  Basic happy-path
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReturnsOkResult()
    {
        SetupRepository(records: SampleRecords(2), total: 2);
        var query = DefaultQuery();

        var result = await _handler.Handle(query, default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsPaginatedListWithCorrectItemCount()
    {
        SetupRepository(records: SampleRecords(3), total: 3);

        var result = await _handler.Handle(DefaultQuery(), default);

        result.Value.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ReturnsPaginatedListWithCorrectTotalCount()
    {
        SetupRepository(records: SampleRecords(3), total: 150);

        var result = await _handler.Handle(DefaultQuery(), default);

        result.Value.TotalCount.Should().Be(150);
    }

    [Fact]
    public async Task Handle_ReturnsPaginatedListWithCorrectPageNumber()
    {
        SetupRepository(records: SampleRecords(1), total: 100);
        var query = DefaultQuery() with { PageNumber = 3 };

        var result = await _handler.Handle(query, default);

        result.Value.PageNumber.Should().Be(3);
    }

    // ─────────────────────────────────────────────
    //  DTO mapping
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_MapsStoreId_Correctly()
    {
        var record = MakeRecord(storeId: "AU-1001");
        SetupRepository(new[] { record }, total: 1);

        var result = await _handler.Handle(DefaultQuery(), default);

        result.Value.Items.First().StoreId.Should().Be("AU-1001");
    }

    [Fact]
    public async Task Handle_MapsSku_Correctly()
    {
        var record = MakeRecord(sku: "SKU-XYZ");
        SetupRepository(new[] { record }, total: 1);

        var result = await _handler.Handle(DefaultQuery(), default);

        result.Value.Items.First().Sku.Should().Be("SKU-XYZ");
    }

    [Fact]
    public async Task Handle_MapsPrice_Correctly()
    {
        var record = MakeRecord(price: 7.49m);
        SetupRepository(new[] { record }, total: 1);

        var result = await _handler.Handle(DefaultQuery(), default);

        result.Value.Items.First().Price.Should().Be(7.49m);
    }

    [Fact]
    public async Task Handle_MapsCurrencyCode_Correctly()
    {
        var record = MakeRecord(currency: "GBP");
        SetupRepository(new[] { record }, total: 1);

        var result = await _handler.Handle(DefaultQuery(), default);

        result.Value.Items.First().CurrencyCode.Should().Be("GBP");
    }

    [Fact]
    public async Task Handle_MapsEffectiveDate_Correctly()
    {
        var date   = new DateOnly(2024, 3, 15);
        var record = MakeRecord(effectiveDate: date);
        SetupRepository(new[] { record }, total: 1);

        var result = await _handler.Handle(DefaultQuery(), default);

        result.Value.Items.First().EffectiveDate.Should().Be(date);
    }

    [Fact]
    public async Task Handle_MapsAllIds_AsNonEmpty()
    {
        SetupRepository(records: SampleRecords(5), total: 5);

        var result = await _handler.Handle(DefaultQuery(), default);

        result.Value.Items.Should().AllSatisfy(dto => dto.Id.Should().NotBeEmpty());
    }

    // ─────────────────────────────────────────────
    //  PageSize hard cap
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_PageSizeOver200_IsCappedAt200()
    {
        SetupRepository(records: SampleRecords(1), total: 1);
        var query = DefaultQuery() with { PageSize = 500 };

        await _handler.Handle(query, default);

        // Verify the repository was called with pageSize <= 200
        await _repository.Received().SearchAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<decimal?>(), Arg.Any<decimal?>(),
            Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(),
            Arg.Any<int>(),
            Arg.Is<int>(ps => ps <= 200),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PageSizeOf50_IsPassedThrough()
    {
        SetupRepository(records: SampleRecords(1), total: 1);
        var query = DefaultQuery() with { PageSize = 50 };

        await _handler.Handle(query, default);

        await _repository.Received().SearchAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<decimal?>(), Arg.Any<decimal?>(),
            Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(),
            Arg.Any<int>(), Arg.Is<int>(ps => ps == 50),
            Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────
    //  Empty results
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoRecordsFound_ReturnsOkWithEmptyList()
    {
        SetupRepository(records: Array.Empty<PricingRecord>(), total: 0);

        var result = await _handler.Handle(DefaultQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // ─────────────────────────────────────────────
    //  Filter pass-through to repository
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_PassesFiltersToRepository()
    {
        SetupRepository(records: Array.Empty<PricingRecord>(), total: 0);

        var query = new SearchPricingRecordsQuery(
            StoreId:    "AU-1001",
            Sku:        "SKU-ABC",
            ProductName: "Milk",
            MinPrice:   1.00m,
            MaxPrice:   9.99m,
            DateFrom:   new DateOnly(2024, 1, 1),
            DateTo:     new DateOnly(2024, 12, 31),
            PageNumber: 2,
            PageSize:   25);

        await _handler.Handle(query, default);

        await _repository.Received(1).SearchAsync(
            "AU-1001", "SKU-ABC", "Milk",
            1.00m, 9.99m,
            new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31),
            2, 25,
            Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private void SetupRepository(IEnumerable<PricingRecord> records, int total) =>
        _repository
            .SearchAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<decimal?>(), Arg.Any<decimal?>(),
                Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(),
                Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<PricingRecord>)records.ToList(), total));

    private static SearchPricingRecordsQuery DefaultQuery() =>
        new(null, null, null, null, null, null, null, PageNumber: 1, PageSize: 50);

    private static PricingRecord MakeRecord(
        string  storeId       = "AU-1001",
        string  sku           = "SKU-001",
        string  productName   = "Test Product",
        decimal price         = 1.99m,
        string  currency      = "AUD",
        DateOnly? effectiveDate = null)
        => PricingRecord.Create(
            storeId, sku, productName, price,
            effectiveDate ?? new DateOnly(2024, 1, 15),
            currency,
            Guid.NewGuid());

    private static IEnumerable<PricingRecord> SampleRecords(int count) =>
        Enumerable.Range(1, count).Select(i => MakeRecord(
            sku: $"SKU-{i:000}",
            productName: $"Product {i}",
            price: i * 1.00m));
}
