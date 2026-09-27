# Development Roadmap: LandWealth

This document establishes the multi-phase engineering plan for building LandWealth. Each phase is executed sequentially with strict quality gates: compilation, unit/integration testing, regression verification, and documentation updates.

---

## Phase Overview & Execution Status

| Phase | Description | Status | Quality Gate Criteria |
|---|---|---|---|
| **Phase 1** | **Architecture & Documentation** | **Completed** | PRD, Architecture, Accounting Rules, DB Design, Roadmap, ADRs, Copilot Instructions & Prompts finalized. |
| **Phase 2** | **Solution & Project Setup** | **Completed** | .NET 9 Clean Architecture projects created, Vite+React+TS initialized, Docker Compose running MySQL, builds & tests succeed. |
| **Phase 3** | **Domain & Database Model** | **Completed** | Core entities, value objects, EF Core configurations, Pomelo MySQL migrations, base seed data. |
| **Phase 4** | **Authentication & Multi-Tenancy** | **Completed** | JWT Auth, BCrypt hashing, CurrentUserService, EF Core Global Query Filters verified with tests. |
| **Phase 5** | **Properties, Parcels & Ownership** | **Completed** | Property CRUD, parcel subdivision logic, joint ownership validation, domain unit tests passing. |
| **Phase 6** | **Financial Accounts** | **Completed** | Bank, Cash, Credit Card management, account masking, running balance calculation verified. |
| **Phase 7** | **Transaction Engine** | **Completed** | Double-entry balanced journal lines, posting rules, immutability, reversal & adjustment handlers. |
| **Phase 8** | **Property Cost Basis & Accounting** | **Completed** | CapEx vs OpEx distinction, cost basis accumulation, realized vs unrealized gain calculation engine. |
| **Phase 9** | **Property Valuations** | **Completed** | Historical valuation log, government guidance value tracking, valuation vs cash independence confirmed. |
| **Phase 10** | **Document Management** | **Completed** | GUID-keyed file storage, magic-byte MIME validation, path traversal defense, streaming downloads. |
| **Phase 11** | **Reminders & Calendar** | **Completed** | Tax & survey deadline tracking, priority escalation, completion workflows. |
| **Phase 12** | **Dashboard** | **Completed** | Liquid vs Illiquid Net worth aggregation, portfolio table, cash flow indicators, React dashboard UI. |
| **Phase 13** | **Reports & Balance Sheet** | **Completed** | Comprehensive cash flow, property profitability statement, valuation timeline charts. |
| **Phase 14** | **Security Review & Hardening** | **Completed** | IDOR tests, SQL injection name round-trip, token expiration checks, path traversal defense. |
| **Phase 15** | **Testing, Polish & Release** | **Completed** | xUnit, FluentAssertions, React Testing Library, Indian Rupee localization, deployment runbook. |

---

## Detailed Phase Breakdown

### Phase 1: Architecture, Accounting Model & Documentation (Immediate)
- Complete PRD, Clean Architecture design, Accounting Rules, DB design, Roadmap.
- Author Architecture Decision Records (ADRs) under `docs/decisions/`.
- Author `.github/copilot-instructions.md` and specialized prompt files.
- Formulate complete Domain & Database models, risks, and ambiguities.

### Phase 2: Solution & Project Setup
- Create `.sln` and C# projects:
  - `src/LandWealth.Domain`
  - `src/LandWealth.Application`
  - `src/LandWealth.Infrastructure`
  - `src/LandWealth.Api`
  - `tests/LandWealth.Domain.UnitTests`
  - `tests/LandWealth.Application.UnitTests`
  - `tests/LandWealth.IntegrationTests`
- Initialize `src/landwealth-web` (Vite, React, TypeScript, TanStack Query, Material UI, React Router).
- Configure `docker-compose.yml` with MySQL 8.4 container and environment configurations.
- Verify solution compilation and React dev server start.

### Phase 3: Database & Domain Model
- Implement Domain base types: `Entity`, `AuditableEntity`, `ValueObject`, `DomainEvent`.
- Implement Core Entities: `User`, `Property`, `PropertyParcel`, `PropertyOwner`, `Account`, `Category`, `Transaction`, `TransactionLine`, `PropertyValuation`, `PropertyDocument`, `PropertyReminder`, `Asset`, `Liability`.
- Configure EF Core Fluent API mappings in `LandWealth.Infrastructure`.
- Generate initial migration and verify MySQL schema generation.

### Phase 4: Authentication & User Scoping
- Implement User Registration, Login, and JWT Token Generator.
- Implement `ICurrentUserService` pulling claims from `HttpContext`.
- Wire EF Core Global Query Filter (`UserId == CurrentUserId`).
- Author unit and integration tests confirming IDOR isolation.

### Phase 5: Properties, Parcels & Ownership
- Property lifecycle management (Planned -> Purchased -> Held -> Under Development -> For Sale -> Sold -> Archived).
- Multiple parcel support: Extent units (Acres, Guntas, Cents, SqFt), survey numbers, sub-plot splitting.
- Joint ownership validation: Validate total percentage share $\le 100\%$.
- Frontend property management views and parcel forms.

### Phase 6: Financial Accounts
- Manage Bank Accounts, Cash Drawers, and Credit Cards.
- Enforce account masking (storing only last 4 digits).
- Opening balance initialization and running balance consistency tests.
- Account statements return a server-computed running balance after each posted line.
- React accounts overview and card management screens.

### Phase 7: Core Transaction Engine
- Double-entry transaction pipeline: Balanced `TransactionLines` (Debits == Credits).
- Enforce transaction classifications: Income, Expense, Transfer, Property Purchase, Property Expense, Property Income, Property Sale.
- Non-destructive correction model: Reversal and Adjustment workflows. Posted entries are not deleted.
- Frontend transaction ledger with date/property/account filters, reversal notes, and adjustments.

### Phase 8: Property Accounting & Cost Basis Engine
- Implement cost basis aggregation:
  - Acquisition cost and acquisition expenses.
  - Capital improvement additions.
  - Maintenance tracked separately and excluded from cost basis.
- Realized gain on full and partial sales, with pro-rata allocation across the active extent in acres.
- A reversal removes the reversed capital cost. A valuation does not change cost basis or cash.

### Phase 9: Valuations Management
- Valuation history keeps government guidance value separate from market estimates.
- Unrealized gain follows the latest market estimate, and uses guidance only when no market estimate exists.
- Recording a valuation does not create a cash transaction.
- Historical valuation chart on the property page.

### Phase 10: Document Management System
- Files are stored under a GUID key that is never returned to the client.
- Magic-byte checks accept PDF, PNG, and JPEG, and reject a file whose bytes do not match the declared type.
- Downloads are limited to the owning user. A parcel tag must belong to the same property.
- React uploader with drag-and-drop and document type, number, date, and notes.

### Phase 11: Reminders & Alerts
- Schedule property tax, lease expiry, and agricultural survey dates.
- A pending reminder past its due date is shown as overdue, and a low priority is raised to high.
- The dashboard lists overdue reminders and those due within 30 days.
- Completed and dismissed reminders leave that list.

### Phase 12: Executive Financial Dashboard
- Aggregate liquid net worth, property investment, unrealized gain, and total net worth.
- Portfolio table shows type, extent, cost basis, and valuation, and opens the property.
- Monthly inflow and outflow cards exclude transfers between accounts.
- The latest transactions appear on the dashboard.

### Phase 13: Reports & Analytics
- Property investment statement separates acquisition, improvements, maintenance, and income.
- Balance sheet lists each asset and liability, and net worth is assets minus liabilities.
- Valuation history covers multiple years and can be exported as CSV.
- Cash-flow, profitability, and balance-sheet reports can be printed or exported.

### Phase 14: Security Hardening & Audit
- Accounts, transactions, valuations, and reminders stay hidden from another user.
- Login and registration are rate limited, tokens must use HMAC-SHA256, and API responses send restrictive headers.
- The audit log records account, transaction, valuation, asset, and liability changes, and a reversal is its own entry.
- Audit rows are append-only and visible only to the user who made the change.

### Phase 15: Quality Assurance & Launch Polish
- A release walk covers registration, a property purchase, valuation, dashboard, audit, and another user's 404.
- Amounts display in Indian grouping, including `₹12,34,567.00`, and paise round from the stored decimal.
- The deployment runbook covers local startup, containers, secrets, and the launch check.
