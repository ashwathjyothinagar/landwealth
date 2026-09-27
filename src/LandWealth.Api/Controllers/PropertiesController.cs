using LandWealth.Application.Features.Documents;
using LandWealth.Application.Features.Properties;
using LandWealth.Application.Features.Valuations;
using LandWealth.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LandWealth.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/properties")]
public sealed class PropertiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PropertySummaryDto>>> List(CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListPropertiesQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyDetailsDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPropertyQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult> Create(CreatePropertyCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePropertyCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> Transition(Guid id, TransitionPropertyCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/parcels/{parcelId:guid}")]
    public async Task<IActionResult> UpdateParcel(Guid id, Guid parcelId, UpdateParcelCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { PropertyId = id, ParcelId = parcelId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/owners/{ownerId:guid}/transfer")]
    public async Task<IActionResult> TransferOwner(Guid id, Guid ownerId, TransferOwnerCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { PropertyId = id, OwnerId = ownerId }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePropertyCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/parcels")]
    public async Task<ActionResult> AddParcel(Guid id, AddParcelCommand command, CancellationToken cancellationToken)
    {
        var parcelId = await sender.Send(command with { PropertyId = id }, cancellationToken);
        return Created($"/api/properties/{id}", new { id = parcelId });
    }

    [HttpPost("{id:guid}/parcels/{parcelId:guid}/subdivide")]
    public async Task<ActionResult> Subdivide(Guid id, Guid parcelId, SubdivideParcelCommand command, CancellationToken cancellationToken)
    {
        var splitId = await sender.Send(command with { PropertyId = id, ParcelId = parcelId }, cancellationToken);
        return Created($"/api/properties/{id}", new { id = splitId });
    }

    [HttpPost("{id:guid}/owners")]
    public async Task<ActionResult> AddOwner(Guid id, AddOwnerCommand command, CancellationToken cancellationToken)
    {
        var ownerId = await sender.Send(command with { PropertyId = id }, cancellationToken);
        return Created($"/api/properties/{id}", new { id = ownerId });
    }

    [HttpGet("{id:guid}/accounting")]
    public async Task<ActionResult<PropertyAccountingDto>> Accounting(
        Guid id, decimal? extentSold, ExtentUnit? extentUnit, decimal? grossProceeds, decimal? sellingExpenses, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPropertyAccountingQuery(id, extentSold, extentUnit, grossProceeds, sellingExpenses), cancellationToken));

    [HttpGet("{id:guid}/valuations")]
    public async Task<ActionResult<IReadOnlyList<ValuationDto>>> Valuations(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListValuationsQuery(id), cancellationToken));

    [HttpPost("{id:guid}/valuations")]
    public async Task<ActionResult> AddValuation(Guid id, AddValuationCommand command, CancellationToken cancellationToken)
    {
        var valuationId = await sender.Send(command with { PropertyId = id }, cancellationToken);
        return Created($"/api/properties/{id}/valuations", new { id = valuationId });
    }

    [HttpGet("{id:guid}/documents")]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> Documents(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListDocumentsQuery(id), cancellationToken));

    [HttpPost("{id:guid}/documents")]
    [RequestSizeLimit(26_214_400)]
    public async Task<ActionResult> UploadDocument(Guid id, [FromForm] DocumentUploadForm form, CancellationToken cancellationToken)
    {
        if (form.File is null)
            throw new LandWealth.Domain.Exceptions.DomainException("A file is required.");
        await using var buffer = new MemoryStream();
        await form.File.CopyToAsync(buffer, cancellationToken);
        var documentId = await sender.Send(new UploadDocumentCommand(
            id, form.ParcelId, form.DocumentType, form.DocumentNumber, form.IssueDate,
            form.File.FileName, form.File.ContentType, buffer.ToArray(), form.Notes), cancellationToken);
        return Created($"/api/documents/{documentId}", new { id = documentId });
    }
}

public sealed class DocumentUploadForm
{
    public IFormFile File { get; set; } = default!;
    public DocumentType DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public DateOnly? IssueDate { get; set; }
    public Guid? ParcelId { get; set; }
    public string? Notes { get; set; }
}
