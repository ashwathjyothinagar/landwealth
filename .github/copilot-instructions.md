# LandWealth: GitHub Copilot & AI Developer Instructions

You are working on **LandWealth**, a production-grade personal wealth, land/property investment, and financial asset management web application.

---

## 1. Golden Architectural & Accounting Rules

### Rule 1: A PROPERTY IS NOT A BANK ACCOUNT
- A **Bank Account** is a liquid financial store where money is held and spent.
- A **Property** is a capital asset and an accounting dimension (cost center).
- Transactions connect Accounts, Properties, Parcels, and Categories.
- The system must always independently report:
  1. Liquid balance in a bank account (e.g., HDFC).
  2. Total cost basis / capital invested in a property (e.g., Chitnahalli).
- Never merge, conflate, or interchange property values with bank account balances.

### Rule 2: Precision & Monetary Types
- **NEVER** use `float` or `double` for currency or financial numbers.
- In C#: Always use `decimal`.
- In MySQL: Always use `DECIMAL(18, 4)`.
- In TypeScript: Maintain exact string or number representation formatted via `Intl.NumberFormat('en-IN')`.

### Rule 3: Architectural Separation of Concerns
- **LandWealth.Domain**: Pure domain models, aggregates, value objects, domain events, domain exceptions. Zero dependencies on external frameworks or EF Core.
- **LandWealth.Application**: CQRS use cases (Commands/Queries), FluentValidation validators, mapping DTOs, interfaces (`IApplicationDbContext`, `ICurrentUserService`, `IFileStorageService`).
- **LandWealth.Infrastructure**: EF Core mappings, Pomelo MySQL migrations, file storage providers, identity/JWT generation, audit interceptors.
- **LandWealth.Api**: REST controllers, filters, swagger, authentication middleware. **No business logic in controllers**.
- **landwealth-web**: React + TypeScript + Vite + TanStack Query + Material UI. **No financial business logic in React components**. React formats and renders values; the backend computes them.

### Rule 4: Financial Immutability & Auditability
- **Never physically delete posted transactions** (`DELETE FROM Transactions` is strictly forbidden).
- All corrections must be performed using explicit **Reversals** (mirrored reversing transaction) or **Adjustments** (with documented reason).
- Audit fields (`CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy`) are mandatory on all primary entities and managed via EF Core interceptors.

### Rule 5: Defense-in-Depth Multi-Tenancy & IDOR Defense
- Every queryable entity belongs to a `UserId`.
- EF Core applies Global Query Filters:
  ```csharp
  modelBuilder.Entity<T>().HasQueryFilter(e => e.UserId == _currentUserService.UserId);
  ```
- Client-supplied IDs must never be blindly trusted. Ownership is validated at the persistence layer.

### Rule 6: Document Security
- Physical filesystem paths are **NEVER** stored or returned to clients.
- Files are stored under cryptographic GUID keys.
- Uploads must be validated: whitelisted extensions, 25MB maximum size, and magic-byte header inspection.
- Downloads are streamed through an authorized endpoint verifying tenant ownership.

---

## 2. Core Accounting Mechanics

1. **Property Purchase**:
   - `Credit` Bank Account (decreases liquid cash).
   - `Debit` Property Asset (increases acquisition cost basis).
2. **Property Improvement (CapEx)**:
   - `Credit` Bank Account.
   - `Debit` Property Improvement Cost Basis (increases capital investment).
3. **Property Maintenance (OpEx)**:
   - `Credit` Bank Account.
   - `Debit` Property Maintenance Operating Expense (P&L expense, does NOT increase capital cost basis).
4. **Property Valuation**:
   - Updates estimated market value in `PropertyValuations`.
   - Recomputes **Unrealized Gain** (`Estimated Value - Cost Basis`).
   - **Does NOT create cash transactions or modify bank balances.**
5. **Property Sale**:
   - `Debit` Bank Account (Proceeds).
   - `Credit` Property Cost Basis (Derecognition).
   - `Credit` Realized Capital Gain (or Debit Realized Capital Loss).
6. **Account Transfer**:
   - `Credit` Source Account.
   - `Debit` Destination Account.
   - $\Delta \text{NetWorth} = 0$. Never classify as Income or Expense.

---

## 3. Technology Stack & Coding Standards

### Backend (.NET 9 / C#)
- Follow standard C# 12/13 conventions: file-scoped namespaces, nullable reference types enabled (`<Nullable>enable</Nullable>`), primary constructors where clean.
- Use explicit DTOs for request/response contracts; never expose EF Core entities to controllers.
- Validate incoming requests with `FluentValidation`.
- Return standard RFC 7807 `ProblemDetails` for errors.
- Write unit tests using `xUnit` and `FluentAssertions`.

### Frontend (React + TypeScript + Vite)
- Functional components with TypeScript interfaces for props.
- State management:
  - TanStack Query for server state.
  - React hook forms for input forms with validation.
  - Material UI (MUI v5/v6) with a cohesive, professional theme.
- Localization: Format currency using Indian Lakhs & Crores (`₹10,00,000` / `₹1.20 Cr`).
- Test UI components with `@testing-library/react` and `vitest`.

---

## 4. Phase-by-Phase Process
When implementing features:
1. Review documentation in `docs/` and relevant ADR in `docs/decisions/`.
2. Implement Domain entity/rules first -> Application CQRS handler -> Infrastructure mapping -> Api endpoint -> React view.
3. Verify compilation: `dotnet build`.
4. Run tests: `dotnet test`.
5. Keep documentation updated.

- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.
