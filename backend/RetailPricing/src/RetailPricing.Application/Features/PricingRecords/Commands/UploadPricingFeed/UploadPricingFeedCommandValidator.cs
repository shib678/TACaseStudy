using FluentValidation;

namespace RetailPricing.Application.Features.PricingRecords.Commands.UploadPricingFeed;

public class UploadPricingFeedCommandValidator : AbstractValidator<UploadPricingFeedCommand>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public UploadPricingFeedCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .Must(f => f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only CSV files are accepted.");

        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("Store ID is required.")
            .MaximumLength(20).WithMessage("Store ID must not exceed 20 characters.")
            .Matches(@"^[A-Za-z0-9\-]+$").WithMessage("Store ID must be alphanumeric with hyphens only.");

        RuleFor(x => x.UploadedBy)
            .NotEmpty().WithMessage("Uploader identity is required.");

        RuleFor(x => x.CsvStream)
            .NotNull().WithMessage("CSV file stream is required.")
            .Must(s => s != null && s.Length <= MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / 1024 / 1024} MB.");
    }
}
