# Solution Architecture — Retail Pricing Feed System

## 1. Architecture Overview

The system follows **Clean Architecture** (Onion Architecture) on the backend, paired with a **Microfrontend (MFE)** pattern on the frontend using Webpack 5 Module Federation.

```
┌─────────────────────────────────────────────────────────────────────┐
│                        FRONTEND LAYER                               │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                  Shell Application (Host)                      │  │
│  │  React 18 | Redux Toolkit | Redux Persist | React Router 6    │  │
│  │                                                               │  │
│  │   ┌─────────────────────┐   ┌───────────────────────────┐    │  │
│  │   │   MFE: Upload       │   │   MFE: Search & Edit      │    │  │
│  │   │  (Remote: port 3001)│   │  (Remote: port 3002)      │    │  │
│  │   │                     │   │                           │    │  │
│  │   │ • Dropzone upload   │   │ • Filter panel            │    │  │
│  │   │ • Progress tracking │   │ • AG Grid / TanStack Table│    │  │
│  │   │ • Batch history     │   │ • Inline cell editing     │    │  │
│  │   │ • Error report      │   │ • Pagination / sorting    │    │  │
│  │   │ • Redux uploadSlice │   │ • Redux searchSlice       │    │  │
│  │   └─────────────────────┘   └───────────────────────────┘    │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                     Module Federation (Webpack 5)                   │
└──────────────────────────────┬──────────────────────────────────────┘
                               │ HTTPS / REST (JWT Bearer)
┌──────────────────────────────▼──────────────────────────────────────┐
│                        BACKEND LAYER (.NET 8)                       │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  Presentation (API Layer)                                    │    │
│  │  ASP.NET Core 8  |  Swagger/OpenAPI  |  API Versioning       │    │
│  │  PricingController  |  UploadController  |  ExceptionMiddleware│  │
│  └────────────────────────────┬────────────────────────────────┘    │
│                               │ MediatR (CQRS)                      │
│  ┌────────────────────────────▼────────────────────────────────┐    │
│  │  Application Layer                                           │    │
│  │  MediatR Handlers  |  FluentResults  |  FluentValidation     │    │
│  │  Commands: UploadPricingFeed, UpdatePricingRecord            │    │
│  │  Queries:  SearchPricingRecords, GetPricingRecordById        │    │
│  └────────────────────────────┬────────────────────────────────┘    │
│                               │ Repository + UnitOfWork Interface   │
│  ┌────────────────────────────▼────────────────────────────────┐    │
│  │  Domain Layer (Core Business)                                │    │
│  │  Entities: PricingRecord, UploadBatch                        │    │
│  │  Repository Interfaces  |  Domain Events                     │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                               │ Implements interfaces               │
│  ┌────────────────────────────▼────────────────────────────────┐    │
│  │  Infrastructure Layer                                        │    │
│  │  EF Core 8  |  SQL Server  |  Redis  |  Blob Storage         │    │
│  │  Repositories  |  DbContext  |  CsvParser  |  Migrations     │    │
│  └─────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
```

## 2. Backend Project Structure

```
RetailPricing/
├── RetailPricing.sln
└── src/
    ├── RetailPricing.Domain/               ← No dependencies
    │   ├── Entities/
    │   │   ├── PricingRecord.cs
    │   │   └── UploadBatch.cs
    │   ├── Common/
    │   │   ├── BaseEntity.cs
    │   │   └── AuditableEntity.cs
    │   └── Repositories/
    │       ├── IPricingRecordRepository.cs
    │       └── IUploadBatchRepository.cs
    │
    ├── RetailPricing.Application/          ← Depends on Domain only
    │   ├── Features/
    │   │   └── PricingRecords/
    │   │       ├── Commands/
    │   │       │   ├── UploadPricingFeed/
    │   │       │   │   ├── UploadPricingFeedCommand.cs
    │   │       │   │   ├── UploadPricingFeedCommandHandler.cs
    │   │       │   │   └── UploadPricingFeedCommandValidator.cs
    │   │       │   └── UpdatePricingRecord/
    │   │       │       ├── UpdatePricingRecordCommand.cs
    │   │       │       ├── UpdatePricingRecordCommandHandler.cs
    │   │       │       └── UpdatePricingRecordCommandValidator.cs
    │   │       └── Queries/
    │   │           ├── SearchPricingRecords/
    │   │           │   ├── SearchPricingRecordsQuery.cs
    │   │           │   └── SearchPricingRecordsQueryHandler.cs
    │   │           └── GetPricingRecordById/
    │   │               ├── GetPricingRecordByIdQuery.cs
    │   │               └── GetPricingRecordByIdQueryHandler.cs
    │   ├── Common/
    │   │   ├── Behaviours/
    │   │   │   ├── ValidationBehaviour.cs
    │   │   │   └── LoggingBehaviour.cs
    │   │   ├── Interfaces/
    │   │   │   ├── ICsvParserService.cs
    │   │   │   └── IUnitOfWork.cs
    │   │   └── Models/
    │   │       ├── PaginatedList.cs
    │   │       └── CsvPricingRow.cs
    │   └── DependencyInjection.cs
    │
    ├── RetailPricing.Infrastructure/       ← Depends on Application + Domain
    │   ├── Persistence/
    │   │   ├── ApplicationDbContext.cs
    │   │   ├── Configurations/
    │   │   │   ├── PricingRecordConfiguration.cs
    │   │   │   └── UploadBatchConfiguration.cs
    │   │   ├── Repositories/
    │   │   │   ├── PricingRecordRepository.cs
    │   │   │   └── UploadBatchRepository.cs
    │   │   ├── UnitOfWork.cs
    │   │   └── Migrations/
    │   │       ├── 20240101000000_InitialCreate.cs
    │   │       └── ApplicationDbContextModelSnapshot.cs
    │   ├── Services/
    │   │   └── CsvParserService.cs
    │   └── DependencyInjection.cs
    │
    └── RetailPricing.API/                  ← Depends on Application + Infrastructure
        ├── Controllers/
        │   ├── PricingController.cs
        │   └── UploadController.cs
        ├── Middleware/
        │   └── ExceptionHandlingMiddleware.cs
        ├── Filters/
        │   └── ApiResponseFilter.cs
        ├── Program.cs
        └── appsettings.json
```

## 3. Frontend Project Structure

```
frontend/
├── shell/                          ← Host Application (port 3000)
│   ├── src/
│   │   ├── App.tsx
│   │   ├── bootstrap.tsx
│   │   ├── store/
│   │   │   ├── index.ts            ← Redux store + persist config
│   │   │   └── rootReducer.ts
│   │   └── components/
│   │       ├── Layout.tsx
│   │       └── Navigation.tsx
│   ├── webpack.config.js           ← Module Federation host
│   └── package.json
│
├── mfe-upload/                     ← Upload Remote (port 3001)
│   ├── src/
│   │   ├── App.tsx
│   │   ├── bootstrap.tsx
│   │   ├── components/
│   │   │   ├── UploadForm.tsx      ← Dropzone + CSV upload
│   │   │   ├── UploadHistory.tsx   ← Batch history table
│   │   │   └── ValidationReport.tsx← Per-row error display
│   │   ├── store/
│   │   │   └── uploadSlice.ts      ← RTK slice
│   │   └── api/
│   │       └── uploadApi.ts        ← RTK Query endpoints
│   ├── webpack.config.js           ← Module Federation remote
│   └── package.json
│
└── mfe-search/                     ← Search Remote (port 3002)
    ├── src/
    │   ├── App.tsx
    │   ├── bootstrap.tsx
    │   ├── components/
    │   │   ├── SearchForm.tsx      ← Filter inputs
    │   │   ├── PricingGrid.tsx     ← TanStack Table with edit
    │   │   └── EditModal.tsx       ← Edit single record modal
    │   ├── store/
    │   │   └── searchSlice.ts      ← RTK slice
    │   └── api/
    │       └── searchApi.ts        ← RTK Query endpoints
    ├── webpack.config.js           ← Module Federation remote
    └── package.json
```

## 4. Technology Stack

| Layer           | Technology                                | Version  | Rationale                                      |
|-----------------|-------------------------------------------|----------|------------------------------------------------|
| Frontend Host   | React                                     | 18.x     | Industry standard, concurrent rendering        |
| State Mgmt      | Redux Toolkit + Redux Persist             | 2.x      | Predictable state, offline-resilient           |
| MFE             | Webpack Module Federation                 | 5.x      | Runtime composition, independent deployments   |
| API Client      | RTK Query                                 | 2.x      | Cache, polling, optimistic updates built-in    |
| UI Components   | Material UI (MUI)                         | 5.x      | Accessible, themeable, enterprise-ready        |
| Data Grid       | TanStack Table                            | 8.x      | Headless, virtualized, supports inline editing |
| CSV Parsing     | PapaParse                                 | 5.x      | Browser-side CSV validation before upload      |
| Backend API     | ASP.NET Core                              | 8.x      | LTS, high performance, cross-platform          |
| CQRS            | MediatR                                   | 12.x     | Clean separation of reads/writes               |
| Results         | FluentResults                             | 3.x      | Functional error handling, no exception abuse  |
| Validation      | FluentValidation                          | 11.x     | Declarative, testable validation rules         |
| ORM             | Entity Framework Core                     | 8.x      | Code-first migrations, LINQ queries            |
| Database        | SQL Server / Azure SQL                    | 2022     | ACID, row-level security, JSON support         |
| Caching         | Redis (StackExchange.Redis)               | 7.x      | Distributed cache for search results           |
| Auth            | Microsoft.Identity.Web (Azure AD / OIDC) | 3.x      | Enterprise SSO, MFA support                    |
| Observability   | OpenTelemetry + Azure App Insights        | latest   | Traces, metrics, logs                          |
| API Docs        | Swashbuckle (Swagger)                     | 6.x      | Auto-generated OpenAPI docs                    |
| CSV Parsing BE  | CsvHelper                                 | 33.x     | High-performance CSV parsing in .NET           |
