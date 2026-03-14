# Context Diagram — Retail Pricing Feed System

## System Context

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          EXTERNAL ACTORS                                    │
│                                                                             │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐  ┌─────────────┐  │
│  │  Store       │   │  Pricing     │   │  Corporate   │  │  Operations │  │
│  │  Managers   │   │  Analysts    │   │  Buyers      │  │  Teams      │  │
│  │ (3000 stores)│   │              │   │              │  │             │  │
│  └──────┬───────┘   └──────┬───────┘   └──────┬───────┘  └──────┬──────┘  │
│         │ Upload CSV        │ Search/Edit       │ View            │ Monitor  │
└─────────┼───────────────────┼───────────────────┼─────────────────┼─────────┘
          │                   │                   │                 │
          ▼                   ▼                   ▼                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    RETAIL PRICING FEED SYSTEM                               │
│                                                                             │
│  ┌─────────────────────────┐      ┌──────────────────────────────────────┐  │
│  │   MFE: Upload Portal    │      │      MFE: Search & Edit Portal       │  │
│  │  ─────────────────────  │      │  ──────────────────────────────────  │  │
│  │  • CSV file upload      │      │  • Multi-criteria search             │  │
│  │  • Validation feedback  │      │  • Inline record editing             │  │
│  │  • Upload history/status│      │  • Bulk edit support                 │  │
│  │  • Batch processing     │      │  • Export results                    │  │
│  └────────────┬────────────┘      └──────────────┬───────────────────────┘  │
│               │                                  │                          │
│               └──────────────┬───────────────────┘                          │
│                              │ REST / HTTPS                                 │
│                              ▼                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                    API GATEWAY / BFF (.NET 8)                         │  │
│  │  ─────────────────────────────────────────────────────────────────── │  │
│  │  • Rate limiting  • Authentication (Azure AD / OIDC)                  │  │
│  │  • Request routing • CORS policy  • API versioning                   │  │
│  └──────────────────────────────┬────────────────────────────────────────┘  │
│                                 │                                           │
│            ┌────────────────────┼────────────────────┐                      │
│            ▼                    ▼                    ▼                      │
│  ┌──────────────────┐  ┌────────────────┐  ┌─────────────────┐             │
│  │  Pricing Feed    │  │  Search &      │  │  Audit Service  │             │
│  │  Upload Service  │  │  Query Service │  │  (Change Log)   │             │
│  └────────┬─────────┘  └───────┬────────┘  └────────┬────────┘             │
│           │                    │                    │                       │
│           └────────────────────┼────────────────────┘                       │
│                                ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    DATA LAYER                                        │    │
│  │   ┌──────────────────┐    ┌──────────────────┐   ┌───────────────┐  │    │
│  │   │  SQL Server /    │    │  Redis Cache     │   │  Blob Storage │  │    │
│  │   │  Azure SQL DB    │    │  (Query Cache)   │   │  (CSV Files)  │  │    │
│  │   └──────────────────┘    └──────────────────┘   └───────────────┘  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
          │                              │
          ▼                              ▼
┌──────────────────┐           ┌──────────────────────┐
│  Azure AD /      │           │  Azure Monitor /      │
│  Identity Server │           │  App Insights         │
│  (Auth Provider) │           │  (Observability)      │
└──────────────────┘           └──────────────────────┘
```

## Key Interactions

| Actor             | System Interaction                                        | Protocol      |
|-------------------|-----------------------------------------------------------|---------------|
| Store Manager     | Uploads CSV pricing feed via browser                      | HTTPS/REST    |
| Pricing Analyst   | Searches records by Store/SKU/Date, edits inline          | HTTPS/REST    |
| Corporate Buyer   | Views aggregated pricing data across stores               | HTTPS/REST    |
| Operations Team   | Monitors upload health, validates batch processing status | HTTPS/REST    |
| Azure AD          | Authenticates all users with JWT/OIDC tokens              | OAuth 2.0     |
| Azure Monitor     | Receives telemetry and logs from all services             | SDK/Agent     |

## Data Flow — CSV Upload

```
Store Manager
     │
     │ 1. Select & upload CSV file
     ▼
MFE Upload Portal
     │
     │ 2. POST /api/v1/pricing/upload  (multipart/form-data)
     ▼
Upload Service (API)
     │
     │ 3. Validate CSV structure & business rules
     │ 4. Save raw CSV to Blob Storage
     │ 5. Parse & persist records to SQL DB (EF Core, batch upsert)
     │ 6. Publish upload-completed event
     ▼
SQL Database + Blob Storage
     │
     │ 7. Return batch ID + validation summary
     ▼
Store Manager (sees success/error report)
```

## Data Flow — Search & Edit

```
Pricing Analyst
     │
     │ 1. Enter search criteria (StoreId, SKU, DateRange, Price range)
     ▼
MFE Search Portal (Redux RTK Query cache)
     │
     │ 2. GET /api/v1/pricing/search?storeId=&sku=&from=&to=
     ▼
Query Service (API)
     │
     │ 3. Check Redis cache → if hit, return cached
     │    if miss → query SQL DB with pagination
     ▼
SQL Database / Redis Cache
     │
     │ 4. Return paginated results
     ▼
Pricing Analyst (sees editable grid)
     │
     │ 5. Edits record inline → PUT /api/v1/pricing/{id}
     ▼
Update Service → SQL DB + invalidate cache + audit log
```
