using LandWealth.Application.Common.Exceptions;
using LandWealth.Application.Common.Interfaces;
using LandWealth.Application.Features.Dashboard;
using LandWealth.Domain.Accounting;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Application.Features.Reports;

public sealed record CashFlowReportDto(DateOnly From, DateOnly To, decimal Inflows, decimal Outflows, decimal Net);

public sealed record GetCashFlowReportQuery(DateOnly From, DateOnly To) : IRequest<CashFlowReportDto>;

public sealed class GetCashFlowReportHandler(IApplicationDbContext context) : IRequestHandler<GetCashFlowReportQuery, CashFlowReportDto>
{
    public async Task<CashFlowReportDto> Handle(GetCashFlowReportQuery request, CancellationToken cancellationToken)
    {
        if (request.From > request.To)
            throw new DomainException("The report start date must be on or before the end date.");

        var accounts = await context.Accounts.AsNoTracking().ToDictionaryAsync(account => account.Id, cancellationToken);
        var transactions = await context.Transactions.AsNoTracking().Include(transaction => transaction.Lines)
            .Where(transaction => transaction.Status != TransactionStatus.Draft
                && transaction.TransactionDate >= request.From
                && transaction.TransactionDate <= request.To)
            .ToListAsync(cancellationToken);

        var lines = transactions.SelectMany(transaction => transaction.Lines
            .Where(line => line.AccountId is Guid accountId && accounts.ContainsKey(accountId))
            .Select(line => new CashLine(accounts[line.AccountId!.Value].AccountType, line.LineType, line.Amount, transaction.TransactionType)));
        var flow = PropertyAccounting.CashFlow(lines);
        return new CashFlowReportDto(request.From, request.To, flow.Inflows, flow.Outflows, flow.Inflows - flow.Outflows);
    }
}

public sealed record BalanceSheetLineDto(string Section, string Name, decimal Amount);

public sealed record BalanceSheetDto(
    decimal LiquidAssets,
    decimal CreditCardDebt,
    decimal PropertyValue,
    decimal OtherAssets,
    decimal TotalAssets,
    decimal Liabilities,
    decimal NetWorth,
    IReadOnlyList<BalanceSheetLineDto> Lines);

public sealed record GetBalanceSheetQuery : IRequest<BalanceSheetDto>;

public sealed class GetBalanceSheetHandler(IApplicationDbContext context, ISender sender) : IRequestHandler<GetBalanceSheetQuery, BalanceSheetDto>
{
    public async Task<BalanceSheetDto> Handle(GetBalanceSheetQuery request, CancellationToken cancellationToken)
    {
        var dashboard = await sender.Send(new GetDashboardQuery(), cancellationToken);
        var accounts = await context.Accounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.Name).ToListAsync(cancellationToken);
        var assets = await context.Assets.AsNoTracking().OrderBy(asset => asset.Name).ToListAsync(cancellationToken);
        var liabilities = await context.Liabilities.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken);
        var liquidAssets = accounts
            .Where(account => account.AccountType is AccountType.BankAccount or AccountType.CashAccount or AccountType.OtherFinancialAccount)
            .Sum(account => account.CurrentBalance);
        var creditCardDebt = accounts.Where(account => account.AccountType == AccountType.CreditCard).Sum(account => account.CurrentBalance);
        var totalAssets = liquidAssets + dashboard.PropertyValue + dashboard.OtherAssets;
        var obligations = creditCardDebt + dashboard.Liabilities;
        var lines = new List<BalanceSheetLineDto>();
        lines.AddRange(accounts
            .Where(account => account.AccountType is AccountType.BankAccount or AccountType.CashAccount or AccountType.OtherFinancialAccount)
            .Select(account => new BalanceSheetLineDto("Asset", account.Name, account.CurrentBalance)));
        lines.AddRange(dashboard.Portfolio.Select(row => new BalanceSheetLineDto("Asset", row.Name, row.EstimatedValue ?? row.CostBasis)));
        lines.AddRange(assets.Select(asset => new BalanceSheetLineDto("Asset", asset.Name, asset.EstimatedValue)));
        lines.AddRange(accounts.Where(account => account.AccountType == AccountType.CreditCard)
            .Select(account => new BalanceSheetLineDto("Liability", account.Name, account.CurrentBalance)));
        lines.AddRange(liabilities.Select(item => new BalanceSheetLineDto("Liability", item.Name, item.OutstandingBalance)));
        return new BalanceSheetDto(
            liquidAssets, creditCardDebt, dashboard.PropertyValue, dashboard.OtherAssets, totalAssets, obligations, totalAssets - obligations, lines);
    }
}

public sealed record PropertyProfitabilityDto(
    Guid PropertyId,
    string Name,
    decimal AcquisitionCost,
    decimal Improvements,
    decimal CostBasis,
    decimal OperatingIncome,
    decimal OperatingExpenses,
    decimal? EstimatedValue,
    decimal? UnrealizedGain);

public sealed record ProfitabilityStatementDto(
    IReadOnlyList<PropertyProfitabilityDto> Properties,
    decimal TotalCostBasis,
    decimal TotalOperatingIncome,
    decimal TotalOperatingExpenses,
    decimal TotalUnrealizedGain);

public sealed record GetPropertyProfitabilityQuery(Guid PropertyId) : IRequest<PropertyProfitabilityDto>;

public sealed class GetPropertyProfitabilityHandler(IApplicationDbContext context)
    : IRequestHandler<GetPropertyProfitabilityQuery, PropertyProfitabilityDto>
{
    public async Task<PropertyProfitabilityDto> Handle(GetPropertyProfitabilityQuery request, CancellationToken cancellationToken)
    {
        var property = await context.Properties.Include(item => item.Valuations).AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.PropertyId, cancellationToken)
            ?? throw new NotFoundException("Property was not found.");
        return await PropertyProfitability.ForProperty(context, property, cancellationToken);
    }
}

public sealed record GetProfitabilityStatementQuery : IRequest<ProfitabilityStatementDto>;

public sealed class GetProfitabilityStatementHandler(IApplicationDbContext context)
    : IRequestHandler<GetProfitabilityStatementQuery, ProfitabilityStatementDto>
{
    public async Task<ProfitabilityStatementDto> Handle(GetProfitabilityStatementQuery request, CancellationToken cancellationToken)
    {
        var properties = await context.Properties.Include(item => item.Valuations).AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var rows = new List<PropertyProfitabilityDto>();
        foreach (var property in properties)
            rows.Add(await PropertyProfitability.ForProperty(context, property, cancellationToken));

        return new ProfitabilityStatementDto(
            rows,
            rows.Sum(row => row.CostBasis),
            rows.Sum(row => row.OperatingIncome),
            rows.Sum(row => row.OperatingExpenses),
            rows.Sum(row => row.UnrealizedGain ?? 0m));
    }
}

internal static class PropertyProfitability
{
    public static async Task<PropertyProfitabilityDto> ForProperty(
        IApplicationDbContext context, Property property, CancellationToken cancellationToken)
    {
        var rows = await (
            from line in context.TransactionLines
            join transaction in context.Transactions on line.TransactionId equals transaction.Id
            join category in context.Categories on line.CategoryId equals category.Id into categories
            from category in categories.DefaultIfEmpty()
            where line.PropertyId == property.Id && transaction.Status != TransactionStatus.Draft
            select new { line.LineType, line.Amount, line.PropertyId, CategoryType = category == null ? (CategoryType?)null : category.CategoryType })
            .ToListAsync(cancellationToken);

        var summary = PropertyAccounting.Summarize(
            rows.Select(row => new BasisLine(row.PropertyId, row.LineType, row.Amount, row.CategoryType)), property.Id);
        var income = Net(rows.Select(row => (row.LineType, row.Amount, row.CategoryType)), CategoryType.Income, TransactionLineType.Credit);
        var expenses = Net(rows.Select(row => (row.LineType, row.Amount, row.CategoryType)), CategoryType.OpEx_Maintenance, TransactionLineType.Debit);
        var estimated = ValuationPolicy.EstimatedValue(property.Valuations.Select(valuation =>
            new ValuationSnapshot(valuation.ValuationDate, valuation.EstimatedValue, valuation.ValuationSource, valuation.CreatedAt)));
        return new PropertyProfitabilityDto(
            property.Id, property.Name, summary.Acquisition, summary.Improvements, summary.CostBasis,
            income, expenses, estimated, PropertyAccounting.UnrealizedGain(estimated, summary.CostBasis));
    }

    private static decimal Net(
        IEnumerable<(TransactionLineType LineType, decimal Amount, CategoryType? CategoryType)> rows,
        CategoryType categoryType,
        TransactionLineType increasing)
    {
        decimal total = 0m;
        foreach (var row in rows)
        {
            if (row.CategoryType != categoryType)
                continue;
            total += row.LineType == increasing ? row.Amount : -row.Amount;
        }

        return total;
    }
}

public sealed record ValuationPointDto(DateOnly Date, decimal EstimatedValue, ValuationSource Source);

public sealed record GetValuationTimelineQuery(Guid PropertyId) : IRequest<IReadOnlyList<ValuationPointDto>>;

public sealed class GetValuationTimelineHandler(IApplicationDbContext context)
    : IRequestHandler<GetValuationTimelineQuery, IReadOnlyList<ValuationPointDto>>
{
    public async Task<IReadOnlyList<ValuationPointDto>> Handle(GetValuationTimelineQuery request, CancellationToken cancellationToken)
    {
        var exists = await context.Properties.AnyAsync(property => property.Id == request.PropertyId, cancellationToken);
        if (!exists)
            throw new NotFoundException("Property was not found.");

        return await context.PropertyValuations.AsNoTracking()
            .Where(valuation => valuation.PropertyId == request.PropertyId)
            .OrderBy(valuation => valuation.ValuationDate)
            .Select(valuation => new ValuationPointDto(valuation.ValuationDate, valuation.EstimatedValue, valuation.ValuationSource))
            .ToListAsync(cancellationToken);
    }
}

public static class CsvExport
{
    public static byte[] Write(IEnumerable<string[]> rows)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', row.Select(Escape)));
        }

        return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
