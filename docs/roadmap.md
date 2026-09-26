# Development Roadmap: LandWealth

This document establishes the multi-phase engineering plan for building LandWealth. Each phase is executed sequentially with strict quality gates: compilation, unit/integration testing, regression verification, and documentation updates.

---

## Phase Overview & Execution Status

| Phase | Description | Status | Quality Gate Criteria |
|---|---|---|---|
| **Phase 1** | **Architecture & Documentation** | **Current** | PRD, Architecture, Accounting Rules, DB Design, Roadmap, ADRs, Copilot Instructions & Prompts finalized. |
| **Phase 2** | **Solution & Project Setup** | Pending | .NET 9 Clean Architecture projects created, Vite+React+TS initialized, Docker Compose running MySQL, builds succeed. |
| **Phase 3** | **Domain & Database Model** | Pending | Core entities, value objects, EF Core configurations, Pomelo MySQL migrations, base seed data. |
| **Phase 4** | **Authentication & Multi-Tenancy** | Pending | JWT Auth, Argon2/BCrypt hashing, CurrentUserService, EF Core Global Query Filters verified with tests. |
| **Phase 5** | **Properties, Parcels & Ownership** | Pending | Property CRUD, parcel subdivision logic, joint ownership validation, domain unit tests passing. |
| **Phase 6** | **Financial Accounts** | Pending | Bank, Cash, Credit Card management, account masking, running balance calculation verified. |
| **Phase 7** | **Transaction Engine** | Pending | Double-entry balanced journal lines, posting rules, immutability, reversal & adjustment handlers. |
| **Phase 8** | **Property Cost Basis & Accounting** | Pending | CapEx vs OpEx distinction, cost basis accumulation, realized vs unrealized gain calculation engine. |
| **Phase 9** | **Property Valuations** | Pending | Historical valuation log, government guidance value tracking, valuation vs cash independence confirmed. |
| **Phase 10** | **Document Management** | Pending | GUID-keyed file storage, magic-byte MIME validation, path traversal defense, streaming downloads. |
| **Phase 11** | **Reminders & Calendar** | Pending | Tax & survey deadline tracking, priority escalation, completion workflows. |
| **Phase 12** | **Dashboard** | Pending | Liquid vs Illiquid Net worth aggregation, portfolio table, cash flow indicators, React dashboard UI. |
| **Phase 13** | **Reports & Balance Sheet** | Pending | Comprehensive cash flow, property profitability statement, valuation timeline charts. |
| **Phase 14** | **Security Review & Hardening** | Pending | IDOR penetration tests, SQL injection audits, token expiration checks, OWASP Top 10 checklist. |
| **Phase 15** | **Testing, Polish & Release** | Pending | Full suite xUnit, FluentAssertions, React Testing Library, Indian Rupee localization polish. |

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
- React accounts overview and card management screens.

### Phase 7: Core Transaction Engine
- Double-entry transaction pipeline: Balanced `TransactionLines` (Debits == Credits).
- Enforce transaction classifications: Income, Expense, Transfer, Property Purchase, Property Expense, Property Income, Property Sale.
- Non-destructive correction model: Reversal and Adjustment workflows.
- Frontend transaction ledger with date/property/account filters.

### Phase 8: Property Accounting & Cost Basis Engine
- Implement cost basis aggregation:
  - Acquisition Cost & Expenses.
  - Capital Improvement (CapEx) basis addition.
  - Maintenance (OpEx) separation.
- Realized gain engine on full and partial parcel sales.
- Pro-rata cost basis allocation logic for subdivided sales.

### Phase 9: Valuations Management
- Valuation history registry (Guidance value vs Market value).
- Recompute unrealized gains on valuation change without creating cash transactions.
- Historical valuation chart component in React.

### Phase 10: Document Management System
- Cryptographic GUID-based local/cloud file storage provider.
- Magic-byte file header verification (PDF, PNG, JPEG).
- Secure download controller preventing path traversal and unauthorized cross-tenant downloads.
- React document uploader with drag-and-drop and metadata tagging.

### Phase 11: Reminders & Alerts
- Schedule property tax deadlines, lease expiries, and agricultural appointments.
- Reminder statuses: Pending, Completed, Overdue.
- Dashboard upcoming reminder notifications.

### Phase 12: Executive Financial Dashboard
- Aggregate Total Net Worth, Liquid Net Worth, Property Investment, and Unrealized Gains.
- Real-time Property Portfolio table with one-click drill-down.
- Monthly cash flow cards (Inflows vs Outflows).

### Phase 13: Reports & Analytics
- Property Profitability and Investment Statement.
- Net Worth Balance Sheet (Assets vs Liabilities breakdown).
- Multi-year valuation appreciation curves.
- Export to CSV / printable format.

### Phase 14: Security Hardening & Audit
- Verification of EF Core Global Query Filters across all endpoints.
- Path traversal and malicious upload testing.
- Penetration testing of API endpoints against unauthorized IDs.
- AuditLog verification for all high-risk financial events.

### Phase 15: Quality Assurance & Launch Polish
- Complete end-to-end testing across C# and React.
- Indian numbering system verification (`₹12,34,567.00`).
- Documentation sync and deployment runbooks.
