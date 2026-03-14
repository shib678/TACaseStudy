using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using RetailPricing.Application.Common.Interfaces;
using RetailPricing.Domain.Entities;
using RetailPricing.Domain.Repositories;

namespace RetailPricing.Application.Features.PricingRecords.Commands.UploadPricingFeed;

public class UploadPricingFeedCommandHandler
    : IRequestHandler<UploadPricingFeedCommand, Result<UploadPricingFeedResult>>
{
    private readonly IUploadBatchRepository _batchRepository;
    private readonly IPricingRecordRepository _pricingRepository;
    private readonly ICsvParserService _csvParser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadPricingFeedCommandHandler> _logger;

    public UploadPricingFeedCommandHandler(
        IUploadBatchRepository batchRepository,
        IPricingRecordRepository pricingRepository,
        ICsvParserService csvParser,
        IUnitOfWork unitOfWork,
        ILogger<UploadPricingFeedCommandHandler> logger)
    {
        _batchRepository = batchRepository;
        _pricingRepository = pricingRepository;
        _csvParser = csvParser;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UploadPricingFeedResult>> Handle(
        UploadPricingFeedCommand request,
        CancellationToken cancellationToken)
    {
        var batch = UploadBatch.Create(request.FileName, request.StoreId, request.UploadedBy);
        await _batchRepository.AddAsync(batch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        batch.StartProcessing();

        try
        {
            var csvRows = await _csvParser.ParseAsync(request.CsvStream, cancellationToken);

            if (csvRows.Count == 0)
            {
                batch.Fail("CSV file contains no data rows.");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Fail("CSV file contains no data rows.");
            }

            var validRows = csvRows.Where(r => r.IsValid).ToList();
            var invalidRows = csvRows.Where(r => !r.IsValid).ToList();

            var records = validRows.Select(row => PricingRecord.Create(
                row.StoreId,
                row.Sku,
                row.ProductName,
                row.Price!.Value,
                DateOnly.FromDateTime(DateTime.UtcNow),
                DeriveCurrency(row.StoreId),
                batch.Id
            )).ToList();

            if (records.Any())
            {
                await _pricingRepository.BulkUpsertAsync(records, cancellationToken);
            }

            var rowErrors = invalidRows.Select(r => new RowError
            {
                RowNumber = r.RowNumber,
                Errors = r.Errors.AsReadOnly()
            }).ToList();

            var errorSummary = invalidRows.Any()
                ? $"{invalidRows.Count} rows failed validation."
                : null;

            batch.Complete(csvRows.Count, validRows.Count, invalidRows.Count, errorSummary);
            _batchRepository.Update(batch);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Upload batch {BatchId} completed. Total: {Total}, Processed: {Processed}, Failed: {Failed}",
                batch.Id, csvRows.Count, validRows.Count, invalidRows.Count);

            return Result.Ok(new UploadPricingFeedResult
            {
                BatchId = batch.Id,
                TotalRows = csvRows.Count,
                ProcessedRows = validRows.Count,
                FailedRows = invalidRows.Count,
                RowErrors = rowErrors,
                Status = batch.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload batch {BatchId} failed unexpectedly", batch.Id);
            batch.Fail(ex.Message);
            _batchRepository.Update(batch);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Fail($"Upload processing failed: {ex.Message}");
        }
    }

    private static string DeriveCurrency(string storeId)
    {
        // Derive currency from country prefix of StoreId (e.g. "AU-1001" -> "AUD")
        var prefix = storeId.Split('-').FirstOrDefault()?.ToUpperInvariant() ?? string.Empty;
        return prefix switch
        {
            "AU" => "AUD",
            "GB" => "GBP",
            "US" => "USD",
            "NZ" => "NZD",
            "CA" => "CAD",
            _ => "USD"
        };
    }
}
