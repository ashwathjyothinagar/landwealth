using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;

namespace LandWealth.Infrastructure.Persistence.Seed;

internal static class SystemCategorySeed
{
    public static readonly Guid SalaryIncome = Guid.Parse("11111111-0000-4000-8000-000000000001");
    public static readonly Guid AgriculturalIncome = Guid.Parse("11111111-0000-4000-8000-000000000002");
    public static readonly Guid RentalIncome = Guid.Parse("11111111-0000-4000-8000-000000000003");
    public static readonly Guid InterestIncome = Guid.Parse("11111111-0000-4000-8000-000000000004");
    public static readonly Guid OtherIncome = Guid.Parse("11111111-0000-4000-8000-000000000005");
    public static readonly Guid HouseholdExpense = Guid.Parse("11111111-0000-4000-8000-000000000006");
    public static readonly Guid OtherExpense = Guid.Parse("11111111-0000-4000-8000-000000000007");
    public static readonly Guid AccountTransfer = Guid.Parse("11111111-0000-4000-8000-000000000008");
    public static readonly Guid PropertyPurchase = Guid.Parse("11111111-0000-4000-8000-000000000009");
    public static readonly Guid StampDutyRegistration = Guid.Parse("11111111-0000-4000-8000-000000000010");
    public static readonly Guid AcquisitionBrokerage = Guid.Parse("11111111-0000-4000-8000-000000000011");
    public static readonly Guid Borewell = Guid.Parse("11111111-0000-4000-8000-000000000012");
    public static readonly Guid Fencing = Guid.Parse("11111111-0000-4000-8000-000000000013");
    public static readonly Guid ElectricalInfrastructure = Guid.Parse("11111111-0000-4000-8000-000000000014");
    public static readonly Guid RoadsAndLandDevelopment = Guid.Parse("11111111-0000-4000-8000-000000000015");
    public static readonly Guid Construction = Guid.Parse("11111111-0000-4000-8000-000000000016");
    public static readonly Guid PropertyTax = Guid.Parse("11111111-0000-4000-8000-000000000017");
    public static readonly Guid CaretakerAndLabour = Guid.Parse("11111111-0000-4000-8000-000000000018");
    public static readonly Guid LandMaintenance = Guid.Parse("11111111-0000-4000-8000-000000000019");
    public static readonly Guid LegalAndProfessionalFees = Guid.Parse("11111111-0000-4000-8000-000000000020");

    public static IReadOnlyList<Category> All { get; } =
    [
        Category.CreateSystem("Salary & Professional Income", CategoryType.Income, "Salary, professional fees, and similar inflows.", id: SalaryIncome),
        Category.CreateSystem("Agricultural Income", CategoryType.Income, "Harvest, timber, and other farm produce sales.", id: AgriculturalIncome),
        Category.CreateSystem("Rental Income", CategoryType.Income, "Lease, rent, and site usage receipts.", id: RentalIncome),
        Category.CreateSystem("Interest Income", CategoryType.Income, "Bank and deposit interest.", id: InterestIncome),
        Category.CreateSystem("Other Income", CategoryType.Income, "Income that does not fit a more specific category.", id: OtherIncome),
        Category.CreateSystem("Household Expense", CategoryType.Expense, "Personal living costs that are not property costs.", id: HouseholdExpense),
        Category.CreateSystem("Other Expense", CategoryType.Expense, "General expenses that do not fit a more specific category.", id: OtherExpense),
        Category.CreateSystem("Account Transfer", CategoryType.Transfer, "Movement of funds between liquid accounts. Not income or expense.", id: AccountTransfer),
        Category.CreateSystem("Property Purchase", CategoryType.CapEx_Acquisition, "Acquisition price of land or buildings.", id: PropertyPurchase),
        Category.CreateSystem("Stamp Duty & Registration", CategoryType.CapEx_Acquisition, "Stamp duty, registration, and related acquisition charges.", id: StampDutyRegistration),
        Category.CreateSystem("Acquisition Brokerage", CategoryType.CapEx_Acquisition, "Brokerage paid to acquire a property.", id: AcquisitionBrokerage),
        Category.CreateSystem("Borewell", CategoryType.CapEx_Improvement, "Borewell drilling and related capital works.", id: Borewell),
        Category.CreateSystem("Fencing", CategoryType.CapEx_Improvement, "Boundary fencing and compound walls.", id: Fencing),
        Category.CreateSystem("Electrical Infrastructure", CategoryType.CapEx_Improvement, "Transformer, meter, and electrical capital works.", id: ElectricalInfrastructure),
        Category.CreateSystem("Roads & Land Development", CategoryType.CapEx_Improvement, "Internal roads, grading, and land development.", id: RoadsAndLandDevelopment),
        Category.CreateSystem("Construction", CategoryType.CapEx_Improvement, "Buildings and other constructed improvements.", id: Construction),
        Category.CreateSystem("Property Tax", CategoryType.OpEx_Maintenance, "Gram panchayat, municipal, and similar property taxes.", id: PropertyTax),
        Category.CreateSystem("Caretaker & Labour", CategoryType.OpEx_Maintenance, "Watchman, caretaker, and recurring labour.", id: CaretakerAndLabour),
        Category.CreateSystem("Land Maintenance", CategoryType.OpEx_Maintenance, "Weeding, clearing, and routine upkeep.", id: LandMaintenance),
        Category.CreateSystem("Legal & Professional Fees", CategoryType.OpEx_Maintenance, "Retainers and fees that do not form part of acquisition cost.", id: LegalAndProfessionalFees)
    ];
}
