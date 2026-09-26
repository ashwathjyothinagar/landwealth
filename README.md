# LandWealth

**LandWealth** is a production-grade personal wealth and real estate investment management web application designed for managing land holdings, multi-parcel properties, bank accounts, liquid transactions, legal documents, historical valuations, liabilities, and net worth.

---

## Core Principle

> **A PROPERTY IS NOT A BANK ACCOUNT.**
>
> A property is a capital asset, an investment vehicle, and an accounting dimension.
> A bank account represents a liquid pool where money resides.
> The system tracks both independently with mathematical precision and double-entry integrity.

---

## Documentation Index

- [Product Requirements Document (PRD)](docs/product-requirements.md)
- [Architecture Specification](docs/architecture.md)
- [Accounting & Financial Rules](docs/accounting-rules.md)
- [Database Design Specification](docs/database-design.md)
- [Development Roadmap](docs/roadmap.md)
- [Architecture Decision Records (ADRs)](docs/decisions/)
  - [ADR 0001: Transaction Header & Balanced Lines Accounting Model](docs/decisions/0001-transaction-double-entry-accounting-model.md)
  - [ADR 0002: Modular Monolith & Clean Architecture](docs/decisions/0002-modular-monolith-clean-architecture.md)
  - [ADR 0003: Multi-Tenant Data Isolation & IDOR Defense](docs/decisions/0003-multi-tenant-data-isolation-security.md)
  - [ADR 0004: Secure Document Storage Strategy & Path Traversal Prevention](docs/decisions/0004-secure-document-storage-and-access.md)
  - [ADR 0005: Indian Rupee Precision & Land Measurement Systems](docs/decisions/0005-indian-rupee-precision-and-number-system.md)
- [GitHub Copilot & AI Developer Instructions](.github/copilot-instructions.md)
- [Reusable Development Prompts](.github/prompts/)

---

## Technology Stack

- **Backend**: ASP.NET Core 9 (.NET 9), Clean Architecture, Entity Framework Core 9, Pomelo MySQL.
- **Frontend**: React, TypeScript, Vite, React Router, TanStack Query, Material UI.
- **Database**: MySQL 8.0+ / 8.4+ (InnoDB, UTF8mb4).
- **Testing**: xUnit, FluentAssertions, React Testing Library, Vitest.
