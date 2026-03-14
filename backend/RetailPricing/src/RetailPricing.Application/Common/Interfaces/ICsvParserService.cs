using RetailPricing.Application.Common.Models;

namespace RetailPricing.Application.Common.Interfaces;

public interface ICsvParserService
{
    Task<IReadOnlyList<CsvPricingRow>> ParseAsync(Stream csvStream, CancellationToken cancellationToken = default);
}
