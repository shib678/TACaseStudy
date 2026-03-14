using FluentResults;
using MediatR;
using RetailPricing.Application.Common.Models;
using RetailPricing.Domain.Repositories;

namespace RetailPricing.Application.Features.PricingRecords.Queries.SearchPricingRecords;

public class SearchPricingRecordsQueryHandler
    : IRequestHandler<SearchPricingRecordsQuery, Result<PaginatedList<PricingRecordDto>>>
{
    private readonly IPricingRecordRepository _repository;

    public SearchPricingRecordsQueryHandler(IPricingRecordRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PaginatedList<PricingRecordDto>>> Handle(
        SearchPricingRecordsQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 200); // hard cap

        var (items, totalCount) = await _repository.SearchAsync(
            request.StoreId,
            request.Sku,
            request.ProductName,
            request.MinPrice,
            request.MaxPrice,
            request.DateFrom,
            request.DateTo,
            request.PageNumber,
            pageSize,
            cancellationToken);

        var dtos = items.Select(r => new PricingRecordDto
        {
            Id = r.Id,
            StoreId = r.StoreId,
            Sku = r.Sku,
            ProductName = r.ProductName,
            Price = r.Price,
            CurrencyCode = r.CurrencyCode,
            EffectiveDate = r.EffectiveDate,
            CreatedAt = r.CreatedAt,
            LastModifiedAt = r.LastModifiedAt,
            LastModifiedBy = r.LastModifiedBy
        }).ToList();

        var result = new PaginatedList<PricingRecordDto>(dtos, totalCount, request.PageNumber, pageSize);

        return Result.Ok(result);
    }
}
