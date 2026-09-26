# Product Requirements Document (PRD): LandWealth

## 1. Executive Summary & Vision

**LandWealth** is a comprehensive, production-grade personal wealth and real estate investment management web application. It is purpose-built to handle the unique complexities of land and real estate holdings—particularly in India (survey numbers, multi-parcel subdivisions, joint ownership, mutation, patta/RTC, encumbrance certificates)—alongside liquid financial accounts, gold, vehicles, loans, and other personal assets and liabilities.

### The Fundamental Thesis
> **A PROPERTY IS NOT A BANK ACCOUNT.**
>
> A property is a capital asset, an investment vehicle, and an accounting dimension.
> A bank account represents a liquid pool where money resides.
> A transaction connects accounts, properties, parcels, and categories to accurately answer two fundamentally independent questions:
> 1. *"How much money is currently in my liquid accounts (e.g., HDFC)?"*
> 2. *"What is my exact total cost basis, capital expenditure, and current unrealized gain in Chitnahalli land?"*

The system prevents conflation between cash flow and asset valuation, ensures historical auditability without destructive mutations, and calculates both liquid and illiquid net worth with precision.

---

## 2. Core User Personas & Use Cases

### Persona: The Land & Real Estate Investor
- Holds multiple parcels of agricultural, residential, and commercial land across different villages and taluks.
- Incurs continuous development costs (borewell drilling, fencing, grading, survey fees, electric connections).
- Receives periodic valuations from government guidance values and local market movements.
- Needs to track critical documents (RTC/Pahani, Sale Deeds, EC, Mutation register, Survey sketches).
- Wants notifications for property tax deadlines, lease renewals, and survey appointments.

### Persona: The Holistic Wealth Manager
- Tracks multiple bank accounts, cash reserves, credit cards, mutual funds, gold holdings, and home loans.
- Demands real-time calculation of Total Net Worth vs. Liquid Net Worth.
- Requires strict Indian Rupee (INR) representation formatted with Indian numbering grouping (e.g., ₹1,20,00,000 / 1.2 Crore).

---

## 3. Functional Requirements & Feature Scope

### 3.1 Properties & Parcel Management
1. **Property Master**:
   - Unique Property Identifier (GUID).
   - Display Name (e.g., "Chitnahalli 5-Acre Farm", "Whitefield Apartment 402").
   - Property Type: `AgriculturalLand`, `ResidentialLand`, `CommercialLand`, `House`, `Apartment`, `IndustrialLand`, `Other`.
   - Geographic Hierarchy: Location/Address, Village, Taluk, District, State, Country, PIN/Postal Code.
   - Primary Survey Number / Khata Number.
   - Acquisition Date & Initial Purchase Price.
   - Property Lifecycle Status: `Planned`, `Purchased`, `Held`, `UnderDevelopment`, `ForSale`, `Sold`, `Archived`.
   - General Notes, Geolocation coordinates (Latitude/Longitude boundaries).

2. **Parcels (Sub-plot Architecture)**:
   - A property consists of **1 to N physical parcels**.
   - Parcel Attributes: Parcel ID, Survey Number, Subdivision/Hissa Number, Extent (quantity), Extent Unit (`Acres`, `Guntas`, `Cents`, `SqFt`, `SqYards`, `SqMeters`, `Bigha`, `Hectares`), Location Notes, Ownership Share.
   - Dynamic Parcel Lifecycle: Enables future subdivision (splitting 5 acres into two 2.5-acre parcels), partial sales, gifting, boundary rectifications, and combination without destroying historical records.

3. **Multi-Party Ownership**:
   - Support co-ownership structures (e.g., 100% User, 50% User / 50% Spouse, or Family Trust).
   - Owner Name/ID, Percentage Share (validation rule: active shares sum to 100%), Effective Start Date, Effective End Date, Ownership Type (`Freehold`, `Leasehold`, `JointTenancy`, `TenancyInCommon`).

### 3.2 Financial Accounts
1. **Account Types**:
   - `BankAccount` (Savings, Current, Overdraft, NRE/NRO).
   - `CashAccount` (Physical cash in hand, petty cash safe).
   - `CreditCard` (Revolving credit, limit, billing cycle).
   - `InvestmentAccount` (Demat, Mutual fund folio, Fixed Deposit).
   - `OtherFinancialAccount`.
2. **Account Fields**:
   - Account ID, Display Name (e.g., "HDFC Salary Account", "ICICI Home Loan Offset").
   - Institution Name, Masked Account Number (storing only last 4 digits, e.g., `•••• 4821`), Opening Balance, Current Balance, Currency (`INR` default), Status (`Active`, `Inactive`, `Closed`), Notes.

### 3.3 Transaction Engine & Accounting Dimensions
1. **Transaction Classifications**:
   - `Income`: Inflows from salary, business, dividends, interest (increases liquid account).
   - `Expense`: General living/operating expenses (decreases liquid account).
   - `Transfer`: Movement between liquid accounts (Account A decreases, Account B increases; Net Worth delta = 0).
   - `PropertyPurchase`: Capital outflow from liquid account; increases Property Acquisition Cost Basis.
   - `PropertyExpense`: Operating/improvement outflow from liquid account; increases Property Cost Basis or recorded as Maintenance Expense.
   - `PropertyIncome`: Inflow into liquid account from agricultural yield, lease rent, or timber sales.
   - `PropertySale`: Capital inflow from buyer into liquid account; marks parcel/property as sold and triggers Realized Gain calculation.
   - `Investment`: Cash converted to financial assets (FD, stocks, gold).
   - `LiabilityPayment`: Cash outflow reducing outstanding debt principal/interest.
   - `Adjustment`: Audited correcting entry with explicit justification.
   - `Reversal`: Formal voiding transaction referencing original Transaction ID; original remains immutable.
2. **Precision & Immutability**:
   - All financial amounts use `decimal(18, 4)` in database and .NET `decimal`.
   - Never use floating point math.
   - Zero physical hard-deletions on posted financial transactions.

### 3.4 Property Cost Basis Architecture
The system explicitly separates expenditure categories to satisfy both managerial accounting and future capital gains tax calculations:
- **Acquisition Cost**: Base land/structure purchase price.
- **Acquisition Expenses**: Stamp duty, registration fees, legal scrutiny, government survey charges, brokerage.
- **Improvement Cost (Capital Additions)**: Borewell drilling, electric pump/transformer installation, solar fencing, boundary wall construction, internal roads, land grading/soil enrichment, building construction.
- **Maintenance Cost (Operating Expenses)**: Security watchman salary, periodic weeding/brush clearing, tractor plowing, local property taxes (gram panchayat/municipality).
- **Selling Expenses**: Sale brokerage, legal discharge, advertising.

### 3.5 Property Valuations
- Independent valuation log over time: Property ID, Valuation Date, Estimated Market Value, Source (`GovernmentGuidanceValue`, `PrivateAppraiser`, `BankValuation`, `LocalMarketSurvey`, `OwnerEstimate`), Reference Documentation, Notes.
- **Accounting Rule**: Valuation updates recalculate **Unrealized Gain** (`Estimated Value - Cost Basis`), but **NEVER** alter bank balances or generate cash transactions.

### 3.6 Document Management
- Document Metadata: Document ID, Property ID, Parcel ID (optional), Document Type (`SaleDeed`, `AgreementOfSale`, `EncumbranceCertificate_EC`, `RTC_Pahani`, `MutationRegister`, `SurveySketch_Tippani_Akarband`, `TaxReceipt`, `KhataCertificate`, `LegalOpinion`, `CourtOrder`, `RegistrationReceipt`, `SitePhoto`, `Other`), Document Number, Issue Date, Original Filename, File Size, MIME Type, Storage Identifier.
- **Security & Integrity**:
  - Raw filesystem paths are never stored or returned to clients.
  - Files are stored using cryptographic/GUID references in isolated, restricted storage.
  - Multi-tenant isolation prevents accessing another user's documents via ID tampering.
  - Anti-path-traversal sanitization and magic-byte MIME type validation.

### 3.7 Property Reminders & Calendar
- Track crucial recurring and one-off milestones: Property tax payment dates, lease expiration/renewal, RTC mutation follow-up, court hearing, agricultural survey, electric meter bill.
- Reminder Model: Property ID, Title, Description, Due Date, Priority (`Low`, `Medium`, `High`, `Critical`), Status (`Pending`, `Completed`, `Overdue`, `Dismissed`), Completed Date.

### 3.8 Assets, Liabilities, and Net Worth
- **Assets**:
  - Liquid Assets (Cash + Bank balances).
  - Property Assets (Reported at both Cost Basis and Current Market Valuation).
  - Other Assets: Gold bullion/jewelry, Vehicles, Equity/Mutual funds, Fixed Deposits, Private loans given.
- **Liabilities**:
  - Mortgages / Land purchase loans, Vehicle loans, Personal loans, Credit card balances, Hand loans payable.
- **Net Worth Metrics**:
  - **Total Net Worth** = Total Assets (with properties at estimated value) − Total Liabilities.
  - **Liquid Net Worth** = Liquid Assets (Cash + Bank) − Short-Term Liabilities (Credit card outstandings).
  - **Total Property Investment** = Sum of all acquisition and improvement costs across held properties.
  - **Unrealized Property Gain** = Total Property Market Value − Total Property Cost Basis.

### 3.9 Dashboard & Analytics
- Overview cards: Net Worth, Liquid Assets, Property Value, Total Invested, Unrealized Gain, Total Liabilities.
- Monthly cash flow summary: Inflows vs Outflows.
- Property portfolio matrix: Property Name, Type, Extent, Invested Capital, Current Market Valuation, Unrealized Gain/Loss, Status.
- Upcoming critical reminders (next 30 days).
- Recent transaction ledger (latest 10 entries with quick filter).

### 3.10 Reports & Export Engine
- Property Investment & Profitability Statement (Per property & aggregate).
- Valuation History Trend (Visual progression across multiple years).
- Income & Expense Breakdown by Category and Property.
- Cash Flow Statement (Monthly / Quarterly / Annual).
- Account Balance Reconciliation Report.
- Net Worth Balance Sheet (Detailed asset/liability breakdown).

---

## 4. Non-Functional Requirements (NFRs)

| Dimension | Specification |
|---|---|
| **Architecture** | Modular Monolith following Clean Architecture principles (Api → Application → Domain ← Infrastructure). |
| **Performance** | Sub-200ms API response time for p95 dashboard and ledger queries. Indexed foreign keys and search columns. |
| **Data Integrity** | Strict ACID transactions in MySQL. Immutability of posted financial entries; balanced debit/credit lines. |
| **Security** | JWT-based authentication, user-scoped tenant isolation on all queries, parameterized SQL via EF Core, secure password hashing (Argon2id or BCrypt), document storage with MIME magic-byte verification and path traversal prevention. |
| **Precision** | Exact monetary arithmetic using 4 decimal places (`decimal(18, 4)`) in storage; 2 decimal display. |
| **Localization** | Indian Rupee (`₹`) default currency symbol, Lakhs and Crores formatting (`₹12,34,567.00`), standard Indian land extent units (Acres, Guntas, Cents). |
| **Reliability** | Comprehensive test suite with xUnit, FluentAssertions, and React Testing Library; zero tolerance for regression. |
| **Maintainability** | Clear separation of concerns, no financial calculations inside UI views, strictly typed contracts (TypeScript & C#). |
