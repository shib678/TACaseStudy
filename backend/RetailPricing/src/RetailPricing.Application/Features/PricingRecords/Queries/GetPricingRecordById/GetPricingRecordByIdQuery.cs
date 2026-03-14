using FluentResults;
using MediatR;
using RetailPricing.Application.Features.PricingRecords.Queries.SearchPricingRecords;

namespace RetailPricing.Application.Features.PricingRecords.Queries.GetPricingRecordById;

public record GetPricingRecordByIdQuery(Guid Id) : IRequest<Result<PricingRecordDto>>;
