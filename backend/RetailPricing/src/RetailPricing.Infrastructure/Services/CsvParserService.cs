using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using RetailPricing.Application.Common.Interfaces;
using RetailPricing.Application.Common.Models;
using System.Globalization;

namespace RetailPricing.Infrastructure.Services;

public class CsvParserService : ICsvParserService
{
    private readonly ILogger<CsvParserService> _logger;

    private static readonly string[] ExpectedHeaders =
        { "StoreId", "SKU", "ProductName", "Price", "Date" };

    public CsvParserService(ILogger<CsvParserService> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<CsvPricingRow>> ParseAsync(
        Stream csvStream, CancellationToken cancellationToken = default)
    {
        var rows = new List<CsvPricingRow>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true,
            MissingFieldFound = null
        };

        using var reader = new StreamReader(csvStream, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        ValidateHeaders(csv.HeaderRecord);

        int rowNumber = 1;
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = new CsvPricingRow { RowNumber = rowNumber };

            row.StoreId = csv.GetField("StoreId") ?? string.Empty;
            row.Sku = csv.GetField("SKU") ?? string.Empty;
            row.ProductName = csv.GetField("ProductName") ?? string.Empty;
            row.PriceRaw = csv.GetField("Price") ?? string.Empty;
            row.DateRaw = csv.GetField("Date") ?? string.Empty;

            ValidateRow(row);

            rows.Add(row);
            rowNumber++;
        }

        _logger.LogInformation("CSV parsed: {TotalRows} rows, {ValidRows} valid, {InvalidRows} invalid",
            rows.Count,
            rows.Count(r => r.IsValid),
            rows.Count(r => !r.IsValid));

        return rows;
    }

    private static void ValidateHeaders(string[]? headers)
    {
        if (headers is null)
            throw new InvalidOperationException("CSV file has no headers.");

        var missing = ExpectedHeaders.Except(headers, StringComparer.OrdinalIgnoreCase).ToList();
        if (missing.Any())
            throw new InvalidOperationException(
                $"CSV is missing required columns: {string.Join(", ", missing)}");
    }

    private static void ValidateRow(CsvPricingRow row)
    {
        if (string.IsNullOrWhiteSpace(row.StoreId))
            row.Errors.Add("StoreId is required.");
        else if (row.StoreId.Length > 20)
            row.Errors.Add("StoreId must not exceed 20 characters.");

        if (string.IsNullOrWhiteSpace(row.Sku))
            row.Errors.Add("SKU is required.");
        else if (row.Sku.Length > 50)
            row.Errors.Add("SKU must not exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(row.ProductName))
            row.Errors.Add("ProductName is required.");
        else if (row.ProductName.Length > 200)
            row.Errors.Add("ProductName must not exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(row.PriceRaw))
        {
            row.Errors.Add("Price is required.");
        }
        else if (!decimal.TryParse(row.PriceRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
        {
            row.Errors.Add($"Price '{row.PriceRaw}' is not a valid decimal number.");
        }
        else if (price <= 0)
        {
            row.Errors.Add("Price must be greater than zero.");
        }
        else
        {
            row.Price = Math.Round(price, 2);
        }

        if (string.IsNullOrWhiteSpace(row.DateRaw))
        {
            row.Errors.Add("Date is required.");
        }
        else if (!DateOnly.TryParseExact(row.DateRaw, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            row.Errors.Add($"Date '{row.DateRaw}' must be in yyyy-MM-dd format.");
        }
        else if (date > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
        {
            row.Errors.Add("Date must not be in the future.");
        }
        else
        {
            row.EffectiveDate = date;
        }

        row.IsValid = row.Errors.Count == 0;
    }
}
