using FluentValidation;
using LandWealth.Application.Common.Exceptions;
using LandWealth.Application.Common.Interfaces;
using LandWealth.Application.Features.Ledger;
using LandWealth.Domain.Accounting;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Application.Features.Transactions;

public sealed record TransactionLineInput(
    TransactionLineType LineType,
    decimal Amount,
    Guid? AccountId,
    Guid? PropertyId,
    Guid? CategoryId,
    string? Memo);

public sealed record TransactionLineDto(
    Guid Id,
    TransactionLineType LineType,
    decimal Amount,
    Guid? AccountId,
    Guid? PropertyId,
    Guid? CategoryId,
    string? Memo);

public sealed record TransactionDto(
    Guid Id,
    DateOnly TransactionDate,
    TransactionType TransactionType,
    decimal Amount,
    string Description,
    TransactionStatus Status,
    string? ReferenceNumber,
    Guid? PropertyId,
    Guid? ParcelId,
    Guid? ReversedTransactionId,
    string? Notes,
    IReadOnlyList<TransactionLineDto> Lines);

public sealed record CreateTransactionCommand(
    DateOnly TransactionDate,
    TransactionType TransactionType,
    decimal Amount,
    string Description,
    Guid? PropertyId,
    Guid? ParcelId,
    string? ReferenceNumber,
    string? Notes,
    IReadOnlyList<TransactionLineInput> Lines) : IRequest<Guid>;

public sealed class CreateTransactionValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionValidator()
    {
        RuleFor(command => command.Description).NotEmpty().MaximumLength(300);
        RuleFor(command => command.Amount).GreaterThan(0);
        RuleFor(command => command.Lines).NotNull().Must(lines => lines.Count >= 2)
            .WithMessage("A transaction needs at least two lines.");
        RuleFor(command => command).Must(Balanced).WithMessage("Debits must equal credits and the transaction amount.");
        RuleFor(command => command.TransactionType)
            .Must(type => type is not (TransactionType.Reversal or TransactionType.Adjustment))
            .WithMessage("Use the reversal or adjustment endpoints for corrections.");
    }

    private static bool Balanced(CreateTransactionCommand command)
    {
        if (command.Lines is null)
            return false;
        var debits = command.Lines.Where(line => line.LineType == TransactionLineType.Debit).Sum(line => line.Amount);
        var credits = command.Lines.Where(line => line.LineType == TransactionLineType.Credit).Sum(line => line.Amount);
        return debits == credits && debits == command.Amount;
    }
}

public sealed class CreateTransactionHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CreateTransactionCommand, Guid>
{
    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        await TransactionClassification.EnsureAsync(
            context, request.TransactionType, request.PropertyId, request.Lines, cancellationToken);
        var transaction = Transaction.Create(
            currentUser.UserId, request.TransactionDate, request.TransactionType, request.Amount, request.Description,
            currentUser.UserId, request.PropertyId, request.ParcelId, request.ReferenceNumber, request.Notes);
        foreach (var line in request.Lines)
        {
            transaction.AddLine(line.LineType, line.Amount, line.AccountId, line.PropertyId, line.CategoryId, line.Memo);
        }

        transaction.Post();
        await LedgerPoster.ApplyAsync(context, transaction.Lines, cancellationToken);
        context.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);
        return transaction.Id;
    }
}

public sealed record CreateAdjustmentCommand(
    DateOnly TransactionDate,
    decimal Amount,
    string Description,
    string Reason,
    Guid? PropertyId,
    IReadOnlyList<TransactionLineInput> Lines) : IRequest<Guid>;

public sealed class CreateAdjustmentValidator : AbstractValidator<CreateAdjustmentCommand>
{
    public CreateAdjustmentValidator()
    {
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(command => command.Description).NotEmpty().MaximumLength(300);
        RuleFor(command => command.Amount).GreaterThan(0);
        RuleFor(command => command.Lines).Must(lines => lines is { Count: >= 2 });
    }
}

public sealed class CreateAdjustmentHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CreateAdjustmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var debits = request.Lines.Where(line => line.LineType == TransactionLineType.Debit).Sum(line => line.Amount);
        var credits = request.Lines.Where(line => line.LineType == TransactionLineType.Credit).Sum(line => line.Amount);
        if (debits != credits || debits != request.Amount)
            throw new DomainException("Debits must equal credits and the transaction amount.");

        await TransactionClassification.EnsureAsync(
            context, TransactionType.Adjustment, request.PropertyId, request.Lines, cancellationToken);
        var transaction = Transaction.Create(
            currentUser.UserId, request.TransactionDate, TransactionType.Adjustment, request.Amount, request.Description,
            currentUser.UserId, request.PropertyId, notes: request.Reason.Trim());
        foreach (var line in request.Lines)
            transaction.AddLine(line.LineType, line.Amount, line.AccountId, line.PropertyId, line.CategoryId, line.Memo);
        transaction.Post();
        await LedgerPoster.ApplyAsync(context, transaction.Lines, cancellationToken);
        context.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);
        return transaction.Id;
    }
}

public sealed record ReverseTransactionCommand(Guid TransactionId, string Description) : IRequest<Guid>;

public sealed class ReverseTransactionValidator : AbstractValidator<ReverseTransactionCommand>
{
    public ReverseTransactionValidator()
    {
        RuleFor(command => command.Description).NotEmpty().MaximumLength(300);
    }
}

public sealed class ReverseTransactionHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<ReverseTransactionCommand, Guid>
{
    public async Task<Guid> Handle(ReverseTransactionCommand request, CancellationToken cancellationToken)
    {
        var original = await context.Transactions.Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == request.TransactionId, cancellationToken)
            ?? throw new NotFoundException("Transaction was not found.");

        var reversal = original.CreateReversal(currentUser.UserId, request.Description);
        reversal.Post();
        await LedgerPoster.ApplyAsync(context, reversal.Lines, cancellationToken);
        original.MarkReversed(currentUser.UserId);
        context.Add(reversal);
        await context.SaveChangesAsync(cancellationToken);
        return reversal.Id;
    }
}

public sealed record PagedTransactions(IReadOnlyList<TransactionDto> Items, int TotalCount, int Page, int PageSize);

public sealed record ListTransactionsQuery(DateOnly? From, DateOnly? To, Guid? PropertyId, Guid? AccountId, int Page = 1, int PageSize = 25)
    : IRequest<PagedTransactions>;

public sealed class ListTransactionsHandler(IApplicationDbContext context) : IRequestHandler<ListTransactionsQuery, PagedTransactions>
{
    public async Task<PagedTransactions> Handle(ListTransactionsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 25 : request.PageSize;
        var filtered = Filtered(context, request);
        var total = await filtered.CountAsync(cancellationToken);
        var ids = await filtered
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(transaction => transaction.Id)
            .ToListAsync(cancellationToken);

        var transactions = await context.Transactions.AsNoTracking()
            .Include(transaction => transaction.Lines)
            .Where(transaction => ids.Contains(transaction.Id))
            .ToListAsync(cancellationToken);
        var items = ids
            .Select(id => transactions.Single(transaction => transaction.Id == id))
            .Select(Map)
            .ToList();
        return new PagedTransactions(items, total, page, pageSize);
    }

    internal static IQueryable<Transaction> Filtered(IApplicationDbContext context, ListTransactionsQuery request)
    {
        var query = context.Transactions.AsNoTracking().AsQueryable();
        if (request.From is not null)
            query = query.Where(transaction => transaction.TransactionDate >= request.From);
        if (request.To is not null)
            query = query.Where(transaction => transaction.TransactionDate <= request.To);
        if (request.PropertyId is not null)
            query = query.Where(transaction => transaction.PropertyId == request.PropertyId);
        if (request.AccountId is not null)
            query = query.Where(transaction => transaction.Lines.Any(line => line.AccountId == request.AccountId));
        return query;
    }

    internal static TransactionDto Map(Transaction transaction) => new(
        transaction.Id, transaction.TransactionDate, transaction.TransactionType, transaction.Amount, transaction.Description,
        transaction.Status, transaction.ReferenceNumber, transaction.PropertyId, transaction.ParcelId,
        transaction.ReversedTransactionId, transaction.Notes,
        transaction.Lines.Select(line => new TransactionLineDto(
            line.Id, line.LineType, line.Amount, line.AccountId, line.PropertyId, line.CategoryId, line.Memo)).ToList());
}

public sealed record ExportTransactionsCommand(IReadOnlyList<Guid> Ids) : IRequest<IReadOnlyList<TransactionDto>>;

public sealed class ExportTransactionsValidator : AbstractValidator<ExportTransactionsCommand>
{
    public ExportTransactionsValidator()
    {
        RuleFor(command => command.Ids).NotEmpty().Must(ids => ids.Count <= 500)
            .WithMessage("Select at most 500 transactions.");
    }
}

public sealed class ExportTransactionsHandler(IApplicationDbContext context)
    : IRequestHandler<ExportTransactionsCommand, IReadOnlyList<TransactionDto>>
{
    public async Task<IReadOnlyList<TransactionDto>> Handle(ExportTransactionsCommand request, CancellationToken cancellationToken)
    {
        var ids = request.Ids.Distinct().ToList();
        var transactions = await context.Transactions.AsNoTracking()
            .Where(transaction => ids.Contains(transaction.Id))
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.Id)
            .ToListAsync(cancellationToken);
        if (transactions.Count == 0)
            throw new NotFoundException("Transaction was not found.");
        return transactions.Select(ListTransactionsHandler.Map).ToList();
    }
}

public sealed record GetTransactionQuery(Guid Id) : IRequest<TransactionDto>;

public sealed class GetTransactionHandler(IApplicationDbContext context) : IRequestHandler<GetTransactionQuery, TransactionDto>
{
    public async Task<TransactionDto> Handle(GetTransactionQuery request, CancellationToken cancellationToken)
    {
        var transaction = await context.Transactions.Include(item => item.Lines).AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Transaction was not found.");
        return ListTransactionsHandler.Map(transaction);
    }
}

public sealed record CategoryDto(Guid Id, string Name, CategoryType CategoryType, string? Description, bool IsSystem);

public sealed record ListCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;

public sealed class ListCategoriesHandler(IApplicationDbContext context) : IRequestHandler<ListCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await context.Categories.AsNoTracking().OrderBy(category => category.Name).ToListAsync(cancellationToken);
        return categories.Select(category => new CategoryDto(
            category.Id, category.Name, category.CategoryType, category.Description, category.IsSystem)).ToList();
    }
}

internal static class TransactionClassification
{
    public static async Task EnsureAsync(
        IApplicationDbContext context,
        TransactionType type,
        Guid? propertyId,
        IReadOnlyList<TransactionLineInput> lines,
        CancellationToken cancellationToken)
    {
        var accountIds = lines.Where(line => line.AccountId.HasValue).Select(line => line.AccountId!.Value).Distinct().ToList();
        var accounts = accountIds.Count == 0
            ? new List<Account>()
            : await context.Accounts.Where(account => accountIds.Contains(account.Id)).ToListAsync(cancellationToken);
        if (accounts.Count != accountIds.Count)
            throw new NotFoundException("Account was not found.");

        var categoryIds = lines.Where(line => line.CategoryId.HasValue).Select(line => line.CategoryId!.Value).Distinct().ToList();
        var categories = categoryIds.Count == 0
            ? new List<Category>()
            : await context.Categories.Where(category => categoryIds.Contains(category.Id)).ToListAsync(cancellationToken);
        if (categories.Count != categoryIds.Count)
            throw new NotFoundException("Category was not found.");

        var posting = lines.Select(line => new PostingLine(
            line.LineType,
            line.Amount,
            line.AccountId,
            line.AccountId is null ? null : accounts.Single(account => account.Id == line.AccountId).AccountType,
            line.PropertyId ?? propertyId,
            line.CategoryId is null ? null : categories.Single(category => category.Id == line.CategoryId).CategoryType)).ToList();

        TransactionPostingRules.Ensure(type, propertyId, posting);
    }
}
