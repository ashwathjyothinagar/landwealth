# Financial & Accounting Rules Specification: LandWealth

## 1. The Core Accounting Axiom

> ### **A PROPERTY IS NOT A BANK ACCOUNT.**
>
> A **Bank Account** is a liquid monetary repository that holds cash and undergoes routine credits and debits.
>
> A **Property** is a capital asset, an investment vehicle, and an accounting dimension (cost center).
>
> A transaction can connect a liquid account, a property, a parcel, and a financial category. Under no circumstances may a property balance be treated as liquid cash or conflated with an account balance.

The application must always deterministically answer:
1. *"What is the exact ledger balance in my HDFC account?"*
2. *"What is the cumulative capital invested in Chitnahalli Land?"*
3. *"What is the current unrealized capital gain across all held land assets?"*
without any cross-contamination of balances.

---

## 2. Chart of Accounts & Dimensional Classification

LandWealth tracks wealth through five core account categories:

| Category | Typical Elements | Normal Balance | Balance Sheet / P&L Role |
|---|---|---|---|
| **Liquid Assets** | Bank Accounts (Savings/Current), Cash in Hand | Debit | Asset (Liquid Net Worth) |
| **Capital Assets** | Land Holdings, Buildings, Gold, Vehicles, Investments | Debit | Asset (Illiquid Net Worth) |
| **Liabilities** | Mortgages, Land Loans, Personal Loans, Credit Cards | Credit | Liability (Debt Obligations) |
| **Income / Inflows** | Salary, Agricultural Yield, Rental Income, Interest | Credit | P&L (Cash Flow / Earnings) |
| **Expenses / Outflows**| General Living, Property Maintenance, Loan Interest | Debit | P&L (Cash Drain) |

---

## 3. Transaction Types & Double-Entry Mechanics

Every posted transaction adheres to balanced double-entry accounting mechanics or balanced multi-line ledger entries:

$$\sum \text{Debits} = \sum \text{Credits}$$

### 3.1 Property Purchase
- **Intent**: Acquisition of a property or parcel.
- **Ledger Entries**:
  - `Debit`: Property Asset Account (Increases Property Acquisition Cost Basis).
  - `Credit`: Source Bank/Cash Account (Decreases Liquid Balance).
- **Impact**:
  - Liquid Assets decrease.
  - Illiquid Assets increase by equivalent cost.
  - Net Worth remains unchanged at the instant of transaction (cash converted to asset at cost).

### 3.2 Property Capital Improvement vs. Property Maintenance Expense

LandWealth makes a strict distinction between **Capital Expenditures (CapEx)** and **Operating Expenses (OpEx)**:

#### A. Capital Improvement (CapEx)
- **Examples**: Borewell drilling, electric transformer/meter installation, solar fencing, boundary stone laying, internal road formation, land grading, clubhouse construction.
- **Ledger Entries**:
  - `Debit`: Property Improvement Cost Basis (Capital Asset).
  - `Credit`: Source Bank / Cash Account.
- **Impact**:
  - Liquid Assets decrease.
  - Property Cost Basis increases.
  - Cumulative Property Investment increases.

#### B. Maintenance Expense (OpEx)
- **Examples**: Periodic weeding, brush clearing, watchman/caretaker wages, tractor rotavator hiring, municipal/gram panchayat property taxes, legal dispute retainers.
- **Ledger Entries**:
  - `Debit`: Property Maintenance Operating Expense (P&L).
  - `Credit`: Source Bank / Cash Account.
- **Impact**:
  - Liquid Assets decrease.
  - Operating expense recorded.
  - **Does NOT increase the capital property cost basis** for market unrealized gain formulas (tracked separately in property P&L).

### 3.3 Property Income
- **Examples**: Sale of agricultural harvest (mangoes, coconuts, sugarcane, timber), lease rentals, cellular tower lease payments.
- **Ledger Entries**:
  - `Debit`: Destination Bank / Cash Account (Increases Liquid Balance).
  - `Credit`: Property Operating Income (P&L).
- **Impact**:
  - Liquid Assets increase.
  - Net Worth increases.
  - Property income ledger incremented.

### 3.4 Property Valuations (Unrealized Gains)
- **Definition**: Periodic reassessment of the property's estimated market value (via government guidance values, local registration data, or certified appraiser reports).
- **Accounting Rule**:
  - **Valuations are NOT cash transactions.**
  - **No bank balance or cash ledger is affected.**
  - No journal entry is made to liquid accounts.
- **Formulas**:
  $$\text{Total Cost Basis} = \text{Acquisition Cost} + \text{Acquisition Expenses} + \text{Capital Improvement Costs}$$
  $$\text{Unrealized Gain / (Loss)} = \text{Current Estimated Market Value} - \text{Total Cost Basis}$$
- **Presentation**: Displayed under Illiquid Assets and Unrealized Gains on the Balance Sheet. Never included in Liquid Net Worth or Cash Flow.

### 3.5 Property Sale & Realized Gain Calculation
- **Intent**: Complete or partial liquidation of a property or parcel.
- **Inputs**:
  - Gross Sale Proceeds.
  - Direct Selling Expenses (brokerage, legal drafting, stamp paper).
  - Allocated Cost Basis of parcel sold.
- **Formula**:
  $$\text{Net Proceeds} = \text{Gross Sale Proceeds} - \text{Selling Expenses}$$
  $$\text{Realized Capital Gain / (Loss)} = \text{Net Proceeds} - \text{Allocated Cost Basis}$$
- **Partial Sale Rule (Pro-Rata Basis Allocation)**:
  When a parcel or partial extent is sold (e.g., selling 1 acre out of a 3-acre parcel):
  $$\text{Cost Basis Allocated} = \text{Total Property Cost Basis} \times \left( \frac{\text{Extent Sold}}{\text{Total Extent}} \right)$$
- **Ledger Entries**:
  - `Debit`: Destination Bank Account (Net Sale Proceeds).
  - `Debit`: Selling Expenses (if paid separately).
  - `Credit`: Property Asset Cost Basis (Derecognized / Removed).
  - `Credit`: Realized Capital Gain (P&L / Equity addition).
- **Status Change**: Parcel marked as `Sold`. If all parcels sold, Property marked as `Sold`.

### 3.6 Account Transfers
- **Definition**: Reallocating funds between liquid accounts (e.g., Transfer ₹1,00,000 from HDFC Savings to ICICI Savings).
- **Ledger Entries**:
  - `Debit`: Destination Account (ICICI).
  - `Credit`: Source Account (HDFC).
- **Accounting Rule**:
  - **Transfers are strictly non-P&L events.**
  - Transfers are **never** categorized as Income or Expense.
  - Net Worth delta is strictly zero ($\Delta \text{NetWorth} = 0$).

### 3.7 Liability Payments
- **Definition**: Payments toward credit cards, personal loans, or land mortgages.
- **Ledger Breakdown**:
  - **Principal Portion**: Reduces outstanding liability balance (`Debit Liability`).
  - **Interest / Charges Portion**: Recorded as financing expense (`Debit Interest Expense`).
  - `Credit`: Bank Account for total payment amount.

---

## 4. Financial Immutability & Audit Trail Rules

1. **No Destructive Modifications**:
   - Posted financial transactions cannot be deleted (`DELETE FROM Transactions` is strictly prohibited in application business flows).
   - If an error occurred in an amount or account, a **Reversal** or **Adjustment** transaction must be posted.
2. **Reversals**:
   - Generates an exact mirrored opposite transaction referencing `ReversedTransactionId`.
   - The original transaction status is updated to `Reversed`.
   - Both transactions remain permanently visible in the audit ledger.
3. **Adjustments**:
   - Explicit adjustment entry with mandatory `AdjustmentReason` field.
4. **Precision**:
   - Storage: `DECIMAL(18, 4)` in MySQL.
   - Application: C# `decimal`.
   - Display: Indian Rupee formatted to 2 decimal places (`₹1,50,000.00`).
   - Floats or doubles are forbidden anywhere in calculation code.
