using FluentAssertions;
using FluentValidation.TestHelper;
using RetailPricing.Application.Features.PricingRecords.Commands.UploadPricingFeed;
using Xunit;

namespace RetailPricing.Application.Tests.Validators;

public class UploadPricingFeedCommandValidatorTests
{
    private readonly UploadPricingFeedCommandValidator _validator = new();

    // ─────────────────────────────────────────────
    //  FileName
    // ─────────────────────────────────────────────

    [Fact]
    public void FileName_Empty_FailsWithRequiredMessage()
    {
        var cmd = ValidCommand() with { FileName = string.Empty };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.FileName)
              .WithErrorMessage("File name is required.");
    }

    [Fact]
    public void FileName_NotCsvExtension_Fails()
    {
        var cmd = ValidCommand() with { FileName = "prices.xlsx" };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.FileName)
              .WithErrorMessage("Only CSV files are accepted.");
    }

    [Theory]
    [InlineData("prices.csv")]
    [InlineData("PRICES.CSV")]
    [InlineData("store-AU-1001_2024-01-15.csv")]
    public void FileName_ValidCsvName_Passes(string fileName)
    {
        var cmd = ValidCommand() with { FileName = fileName };

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.FileName);
    }

    // ─────────────────────────────────────────────
    //  StoreId
    // ─────────────────────────────────────────────

    [Fact]
    public void StoreId_Empty_FailsWithRequiredMessage()
    {
        var cmd = ValidCommand() with { StoreId = string.Empty };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.StoreId)
              .WithErrorMessage("Store ID is required.");
    }

    [Fact]
    public void StoreId_ExceedsMaxLength_Fails()
    {
        var cmd = ValidCommand() with { StoreId = new string('A', 21) };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.StoreId)
              .WithErrorMessage("Store ID must not exceed 20 characters.");
    }

    [Theory]
    [InlineData("AU-1001 BAD")]   // contains space
    [InlineData("AU_1001")]       // contains underscore
    [InlineData("AU@1001")]       // contains @
    public void StoreId_InvalidCharacters_Fails(string storeId)
    {
        var cmd = ValidCommand() with { StoreId = storeId };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.StoreId)
              .WithErrorMessage("Store ID must be alphanumeric with hyphens only.");
    }

    [Theory]
    [InlineData("AU-1001")]
    [InlineData("GB2001")]
    [InlineData("US-NYC-42")]
    public void StoreId_ValidFormat_Passes(string storeId)
    {
        var cmd = ValidCommand() with { StoreId = storeId };

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.StoreId);
    }

    // ─────────────────────────────────────────────
    //  UploadedBy
    // ─────────────────────────────────────────────

    [Fact]
    public void UploadedBy_Empty_Fails()
    {
        var cmd = ValidCommand() with { UploadedBy = string.Empty };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.UploadedBy)
              .WithErrorMessage("Uploader identity is required.");
    }

    [Fact]
    public void UploadedBy_WithValue_Passes()
    {
        var cmd = ValidCommand() with { UploadedBy = "manager@retail.com" };

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.UploadedBy);
    }

    // ─────────────────────────────────────────────
    //  CsvStream
    // ─────────────────────────────────────────────

    [Fact]
    public void CsvStream_ExceedsMaxSize_Fails()
    {
        // Create a stream that reports 11 MB
        var oversizeStream = new FakeStream(length: 11 * 1024 * 1024);
        var cmd            = ValidCommand() with { CsvStream = oversizeStream };

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.CsvStream)
              .WithErrorMessage("File size must not exceed 10 MB.");
    }

    [Fact]
    public void CsvStream_WithinLimit_Passes()
    {
        var stream = new MemoryStream(new byte[1024]);
        var cmd    = ValidCommand() with { CsvStream = stream };

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.CsvStream);
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

    private static UploadPricingFeedCommand ValidCommand() =>
        new(
            FileName:   "prices.csv",
            CsvStream:  new MemoryStream(new byte[512]),
            StoreId:    "AU-1001",
            UploadedBy: "manager@store.com");

    /// <summary>Minimal Stream stub that exposes a fixed Length.</summary>
    private sealed class FakeStream : Stream
    {
        private readonly long _length;
        public FakeStream(long length) => _length = length;

        public override bool CanRead  => true;
        public override bool CanSeek  => true;
        public override bool CanWrite => false;
        public override long Length   => _length;
        public override long Position { get; set; }

        public override void  Flush()                                    { }
        public override int   Read(byte[] buffer, int offset, int count) => 0;
        public override long  Seek(long offset, SeekOrigin origin)       => 0;
        public override void  SetLength(long value)                      { }
        public override void  Write(byte[] buffer, int offset, int count){ }
    }
}
