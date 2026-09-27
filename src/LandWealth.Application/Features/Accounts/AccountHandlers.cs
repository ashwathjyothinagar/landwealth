using FluentValidation;
using LandWealth.Application.Common.Exceptions;
using LandWealth.Application.Common.Interfaces;
using LandWealth.Domain.Accounting;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Application.Features.Accounts;

public sealed record AccountDto(
    Guid Id,
    string Name,
    AccountType AccountType,
    string? Institution,
    string? MaskedAccountNumber,
    decimal OpeningBalance,
    decimal CurrentBalance,
    string Currency,
    bool IsActive,
    string? Notes);

public sealed record CreateAccountCommand(
    string Name,
    AccountType AccountType,
    decimal OpeningBalance,
    string? Institution,
    string? LastFourDigits,
    string? Notes) : IRequest<Guid>;

public sealed class CreateAccountValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.OpeningBalance).GreaterThanOrEqualTo(0);
        RuleFor(command => command.LastFourDigits)
            .Must(value => string.IsNullOrWhiteSpace(value) || (value.Length == 4 && value.All(char.IsDigit)))
            .WithMessage("Only the last 4 digits may be stored.");
        RuleFor(command => command.LastFourDigits)
            .NotEmpty()
            .When(command => command.AccountType is AccountType.BankAccount or AccountType.CreditCard)
            .WithMessage("Bank accounts and credit cards store only the last 4 digits.");
        RuleFor(command => command.Institution).MaximumLength(100);
        RuleFor(command => command.Notes).MaximumLength(2000);
    }
}

public sealed class CreateAccountHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CreateAccountCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = Account.Create(
            currentUser.UserId, request.Name, request.AccountType, request.OpeningBalance, currentUser.UserId,
            request.Institution, request.LastFourDigits, request.Notes);
        context.Add(account);
        await context.SaveChangesAsync(cancellationToken);
        return account.Id;
    }
}

public sealed record UpdateAccountCommand(Guid Id, string Name, string? Institution, bool IsActive, string? Notes) : IRequest;

public sealed class UpdateAccountValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Institution).MaximumLength(100);
        RuleFor(command => command.Notes).MaximumLength(2000);
    }
}

public sealed class UpdateAccountHandler(IApplicationDbContext context, ICurrentUserService currentUser) : IRequestHandler<UpdateAccountCommand>
{
    public async Task Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await context.Accounts.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Account was not found.");
        account.UpdateDetails(request.Name, request.Institution, request.IsActive, request.Notes, currentUser.UserId);
        await context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record ListAccountsQuery : IRequest<IReadOnlyList<AccountDto>>;

public sealed class ListAccountsHandler(IApplicationDbContext context) : IRequestHandler<ListAccountsQuery, IReadOnlyList<AccountDto>>
{
    public async Task<IReadOnlyList<AccountDto>> Handle(ListAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await context.Accounts.AsNoTracking().OrderBy(account => account.Name).ToListAsync(cancellationToken);
        return accounts.Select(account => new AccountDto(
            account.Id, account.Name, account.AccountType, account.Institution, account.MaskedAccountNumber,
            account.OpeningBalance, account.CurrentBalance, account.Currency, account.IsActive, account.Notes)).ToList();
    }
}

public sealed record GetAccountQuery(Guid Id) : IRequest<AccountDto>;

public sealed class GetAccountHandler(IApplicationDbContext context) : IRequestHandler<GetAccountQuery, AccountDto>
{
    public async Task<AccountDto> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        var accounts = await new ListAccountsHandler(context).Handle(new ListAccountsQuery(), cancellationToken);
        return accounts.FirstOrDefault(account => account.Id == request.Id)
            ?? throw new NotFoundException("Account was not found.");
    }
}

public sealed record AccountActivityDto(
    Guid TransactionId,
    DateOnly Date,
    string Description,
    TransactionLineType LineType,
    decimal Amount,
    decimal Balance);

public sealed record AccountStatementDto(AccountDto Account, IReadOnlyList<AccountActivityDto> Activity);

public sealed record GetAccountStatementQuery(Guid Id) : IRequest<AccountStatementDto>;

public sealed class GetAccountStatementHandler(IApplicationDbContext context)
    : IRequestHandler<GetAccountStatementQuery, AccountStatementDto>
{
    public async Task<AccountStatementDto> Handle(GetAccountStatementQuery request, CancellationToken cancellationToken)
    {
        var account = await new GetAccountHandler(context).Handle(new GetAccountQuery(request.Id), cancellationToken);
        var rows = await context.TransactionLines
            .Where(line => line.AccountId == request.Id)
            .Join(
                context.Transactions,
                line => line.TransactionId,
                transaction => transaction.Id,
                (line, transaction) => new
                {
                    transaction.Id,
                    transaction.TransactionDate,
                    transaction.CreatedAt,
                    transaction.Description,
                    line.LineType,
                    line.Amount,
                    LineId = line.Id
                })
            .OrderBy(row => row.TransactionDate)
            .ThenBy(row => row.CreatedAt)
            .ThenBy(row => row.LineId)
            .ToListAsync(cancellationToken);

        var balances = AccountLedger.BalancesAfter(
            account.AccountType,
            account.OpeningBalance,
            rows.Select(row => (row.LineType, row.Amount)));

        var activity = rows.Select((row, index) => new AccountActivityDto(
            row.Id, row.TransactionDate, row.Description, row.LineType, row.Amount, balances[index])).ToList();

        return new AccountStatementDto(account, activity);
    }
}
