using FluentAssertions;
using RetailPricing.Application.Tests.TestData;
using Xunit;

namespace RetailPricing.Application.Tests.TestData;

/// <summary>
/// Validates the structure and content of the CSV test fixture files
/// so that downstream integration tests can trust the data they receive.
/// </summary>
public class CsvFileTests
{
    // ─────────────────────────────────────────────────────────────────────
    //  File existence
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(CsvTestDataHelper.ValidPrices)]
    [InlineData(CsvTestDataHelper.MixedPrices)]
    [InlineData(CsvTestDataHelper.InvalidPrices)]
    [InlineData(CsvTestDataHelper.EmptyPrices)]
    public void CsvFile_Exists_AtExpectedPath(string fileName)
    {
        var act = () => CsvTestDataHelper.GetPath(fileName);

        act.Should().NotThrow();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Header row
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(CsvTestDataHelper.ValidPrices)]
    [InlineData(CsvTestDataHelper.MixedPrices)]
    [InlineData(CsvTestDataHelper.InvalidPrices)]
    [InlineData(CsvTestDataHelper.EmptyPrices)]
    public void CsvFile_FirstLine_IsExpectedHeader(string fileName)
    {
        var firstLine = File.ReadLines(CsvTestDataHelper.GetPath(fileName)).First();

        firstLine.Should().Be("StoreId,SKU,ProductName,Price,Date");
    }

    [Theory]
    [InlineData(CsvTestDataHelper.ValidPrices)]
    [InlineData(CsvTestDataHelper.MixedPrices)]
    [InlineData(CsvTestDataHelper.InvalidPrices)]
    public void CsvFile_Header_HasExactlyFiveColumns(string fileName)
    {
        var header = File.ReadLines(CsvTestDataHelper.GetPath(fileName)).First();

        header.Split(',').Should().HaveCount(5);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Row counts
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidPricesCsv_HasExpectedDataRowCount()
    {
        var rows = CsvTestDataHelper.DataLines(CsvTestDataHelper.ValidPrices);

        rows.Should().HaveCount(CsvTestDataHelper.ValidPricesTotalRows);
    }

    [Fact]
    public void MixedPricesCsv_HasExpectedDataRowCount()
    {
        var rows = CsvTestDataHelper.DataLines(CsvTestDataHelper.MixedPrices);

        rows.Should().HaveCount(CsvTestDataHelper.MixedPricesTotalRows);
    }

    [Fact]
    public void InvalidPricesCsv_HasExpectedDataRowCount()
    {
        var rows = CsvTestDataHelper.DataLines(CsvTestDataHelper.InvalidPrices);

        rows.Should().HaveCount(CsvTestDataHelper.InvalidPricesTotalRows);
    }

    [Fact]
    public void EmptyPricesCsv_HasNoDataRows()
    {
        var rows = CsvTestDataHelper.DataLines(CsvTestDataHelper.EmptyPrices);

        rows.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  valid-prices.csv — content assertions
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidPricesCsv_AllDataRows_HaveFiveColumns()
    {
        var rows = CsvTestDataHelper.DataLines(CsvTestDataHelper.ValidPrices);

        rows.Should().AllSatisfy(row => row.Split(',').Should().HaveCount(5));
    }

    [Fact]
    public void ValidPricesCsv_ContainsAllFiveCountryPrefixes()
    {
        var storeIds = CsvTestDataHelper
            .DataLines(CsvTestDataHelper.ValidPrices)
            .Select(row => row.Split(',')[0].Trim());

        storeIds.Should().Contain(id => id.StartsWith("AU-"));
        storeIds.Should().Contain(id => id.StartsWith("GB-"));
        storeIds.Should().Contain(id => id.StartsWith("US-"));
        storeIds.Should().Contain(id => id.StartsWith("NZ-"));
        storeIds.Should().Contain(id => id.StartsWith("CA-"));
    }

    [Fact]
    public void ValidPricesCsv_AllPrices_ArePositiveDecimals()
    {
        var prices = CsvTestDataHelper
            .DataLines(CsvTestDataHelper.ValidPrices)
            .Select(row => row.Split(',')[3].Trim());

        prices.Should().AllSatisfy(p =>
        {
            decimal.TryParse(p, out var value).Should().BeTrue(because: $"'{p}' should be a decimal");
            value.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public void ValidPricesCsv_AllDates_AreIso8601()
    {
        var dates = CsvTestDataHelper
            .DataLines(CsvTestDataHelper.ValidPrices)
            .Select(row => row.Split(',')[4].Trim());

        dates.Should().AllSatisfy(d =>
            DateOnly.TryParseExact(d, "yyyy-MM-dd", out _)
                    .Should().BeTrue(because: $"'{d}' should be in yyyy-MM-dd format"));
    }

    [Fact]
    public void ValidPricesCsv_ContainsMultiplePricingDates()
    {
        var distinctDates = CsvTestDataHelper
            .DataLines(CsvTestDataHelper.ValidPrices)
            .Select(row => row.Split(',')[4].Trim())
            .Distinct();

        // The file includes rows for 2024-01-15 and 2024-01-22
        distinctDates.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void ValidPricesCsv_ContainsMultipleStores()
    {
        var distinctStores = CsvTestDataHelper
            .DataLines(CsvTestDataHelper.ValidPrices)
            .Select(row => row.Split(',')[0].Trim())
            .Distinct();

        distinctStores.Should().HaveCountGreaterThanOrEqualTo(6);
    }

    [Fact]
    public void ValidPricesCsv_ContainsUpsertCandidate_SameStoreSkuOnDifferentDates()
    {
        // AU-1001 / SKU-DAIRY-001 appears on 2024-01-15 AND 2024-01-22
        var rows = CsvTestDataHelper
            .DataLines(CsvTestDataHelper.ValidPrices)
            .Select(r => r.Split(','))
            .Where(c => c[0].Trim() == "AU-1001" && c[1].Trim() == "SKU-DAIRY-001")
            .ToList();

        rows.Should().HaveCountGreaterThanOrEqualTo(2,
            because: "the same store+SKU on multiple dates proves idempotent upsert behaviour");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  mixed-prices.csv — known-invalid row assertions
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void MixedPricesCsv_ContainsAtLeastOneRowWithEmptyStoreId()
    {
        var rows = CsvTestDataHelper
            .DataLines(CsvTestDataHelper.MixedPrices)
            .Select(r => r.Split(','));

        rows.Should().Contain(cols => string.IsNullOrWhiteSpace(cols[0]));
    }

    [Fact]
    public void MixedPricesCsv_ContainsAtLeastOneRowWithEmptySku()
    {
        var rows = CsvTestDataHelper
            .DataLines(CsvTestDataHelper.MixedPrices)
            .Select(r => r.Split(','));

        rows.Should().Contain(cols => string.IsNullOrWhiteSpace(cols[1]));
    }

    [Fact]
    public void MixedPricesCsv_ContainsAtLeastOneRowWithNegativePrice()
    {
        var prices = CsvTestDataHelper
            .DataLines(CsvTestDataHelper.MixedPrices)
            .Select(r => r.Split(',')[3].Trim())
            .Where(p => decimal.TryParse(p, out var v) && v < 0);

        prices.Should().NotBeEmpty();
    }

    [Fact]
    public void MixedPricesCsv_ContainsAtLeastOneRowWithNonParsablePrice()
    {
        var prices = CsvTestDataHelper
            .DataLines(CsvTestDataHelper.MixedPrices)
            .Select(r => r.Split(',')[3].Trim())
            .Where(p => !decimal.TryParse(p, out _));

        prices.Should().NotBeEmpty();
    }

    [Fact]
    public void MixedPricesCsv_ContainsAtLeastOneRowWithBadDateFormat()
    {
        var dates = CsvTestDataHelper
            .DataLines(CsvTestDataHelper.MixedPrices)
            .Select(r => r.Split(',')[4].Trim())
            .Where(d => !DateOnly.TryParseExact(d, "yyyy-MM-dd", out _));

        dates.Should().NotBeEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  invalid-prices.csv — every row should fail at least one rule
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void InvalidPricesCsv_EveryDataRow_FailsAtLeastOneValidationRule()
    {
        var rows = CsvTestDataHelper
            .DataLines(CsvTestDataHelper.InvalidPrices)
            .Select(r => r.Split(','));

        rows.Should().AllSatisfy(cols =>
        {
            var storeId     = cols[0].Trim();
            var sku         = cols[1].Trim();
            var productName = cols[2].Trim();
            var priceRaw    = cols[3].Trim();
            var dateRaw     = cols[4].Trim();

            bool validPrice = decimal.TryParse(priceRaw, out var price) && price > 0
                              && decimal.Round(price, 2) == price;
            bool validDate  = DateOnly.TryParseExact(dateRaw, "yyyy-MM-dd", out _);
            bool validStore = !string.IsNullOrWhiteSpace(storeId)
                              && System.Text.RegularExpressions.Regex.IsMatch(storeId, @"^[A-Za-z0-9\-]+$")
                              && storeId.Length <= 20;
            bool validSku   = !string.IsNullOrWhiteSpace(sku) && sku.Length <= 50;
            bool validName  = !string.IsNullOrWhiteSpace(productName);

            (validStore && validSku && validName && validPrice && validDate)
                .Should().BeFalse(because: $"row [{string.Join(",", cols)}] should have at least one invalid field");
        });
    }

    // ─────────────────────────────────────────────────────────────────────
    //  OpenStream helper
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void OpenStream_ReturnsReadableStream()
    {
        using var stream = CsvTestDataHelper.OpenStream(CsvTestDataHelper.ValidPrices);

        stream.CanRead.Should().BeTrue();
        stream.Length.Should().BeGreaterThan(0);
    }
}
