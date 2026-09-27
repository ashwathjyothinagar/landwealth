using LandWealth.Application.Common.Interfaces;
using LandWealth.Domain.Accounting;
using LandWealth.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Application.Features.Dashboard;

public sealed record PortfolioRowDto(
    Guid PropertyId,
    string Name,
    PropertyType PropertyType,
    PropertyStatus Status,
    decimal ActiveExtentAcres,
    decimal CostBasis,
    decimal? EstimatedValue,
    decimal? UnrealizedGain);

public sealed record RecentTransactionDto(
    Guid Id,
    DateOnly TransactionDate,
    TransactionType TransactionType,
    string Description,
    decimal Amount,
    TransactionStatus Status);

public sealed record DashboardDto(
    decimal LiquidNetWorth,
    decimal PropertyInvestment,
    decimal PropertyValue,
    decimal UnrealizedGain,
    decimal OtherAssets,
    decimal Liabilities,
    decimal TotalNetWorth,
    decimal MonthInflows,
    decimal MonthOutflows,
    IReadOnlyList<PortfolioRowDto> Portfolio,
    IReadOnlyList<Reminders.ReminderDto> UpcomingReminders,
    IReadOnlyList<RecentTransactionDto> RecentTransactions);

public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed class GetDashboardHandler(IApplicationDbContext context, IDateTimeProvider clock)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var accounts = await context.Accounts.AsNoTracking().Where(account => account.IsActive).ToListAsync(cancellationToken);
        var properties = await context.Properties.AsNoTracking().Include(property => property.Parcels).Include(property => property.Valuations).ToListAsync(cancellationToken);
        var transactions = await context.Transactions.AsNoTracking().Include(transaction => transaction.Lines)
            .Where(transaction => transaction.Status != TransactionStatus.Draft)
            .ToListAsync(cancellationToken);
        var categories = await context.Categories.AsNoTracking().ToDictionaryAsync(category => category.Id, cancellationToken);
        var assets = await context.Assets.AsNoTracking().Where(asset => !asset.IsDeleted).ToListAsync(cancellationToken);
        var liabilities = await context.Liabilities.AsNoTracking().ToListAsync(cancellationToken);
        var reminders = await context.PropertyReminders.AsNoTracking().OrderBy(reminder => reminder.DueDate).ToListAsync(cancellationToken);

        var basisLines = transactions.SelectMany(transaction => transaction.Lines.Select(line =>
        {
            CategoryType? categoryType = line.CategoryId is Guid categoryId && categories.TryGetValue(categoryId, out var category)
                ? category.CategoryType
                : null;
            return new BasisLine(line.PropertyId, line.LineType, line.Amount, categoryType);
        })).ToList();

        var portfolio = properties.Select(property =>
        {
            var basis = PropertyAccounting.CostBasis(basisLines, property.Id);
            var estimated = ValuationPolicy.EstimatedValue(property.Valuations.Select(valuation =>
                new ValuationSnapshot(valuation.ValuationDate, valuation.EstimatedValue, valuation.ValuationSource, valuation.CreatedAt)));
            var extent = property.Parcels.Where(parcel => parcel.Status == ParcelStatus.Active).Sum(parcel => parcel.ExtentInAcres);
            return new PortfolioRowDto(
                property.Id, property.Name, property.PropertyType, property.Status, extent, basis, estimated,
                PropertyAccounting.UnrealizedGain(estimated, basis));
        }).ToList();

        var liquid = PropertyAccounting.LiquidNetWorth(accounts.Select(account => (account.AccountType, account.CurrentBalance)));
        var propertyInvestment = portfolio.Sum(row => row.CostBasis);
        var propertyValue = portfolio.Sum(row => row.EstimatedValue ?? row.CostBasis);
        var unrealized = portfolio.Sum(row => row.UnrealizedGain ?? 0m);
        var otherAssets = assets.Sum(asset => asset.EstimatedValue);
        var liabilityTotal = liabilities.Sum(liability => liability.OutstandingBalance);
        var monthStart = new DateOnly(clock.Today.Year, clock.Today.Month, 1);
        var monthLines = transactions
            .Where(transaction => transaction.TransactionDate >= monthStart && transaction.TransactionDate <= clock.Today)
            .SelectMany(transaction => transaction.Lines
                .Where(line => line.AccountId is not null)
                .Select(line =>
                {
                    var account = accounts.FirstOrDefault(candidate => candidate.Id == line.AccountId);
                    return account is null
                        ? (CashLine?)null
                        : new CashLine(account.AccountType, line.LineType, line.Amount, transaction.TransactionType);
                }))
            .Where(line => line is not null)
            .Select(line => line!.Value);
        var cashFlow = PropertyAccounting.CashFlow(monthLines);
        var upcoming = reminders
            .Where(reminder => ReminderPolicy.ShowOnDashboard(reminder.Status, reminder.DueDate, clock.Today))
            .Select(reminder => Reminders.ListRemindersHandler.Map(reminder, clock.Today))
            .Take(5)
            .ToList();
        var recent = transactions
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.CreatedAt)
            .Take(10)
            .Select(transaction => new RecentTransactionDto(
                transaction.Id, transaction.TransactionDate, transaction.TransactionType, transaction.Description,
                transaction.Amount, transaction.Status))
            .ToList();

        return new DashboardDto(
            liquid, propertyInvestment, propertyValue, unrealized, otherAssets, liabilityTotal,
            liquid + propertyValue + otherAssets - liabilityTotal,
            cashFlow.Inflows, cashFlow.Outflows, portfolio, upcoming, recent);
    }
}
