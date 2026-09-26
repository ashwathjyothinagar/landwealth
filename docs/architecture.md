# Architecture Specification: LandWealth

## 1. Architectural Philosophy & Strategy

LandWealth is engineered as a **Modular Monolith** adhering to the tenets of **Clean Architecture** (Ports and Adapters / Onion Architecture). The application is structured to ensure:

1. **Independent Core Domain**: The financial domain models, valuation rules, and parcel structures have zero external dependencies on frameworks, databases, or UI layers.
2. **Deterministic Financial Flow**: Financial logic lives strictly inside the Application and Domain boundaries. Neither the presentation API nor the React UI perform unverified financial arithmetic.
3. **Multi-Tenant User Isolation**: Every database interaction is scoped to the authenticated user's boundary, enforced at the data access tier (EF Core Global Query Filters).
4. **Auditability & Non-Destructive Operations**: Financial transactions are immutable once posted. Corrections are executed through adjustments and reversals, preserving full ledger audit trails.

---

## 2. Layered Architecture & Dependency Flow

```
+-------------------------------------------------------------------+
|                        LandWealth.Api                             |
|  (ASP.NET Core 9, REST Controllers, Filters, Swagger, Auth Config) |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|                     LandWealth.Application                        |
|   (CQRS Commands/Queries, MediatR/Handlers, DTOs, FluentValidation|
|                Domain Event Handlers, Interfaces)                 |
+-------------------------------------------------------------------+
               |                                     ^
               v                                     |
+------------------------------------+   +---------------------------+
|         LandWealth.Domain          |   | LandWealth.Infrastructure |
| (Entities, Value Objects, Enums,   |   | (EF Core, MySQL Pomelo,   |
|  Exceptions, Accounting Logic)     |   |  File Storage, JWT, Clock,|
+------------------------------------+   |  Audit Interceptors)      |
                                         +---------------------------+
```

### 2.1 Dependency Rules
- **LandWealth.Domain**: Core entity graph and enterprise business rules. Contains no dependencies on other projects, no NuGet references to web or ORM packages.
- **LandWealth.Application**: Depends **only** on `LandWealth.Domain`. Defines interfaces (`IApplicationDbContext`, `ICurrentUserService`, `IFileStorageService`, `IDateTimeProvider`) and orchestrates use cases.
- **LandWealth.Infrastructure**: Depends on `LandWealth.Application` and `LandWealth.Domain`. Implements persistence, external storage, hashing, and database migrations.
- **LandWealth.Api**: Depends on `LandWealth.Application` and `LandWealth.Infrastructure` (solely for dependency injection wiring). Serves HTTP endpoints, validates input models, and translates exceptions into standard RFC 7807 Problem Details.
- **landwealth-web**: Standalone Single Page Application (SPA). Communicates strictly with `LandWealth.Api` over HTTPS JSON REST contracts.

---

## 3. Project Structure

```
LandWealth/
├── .github/
│   ├── copilot-instructions.md
│   └── prompts/
│       ├── architecture.prompt.md
│       ├── backend-feature.prompt.md
│       ├── code-review.prompt.md
│       ├── database.prompt.md
│       ├── frontend-feature.prompt.md
│       ├── security-review.prompt.md
│       └── testing.prompt.md
├── docs/
│   ├── accounting-rules.md
│   ├── architecture.md
│   ├── database-design.md
│   ├── product-requirements.md
│   ├── roadmap.md
│   └── decisions/
│       ├── 0001-transaction-double-entry-accounting-model.md
│       ├── 0002-modular-monolith-clean-architecture.md
│       ├── 0003-multi-tenant-data-isolation-security.md
│       ├── 0004-secure-document-storage-and-access.md
│       └── 0005-indian-rupee-precision-and-number-system.md
├── src/
│   ├── LandWealth.Api/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Filters/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── LandWealth.Application/
│   │   ├── Common/
│   │   │   ├── Behaviors/
│   │   │   ├── Exceptions/
│   │   │   ├── Interfaces/
│   │   │   └── Models/
│   │   ├── Features/
│   │   │   ├── Accounts/
│   │   │   ├── Dashboard/
│   │   │   ├── Documents/
│   │   │   ├── NetWorth/
│   │   │   ├── Properties/
│   │   │   ├── Reminders/
│   │   │   ├── Reports/
│   │   │   ├── Transactions/
│   │   │   └── Valuations/
│   ├── LandWealth.Domain/
│   │   ├── Common/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── Events/
│   │   ├── Exceptions/
│   │   └── ValueObjects/
│   ├── LandWealth.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Configurations/
│   │   │   ├── Interceptors/
│   │   │   └── Migrations/
│   │   ├── Services/
│   │   └── Storage/
│   └── landwealth-web/
│       ├── src/
│       │   ├── api/
│       │   ├── components/
│       │   ├── features/
│       │   ├── hooks/
│       │   ├── layout/
│       │   ├── routes/
│       │   ├── types/
│       │   └── utils/
│       ├── package.json
│       └── vite.config.ts
├── tests/
│   ├── LandWealth.Domain.UnitTests/
│   ├── LandWealth.Application.UnitTests/
│   ├── LandWealth.IntegrationTests/
│   └── landwealth-web.tests/
├── docker-compose.yml
├── LandWealth.sln
└── README.md
```

---

## 4. Key Architectural Patterns

### 4.1 Clean Architecture / Vertical Slice Hybrid
Within `LandWealth.Application`, code is organized by feature domain (e.g., `Features/Properties/Commands/CreateProperty`, `Features/Transactions/Queries/GetTransactionSummary`). This combines the encapsulation of Clean Architecture with the cohesive discoverability of Vertical Slices.

### 4.2 CQRS (Command Query Responsibility Segregation)
- **Commands**: Modify state, execute business rules, emit domain events, return minimal resource identifiers or updated view models. Enforced by `FluentValidation` pipelines.
- **Queries**: Read-only, projections to flat DTOs (`AsNoTracking()`), optimized for fast dashboard aggregation and ledger pagination.

### 4.3 Data Isolation & Multi-Tenancy Architecture
- Every entity belonging to a user inherits from `AuditableUserEntity` with a required `UserId` property.
- `ApplicationDbContext` applies an EF Core Global Query Filter on all user-scoped entities:
  ```csharp
  builder.Entity<TEntity>().HasQueryFilter(e => e.UserId == _currentUserService.UserId);
  ```
- This guarantees defense-in-depth against Insecure Direct Object Reference (IDOR) attacks: even if a client requests an entity by another user's GUID, EF Core automatically returns null/not found.

### 4.4 Auditing & Change Tracking Interceptor
An EF Core `SaveChangesInterceptor` (`AuditInterceptor`) automatically populates:
- `CreatedAt` (UTC timestamp)
- `CreatedBy` (User ID from JWT claims)
- `LastModifiedAt` (UTC timestamp)
- `LastModifiedBy` (User ID from JWT claims)

For high-consequence entities (Transactions, Accounts, Valuations), an `AuditLog` entry is appended capturing the change event, previous snapshot, and new snapshot.

---

## 5. Security & Threat Mitigation Architecture

1. **Authentication**: JWT Bearer Tokens issued via standard ASP.NET Core Identity or a dedicated Auth handler using Argon2id/BCrypt password hashing.
2. **Zero-Trust Document Access**:
   - Files are stored on physical disk or cloud bucket under cryptographically generated GUIDs (`StorageKey`).
   - The user never accesses storage paths directly.
   - All download requests go through an authorized endpoint: `GET /api/documents/{id}/download`.
   - The endpoint verifies user ownership, checks file existence, and streams bytes with a sanitized `Content-Disposition` header.
3. **Input Validation**: Strict validation using FluentValidation pipeline behaviors before requests ever reach domain handlers.
4. **Data Masking**: Account numbers are masked at ingestion (only the last 4 digits are saved, e.g., `•••• 5521`), preventing storage of raw banking credentials.

---

## 6. Frontend Architectural Guidelines

1. **State Management**:
   - **Server State**: Managed exclusively by **TanStack Query (React Query)** with clear cache invalidation tags (`['properties']`, `['accounts']`, `['transactions']`).
   - **Client/UI State**: Local component state (`useState`, `useReducer`) or minimal lightweight context (for theme/auth).
2. **Component Separation**:
   - UI views consume typed API hooks (`useProperties()`, `useCreateTransaction()`).
   - Pure presentation components (tables, cards, dialogs) are decoupled from network calls.
3. **No Financial Math in React**:
   - The frontend formats and displays numbers (`formatIndianRupee(amount)`).
   - The frontend **never** calculates cost basis, unrealized gain, or account balances. All aggregated figures are computed and certified by the backend domain/application tier.
