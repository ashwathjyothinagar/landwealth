using FluentValidation;
using LandWealth.Application.Common;
using LandWealth.Application.Common.Exceptions;
using LandWealth.Application.Common.Interfaces;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Application.Features.Documents;

public sealed record DocumentDto(
    Guid Id,
    Guid PropertyId,
    Guid? ParcelId,
    DocumentType DocumentType,
    string? DocumentNumber,
    DateOnly? IssueDate,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string? Notes);

public sealed record UploadDocumentCommand(
    Guid PropertyId,
    Guid? ParcelId,
    DocumentType DocumentType,
    string? DocumentNumber,
    DateOnly? IssueDate,
    string OriginalFileName,
    string ContentType,
    byte[] Content,
    string? Notes) : IRequest<Guid>;

public sealed class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentValidator()
    {
        RuleFor(command => command.OriginalFileName).NotEmpty().MaximumLength(255);
        RuleFor(command => command.ContentType).NotEmpty();
        RuleFor(command => command.DocumentNumber).MaximumLength(100);
        RuleFor(command => command.Notes).MaximumLength(2000);
        RuleFor(command => command.Content).NotNull().Must(content => content.Length > 0 && content.Length <= 25 * 1024 * 1024)
            .WithMessage("File size must be between 1 byte and 25 MB.");
    }
}

public sealed class UploadDocumentHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IFileStorageService storage) : IRequestHandler<UploadDocumentCommand, Guid>
{
    public async Task<Guid> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var propertyExists = await context.Properties.AnyAsync(property => property.Id == request.PropertyId, cancellationToken);
        if (!propertyExists)
            throw new NotFoundException("Property was not found.");
        if (request.ParcelId is Guid parcelId)
        {
            var parcelOnProperty = await context.PropertyParcels.AnyAsync(
                parcel => parcel.Id == parcelId && parcel.PropertyId == request.PropertyId, cancellationToken);
            if (!parcelOnProperty)
                throw new NotFoundException("Parcel was not found on this property.");
        }

        if (!FileSignatures.Matches(request.ContentType, request.Content))
            throw new DomainException("The file contents do not match the declared type.");

        var fileName = DocumentFiles.RequireSafeFileName(request.OriginalFileName);

        await using var stream = new MemoryStream(request.Content, writable: false);
        var storageKey = await storage.SaveAsync(stream, cancellationToken);
        var document = PropertyDocument.Create(
            request.PropertyId, currentUser.UserId, request.DocumentType, fileName, request.ContentType,
            request.Content.LongLength, storageKey, currentUser.UserId, request.ParcelId, request.DocumentNumber,
            request.IssueDate, request.Notes);
        context.Add(document);
        await context.SaveChangesAsync(cancellationToken);
        return document.Id;
    }
}

public sealed record ListDocumentsQuery(Guid PropertyId) : IRequest<IReadOnlyList<DocumentDto>>;

public sealed class ListDocumentsHandler(IApplicationDbContext context) : IRequestHandler<ListDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    public async Task<IReadOnlyList<DocumentDto>> Handle(ListDocumentsQuery request, CancellationToken cancellationToken)
    {
        var exists = await context.Properties.AnyAsync(property => property.Id == request.PropertyId, cancellationToken);
        if (!exists)
            throw new NotFoundException("Property was not found.");

        var documents = await context.PropertyDocuments.AsNoTracking()
            .Where(document => document.PropertyId == request.PropertyId)
            .OrderByDescending(document => document.CreatedAt)
            .ToListAsync(cancellationToken);
        return documents.Select(Map).ToList();
    }

    internal static DocumentDto Map(PropertyDocument document) => new(
        document.Id, document.PropertyId, document.ParcelId, document.DocumentType, document.DocumentNumber,
        document.IssueDate, document.OriginalFileName, document.ContentType, document.FileSize, document.Notes);
}

public sealed record DocumentDownload(DocumentDto Metadata, Stream Content);

public sealed record DownloadDocumentQuery(Guid Id) : IRequest<DocumentDownload>;

public sealed class DownloadDocumentHandler(IApplicationDbContext context, IFileStorageService storage)
    : IRequestHandler<DownloadDocumentQuery, DocumentDownload>
{
    public async Task<DocumentDownload> Handle(DownloadDocumentQuery request, CancellationToken cancellationToken)
    {
        var document = await context.PropertyDocuments.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Document was not found.");
        var stream = storage.OpenRead(document.StorageKey);
        return new DocumentDownload(ListDocumentsHandler.Map(document), stream);
    }
}
