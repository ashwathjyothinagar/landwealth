# ADR 0002: Modular Monolith & Clean Architecture

## Context & Problem Statement
The application needs to support personal wealth, diverse real estate holdings, multi-parcel structures, documents, valuations, reminders, and net worth calculations. We must choose an architectural topology that enables long-term maintainability, testability, and developer velocity without unnecessary distributed systems complexity.

## Options Considered

### Option A: Microservices Architecture
- Split into independent microservices: Property Service, Banking Service, Transaction Service, Document Service, Reporting Service.
- **Pros**: Independent deployability of individual services.
- **Cons**: Distributed transaction complexity (two-phase commits or saga orchestrations for transactions spanning accounts and properties), network latency, operational overhead, Kubernetes/Docker cluster management complexity, and high cognitive load for personal wealth management.

### Option B: Monolithic Layered Architecture ("Traditional N-Tier")
- A single solution with UI, Business Logic, and Data Access layers tightly coupled to Entity Framework or database tables.
- **Pros**: Easy to start.
- **Cons**: High risk of business logic leaking into presentation controllers or database stored procedures; difficult to unit test domain rules in isolation; code quickly degrades into spaghetti.

### Option C: Modular Monolith with Clean Architecture (Selected)
- A single deployable unit divided into strictly bounded projects:
  - `LandWealth.Domain`: Pure business entities, aggregates, value objects, and domain rules. Zero dependencies.
  - `LandWealth.Application`: Use cases, CQRS commands/queries, input validation, and abstraction interfaces.
  - `LandWealth.Infrastructure`: EF Core, database migrations, Pomelo MySQL, file storage, security token generation.
  - `LandWealth.Api`: REST API controllers, authentication middleware, swagger documentation.
  - `landwealth-web`: Feature-sliced React SPA.
- Internal boundaries are cleanly modularized by business feature (Properties, Parcels, Accounts, Transactions, Documents, Valuations).

## Decision
We select **Option C: Modular Monolith with Clean Architecture**.

## Consequences
- **Positive**:
  - In-process transactions with ACID guarantees inside MySQL.
  - Domain rules can be tested in milliseconds without database mocks or network fixtures.
  - Eliminates distributed system failure modes while maintaining crisp separation of concerns.
  - Clear boundaries make it straightforward to carve out specific modules in the future if scale demands.
- **Negative**:
  - Developers must respect project dependency rules (no referencing Infrastructure or EF Core inside Domain).
