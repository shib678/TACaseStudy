using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using RetailPricing.Application.Common.Interfaces;
using RetailPricing.Domain.Repositories;

namespace RetailPricing.Application.Features.PricingRecords.Commands.UpdatePricingRecord;

public class UpdatePricingRecordCommandHandler
    : IRequestHandler<UpdatePricingRecordCommand, Result<UpdatePricingRecordResult>>
{
    private readonly IPricingRecordRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePricingRecordCommandHandler> _logger;

    public UpdatePricingRecordCommandHandler(
        IPricingRecordRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdatePricingRecordCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UpdatePricingRecordResult>> Handle(
        UpdatePricingRecordCommand request,
        CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (record is null)
        {
            return Result.Fail($"Pricing record with ID '{request.Id}' was not found.");
        }

        record.UpdatePrice(request.Price, request.ProductName, request.ModifiedBy);
        _repository.Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated pricing record {Id} by {User}", request.Id, request.ModifiedBy);

        return Result.Ok(new UpdatePricingRecordResult
        {
            Id = record.Id,
            StoreId = record.StoreId,
            Sku = record.Sku,
            ProductName = record.ProductName,
            Price = record.Price,
            EffectiveDate = record.EffectiveDate,
            CurrencyCode = record.CurrencyCode,
            LastModifiedAt = record.LastModifiedAt ?? DateTime.UtcNow,
            LastModifiedBy = record.LastModifiedBy ?? request.ModifiedBy
        });
    }
}
