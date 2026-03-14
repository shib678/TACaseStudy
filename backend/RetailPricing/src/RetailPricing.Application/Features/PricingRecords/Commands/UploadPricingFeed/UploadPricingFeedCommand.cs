using FluentResults;
using MediatR;

namespace RetailPricing.Application.Features.PricingRecords.Commands.UploadPricingFeed;

public record UploadPricingFeedCommand(
    string FileName,
    Stream CsvStream,
    string StoreId,
    string UploadedBy
) : IRequest<Result<UploadPricingFeedResult>>;

public class UploadPricingFeedResult
{
    public Guid BatchId { get; init; }
    public int TotalRows { get; init; }
    public int ProcessedRows { get; init; }
    public int FailedRows { get; init; }
    public IReadOnlyList<RowError> RowErrors { get; init; } = Array.Empty<RowError>();
    public string Status { get; init; } = string.Empty;
}

public class RowError
{
    public int RowNumber { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
