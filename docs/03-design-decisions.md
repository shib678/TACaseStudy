# Design Decisions — Retail Pricing Feed System

## 1. Clean Architecture (Backend)

**Decision:** Use Clean Architecture with four explicit layers: Domain, Application, Infrastructure, API.

**Rationale:**
- Enforces strict dependency rule (inner layers have no knowledge of outer layers)
- Domain and Application layers are infrastructure-agnostic, enabling testability without a database
- Enables swap of ORM, database, or external services without touching business logic
- Aligns with SOLID principles, especially Dependency Inversion

**Trade-off:** More boilerplate than a simple 2-layer app. Justified by the scale (3000 stores, multi-country) and the need for long-term maintainability.

---

## 2. CQRS via MediatR

**Decision:** Separate read (Query) and write (Command) operations using MediatR pipeline.

**Rationale:**
- Upload is a write-heavy operation; search is read-heavy — they have different scaling needs
- Commands can be scaled independently of queries (e.g., async batch processing)
- Pipeline behaviours (logging, validation, caching) can be applied cross-cutting without polluting handlers
- Aligns with Event Sourcing if required in future

**Trade-off:** Extra indirection for simple use cases. Acceptable given the separation of upload and search concerns.

---

## 3. FluentResults for Error Handling

**Decision:** Return `Result<T>` from all Application handlers rather than throwing exceptions.

**Rationale:**
- Exceptions are for exceptional cases; business errors (invalid CSV row, duplicate SKU) are expected
- `Result<T>` forces callers to handle error paths explicitly
- Chain-able `.IsSuccess`, `.IsFailed`, `.Errors` makes API layer responses clean
- Avoids the performance cost of exception stack-unwinding on hot paths (CSV upload of 50k rows)

**Trade-off:** Developers must adapt to functional-style error handling. Mitigated by consistent patterns in handlers.

---

## 4. Microfrontend Architecture (Module Federation)

**Decision:** Separate Upload and Search as independent Webpack 5 remote applications, composed by a Shell host.

**Rationale:**
- 3000-store chain means multiple teams own different parts of the UI
- Independent deployments: Upload team can ship fixes without coordinating with Search team
- Lazy loading: Shell loads MFEs on demand, reducing initial bundle size
- Shared dependencies (React, Redux) are deduplicated by Module Federation

**Trade-off:** Added operational complexity (three build pipelines, CORS for localhost dev). Mitigated by Docker Compose for local dev and a shared CI/CD template.

---

## 5. Redux Toolkit with Redux Persist

**Decision:** Use Redux Toolkit as the state management layer with redux-persist (localStorage) for the shell.

**Rationale:**
- RTK Query handles server state (caching, polling, optimistic updates) out of the box
- redux-persist keeps user preferences (search filters, column visibility) across page refreshes
- RTK's `createSlice` eliminates Redux boilerplate
- Each MFE exposes its own slice; Shell composes them into a single store

**Trade-off:** Local storage for pricing data could become stale. Mitigated by RTK Query's `keepUnusedDataFor` TTL and `refetchOnMountOrArgChange`.

---

## 6. EF Core with Bulk Upsert Strategy

**Decision:** Use EF Core 8 with `ExecuteUpdateAsync` / `BulkExtensions` for CSV batch inserts.

**Rationale:**
- A single store CSV may contain 10,000–50,000 rows
- Standard `SaveChangesAsync()` for 50k entities would cause N+1 inserts
- `MERGE` (upsert) statement handles "re-upload same feed" scenarios idempotently
- Using `Z.EntityFramework.Extensions` or `EFCore.BulkExtensions` for true bulk operations

**Trade-off:** Bulk libraries bypass EF change tracking; domain events must be dispatched manually after bulk upsert.

---

## 7. Paginated Search with Redis Caching

**Decision:** All search queries return paginated results (default page size: 50). Frequent search combinations are cached in Redis (TTL: 5 minutes).

**Rationale:**
- 3000 stores × potentially hundreds of SKUs = millions of records
- Unbounded queries would overwhelm the database and time out
- Redis distributed cache eliminates repeated SQL queries for popular searches (e.g., "all stores, today's date")
- Cache invalidation happens on any write to affected records

**Trade-off:** Stale cache window of up to 5 minutes for search results. Acceptable for pricing data (not real-time critical at search level).

---

## 8. API Versioning

**Decision:** URL-based API versioning (`/api/v1/...`).

**Rationale:**
- Supports backwards-compatible evolution as new store regions onboard
- MFE remotes may be deployed independently at different versions
- URL versioning is most visible and easily testable vs header-based

---

## 9. CSV Upload — Validation Strategy

**Decision:** Two-pass validation: client-side (PapaParse in MFE) then server-side (FluentValidation).

**Rationale:**
- Client-side catches obvious errors (missing columns, wrong format) immediately without a network round-trip
- Server-side validation enforces business rules (valid StoreId in master data, non-negative price, date not in future)
- Returns a per-row error report so stores can correct and re-upload targeted rows

---

## 10. Authentication — Azure AD / OIDC

**Decision:** Use Microsoft.Identity.Web for OIDC/JWT authentication backed by Azure Active Directory.

**Rationale:**
- Enterprise requirement for a 3000-store chain operating across countries
- Supports MFA, Conditional Access, RBAC role claims
- Single Sign-On across Store Manager, Analyst, and Buyer personas

**Trade-off:** Ties production deployment to Azure AD. Mitigated by using standard OIDC so any provider can replace it.
