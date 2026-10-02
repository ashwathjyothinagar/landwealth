using FluentValidation;
using LandWealth.Application.Common.Exceptions;
using LandWealth.Application.Common.Interfaces;
using LandWealth.Application.Features.Ledger;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Application.Features.MonthlyPayments;

public sealed record MonthlyPaymentDto(
    Guid Id,
    string Name,
    MonthlyPaymentKind Kind,
    decimal Amount,
    int DueDay,
    Guid AccountId,
    Guid CategoryId,
    int? TotalInstallments,
    DateOnly StartsOn,
    DateOnly? StoppedOn,
    bool IsActive,
    string? Notes);

public sealed record CreateMonthlyPaymentCommand(
    string Name,
    MonthlyPaymentKind Kind,
    decimal Amount,
    int DueDay,
    Guid AccountId,
    Guid CategoryId,
    DateOnly StartsOn,
    int? TotalInstallments,
    string? Notes) : IRequest<Guid>;

public sealed class CreateMonthlyPaymentValidator : AbstractValidator<CreateMonthlyPaymentCommand>
{
    public CreateMonthlyPaymentValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Amount).GreaterThan(0);
        RuleFor(command => command.DueDay).InclusiveBetween(1, 31);
        RuleFor(command => command.Notes).MaximumLength(1000);
    }
}

public sealed class CreateMonthlyPaymentHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CreateMonthlyPaymentCommand, Guid>
{
    public async Task<Guid> Handle(CreateMonthlyPaymentCommand request, CancellationToken cancellationToken)
    {
        await EnsureAccountAndCategoryAsync(context, request.AccountId, request.CategoryId, cancellationToken);
        var payment = MonthlyPayment.Create(
            currentUser.UserId, request.Name, request.Kind, request.Amount, request.DueDay,
            request.AccountId, request.CategoryId, request.StartsOn, currentUser.UserId,
            request.TotalInstallments, request.Notes);
        context.Add(payment);
        await context.SaveChangesAsync(cancellationToken);
        return payment.Id;
    }

    internal static async Task EnsureAccountAndCategoryAsync(
        IApplicationDbContext context, Guid accountId, Guid categoryId, CancellationToken cancellationToken)
    {
        var account = await context.Accounts.AsNoTracking().FirstOrDefaultAsync(item => item.Id == accountId, cancellationToken)
            ?? throw new NotFoundException("Account was not found.");
        if (!account.IsActive)
            throw new DomainException("The account is not active.");
        if (account.AccountType is not (AccountType.BankAccount or AccountType.CashAccount or AccountType.CreditCard or AccountType.OtherFinancialAccount))
            throw new DomainException("Choose the account that pays this bill.");

        var category = await context.Categories.AsNoTracking().FirstOrDefaultAsync(item => item.Id == categoryId, cancellationToken)
            ?? throw new NotFoundException("Category was not found.");
        if (category.CategoryType != CategoryType.Expense)
            throw new DomainException("A monthly payment uses an expense category.");
    }
}

public sealed record StopMonthlyPaymentCommand(Guid Id) : IRequest;

public sealed class StopMonthlyPaymentHandler(IApplicationDbContext context, ICurrentUserService currentUser, IDateTimeProvider clock)
    : IRequestHandler<StopMonthlyPaymentCommand>
{
    public async Task Handle(StopMonthlyPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await context.MonthlyPayments.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Monthly payment was not found.");
        payment.Stop(clock.Today, currentUser.UserId);
        await context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record ListMonthlyPaymentsQuery : IRequest<IReadOnlyList<MonthlyPaymentDto>>;

public sealed class ListMonthlyPaymentsHandler(IApplicationDbContext context)
    : IRequestHandler<ListMonthlyPaymentsQuery, IReadOnlyList<MonthlyPaymentDto>>
{
    public async Task<IReadOnlyList<MonthlyPaymentDto>> Handle(ListMonthlyPaymentsQuery request, CancellationToken cancellationToken)
    {
        var payments = await context.MonthlyPayments.AsNoTracking()
            .OrderBy(payment => payment.Name)
            .ToListAsync(cancellationToken);
        return payments.Select(Map).ToList();
    }

    internal static MonthlyPaymentDto Map(MonthlyPayment payment) => new(
        payment.Id, payment.Name, payment.Kind, payment.Amount, payment.DueDay, payment.AccountId, payment.CategoryId,
        payment.TotalInstallments, payment.StartsOn, payment.StoppedOn, payment.IsActive, payment.Notes);
}

public sealed record MonthlyPaymentMonthItemDto(
    Guid Id,
    string Name,
    MonthlyPaymentKind Kind,
    decimal ExpectedAmount,
    DateOnly DueDate,
    bool Paid,
    DateOnly? PaidOn,
    Guid? TransactionId,
    decimal? PaidAmount,
    int? InstallmentsRemaining);

public sealed record MonthlyPaymentTransactionDto(
    Guid TransactionId,
    Guid MonthlyPaymentId,
    string Name,
    DateOnly PaidOn,
    decimal Amount,
    string Description,
    TransactionStatus Status);

public sealed record MonthlyPaymentMonthDto(
    int Year,
    int Month,
    int DueCount,
    int PaidCount,
    int RemainingCount,
    decimal DueAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    IReadOnlyList<MonthlyPaymentMonthItemDto> Items,
    IReadOnlyList<MonthlyPaymentTransactionDto> Transactions);

public sealed record GetMonthlyPaymentMonthQuery(int Year, int Month) : IRequest<MonthlyPaymentMonthDto>;

public sealed class GetMonthlyPaymentMonthHandler(IApplicationDbContext context)
    : IRequestHandler<GetMonthlyPaymentMonthQuery, MonthlyPaymentMonthDto>
{
    public async Task<MonthlyPaymentMonthDto> Handle(GetMonthlyPaymentMonthQuery request, CancellationToken cancellationToken)
    {
        if (request.Month is < 1 or > 12)
            throw new DomainException("Month must be from 1 to 12.");

        var payments = await context.MonthlyPayments.AsNoTracking().ToListAsync(cancellationToken);
        var paymentIds = payments.Select(payment => payment.Id).ToList();
        var clearings = paymentIds.Count == 0
            ? []
            : await context.MonthlyPaymentClearings.AsNoTracking()
                .Where(clearing => paymentIds.Contains(clearing.MonthlyPaymentId))
                .ToListAsync(cancellationToken);
        var transactionIds = clearings.Select(clearing => clearing.TransactionId).Distinct().ToList();
        var transactions = transactionIds.Count == 0
            ? []
            : await context.Transactions.AsNoTracking()
                .Where(transaction => transactionIds.Contains(transaction.Id))
                .ToListAsync(cancellationToken);
        var postedIds = transactions.Where(transaction => transaction.Status == TransactionStatus.Posted)
            .Select(transaction => transaction.Id)
            .ToHashSet();

        var cursor = request.Year * 12 + request.Month;
        var items = new List<MonthlyPaymentMonthItemDto>();
        foreach (var payment in payments.OrderBy(payment => payment.Name))
        {
            if (!MonthlyPaymentSchedule.IsDue(payment.StartsOn, payment.StoppedOn, payment.TotalInstallments, payment.IsActive, request.Year, request.Month))
                continue;

            var clearing = clearings
                .Where(item => item.MonthlyPaymentId == payment.Id && item.Year == request.Year && item.Month == request.Month && postedIds.Contains(item.TransactionId))
                .OrderByDescending(item => item.PaidOn)
                .FirstOrDefault();
            int? remaining = null;
            if (payment.TotalInstallments is int total)
            {
                var paidInstallments = clearings.Count(item =>
                    item.MonthlyPaymentId == payment.Id
                    && postedIds.Contains(item.TransactionId)
                    && item.Year * 12 + item.Month <= cursor);
                remaining = Math.Max(0, total - paidInstallments);
            }

            items.Add(new MonthlyPaymentMonthItemDto(
                payment.Id,
                payment.Name,
                payment.Kind,
                payment.Amount,
                MonthlyPaymentSchedule.DueDate(request.Year, request.Month, payment.DueDay),
                clearing is not null,
                clearing?.PaidOn,
                clearing?.TransactionId,
                clearing?.Amount,
                remaining));
        }

        var monthTransactions = clearings
            .Where(clearing => clearing.Year == request.Year && clearing.Month == request.Month)
            .Join(transactions, clearing => clearing.TransactionId, transaction => transaction.Id, (clearing, transaction) => new { clearing, transaction })
            .Where(pair => pair.transaction.Status == TransactionStatus.Posted)
            .OrderBy(pair => pair.clearing.PaidOn)
            .Select(pair => new MonthlyPaymentTransactionDto(
                pair.transaction.Id,
                pair.clearing.MonthlyPaymentId,
                payments.Single(payment => payment.Id == pair.clearing.MonthlyPaymentId).Name,
                pair.clearing.PaidOn,
                pair.clearing.Amount,
                pair.transaction.Description,
                pair.transaction.Status))
            .ToList();

        var paid = items.Where(item => item.Paid).ToList();
        var open = items.Where(item => !item.Paid).ToList();
        return new MonthlyPaymentMonthDto(
            request.Year,
            request.Month,
            items.Count,
            paid.Count,
            open.Count,
            items.Sum(item => item.ExpectedAmount),
            paid.Sum(item => item.PaidAmount ?? 0),
            open.Sum(item => item.ExpectedAmount),
            items,
            monthTransactions);
    }
}

public sealed record RecordMonthlyPaymentCommand(Guid MonthlyPaymentId, int Year, int Month, DateOnly PaidOn, decimal Amount) : IRequest<Guid>;

public sealed class RecordMonthlyPaymentValidator : AbstractValidator<RecordMonthlyPaymentCommand>
{
    public RecordMonthlyPaymentValidator()
    {
        RuleFor(command => command.Amount).GreaterThan(0);
        RuleFor(command => command.Month).InclusiveBetween(1, 12);
    }
}

public sealed class RecordMonthlyPaymentHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<RecordMonthlyPaymentCommand, Guid>
{
    public async Task<Guid> Handle(RecordMonthlyPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await context.MonthlyPayments.AsNoTracking().FirstOrDefaultAsync(item => item.Id == request.MonthlyPaymentId, cancellationToken)
            ?? throw new NotFoundException("Monthly payment was not found.");
        if (!MonthlyPaymentSchedule.IsDue(payment.StartsOn, payment.StoppedOn, payment.TotalInstallments, payment.IsActive, request.Year, request.Month))
            throw new DomainException("This payment is not due in that month.");

        var existing = await context.MonthlyPaymentClearings
            .Where(clearing => clearing.MonthlyPaymentId == payment.Id && clearing.Year == request.Year && clearing.Month == request.Month)
            .Select(clearing => clearing.TransactionId)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            var alreadyPosted = await context.Transactions.AnyAsync(
                transaction => existing.Contains(transaction.Id) && transaction.Status == TransactionStatus.Posted,
                cancellationToken);
            if (alreadyPosted)
                throw new DomainException("This payment is already recorded for that month.");
        }

        await CreateMonthlyPaymentHandler.EnsureAccountAndCategoryAsync(context, payment.AccountId, payment.CategoryId, cancellationToken);
        var description = $"{payment.Name} {request.Year:0000}-{request.Month:00}";
        var transaction = Transaction.Create(
            currentUser.UserId, request.PaidOn, TransactionType.Expense, request.Amount, description, currentUser.UserId);
        transaction.AddLine(TransactionLineType.Debit, request.Amount, categoryId: payment.CategoryId);
        transaction.AddLine(TransactionLineType.Credit, request.Amount, accountId: payment.AccountId);
        transaction.Post();
        await LedgerPoster.ApplyAsync(context, transaction.Lines, cancellationToken);

        var clearing = MonthlyPaymentClearing.Record(
            currentUser.UserId, payment.Id, transaction.Id, request.Year, request.Month, request.PaidOn, request.Amount, currentUser.UserId);
        context.Add(transaction);
        context.Add(clearing);
        await context.SaveChangesAsync(cancellationToken);
        return transaction.Id;
    }
}
