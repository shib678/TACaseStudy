using FluentAssertions;
using RetailPricing.Domain.Entities;
using Xunit;

namespace RetailPricing.Application.Tests.Domain;

public class PricingRecordTests
{
    // ─────────────────────────────────────────────
    //  Create factory
    // ─────────────────────────────────────────────

    [Fact]
    public void Create_WithValidArguments_SetsAllProperties()
    {
        // Arrange
        var batchId       = Guid.NewGuid();
        var effectiveDate = new DateOnly(2024, 6, 1);

        // Act
        var record = PricingRecord.Create(
            storeId:       "AU-1001",
            sku:           "SKU-ABC123",
            productName:   "Full Cream Milk 2L",
            price:         3.99m,
            effectiveDate: effectiveDate,
            currencyCode:  "AUD",
            uploadBatchId: batchId);

        // Assert
        record.StoreId.Should().Be("AU-1001");
        record.Sku.Should().Be("SKU-ABC123");
        record.ProductName.Should().Be("Full Cream Milk 2L");
        record.Price.Should().Be(3.99m);
        record.EffectiveDate.Should().Be(effectiveDate);
        record.CurrencyCode.Should().Be("AUD");
        record.UploadBatchId.Should().Be(batchId);
    }

    [Fact]
    public void Create_AssignsNewGuidId()
    {
        var r1 = BuildRecord();
        var r2 = BuildRecord();

        r1.Id.Should().NotBe(Guid.Empty);
        r2.Id.Should().NotBe(Guid.Empty);
        r1.Id.Should().NotBe(r2.Id);
    }

    [Fact]
    public void Create_DoesNotSetAuditFields_Initially()
    {
        var record = BuildRecord();

        record.LastModifiedAt.Should().BeNull();
        record.LastModifiedBy.Should().BeNull();
    }

    // ─────────────────────────────────────────────
    //  UpdatePrice
    // ─────────────────────────────────────────────

    [Fact]
    public void UpdatePrice_ChangesPrice()
    {
        var record = BuildRecord(price: 1.99m);

        record.UpdatePrice(4.99m, "Updated Milk", "analyst@store.com");

        record.Price.Should().Be(4.99m);
    }

    [Fact]
    public void UpdatePrice_ChangesProductName()
    {
        var record = BuildRecord(productName: "Old Name");

        record.UpdatePrice(1.99m, "New Name", "user@store.com");

        record.ProductName.Should().Be("New Name");
    }

    [Fact]
    public void UpdatePrice_SetsLastModifiedBy()
    {
        var record = BuildRecord();
        const string editor = "manager@retail.com";

        record.UpdatePrice(5.00m, "Milk", editor);

        record.LastModifiedBy.Should().Be(editor);
    }

    [Fact]
    public void UpdatePrice_SetsLastModifiedAt_ToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var record = BuildRecord();

        record.UpdatePrice(5.00m, "Milk", "user");

        record.LastModifiedAt.Should().NotBeNull();
        record.LastModifiedAt.Should().BeAfter(before);
        record.LastModifiedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void UpdatePrice_CalledTwice_KeepsLatestValues()
    {
        var record = BuildRecord(price: 1.00m);

        record.UpdatePrice(2.00m, "Version 2", "user1");
        record.UpdatePrice(3.00m, "Version 3", "user2");

        record.Price.Should().Be(3.00m);
        record.ProductName.Should().Be("Version 3");
        record.LastModifiedBy.Should().Be("user2");
    }

    [Fact]
    public void UpdatePrice_DoesNotChangeStoreIdOrSku()
    {
        var record = BuildRecord(storeId: "GB-2001", sku: "SKU-XYZ");

        record.UpdatePrice(9.99m, "New Product", "user");

        record.StoreId.Should().Be("GB-2001");
        record.Sku.Should().Be("SKU-XYZ");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private static PricingRecord BuildRecord(
        string storeId      = "AU-1001",
        string sku          = "SKU-001",
        string productName  = "Test Product",
        decimal price       = 9.99m,
        string currencyCode = "AUD")
        => PricingRecord.Create(
            storeId,
            sku,
            productName,
            price,
            new DateOnly(2024, 1, 15),
            currencyCode,
            Guid.NewGuid());
}
