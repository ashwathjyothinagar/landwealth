using LandWealth.Application.Features.Accounts;
using LandWealth.Application.Features.Audit;
using LandWealth.Application.Features.Dashboard;
using LandWealth.Application.Features.Documents;
using LandWealth.Application.Features.NetWorth;
using LandWealth.Application.Features.Reminders;
using LandWealth.Application.Features.Reports;
using LandWealth.Application.Features.Transactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LandWealth.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts")]
public sealed class AccountsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> List(CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListAccountsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetAccountQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult> Create(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpGet("{id:guid}/statement")]
    public async Task<ActionResult<AccountStatementDto>> Statement(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetAccountStatementQuery(id), cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateAccountCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> List(CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListCategoriesQuery(), cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/transactions")]
public sealed class TransactionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> List(
        DateOnly? from, DateOnly? to, Guid? propertyId, Guid? accountId, CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListTransactionsQuery(from, to, propertyId, accountId), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetTransactionQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult> Create(CreateTransactionCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPost("adjustments")]
    public async Task<ActionResult> Adjust(CreateAdjustmentCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult> Reverse(Guid id, ReverseTransactionCommand command, CancellationToken cancellationToken)
    {
        var reversalId = await sender.Send(command with { TransactionId = id }, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = reversalId }, new { id = reversalId });
    }
}

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var download = await sender.Send(new DownloadDocumentQuery(id), cancellationToken);
        var fileName = Path.GetFileName(download.Metadata.OriginalFileName);
        return File(download.Content, download.Metadata.ContentType, fileName);
    }
}

[ApiController]
[Authorize]
[Route("api/reminders")]
public sealed class RemindersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReminderDto>>> List(bool upcomingOnly, CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListRemindersQuery(upcomingOnly), cancellationToken));

    [HttpPost]
    public async Task<ActionResult> Create(CreateReminderCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return Created($"/api/reminders/{id}", new { id });
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CompleteReminderCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DismissReminderCommand(id), cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetDashboardQuery(), cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController(ISender sender) : ControllerBase
{
    [HttpGet("cash-flow")]
    public async Task<ActionResult<CashFlowReportDto>> CashFlow(DateOnly from, DateOnly to, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCashFlowReportQuery(from, to), cancellationToken));

    [HttpGet("balance-sheet")]
    public async Task<ActionResult<BalanceSheetDto>> BalanceSheet(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetBalanceSheetQuery(), cancellationToken));

    [HttpGet("balance-sheet.csv")]
    public async Task<IActionResult> BalanceSheetCsv(CancellationToken cancellationToken)
    {
        var sheet = await sender.Send(new GetBalanceSheetQuery(), cancellationToken);
        var rows = new List<string[]> { new[] { "Section", "Name", "Amount" } };
        rows.AddRange(sheet.Lines.Select(line => new[]
        {
            line.Section,
            line.Name,
            line.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
        }));
        rows.Add(new[] { "Total", "Assets", sheet.TotalAssets.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        rows.Add(new[] { "Total", "Liabilities", sheet.Liabilities.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        rows.Add(new[] { "Total", "Net worth", sheet.NetWorth.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        return File(CsvExport.Write(rows), "text/csv", "balance-sheet.csv");
    }

    [HttpGet("profitability")]
    public async Task<ActionResult<ProfitabilityStatementDto>> Profitability(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetProfitabilityStatementQuery(), cancellationToken));

    [HttpGet("profitability.csv")]
    public async Task<IActionResult> ProfitabilityCsv(CancellationToken cancellationToken)
    {
        var statement = await sender.Send(new GetProfitabilityStatementQuery(), cancellationToken);
        var rows = new List<string[]>
        {
            new[] { "Property", "Acquisition", "Improvements", "CostBasis", "OperatingIncome", "OperatingExpenses", "UnrealizedGain" }
        };
        rows.AddRange(statement.Properties.Select(row => new[]
        {
            row.Name,
            row.AcquisitionCost.ToString(System.Globalization.CultureInfo.InvariantCulture),
            row.Improvements.ToString(System.Globalization.CultureInfo.InvariantCulture),
            row.CostBasis.ToString(System.Globalization.CultureInfo.InvariantCulture),
            row.OperatingIncome.ToString(System.Globalization.CultureInfo.InvariantCulture),
            row.OperatingExpenses.ToString(System.Globalization.CultureInfo.InvariantCulture),
            (row.UnrealizedGain ?? 0m).ToString(System.Globalization.CultureInfo.InvariantCulture)
        }));
        return File(CsvExport.Write(rows), "text/csv", "profitability.csv");
    }

    [HttpGet("properties/{id:guid}/profitability")]
    public async Task<ActionResult<PropertyProfitabilityDto>> Profitability(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPropertyProfitabilityQuery(id), cancellationToken));

    [HttpGet("properties/{id:guid}/valuations")]
    public async Task<ActionResult<IReadOnlyList<ValuationPointDto>>> ValuationTimeline(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetValuationTimelineQuery(id), cancellationToken));

    [HttpGet("properties/{id:guid}/valuations.csv")]
    public async Task<IActionResult> ValuationCsv(Guid id, CancellationToken cancellationToken)
    {
        var points = await sender.Send(new GetValuationTimelineQuery(id), cancellationToken);
        var rows = new List<string[]> { new[] { "Date", "EstimatedValue", "Source" } };
        rows.AddRange(points.Select(point => new[]
        {
            point.Date.ToString("yyyy-MM-dd"),
            point.EstimatedValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            point.Source.ToString()
        }));
        return File(CsvExport.Write(rows), "text/csv", "valuations.csv");
    }
}

[ApiController]
[Authorize]
[Route("api/audit-logs")]
public sealed class AuditLogsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> List(CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListAuditLogsQuery(), cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/assets")]
public sealed class AssetsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> List(CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListAssetsQuery(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult> Create(CreateAssetCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return Created($"/api/assets/{id}", new { id });
    }
}

[ApiController]
[Authorize]
[Route("api/liabilities")]
public sealed class LiabilitiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LiabilityDto>>> List(CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListLiabilitiesQuery(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult> Create(CreateLiabilityCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return Created($"/api/liabilities/{id}", new { id });
    }
}
