namespace LandWealth.Domain.Enums;

public enum PropertyType
{
    AgriculturalLand,
    ResidentialLand,
    CommercialLand,
    House,
    Apartment,
    IndustrialLand,
    Other
}

public enum PropertyStatus
{
    Planned,
    Purchased,
    Held,
    UnderDevelopment,
    ForSale,
    Sold,
    Archived
}

public enum ExtentUnit
{
    Acres,
    Guntas,
    Cents,
    SqFt,
    SqYards,
    SqMeters,
    Bigha,
    Hectares
}

public enum ParcelStatus
{
    Active,
    Subdivided,
    PartiallySold,
    Sold,
    Transferred
}

public enum OwnershipType
{
    Freehold,
    Leasehold,
    JointTenancy,
    TenancyInCommon
}

public enum AccountType
{
    BankAccount,
    CashAccount,
    CreditCard,
    OtherFinancialAccount
}

public enum CategoryType
{
    Income,
    Expense,
    Transfer,
    CapEx_Acquisition,
    CapEx_Improvement,
    OpEx_Maintenance
}

public enum TransactionType
{
    Income,
    Expense,
    Transfer,
    PropertyPurchase,
    PropertyExpense,
    PropertyIncome,
    PropertySale,
    Investment,
    LiabilityPayment,
    Adjustment,
    Reversal
}

public enum TransactionStatus
{
    Draft,
    Posted,
    Reversed
}

public enum TransactionLineType
{
    Debit,
    Credit
}

public enum MonthlyPaymentKind
{
    Bill,
    Emi
}

public enum ValuationSource
{
    GovernmentGuidanceValue,
    PrivateAppraiser,
    BankValuation,
    LocalMarketSurvey,
    OwnerEstimate
}

public enum DocumentType
{
    SaleDeed,
    AgreementOfSale,
    EncumbranceCertificate_EC,
    RTC_Pahani,
    MutationRegister,
    SurveySketch_Tippani_Akarband,
    TaxReceipt,
    KhataCertificate,
    LegalOpinion,
    CourtOrder,
    RegistrationReceipt,
    SitePhoto,
    Other
}

public enum ReminderPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum ReminderStatus
{
    Pending,
    Completed,
    Overdue,
    Dismissed
}

public enum AssetType
{
    Gold,
    Vehicle,
    StockInvestment,
    MutualFund,
    FixedDeposit,
    Other
}

public enum LiabilityType
{
    Mortgage,
    LandLoan,
    PersonalLoan,
    VehicleLoan,
    CreditCardOutstanding,
    Other
}

public enum AuditAction
{
    Insert,
    Update,
    SoftDelete,
    Reversal
}
