using FluentResults;
using MediatR;
using RetailPricing.Application.Common.Models;

namespace RetailPricing.Application.Features.PricingRecords.Queries.SearchPricingRecords;

public record SearchPricingRecordsQuery(
    string? StoreId,
    string? Sku,
    string? ProductName,
    decimal? MinPrice,
    decimal? MaxPrice,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int PageNumber = 1,
    int PageSize = 50
) : IRequest<Result<PaginatedList<PricingRecordDto>>>;

public class PricingRecordDto
{
    public Guid Id { get; init; }
    public string StoreId { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public DateOnly EffectiveDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastModifiedAt { get; init; }
    public string? LastModifiedBy { get; init; }
}
