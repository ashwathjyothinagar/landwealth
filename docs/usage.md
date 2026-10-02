# LandWealth usage

LandWealth tracks your land and property investments beside your bank and cash accounts. A property is a capital asset. A bank account is where the money sits. Buying land reduces the bank balance and increases the property's cost. A later valuation changes the estimated worth and does not move cash.

Amounts on screen use Indian grouping, for example `₹12,34,567.00`. Each signed-in user sees only their own properties, accounts, documents, and reports.

Open the app and use **Register** the first time. The password must be at least 8 characters. After that, use **Sign in**. Your name stays in the top bar until you choose **Sign out**.

## Suggested first session

1. Add a bank account or cash drawer under **Accounts**, including the opening balance.
2. Add a property under **Properties**.
3. Open the property and add at least one parcel (survey number and extent).
4. Post the purchase under **Transactions** as **Property purchase**, paid from that account.
5. Return to the property to record a valuation, upload a deed, or add a reminder.
6. Open **Dashboard** to see net worth.

## Dashboard

The dashboard is the home page.

| Figure | Meaning |
|---|---|
| Liquid net worth | Bank, cash, and other liquid accounts, minus credit-card balances. Closed accounts are left out. |
| Property investment | Cost basis of properties: purchase and capital improvements. Maintenance is excluded. |
| Unrealized gain | Latest estimated value minus cost basis. |
| Total net worth | Liquid net worth, plus property value, plus other assets, minus liabilities. |
| This month | Money in and money out. Transfers between your own accounts are excluded. |

The portfolio table lists each property with type, active extent in acres, cost basis, estimated value, and unrealized gain. The property name opens that property. Overdue reminders, and reminders due within 30 days, appear here. The latest transactions are listed below.

## Properties

Create a property with a name, type, village, and state. Types are agricultural land, residential land, commercial land, house, apartment, industrial land, and other. A new property starts as **Planned**.

Open a property to manage it. Status buttons move it forward. The usual path is Planned, Purchased, Held, Under development, For sale, Sold, and Archived. The buttons shown are the only moves allowed from the current status.

### Parcels

A parcel is one survey holding: survey number, optional subdivision (hissa), extent, unit, and boundaries. Extent can be entered in acres, guntas, cents, square feet, square yards, square metres, bigha, or hectares. The app keeps the unit you entered and converts to acres when it totals the holding.

**Split parcel** divides one active parcel. Enter how much extent to split off, a name for the remainder hissa, and a name for the new hissa. The parent parcel is no longer the active extent. The two resulting parcels are.

### Owners

Add an owner with a share percentage. The shares on a property cannot total more than 100%. Ownership types are freehold, joint tenancy, tenancy in common, and leasehold. **Transfer** replaces an owner with a new name and share.

### Cost basis

The property page shows acquisition cost, capital improvements, maintenance, and cost basis. Maintenance is tracked and is not part of the cost basis.

To estimate a sale, enter the extent sold, its unit, the gross proceeds, and the selling expenses, then choose **Estimate sale**. The result is an estimate only. It does not post a transaction or change the bank balance. Post a real sale from **Transactions** as **Property sale**.

### Valuations

Record a date, an estimated value, and a source:

- Guidance value (government)
- Private appraiser
- Bank valuation
- Local market survey
- Owner estimate

Guidance value is kept separate from market estimates. Unrealized gain uses the latest market estimate. Guidance is used only when no market estimate exists. Saving a valuation does not create a cash transaction.

The chart draws guidance and market values across the dates you have recorded.

### Documents

Drop a PDF, PNG, JPEG, or WebP file (up to 25 MB) onto the document area. Choose the document type, and optionally a document number, issue date, parcel, and notes. Types include sale deed, agreement of sale, encumbrance certificate, RTC / Pahani, mutation register, survey sketch, tax receipt, khata certificate, legal opinion, court order, registration receipt, site photo, and other.

The file you download is the one you uploaded. A parcel tag must belong to the same property.

## Accounts

Add one of these:

| Type | What to enter |
|---|---|
| Bank account | Name, institution, exactly four digits of the account number, and opening balance. |
| Cash drawer | Name and opening balance. The last four digits are optional. |
| Credit card | Name, institution, exactly four digits, and the opening amount owed. |
| Other account | Name and opening balance. |

The full account number is never stored. The screen shows a mask such as `•••• 4821`.

Open an account to see the statement. Each posted line shows a running balance computed by the server. You can rename the account, edit the institution and notes, or mark it inactive. An inactive account stays in history and cannot be used on a new transaction. Its balance is left out of liquid net worth.

A credit-card balance is money you owe. Paying it down is a **Liability payment** on the Transactions page, not a transfer.

## Transactions

Every amount you post is a balanced journal entry. You enter the business amount once. The app builds the two sides.

| Type | What it does |
|---|---|
| Income | Money received into a bank, cash, or other account. Choose an income category. No property. |
| Expense | Money paid from a bank, cash, other account, or credit card. Choose an expense category. No property. |
| Transfer | Move money between two different liquid accounts. A credit card cannot be one of the sides. |
| Property purchase | Pay for a property from an account. Choose an acquisition category, such as property purchase or stamp duty. This increases cost basis. |
| Property expense | Spend on a property. An improvement category increases cost basis. A maintenance category, such as land maintenance, does not. |
| Property income | Income tied to a property, such as rent, received into an account. |
| Property sale | Proceeds received into a bank or cash account for a property. |
| Investment | Move money from a funding account into an other-account investment. |
| Liability payment | Pay a credit card from a bank or cash account. |

Only active accounts appear in the form. Filter the list by date, property, or account. The list is paged. Tick the rows you want, including rows on other pages, then choose **Export selected** to download an Excel file.

Posted rows are not deleted. If an entry is wrong:

- **Reverse** on a posted row asks for a note. The reversal posts the opposite entry and restores the affected balance. A reversal itself cannot be reversed. Correct that with an adjustment.
- **Adjustment** posts an explicit debit or credit to an account, offset by a category, and requires a reason.

## Monthly payments

Open **Bills**. Add each EMI or bill you clear every month: name, amount, the day it is due, the account that pays it, and an expense category. An EMI also needs the number of installments. The first month is when the set starts.

Choose a month. The page lists each payment due that month, whether it is still remaining or the date it was paid, and how many EMI installments are left. The counts at the top are how many are due, paid, and remaining. The transaction table lists the payments recorded for that month.

**Add transaction** on a remaining row posts an expense for the date and amount you enter and marks that month cleared. A month that is already paid cannot be recorded again. **Stop** keeps earlier months and drops the payment from later ones.

## Reminders

Add a reminder with a title, optional description, due date, priority, and an optional property. Shortcuts fill the title for property tax, lease expiry, and an agricultural survey.

A reminder that is still pending after its due date is shown as **Overdue**. Low and medium priority are raised to high when the reminder is overdue. **Complete** and **Dismiss** remove it from the dashboard list. Those actions apply only while the reminder is pending.

## Reports

Choose a start date and an end date. The start date must be on or before the end date.

- **Cash flow** is inflows, outflows, and the net for that period. Transfers between your accounts are excluded.
- **Balance sheet** lists each asset and each liability. Net worth is total assets minus liabilities. Credit cards are liabilities on this sheet.
- **Property investment** lists each property's acquisition, improvements, cost basis, operating income, maintenance, and unrealized gain.
- **Valuation history** charts one property across the years you have recorded. Guidance and market values are separate lines.

**Print** uses the browser print dialog. **Export balance sheet**, **Export profitability**, and **Export valuations** download CSV files.

## Audit log

The audit log lists account, transaction, valuation, asset, and liability changes for your sign-in. A reversal appears as its own row next to the original posting. Rows stay in the log. They are not edited or deleted from this screen. Another person's sign-in cannot see them.

## Everyday rules

- Record the purchase and the stamp duty as property purchases if both should sit in the cost basis.
- Record weeding, tax that is a running cost, and similar upkeep as a property expense in a maintenance category.
- Record a borewell, fencing, or other lasting work as a property expense in an improvement category.
- Record rent as property income.
- Record a valuation when your estimate of worth changes. Do not also post a cash transaction for that estimate.
- To undo a posted amount, reverse it and write why. Do not expect a delete button.
