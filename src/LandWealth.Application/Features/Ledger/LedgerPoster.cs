using LandWealth.Application.Common.Exceptions;
using LandWealth.Application.Common.Interfaces;
using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Application.Features.Ledger;

internal static class LedgerPoster
{
    public static async Task ApplyAsync(IApplicationDbContext context, IEnumerable<TransactionLine> lines, CancellationToken cancellationToken)
    {
        var accountIds = lines.Where(line => line.AccountId.HasValue).Select(line => line.AccountId!.Value).Distinct().ToList();
        if (accountIds.Count == 0)
            return;

        var accounts = await context.Accounts.Where(account => accountIds.Contains(account.Id)).ToListAsync(cancellationToken);
        foreach (var line in lines.Where(line => line.AccountId.HasValue))
        {
            var account = accounts.FirstOrDefault(candidate => candidate.Id == line.AccountId)
                ?? throw new NotFoundException("Account was not found.");
            account.ApplyJournalLine(line.LineType, line.Amount);
        }
    }
}
