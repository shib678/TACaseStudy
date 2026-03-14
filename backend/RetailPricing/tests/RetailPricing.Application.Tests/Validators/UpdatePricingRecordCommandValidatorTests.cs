using FluentAssertions;
using FluentValidation.TestHelper;
using RetailPricing.Application.Features.PricingRecords.Commands.UpdatePricingRecord;
using Xunit;

namespace RetailPricing.Application.Tests.Validators;

public class UpdatePricingRecordCommandValidatorTests
{
    private readonly UpdatePricingRecordCommandValidator _validator = new();

    // ─────────────────────────────────────────────
    //  Id
    // ─────────────────────────────────────────────

    [Fact]
    public void Id_EmptyGuid_Fails()
    {
        var cmd = ValidCommand() with { Id = Guid.Empty };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Record ID is required.");
    }

    [Fact]
    public void Id_NonEmptyGuid_Passes()
    {
        var cmd = ValidCommand() with { Id = Guid.NewGuid() };

        _validator.TestValidate(cmd).ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    // ─────────────────────────────────────────────
    //  Price
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Price_ZeroOrNegative_Fails(decimal price)
    {
        var cmd = ValidCommand() with { Price = price };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Price must be greater than zero.");
    }

    [Fact]
    public void Price_ExceedsMaximum_Fails()
    {
        var cmd = ValidCommand() with { Price = 1_000_001m };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Price exceeds maximum allowed value.");
    }

    [Theory]
    [InlineData(1.999)]     // 3 decimal places
    [InlineData(10.123)]
    [InlineData(0.001)]
    public void Price_MoreThanTwoDecimalPlaces_Fails(decimal price)
    {
        var cmd = ValidCommand() with { Price = price };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Price must have at most 2 decimal places.");
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1.99)]
    [InlineData(999.99)]
    [InlineData(999999.99)]
    [InlineData(100)]       // integer — still 2dp
    public void Price_ValidValue_Passes(decimal price)
    {
        var cmd = ValidCommand() with { Price = price };

        _validator.TestValidate(cmd).ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    // ─────────────────────────────────────────────
    //  ProductName
    // ─────────────────────────────────────────────

    [Fact]
    public void ProductName_Empty_Fails()
    {
        var cmd = ValidCommand() with { ProductName = string.Empty };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ProductName)
              .WithErrorMessage("Product name is required.");
    }

    [Fact]
    public void ProductName_Exceeds200Chars_Fails()
    {
        var cmd = ValidCommand() with { ProductName = new string('A', 201) };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ProductName)
              .WithErrorMessage("Product name must not exceed 200 characters.");
    }

    [Fact]
    public void ProductName_Exactly200Chars_Passes()
    {
        var cmd = ValidCommand() with { ProductName = new string('A', 200) };

        _validator.TestValidate(cmd).ShouldNotHaveValidationErrorFor(x => x.ProductName);
    }

    // ─────────────────────────────────────────────
    //  ModifiedBy
    // ─────────────────────────────────────────────

    [Fact]
    public void ModifiedBy_Empty_Fails()
    {
        var cmd = ValidCommand() with { ModifiedBy = string.Empty };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ModifiedBy)
              .WithErrorMessage("Modifier identity is required.");
    }

    [Fact]
    public void ModifiedBy_WithValue_Passes()
    {
        var cmd = ValidCommand() with { ModifiedBy = "admin@retail.com" };

        _validator.TestValidate(cmd).ShouldNotHaveValidationErrorFor(x => x.ModifiedBy);
    }

    // ─────────────────────────────────────────────
    //  Multiple errors at once
    // ─────────────────────────────────────────────

    [Fact]
    public void AllInvalidFields_ReturnsMultipleErrors()
    {
        var cmd = new UpdatePricingRecordCommand(
            Id:          Guid.Empty,
            Price:       -5m,
            ProductName: string.Empty,
            ModifiedBy:  string.Empty);

        var result = _validator.TestValidate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    // ─────────────────────────────────────────────
    //  Fully valid command passes all rules
    // ─────────────────────────────────────────────

    [Fact]
    public void ValidCommand_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private static UpdatePricingRecordCommand ValidCommand() =>
        new(
            Id:          Guid.NewGuid(),
            Price:       4.99m,
            ProductName: "Full Cream Milk 2L",
            ModifiedBy:  "analyst@retail.com");
}
