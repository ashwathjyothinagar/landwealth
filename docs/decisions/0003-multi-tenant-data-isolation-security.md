# ADR 0003: Multi-Tenant Data Isolation & IDOR Defense

## Context & Problem Statement
LandWealth stores highly sensitive financial records, real estate deeds, bank details, and net worth figures. Even in a personal wealth setup, the application supports multiple user accounts. We must establish a bulletproof security architecture that prevents Insecure Direct Object Reference (IDOR) attacks, ensuring a user can never inspect, modify, or download another user's financial assets or documents by tampering with GUID identifiers in API requests.

## Options Considered

### Option A: Manual Controller/Handler Authorization Checks
- Each command and query handler manually runs: `if (entity.UserId != _currentUserService.UserId) throw new ForbiddenException();`.
- **Pros**: Explicit logic inside each handler.
- **Cons**: Extremely prone to human omission. If a developer forgets the check on a single endpoint (e.g., `GetValuationById` or `DownloadDocument`), a catastrophic IDOR vulnerability is introduced.

### Option B: Separate Database Per User
- Dynamic connection string routing where each user has a dedicated MySQL database schema.
- **Pros**: Complete physical separation of data.
- **Cons**: Excessive operational overhead for database schema migrations, connection pool starvation, and complex local development.

### Option C: Architectural Global Query Filters + Tenant Verification (Selected)
- Single shared schema where every user-scoped entity inherits from an interface `IUserOwnedEntity` containing `Guid UserId { get; set; }`.
- EF Core `OnModelCreating` automatically applies a Global Query Filter to all `IUserOwnedEntity` implementations:
  `modelBuilder.Entity<T>().HasQueryFilter(e => e.UserId == _currentUserService.UserId);`
- `AuditInterceptor` automatically injects the authenticated `UserId` upon entity insertion.
- Explicit cross-check on file downloads and modifications as defense-in-depth.

## Decision
We select **Option C: Architectural Global Query Filters + Defense-in-Depth**.

## Consequences
- **Positive**:
  - Impossible for a query or handler to accidentally leak another user's records; EF Core automatically injects `WHERE UserId = @currentUserId` into generated SQL.
  - Zero performance overhead with proper `(UserId, ...)` composite indexing.
  - Testable via automated security unit and integration tests.
- **Negative**:
  - Background jobs or system maintenance processes requiring global administrative scope must explicitly use `.IgnoreQueryFilters()`.
