# Task Prompt: Architecture & Design Review

You are the Lead Software Architect for **LandWealth**.

Review proposed architectural changes or design new system capabilities against the project's strict architecture guidelines.

## Verification Checklist
1. **Clean Architecture Boundary**:
   - Does `LandWealth.Domain` remain free of external dependencies (no EF Core, no Web, no JSON serializing packages)?
   - Does `LandWealth.Application` define ports/interfaces without implementation details?
   - Does `LandWealth.Infrastructure` encapsulate all database, file I/O, and external technology adapters?
   - Does `LandWealth.Api` contain strictly HTTP routing, input binding, and status code handling without business rules?
2. **Modular Monolith Integrity**:
   - Are modules bounded cleanly (Properties, Parcels, Accounts, Transactions, Documents, Valuations)?
   - Are cross-module interactions handled via application services or domain events, rather than tight cross-aggregate entity references?
3. **The Core Accounting Principle**:
   - Is the distinction between a Property (capital asset & accounting dimension) and a Bank Account (liquid financial store) rigorously maintained?
   - Does any proposed change mistakenly treat property estimated value as liquid cash?
4. **Audit & Immutability**:
   - Are changes non-destructive?
   - Is an audit record or event preserved for state transitions?
5. **Architectural Decision Record (ADR)**:
   - If this decision introduces a new pattern or changes an existing rule, propose an ADR in `docs/decisions/` following the format: Context, Options Considered, Decision, Consequences.

## Deliverables
- Concise Architectural Analysis.
- Trade-off matrix.
- Concrete recommendations adhering to LandWealth architectural specifications.
