# ADR 0001: Transaction Header and Balanced Transaction Lines Accounting Model

## Context & Problem Statement
LandWealth requires a transaction architecture capable of representing diverse financial activities:
1. Pure liquid transfers between bank accounts (Account A decreases, Account B increases; no impact on net worth or income/expenses).
2. Property acquisitions (Bank account decreases, Property asset basis increases).
3. Capital improvements (Bank account decreases, Property improvement basis increases).
4. Operating maintenance (Bank account decreases, Maintenance operating expense increases).
5. Property income (Bank account increases, Property income increases).
6. Property sales with proceeds, selling expenses, cost basis write-off, and realized capital gain.
7. Corrections, reversals, and audited adjustments.

We must evaluate whether a single-table transaction model (flat source/destination columns) or a **Transaction Header + Balanced Transaction Lines** model should be adopted, without sliding into the overwhelming overhead of a full corporate ERP.

## Options Considered

### Option A: Flat Single-Table Transaction Model
- A single `Transactions` table containing `SourceAccountId`, `DestinationAccountId`, `PropertyId`, `Amount`, `TransactionType`.
- **Pros**: Simple to query initially; straightforward CRUD.
- **Cons**: Severe architectural limitations. Handling complex transactions such as a property sale (where cash proceeds are received, legal expenses are deducted, cost basis is derecognized, and realized gain is recognized) becomes messy, requiring ad-hoc hack columns or synthetic secondary rows. Partial sales and split payments across multiple accounts cannot be modeled cleanly.

### Option B: Transaction Header + Balanced Transaction Lines (Selected)
- A `Transactions` header table representing the business event (Date, Type, Description, Status, Reference, Property link).
- A child `TransactionLines` table representing the monetary legs of the event:
  - Each line has `AccountId` (or `PropertyId`), `CategoryId`, `LineType` (`Debit` or `Credit`), and `Amount`.
  - Invariant rule: $\sum \text{Debits} = \sum \text{Credits}$ for all posted transactions.
- **Pros**:
  - Naturally handles transfers (Credit Source Bank, Debit Destination Bank).
  - Naturally handles property purchases (Credit Bank, Debit Property Cost Basis).
  - Naturally handles multi-split property sales (Debit Bank for cash received, Debit Selling Expense, Credit Property Asset Basis, Credit Realized Gain).
  - Non-destructive reversals simply invert line directions.
  - Extensible to future multi-currency or split-category needs without altering the database schema.
- **Cons**: Requires joining two tables for transaction detail views; slightly more code in repository commands to save lines transactionally.

### Option C: Full Enterprise Double-Entry Ledger (General Ledger, Journals, Subledgers)
- Standard SAP/Oracle ERP chart of accounts with automated journal batch posting, period closing, trial balances, and reconciliation engines.
- **Pros**: Extreme enterprise compliance.
- **Cons**: Massive overkill for personal wealth and land portfolio tracking. Excessive complexity and slow developer velocity.

## Decision
We select **Option B: Transaction Header + Balanced Transaction Lines**.

### Key Rules:
1. Every financial transaction consists of a single `Transaction` header and at least two `TransactionLines`.
2. The domain model enforces that $\sum \text{Debits} = \sum \text{Credits}$ before any transaction is transitioned to `Posted` status.
3. Liquid accounts (Bank, Cash, Credit Card) are represented on lines via `AccountId`.
4. Property cost bases and investments are represented on lines via `PropertyId`.
5. Valuations are kept completely separate in a dedicated `PropertyValuations` table and do **NOT** enter the double-entry transaction engine, guaranteeing that estimated market swings never contaminate liquid ledgers.

## Consequences
- **Positive**: Clean, robust financial integrity; zero schema changes needed for complex split transactions; mathematical guarantee of balanced books.
- **Negative**: Queries displaying transaction summaries require an aggregated query or projection DTO, which is readily encapsulated in the `LandWealth.Application` CQRS layer.
