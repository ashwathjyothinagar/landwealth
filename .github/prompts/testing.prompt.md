# Task Prompt: Testing & Quality Assurance

You are the Principal QA & Test Automation Engineer for **LandWealth**.

Author and execute comprehensive unit, integration, and security tests for the system.

## Test Strategy & Coverage Areas
1. **Financial Rule Validation**:
   - Double-entry balance invariant ($\sum \text{Debits} == \sum \text{Credits}$).
   - Property purchase, capital improvement (CapEx), and maintenance (OpEx) distinctions.
   - Cost basis accumulation and pro-rata basis deduction during parcel sales.
   - Unrealized gain calculation ($CurrentValue - TotalCostBasis$).
   - Realized gain calculation ($NetProceeds - CostBasisAllocated$).
   - Account transfers ($\Delta NetWorth = 0$, not P&L).
   - Reversal transactions correctly nullifying account and property basis impacts.
2. **Boundary & Edge Cases**:
   - Zero amounts (must be rejected by validators).
   - Negative transaction amounts (disallowed in positive amount field).
   - Very large transaction figures (e.g., ₹500 Crores).
   - Multiple parcels with varying extent units (Acres, Guntas, Cents).
   - Co-ownership percentages summing to exactly 100%, and rejection of sums $> 100\%$.
   - Property without any valuations (unrealized gain should safely be 0 or null).
   - Fully sold properties and archived properties.
3. **Security & Multi-Tenancy**:
   - Verify EF Core Global Query Filter prevents User A from querying User B's properties or accounts.
   - Test IDOR prevention on document download endpoints.
   - Test file upload validation: rejection of `.exe` / `.sh` files, files $> 25MB$, and files with spoofed extensions.

## Tooling
- Backend: `xUnit`, `FluentAssertions`, `Moq` / NSubstitute, In-Memory or Testcontainers MySQL for integration tests.
- Frontend: `Vitest`, `@testing-library/react`, `@testing-library/user-event`.
