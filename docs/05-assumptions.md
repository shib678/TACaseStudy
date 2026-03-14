# Assumptions — Retail Pricing Feed System

## Business Assumptions

| # | Assumption                                                                                                        |
|---|-------------------------------------------------------------------------------------------------------------------|
| 1 | Store IDs are pre-existing master data; the system validates uploads against a known list of valid Store IDs.     |
| 2 | SKUs are alphanumeric strings managed by the product catalogue system (out of scope). No SKU validation against an external catalogue in this implementation. |
| 3 | Prices are stored in the currency of the store's country. Currency conversion is out of scope.                    |
| 4 | A single store may submit multiple CSV uploads per day; the latest upload for a (StoreId, SKU, Date) combination is the authoritative price. |
| 5 | "Date" in the CSV represents the effective pricing date (the date the price is valid), not the upload date.       |
| 6 | Historical pricing records are retained; old records are not deleted when new ones are uploaded — they are updated in place via upsert. |
| 7 | Store managers upload pricing for their own store only. Cross-store uploads are rejected.                         |
| 8 | Pricing Analysts and Corporate Buyers have read-only search access. Only designated admin users can edit records. |
| 9 | The CSV file format is fixed: `StoreId, SKU, ProductName, Price, Date` with a mandatory header row.              |
| 10| Maximum CSV file size per upload is 10MB (~100,000 rows). Larger files require a bulk import process (out of scope). |

## Technical Assumptions

| # | Assumption                                                                                                        |
|---|-------------------------------------------------------------------------------------------------------------------|
| 1 | Azure is the target cloud platform. The architecture uses Azure SQL, Azure Blob Storage, Azure Redis Cache, and Azure AD. AWS/GCP equivalents would work with configuration changes. |
| 2 | The development environment uses Docker Compose for local SQL Server, Redis, and the three frontend apps.         |
| 3 | CI/CD pipelines (Azure DevOps or GitHub Actions) are assumed to exist but are not implemented in this deliverable. |
| 4 | The EF Core migrations target SQL Server. The connection string uses a placeholder `[YourConnectionString]`.     |
| 5 | Authentication configuration (Azure AD tenant ID, client ID) uses placeholder values in `appsettings.json`. Production values would be in Azure Key Vault. |
| 6 | Node.js 18+ and .NET 8 SDK are installed in the development environment.                                         |
| 7 | HTTPS certificates for local development use the .NET dev certificate (`dotnet dev-certs https --trust`).        |
| 8 | Shared Redux state between microfrontends is managed at the Shell level; MFEs communicate via shared store slices, not direct MFE-to-MFE communication. |
| 9 | The microfrontends run on separate ports locally (Shell: 3000, Upload MFE: 3001, Search MFE: 3002).             |
| 10| Background processing (large file uploads) would use Azure Service Bus in production. For the MVP, this is synchronous with a 30-second timeout. |

## Scope Exclusions

The following are explicitly out of scope for this implementation:

- Price approval workflows / price change authorisation
- Integration with external ERP or product catalogue systems
- Real-time price push to point-of-sale systems
- Mobile application
- Reporting / analytics dashboards (PowerBI integration assumed separate)
- Multi-tenancy (each region's data is in the same database, separated by StoreId)
- CSV template download / guided upload wizard
- Email notifications on upload completion

## CSV Format Assumption

```csv
StoreId,SKU,ProductName,Price,Date
AU-1001,SKU-ABC123,Full Cream Milk 2L,3.99,2024-01-15
AU-1001,SKU-DEF456,Organic Bread 700g,5.49,2024-01-15
GB-2001,SKU-ABC123,Full Cream Milk 2L,1.89,2024-01-15
```

| Column      | Type          | Validation                              |
|-------------|---------------|-----------------------------------------|
| StoreId     | string        | Required, max 20 chars, alphanumeric+hyphen |
| SKU         | string        | Required, max 50 chars, alphanumeric    |
| ProductName | string        | Required, max 200 chars                 |
| Price       | decimal       | Required, > 0, max 2 decimal places     |
| Date        | date (ISO 8601) | Required, yyyy-MM-dd, not future date |
