using FluentResults;
using MediatR;
using RetailPricing.Application.Features.PricingRecords.Queries.SearchPricingRecords;
using RetailPricing.Domain.Repositories;

namespace RetailPricing.Application.Features.PricingRecords.Queries.GetPricingRecordById;

public class GetPricingRecordByIdQueryHandler
    : IRequestHandler<GetPricingRecordByIdQuery, Result<PricingRecordDto>>
{
    private readonly IPricingRecordRepository _repository;

    public GetPricingRecordByIdQueryHandler(IPricingRecordRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PricingRecordDto>> Handle(
        GetPricingRecordByIdQuery request,
        CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (record is null)
            return Result.Fail($"Pricing record '{request.Id}' was not found.");

        return Result.Ok(new PricingRecordDto
        {
            Id = record.Id,
            StoreId = record.StoreId,
            Sku = record.Sku,
            ProductName = record.ProductName,
            Price = record.Price,
            CurrencyCode = record.CurrencyCode,
            EffectiveDate = record.EffectiveDate,
            CreatedAt = record.CreatedAt,
            LastModifiedAt = record.LastModifiedAt,
            LastModifiedBy = record.LastModifiedBy
        });
    }
}
