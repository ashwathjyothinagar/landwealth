# Database Design Specification: LandWealth

## 1. Relational Database Strategy & Conventions

- **Target Engine**: MySQL 8.0+ / 8.4+ (InnoDB Engine, `utf8mb4` character set, `utf8mb4_unicode_ci` collation).
- **ORM**: Entity Framework Core 9 using Pomelo MySQL Provider.
- **Normalization**: Third Normal Form (3NF) to eliminate data anomalies while preserving query efficiency.
- **Precision**: Monetary columns use `DECIMAL(18, 4)` for zero-loss precision.
- **Multi-Tenancy**: Shared database, tenant-isolated via indexed `UserId` with EF Core Global Query Filters.
- **Immutability & Auditing**: Primary transaction records are append-only. All primary entities contain standard audit tracking fields (`CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy`, `IsDeleted`).

---

## 2. Entity Relationship Overview

```
+---------------+       +------------------+       +-------------------+
|     Users     |<------+    Properties    |<------+  PropertyParcels  |
+---------------+   |   +------------------+   |   +-------------------+
        ^           |     |        |       |   |
        |           |     |        |       |   +---+
        |           |     |        |       v       |
        |           |     |        |   +-------------------+
        |           |     |        |   |  PropertyOwners   |
        |           |     |        |   +-------------------+
        |           |     |        v
        |           |     |    +-------------------+
        |           |     |    |PropertyValuations |
        |           |     |    +-------------------+
        |           |     v
        |           |   +-------------------+
        |           |   | PropertyDocuments |
        |           |   +-------------------+
        |           v
        |       +-------------------+
        |       | PropertyReminders |
        |       +-------------------+
        |
        |       +-------------------+
        +<------+     Accounts      |
        |       +-------------------+
        |                 ^
        |                 |
        |       +-------------------+       +-------------------+
        +<------+   Transactions    |<------+ TransactionLines  |
        |       +-------------------+       +-------------------+
        |                 |
        |                 v
        |       +-------------------+
        +<------+    Categories     |
        |       +-------------------+
        |
        |       +-------------------+
        +<------+  Assets / Liab.   |
        |       +-------------------+
        |
        |       +-------------------+
        +<------+     AuditLogs     |
                +-------------------+
```

---

## 3. Detailed Table Schemas

### 3.1 `Users`
Stores authenticated users and system security profiles.

| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | CHAR(36) | No | Primary Key (GUID string format). |
| `Email` | VARCHAR(256) | No | Unique, Indexed, Normalized Email. |
| `PasswordHash` | VARCHAR(512) | No | Argon2id / BCrypt hash. |
| `FullName` | VARCHAR(150) | No | User's full legal name. |
| `PreferredCurrency` | VARCHAR(3) | No | Default `INR`. |
| `IsActive` | TINYINT(1) | No | Default `1`. |
| `CreatedAt` | DATETIME(6) | No | UTC timestamp. |
| `LastModifiedAt`| DATETIME(6) | Yes | UTC timestamp. |

---

### 3.2 `Properties`
The master catalog of real estate properties and land investments.

| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | CHAR(36) | No | Primary Key (GUID). |
| `UserId` | CHAR(36) | No | Foreign Key -> `Users(Id)`. Indexed. |
| `Name` | VARCHAR(200) | No | e.g., "Chitnahalli 5-Acre Farm". |
| `PropertyType` | VARCHAR(50) | No | `AgriculturalLand`, `ResidentialLand`, etc. |
| `Status` | VARCHAR(50) | No | `Planned`, `Purchased`, `Held`, `UnderDevelopment`, `ForSale`, `Sold`, `Archived`. |
| `PrimarySurveyNumber` | VARCHAR(100) | Yes | Main village survey number. |
| `PurchaseDate` | DATE | Yes | Acquisition date. |
| `PurchasePrice` | DECIMAL(18, 4) | No | Initial base purchase price (Default 0). |
| `Location` | VARCHAR(255) | Yes | Street / Area description. |
| `Village` | VARCHAR(100) | Yes | Grama / Village name. |
| `Taluk` | VARCHAR(100) | Yes | Sub-district / Taluk. |
| `District` | VARCHAR(100) | Yes | District. |
| `State` | VARCHAR(100) | No | State (e.g., Karnataka). |
| `Country` | VARCHAR(100) | No | Default `India`. |
| `PostalCode` | VARCHAR(20) | Yes | Postal PIN code. |
| `Latitude` | DECIMAL(10, 7) | Yes | Geolocation coordinate. |
| `Longitude` | DECIMAL(10, 7) | Yes | Geolocation coordinate. |
| `Notes` | TEXT | Yes | Rich narrative or legal annotations. |
| `CreatedAt` | DATETIME(6) | No | Standard audit field. |
| `CreatedBy` | CHAR(36) | No | Standard audit field. |
| `LastModifiedAt` | DATETIME(6) | Yes | Standard audit field. |
| `LastModifiedBy` | CHAR(36) | Yes | Standard audit field. |
| `IsDeleted` | TINYINT(1) | No | Soft-delete flag (Default 0). |

*Indexes*:
- `IX_Properties_UserId` on (`UserId`)
- `IX_Properties_UserId_Status` on (`UserId`, `Status`)

---

### 3.3 `PropertyParcels`
Physical sub-plots, hissas, and survey subdivisions belonging to a property.

| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | CHAR(36) | No | Primary Key (GUID). |
| `UserId` | CHAR(36) | No | Foreign Key -> `Users(Id)`. Indexed. |
| `PropertyId` | CHAR(36) | No | Foreign Key -> `Properties(Id)`. Indexed. |
| `SurveyNumber` | VARCHAR(100) | No | Specific survey number for this parcel. |
| `SubdivisionNumber` | VARCHAR(50) | Yes | Hissa / sub-division (e.g., `/1A`, `/2B`). |
| `Extent` | DECIMAL(18, 4) | No | Numerical size (e.g., 2.5000). |
| `ExtentUnit` | VARCHAR(30) | No | `Acres`, `Guntas`, `Cents`, `SqFt`, `SqYards`, `SqMeters`, `Bigha`, `Hectares`. |
| `BoundaryDescription`| TEXT | Yes | North, South, East, West boundary markers. |
| `OwnershipPercentage`| DECIMAL(5, 2) | No | Percentage allocation (0 to 100). |
| `Status` | VARCHAR(50) | No | `Active`, `Subdivided`, `PartiallySold`, `Sold`, `Transferred`. |
| `Notes` | TEXT | Yes | Parcel specifics. |
| `CreatedAt`, `LastModifiedAt`, `IsDeleted` | ... | No | Standard audit fields. |

*Indexes*:
- `IX_PropertyParcels_PropertyId` on (`PropertyId`)
- `IX_PropertyParcels_UserId` on (`UserId`)

---

### 3.4 `PropertyOwners`
Tracks fractional or joint ownership arrangements across entities/people.

| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | CHAR(36) | No | Primary Key. |
| `UserId` | CHAR(36) | No | Foreign Key -> `Users(Id)`. |
| `PropertyId` | CHAR(36) | No | Foreign Key -> `Properties(Id)`. |
| `OwnerName` | VARCHAR(150) | No | Legal owner name. |
| `OwnershipPercentage` | DECIMAL(5, 2) | No | Validated: Active percentages sum <= 100%. |
| `OwnershipType` | VARCHAR(50) | No | `Freehold`, `JointTenancy`, `TenancyInCommon`, `Leasehold`. |
| `StartDate` | DATE | No | Date ownership became effective. |
| `EndDate` | DATE | Yes | Date ownership ceased (transfer/sale). |
| `Notes` | VARCHAR(500) | Yes | PAN, Aadhaar, or relationship references. |
| `CreatedAt`, `LastModifiedAt`, `IsDeleted` | ... | No | Standard audit fields. |

---

### 3.5 `Accounts`
Liquid financial accounts (Bank, Cash, Credit Cards).

| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | CHAR(36) | No | Primary Key. |
| `UserId` | CHAR(36) | No | Foreign Key -> `Users(Id)`. Indexed. |
| `Name` | VARCHAR(150) | No | e.g., "HDFC Bank Savings". |
| `AccountType` | VARCHAR(50) | No | `BankAccount`, `CashAccount`, `CreditCard`, `OtherFinancialAccount`. |
| `Institution` | VARCHAR(100) | Yes | e.g., "HDFC Bank", "SBI", "ICICI". |
| `MaskedAccountNumber` | VARCHAR(30) | Yes | Stored masked format: `•••• 4821`. |
| `OpeningBalance` | DECIMAL(18, 4) | No | Initial balance at tracking onset. |
| `CurrentBalance` | DECIMAL(18, 4) | No | Maintained running balance for fast reads. |
| `Currency` | VARCHAR(3) | No | Default `INR`. |
| `IsActive` | TINYINT(1) | No | Default `1`. |
| `Notes` | TEXT | Yes | Purpose and notes. |
| `CreatedAt`, `LastModifiedAt`, `IsDeleted` | ... | No | Standard audit fields. |

*Indexes*:
- `IX_Accounts_UserId` on (`UserId`)
- `IX_Accounts_UserId_IsActive` on (`UserId`, `IsActive`)

---

### 3.6 `Categories`
Accounting taxonomy classifying income, capital expenses, operating expenses, and transfers.

| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | CHAR(36) | No | Primary Key. |
| `UserId` | CHAR(36) | Yes | Null for system defaults, Populated for custom. |
| `Name` | VARCHAR(100) | No | e.g., "Fencing", "Borewell", "Survey Fees", "Property Tax". |
| `CategoryType` | VARCHAR(50) | No | `Income`, `Expense`, `Transfer`, `CapEx_Acquisition`, `CapEx_Improvement`, `OpEx_Maintenance`. |
| `Description` | VARCHAR(255) | Yes | Clarifying details. |
| `IsSystem` | TINYINT(1) | No | 1 for immutable system categories, 0 for user-created. |
| `ParentCategoryId` | CHAR(36) | Yes | Self-referencing FK for subcategories. |

---

### 3.7 `Transactions` & `TransactionLines`
The double-entry core of LandWealth.

#### `Transactions` (Header)
| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | CHAR(36) | No | Primary Key. |
| `UserId` | CHAR(36) | No | Foreign Key -> `Users(Id)`. Indexed. |
| `TransactionDate` | DATE | No | Transaction date. Indexed. |
| `TransactionType` | VARCHAR(50) | No | `Income`, `Expense`, `Transfer`, `PropertyPurchase`, `PropertyExpense`, `PropertyIncome`, `PropertySale`, `Investment`, `LiabilityPayment`, `Adjustment`, `Reversal`. |
| `Amount` | DECIMAL(18, 4) | No | Positive total transaction volume. |
| `Description` | VARCHAR(300) | No | Brief narration. |
| `Status` | VARCHAR(30) | No | `Posted`, `Reversed`, `Draft`. |
| `ReferenceNumber` | VARCHAR(100) | Yes | Cheque number, UTR number, Receipt number. |
| `PropertyId` | CHAR(36) | Yes | FK -> `Properties(Id)` (dimensional link). |
| `ParcelId` | CHAR(36) | Yes | FK -> `PropertyParcels(Id)` (sub-dimensional link). |
| `ReversedTransactionId` | CHAR(36) | Yes | FK -> `Transactions(Id)` (for audit trail). |
| `Notes` | TEXT | Yes | Full context. |
| `CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy` | ... | No | Strict audit stamps. |

#### `TransactionLines` (Balanced Debits & Credits)
| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | CHAR(36) | No | Primary Key. |
| `TransactionId` | CHAR(36) | No | Foreign Key -> `Transactions(Id)`. Indexed. |
| `AccountId` | CHAR(36) | Yes | FK -> `Accounts(Id)` (for liquid/liability lines). |
| `PropertyId` | CHAR(36) | Yes | FK -> `Properties(Id)` (for asset cost basis lines). |
| `CategoryId` | CHAR(36) | Yes | FK -> `Categories(Id)`. |
| `LineType` | VARCHAR(10) | No | `Debit` or `Credit`. |
| `Amount` | DECIMAL(18, 4) | No | Line value ($\ge 0$). |
| `Memo` | VARCHAR(255) | Yes | Line-specific memo. |

*Indexes*:
- `IX_Transactions_UserId_Date` on (`UserId`, `TransactionDate` DESC)
- `IX_Transactions_PropertyId` on (`PropertyId`)
- `IX_TransactionLines_TransactionId` on (`TransactionId`)
- `IX_TransactionLines_AccountId` on (`AccountId`)

---

### 3.8 `PropertyValuations`
Historical real estate valuation entries.

| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | CHAR(36) | No | Primary Key. |
| `UserId` | CHAR(36) | No | Foreign Key -> `Users(Id)`. |
| `PropertyId` | CHAR(36) | No | Foreign Key -> `Properties(Id)`. Indexed. |
| `ValuationDate` | DATE | No | Date valuation applies. Indexed. |
| `EstimatedValue` | DECIMAL(18, 4) | No | Valuation amount in INR. |
| `ValuationSource` | VARCHAR(100) | No | `GovernmentGuidanceValue`, `PrivateAppraiser`, `BankValuation`, `LocalMarketSurvey`, `OwnerEstimate`. |
| `Notes` | TEXT | Yes | Reference document, registrar office rate. |
| `CreatedAt`, `CreatedBy` | ... | No | Audit stamps. |

---

### 3.9 `PropertyDocuments`
Secure metadata for deed copies, survey sketches, ECs, and photos.

| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | CHAR(36) | No | Primary Key. |
| `UserId` | CHAR(36) | No | Foreign Key -> `Users(Id)`. Indexed. |
| `PropertyId` | CHAR(36) | No | Foreign Key -> `Properties(Id)`. Indexed. |
| `ParcelId` | CHAR(36) | Yes | Foreign Key -> `PropertyParcels(Id)`. |
| `DocumentType` | VARCHAR(50) | No | `SaleDeed`, `RTC_Pahani`, `EncumbranceCertificate_EC`, `MutationRegister`, `SurveySketch`, etc. |
| `DocumentNumber` | VARCHAR(100) | Yes | Registration number, EC volume/page, etc. |
| `IssueDate` | DATE | Yes | Date on the physical document. |
| `OriginalFileName` | VARCHAR(255) | No | User's uploaded file name (sanitized). |
| `ContentType` | VARCHAR(100) | No | MIME type (`application/pdf`, `image/jpeg`). |
| `FileSize` | BIGINT | No | File size in bytes. |
| `StorageKey` | VARCHAR(255) | No | Internal secure GUID key (never client exposed). |
| `Notes` | TEXT | Yes | Legal notes, pending mutation reference. |
| `CreatedAt`, `CreatedBy`, `IsDeleted` | ... | No | Standard audit fields. |

---

### 3.10 `PropertyReminders`
Scheduled dates for taxes, hearings, renewals, and farm tasks.

| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | CHAR(36) | No | Primary Key. |
| `UserId` | CHAR(36) | No | Foreign Key -> `Users(Id)`. Indexed. |
| `PropertyId` | CHAR(36) | Yes | Optional Property Link. |
| `Title` | VARCHAR(200) | No | e.g., "Pay Annual Gram Panchayat Property Tax". |
| `Description` | TEXT | Yes | Narrative. |
| `DueDate` | DATE | No | Deadline date. Indexed. |
| `Priority` | VARCHAR(20) | No | `Low`, `Medium`, `High`, `Critical`. |
| `Status` | VARCHAR(30) | No | `Pending`, `Completed`, `Overdue`, `Dismissed`. |
| `CompletedDate` | DATE | Yes | Date marked completed. |
| `CreatedAt`, `LastModifiedAt` | ... | No | Standard audit fields. |

---

### 3.11 `Assets` & `Liabilities`
Non-property personal assets and debt obligations.

#### `Assets`
| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | CHAR(36) | No | Primary Key. |
| `UserId` | CHAR(36) | No | Foreign Key -> `Users(Id)`. |
| `Name` | VARCHAR(150) | No | e.g., "24K Sovereign Gold Coins", "Toyota Fortuner". |
| `AssetType` | VARCHAR(50) | No | `Gold`, `Vehicle`, `StockInvestment`, `MutualFund`, `FixedDeposit`, `Other`. |
| `EstimatedValue` | DECIMAL(18, 4) | No | Current valuation. |
| `AcquisitionCost` | DECIMAL(18, 4) | No | Purchase price. |
| `AcquisitionDate` | DATE | Yes | Purchase date. |
| `Notes` | TEXT | Yes | Folio numbers, locker details. |
| `CreatedAt`, `LastModifiedAt`, `IsDeleted` | ... | No | Standard audit fields. |

#### `Liabilities`
| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | CHAR(36) | No | Primary Key. |
| `UserId` | CHAR(36) | No | Foreign Key -> `Users(Id)`. |
| `Name` | VARCHAR(150) | No | e.g., "SBI Home Loan", "HDFC Land Loan". |
| `LiabilityType` | VARCHAR(50) | No | `Mortgage`, `LandLoan`, `PersonalLoan`, `VehicleLoan`, `CreditCardOutstanding`, `Other`. |
| `Lender` | VARCHAR(150) | Yes | Bank or private lender. |
| `PrincipalAmount` | DECIMAL(18, 4) | No | Original borrowed sum. |
| `OutstandingBalance` | DECIMAL(18, 4) | No | Current unpaid principal. |
| `InterestRate` | DECIMAL(5, 2) | Yes | Annual interest percentage. |
| `StartDate` | DATE | Yes | Inception date. |
| `EndDate` | DATE | Yes | Target closure date. |
| `CreatedAt`, `LastModifiedAt`, `IsDeleted` | ... | No | Standard audit fields. |

---

### 3.12 `AuditLogs`
Immutable security and financial state change logs.

| Column | Type | Nullable | Description / Constraints |
|---|---|---|---|
| `Id` | BIGINT AUTO_INCREMENT | No | Primary Key. |
| `UserId` | CHAR(36) | Yes | Acting user. |
| `EntityName` | VARCHAR(100) | No | e.g., "Transaction", "Property", "Account". |
| `EntityId` | VARCHAR(100) | No | Target entity ID. |
| `Action` | VARCHAR(50) | No | `Insert`, `Update`, `SoftDelete`, `Reversal`. |
| `Timestamp` | DATETIME(6) | No | UTC timestamp. |
| `OldValues` | JSON | Yes | Snapshot prior to modification. |
| `NewValues` | JSON | Yes | Snapshot after modification. |
| `IpAddress` | VARCHAR(45) | Yes | Client IP. |
| `UserAgent` | VARCHAR(255) | Yes | Client User-Agent. |
