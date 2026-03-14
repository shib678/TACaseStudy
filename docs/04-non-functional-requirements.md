# Non-Functional Requirements — Retail Pricing Feed System

## NFR Summary Table

| Category        | Requirement                                                                  | Target                              | How Addressed                                                                                       |
|-----------------|------------------------------------------------------------------------------|-------------------------------------|-----------------------------------------------------------------------------------------------------|
| Performance     | CSV upload processing time                                                   | < 30 seconds for 50,000 rows        | EF Core Bulk Extensions, async pipeline, background job for large files                             |
| Performance     | Search query response time                                                   | < 500ms P95                         | Redis distributed cache, indexed DB columns, pagination (max 50 rows/page)                          |
| Performance     | API throughput                                                               | 500 concurrent uploads              | Horizontal scaling via Kubernetes/AKS pods + Azure Service Bus for async large uploads              |
| Scalability     | Store growth                                                                 | 3000 → 10,000 stores                | Stateless API pods, partitioned SQL DB by region, Redis cluster                                     |
| Scalability     | Data volume                                                                  | 500M+ pricing records               | Table partitioning by StoreId/Date, archival strategy (cold tier after 90 days)                     |
| Availability    | System uptime                                                                | 99.9% (< 8.7 hours downtime/year)   | Azure App Service / AKS multi-zone, Azure SQL geo-redundant replicas, health check endpoints        |
| Availability    | Graceful degradation                                                         | Read-only during DB maintenance     | Read replicas for queries, circuit breaker pattern (Polly) on write paths                           |
| Security        | Authentication                                                               | All APIs require valid JWT           | Microsoft.Identity.Web, Azure AD OIDC, OAuth 2.0 Bearer tokens                                     |
| Security        | Authorisation                                                                | Role-based (Store Manager, Analyst) | ASP.NET Core policy-based auth, Azure AD groups → app roles                                         |
| Security        | Data in transit                                                              | TLS 1.2+                            | HTTPS enforced at API Gateway, HSTS headers                                                         |
| Security        | Data at rest                                                                 | Encrypted                           | Azure SQL Transparent Data Encryption (TDE), Blob Storage encryption                               |
| Security        | Input validation                                                             | Prevent injection, XSS              | FluentValidation, parameterised EF queries, CSP headers, React escaping                             |
| Security        | File upload                                                                  | Only CSV, max 10MB                  | Server-side MIME validation, file size limit middleware                                              |
| Security        | Audit trail                                                                  | All changes logged                  | EF Core interceptor writes audit records with user/timestamp/old+new values                         |
| Reliability     | Idempotent uploads                                                           | Re-upload same CSV produces no dupes| MERGE upsert by (StoreId, SKU, Date) composite unique constraint                                   |
| Reliability     | Partial upload failure handling                                              | Resume/retry capability             | Transaction per batch chunk (500 rows), partial success report returned                             |
| Maintainability | Code quality                                                                 | Consistent, testable                | Clean Architecture, 80%+ unit test coverage target, xUnit + Moq, SonarQube                         |
| Maintainability | API backwards compatibility                                                  | No breaking changes to v1           | URL versioning, Swagger contract tests                                                               |
| Observability   | Logging                                                                      | Structured, centralised             | Serilog → Azure App Insights / Log Analytics; correlation IDs on every request                      |
| Observability   | Tracing                                                                      | Distributed traces                  | OpenTelemetry SDK, Jaeger/App Insights end-to-end traces                                            |
| Observability   | Metrics                                                                      | Business + technical KPIs           | Custom App Insights metrics: upload count/day, validation error rate, p95 latency                   |
| Observability   | Alerting                                                                     | Proactive incident detection        | Azure Monitor alerts: error rate > 1%, latency > 1s, upload failures > 5/hour                      |
| Internationalisation | Multi-country pricing                                                   | Currency, locale, date formats      | Store entity includes CountryCode + CurrencyCode; dates stored as UTC, displayed in local timezone  |
| Compliance      | GDPR                                                                         | Personal data minimisation          | No PII in pricing records; store manager identity in audit log only                                 |
| Compliance      | Data retention                                                               | Configurable per country            | Soft-delete + archival job; retention policy table by CountryCode                                   |
| Usability       | Upload feedback                                                              | Real-time progress                  | SignalR or polling endpoint for batch progress; per-row error report downloadable as CSV            |
| Usability       | Search responsiveness                                                        | Instant filter feedback             | Debounced search (300ms), optimistic cache updates in RTK Query                                     |
| Disaster Recovery| RPO                                                                         | < 1 hour data loss                  | Azure SQL geo-redundant backups, point-in-time restore                                              |
| Disaster Recovery| RTO                                                                         | < 4 hours full restore              | Azure AKS blue-green deployment, Traffic Manager failover                                           |
| Cost            | Cloud cost management                                                        | Optimised for usage patterns        | Auto-scaling (scale to zero for non-prod), reserved instances for prod DB, Redis cache reduces DB load |

## Detailed NFR Elaboration

### Performance

**CSV Upload Pipeline:**
- Files > 5MB are processed asynchronously via a background queue (Azure Service Bus)
- Client receives a `batchId` immediately and polls `/api/v1/upload/{batchId}/status`
- Bulk insert uses `EFCore.BulkExtensions` — benchmarked at ~50,000 rows/second on Azure SQL Business Critical

**Search Performance:**
```
SQL Indexes:
  - PricingRecords(StoreId, Date) — most common search
  - PricingRecords(SKU) — product-level pricing lookup
  - PricingRecords(Date, Price) — price range queries

Redis Cache:
  - Key pattern: pricing:search:{storeId}:{sku}:{dateFrom}:{dateTo}:{page}
  - TTL: 300 seconds
  - Eviction: on PUT /pricing/{id} invalidates all keys matching storeId
```

### Security Architecture

```
Request Flow with Security:

Browser → Azure Front Door (WAF) → API Gateway → .NET 8 API
              │
              ├── DDoS Protection
              ├── OWASP rule set
              ├── Rate limiting (100 req/min per user)
              └── TLS termination

API Gateway:
  ├── JWT validation (Azure AD public keys)
  ├── Role extraction from JWT claims
  └── Forward to downstream with correlation ID

.NET 8 API:
  ├── [Authorize(Roles = "StoreManager")] on upload endpoints
  ├── [Authorize(Roles = "PricingAnalyst,CorporateBuyer")] on search
  └── Row-level: store managers can only see their own store's data
```

### Multi-Country / Multi-Currency

- `PricingRecord` stores `CurrencyCode` (ISO 4217) alongside `Price`
- All DateTime stored as UTC; frontend converts using `Intl.DateTimeFormat` with user locale
- `StoreId` format: `{CountryCode}-{StoreNumber}` (e.g., `AU-1234`, `GB-5678`)

### Observability Stack

```
Application → OpenTelemetry SDK
                  │
                  ├── Traces → Azure App Insights (correlation across MFEs + API)
                  ├── Metrics → Azure Monitor
                  └── Logs → Serilog → Azure Log Analytics

Dashboard (Grafana / Azure Workbooks):
  - Uploads per hour per country
  - Average rows per upload
  - Validation error rate
  - Search query p50/p95/p99 latency
  - Cache hit ratio
```
