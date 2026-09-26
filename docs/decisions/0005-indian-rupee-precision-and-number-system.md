# ADR 0005: Indian Rupee Precision & Indian Land Measurement Systems

## Context & Problem Statement
LandWealth operates fundamentally within the Indian financial and real estate context:
1. Currency representation: Indian Rupee (`INR` / `₹`) with the Indian numbering format (Lakhs and Crores, e.g., `₹12,34,567.00` rather than `1,234,567.00`).
2. High precision: Property purchases and valuations run into tens of crores, while petty maintenance transactions can be a few hundred rupees. Floating point arithmetic (`float`, `double`) introduces rounding errors that violate financial integrity.
3. Land Extent: Land in India is measured in a mix of conventional and regional units:
   - 1 Acre = 40 Guntas = 43,560 Sq Ft
   - 1 Gunta = 1,089 Sq Ft = 121 Sq Yards
   - 1 Cent = 435.6 Sq Ft
   - 1 Hectare = 2.47105 Acres
   - Bigha varies by state.

## Options Considered

### Currency Data Types:
- `FLOAT` / `DOUBLE`: Inherently imprecise; can cause fractional rupee loss ($0.1 + 0.2 \ne 0.3$). **Rejected**.
- `DECIMAL(18, 4)`: Stores numbers with exact decimal precision up to 14 integral digits and 4 decimal places. Sufficient to represent figures up to ₹999 Trillion with 4 decimal sub-cent precision. **Selected**.

### Number Formatting Strategy:
- Backend formats currency strings: Leaks presentation logic into domain services; disrupts numeric client calculations. **Rejected**.
- Client-side formatting utility using `Intl.NumberFormat('en-IN')`:
  - Produces standard Indian grouping: `₹1,00,000` (1 Lakh), `₹1,00,00,000` (1 Crore).
  - Also provide intuitive shorthand helpers: `₹1.25 Cr`, `₹45.5 L`.
  - Backend strictly emits raw numerical decimal values. **Selected**.

### Land Extent Strategy:
- Convert everything to a single unit (e.g. square meters) internally: Causes confusion when a farmer or registrar document specifies "2 Acres 18 Guntas" or "50 Cents". Rounding back and forth causes micro-discrepancies on survey sketches.
- Store original `Extent` (decimal) + `ExtentUnit` (enum):
  - Preserves exact document fidelity.
  - Domain service provides unit conversion utilities for aggregate reporting (e.g., total acreage owned). **Selected**.

## Decision
1. All monetary columns in MySQL use `DECIMAL(18, 4)`. All monetary properties in C# use `decimal`.
2. Frontend employs `Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' })` and Indian shorthand formatters.
3. Land extents are stored with their native unit (`Extent` + `ExtentUnit`), with automated normalized acreage calculations provided by domain methods.

## Consequences
- Guaranteed mathematical precision across all ledger operations.
- Full respect for legal real estate document units in India.
- Intuitive, culturally accurate UX for Indian wealth tracking.
