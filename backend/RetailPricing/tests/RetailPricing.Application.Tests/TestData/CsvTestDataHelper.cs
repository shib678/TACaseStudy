namespace RetailPricing.Application.Tests.TestData;

/// <summary>
/// Resolves paths to CSV test fixture files that are copied to the build output directory.
/// </summary>
public static class CsvTestDataHelper
{
    // ── File name constants ─────────────────────────────────────────────
    public const string ValidPrices   = "valid-prices.csv";
    public const string MixedPrices   = "mixed-prices.csv";
    public const string InvalidPrices = "invalid-prices.csv";
    public const string EmptyPrices   = "empty-prices.csv";

    // ── Row-count expectations (kept in one place for DRY tests) ────────
    /// <summary>Total data rows in valid-prices.csv (excludes header).</summary>
    public const int ValidPricesTotalRows = 75;

    /// <summary>Total data rows in mixed-prices.csv (excludes header).</summary>
    public const int MixedPricesTotalRows = 20;

    /// <summary>Expected number of valid rows in mixed-prices.csv.</summary>
    public const int MixedPricesValidRows = 13;

    /// <summary>Expected number of invalid rows in mixed-prices.csv.</summary>
    public const int MixedPricesInvalidRows = 7;

    /// <summary>Total data rows in invalid-prices.csv (excludes header).</summary>
    public const int InvalidPricesTotalRows = 14;

    // ── Path resolution ─────────────────────────────────────────────────

    /// <summary>
    /// Returns the absolute path to the given CSV fixture file.
    /// The file must be listed under TestData\ in the .csproj with
    /// <c>CopyToOutputDirectory = PreserveNewest</c>.
    /// </summary>
    public static string GetPath(string fileName)
    {
        var dir = Path.GetDirectoryName(typeof(CsvTestDataHelper).Assembly.Location)
                  ?? AppContext.BaseDirectory;

        var path = Path.Combine(dir, "TestData", fileName);

        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"CSV test fixture '{fileName}' was not found at '{path}'. " +
                "Ensure CopyToOutputDirectory=PreserveNewest is set in the .csproj.", path);

        return path;
    }

    /// <summary>Opens a read-only <see cref="FileStream"/> for the given CSV fixture.</summary>
    public static FileStream OpenStream(string fileName)
        => File.OpenRead(GetPath(fileName));

    /// <summary>Reads the raw CSV text of the given fixture file.</summary>
    public static string ReadAllText(string fileName)
        => File.ReadAllText(GetPath(fileName));

    /// <summary>
    /// Returns all data lines (strips the header row and any blank lines).
    /// </summary>
    public static IReadOnlyList<string> DataLines(string fileName)
        => File.ReadAllLines(GetPath(fileName))
               .Skip(1)                           // skip header
               .Where(l => !string.IsNullOrWhiteSpace(l))
               .ToList();
}
