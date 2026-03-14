using FluentValidation;

namespace RetailPricing.Application.Features.PricingRecords.Commands.UpdatePricingRecord;

public class UpdatePricingRecordCommandValidator : AbstractValidator<UpdatePricingRecordCommand>
{
    public UpdatePricingRecordCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Record ID is required.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.")
            .LessThan(1_000_000).WithMessage("Price exceeds maximum allowed value.")
            .Must(p => decimal.Round(p, 2) == p).WithMessage("Price must have at most 2 decimal places.");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.");

        RuleFor(x => x.ModifiedBy)
            .NotEmpty().WithMessage("Modifier identity is required.");
    }
}
