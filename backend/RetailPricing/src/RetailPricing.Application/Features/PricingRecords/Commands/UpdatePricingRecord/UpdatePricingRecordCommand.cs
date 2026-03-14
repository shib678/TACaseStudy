using FluentResults;
using MediatR;

namespace RetailPricing.Application.Features.PricingRecords.Commands.UpdatePricingRecord;

public record UpdatePricingRecordCommand(
    Guid Id,
    decimal Price,
    string ProductName,
    string ModifiedBy
) : IRequest<Result<UpdatePricingRecordResult>>;

public class UpdatePricingRecordResult
{
    public Guid Id { get; init; }
    public string StoreId { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public DateOnly EffectiveDate { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public DateTime LastModifiedAt { get; init; }
    public string LastModifiedBy { get; init; } = string.Empty;
}
