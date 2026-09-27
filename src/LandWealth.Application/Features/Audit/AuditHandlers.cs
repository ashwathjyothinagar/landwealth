using System.Text.Json;
using LandWealth.Application.Common.Interfaces;
using LandWealth.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Application.Features.Audit;

public sealed record AuditLogDto(
    long Id,
    string EntityName,
    string EntityId,
    AuditAction Action,
    DateTime Timestamp,
    string? Summary);

public sealed record ListAuditLogsQuery : IRequest<IReadOnlyList<AuditLogDto>>;

public sealed class ListAuditLogsHandler(IApplicationDbContext context) : IRequestHandler<ListAuditLogsQuery, IReadOnlyList<AuditLogDto>>
{
    public async Task<IReadOnlyList<AuditLogDto>> Handle(ListAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var logs = await context.AuditLogs.AsNoTracking()
            .OrderByDescending(log => log.Timestamp)
            .ThenByDescending(log => log.Id)
            .Take(200)
            .ToListAsync(cancellationToken);

        return logs.Select(log => new AuditLogDto(
            log.Id,
            log.EntityName,
            log.EntityId,
            log.Action,
            log.Timestamp,
            AuditSummary.From(log.NewValues))).ToList();
    }
}

internal static class AuditSummary
{
    public static string? From(string? newValues)
    {
        if (string.IsNullOrWhiteSpace(newValues))
            return null;

        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, string?>>(newValues);
            if (payload is null)
                return null;
            if (payload.TryGetValue("Description", out var description) && !string.IsNullOrWhiteSpace(description))
                return description;
            if (payload.TryGetValue("Name", out var name) && !string.IsNullOrWhiteSpace(name))
                return name;
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
