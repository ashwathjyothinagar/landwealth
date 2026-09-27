using FluentValidation;
using LandWealth.Application.Common.Exceptions;
using LandWealth.Application.Common.Interfaces;
using LandWealth.Domain.Accounting;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;
using LandWealth.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Application.Features.Properties;

public sealed record PropertySummaryDto(
    Guid Id,
    string Name,
    PropertyType PropertyType,
    PropertyStatus Status,
    string? Village,
    string? District,
    string State,
    decimal PurchasePrice,
    decimal ActiveExtentAcres);

public sealed record ParcelDto(
    Guid Id,
    string SurveyNumber,
    string? SubdivisionNumber,
    decimal Extent,
    ExtentUnit ExtentUnit,
    decimal ExtentInAcres,
    decimal OwnershipPercentage,
    ParcelStatus Status,
    string? BoundaryDescription,
    string? Notes);

public sealed record OwnerDto(
    Guid Id,
    string OwnerName,
    decimal OwnershipPercentage,
    OwnershipType OwnershipType,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive,
    string? Notes);

public sealed record PropertyDetailsDto(
    Guid Id,
    string Name,
    PropertyType PropertyType,
    PropertyStatus Status,
    string? PrimarySurveyNumber,
    DateOnly? PurchaseDate,
    decimal PurchasePrice,
    string? Location,
    string? Village,
    string? Taluk,
    string? District,
    string State,
    string Country,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    string? Notes,
    decimal ActiveExtentAcres,
    decimal ActiveOwnershipPercentage,
    IReadOnlyList<PropertyStatus> NextStatuses,
    IReadOnlyList<ParcelDto> Parcels,
    IReadOnlyList<OwnerDto> Owners);

public sealed record CreatePropertyCommand(
    string Name,
    PropertyType PropertyType,
    string State,
    string? Village,
    string? District,
    string? PrimarySurveyNumber) : IRequest<Guid>;

public sealed class CreatePropertyValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.State).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Village).MaximumLength(100);
        RuleFor(command => command.District).MaximumLength(100);
        RuleFor(command => command.PrimarySurveyNumber).MaximumLength(100);
    }
}

public sealed class CreatePropertyHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CreatePropertyCommand, Guid>
{
    public async Task<Guid> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = Property.Create(currentUser.UserId, request.Name, request.PropertyType, request.State, currentUser.UserId);
        property.UpdateDetails(
            request.Name, request.PropertyType, PropertyStatus.Planned, request.PrimarySurveyNumber,
            null, 0m, null, request.Village, null, request.District, request.State, "India", null, null, null, null,
            currentUser.UserId);
        context.Add(property);
        await context.SaveChangesAsync(cancellationToken);
        return property.Id;
    }
}

public sealed record UpdatePropertyCommand(
    Guid Id,
    string Name,
    PropertyType PropertyType,
    PropertyStatus Status,
    string? PrimarySurveyNumber,
    DateOnly? PurchaseDate,
    decimal PurchasePrice,
    string? Location,
    string? Village,
    string? Taluk,
    string? District,
    string State,
    string Country,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    string? Notes) : IRequest;

public sealed class UpdatePropertyValidator : AbstractValidator<UpdatePropertyCommand>
{
    public UpdatePropertyValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.State).NotEmpty().MaximumLength(100);
        RuleFor(command => command.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Country).MaximumLength(100);
    }
}

public sealed class UpdatePropertyHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<UpdatePropertyCommand>
{
    public async Task Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await context.Properties.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Property was not found.");
        property.UpdateDetails(
            request.Name, request.PropertyType, request.Status, request.PrimarySurveyNumber, request.PurchaseDate,
            request.PurchasePrice, request.Location, request.Village, request.Taluk, request.District, request.State,
            request.Country, request.PostalCode, request.Latitude, request.Longitude, request.Notes, currentUser.UserId);
        await context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record ListPropertiesQuery : IRequest<IReadOnlyList<PropertySummaryDto>>;

public sealed class ListPropertiesHandler(IApplicationDbContext context) : IRequestHandler<ListPropertiesQuery, IReadOnlyList<PropertySummaryDto>>
{
    public async Task<IReadOnlyList<PropertySummaryDto>> Handle(ListPropertiesQuery request, CancellationToken cancellationToken)
    {
        var properties = await context.Properties.Include(property => property.Parcels).AsNoTracking().ToListAsync(cancellationToken);
        return properties.Select(MapSummary).ToList();
    }

    internal static PropertySummaryDto MapSummary(Property property) => new(
        property.Id, property.Name, property.PropertyType, property.Status, property.Village, property.District,
        property.State, property.PurchasePrice,
        property.Parcels.Where(parcel => parcel.Status == ParcelStatus.Active).Sum(parcel => parcel.ExtentInAcres));
}

public sealed record GetPropertyQuery(Guid Id) : IRequest<PropertyDetailsDto>;

public sealed class GetPropertyHandler(IApplicationDbContext context) : IRequestHandler<GetPropertyQuery, PropertyDetailsDto>
{
    public async Task<PropertyDetailsDto> Handle(GetPropertyQuery request, CancellationToken cancellationToken)
    {
        var property = await context.Properties
            .Include(item => item.Parcels)
            .Include(item => item.Owners)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Property was not found.");

        return new PropertyDetailsDto(
            property.Id, property.Name, property.PropertyType, property.Status, property.PrimarySurveyNumber,
            property.PurchaseDate, property.PurchasePrice, property.Location, property.Village, property.Taluk,
            property.District, property.State, property.Country, property.PostalCode, property.Latitude, property.Longitude,
            property.Notes,
            property.Parcels.Where(parcel => parcel.Status == ParcelStatus.Active).Sum(parcel => parcel.ExtentInAcres),
            property.Owners.Where(owner => owner.IsActive).Sum(owner => owner.OwnershipPercentage),
            property.NextStatuses(),
            property.Parcels.Select(parcel => new ParcelDto(
                parcel.Id, parcel.SurveyNumber, parcel.SubdivisionNumber, parcel.Extent.Value, parcel.Extent.Unit,
                parcel.ExtentInAcres, parcel.OwnershipPercentage, parcel.Status, parcel.BoundaryDescription, parcel.Notes)).ToList(),
            property.Owners.Select(owner => new OwnerDto(
                owner.Id, owner.OwnerName, owner.OwnershipPercentage, owner.OwnershipType, owner.StartDate, owner.EndDate,
                owner.IsActive, owner.Notes)).ToList());
    }
}

public sealed record TransitionPropertyCommand(Guid Id, PropertyStatus Status) : IRequest;

public sealed class TransitionPropertyHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<TransitionPropertyCommand>
{
    public async Task Handle(TransitionPropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await context.Properties.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Property was not found.");
        property.Transition(request.Status, currentUser.UserId);
        await context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record UpdateParcelCommand(
    Guid PropertyId,
    Guid ParcelId,
    string SurveyNumber,
    string? SubdivisionNumber,
    decimal Extent,
    ExtentUnit ExtentUnit,
    decimal OwnershipPercentage,
    string? BoundaryDescription,
    string? Notes) : IRequest;

public sealed class UpdateParcelValidator : AbstractValidator<UpdateParcelCommand>
{
    public UpdateParcelValidator()
    {
        RuleFor(command => command.SurveyNumber).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Extent).GreaterThan(0);
        RuleFor(command => command.OwnershipPercentage).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(command => command.SubdivisionNumber).MaximumLength(50);
    }
}

public sealed class UpdateParcelHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<UpdateParcelCommand>
{
    public async Task Handle(UpdateParcelCommand request, CancellationToken cancellationToken)
    {
        var parcel = await context.PropertyParcels.FirstOrDefaultAsync(
            item => item.Id == request.ParcelId && item.PropertyId == request.PropertyId, cancellationToken)
            ?? throw new NotFoundException("Parcel was not found.");
        parcel.Update(
            request.SurveyNumber, request.SubdivisionNumber, request.Extent, request.ExtentUnit,
            request.OwnershipPercentage, request.BoundaryDescription, request.Notes, currentUser.UserId);
        await context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record TransferOwnerCommand(Guid PropertyId, Guid OwnerId, DateOnly EndDate) : IRequest;

public sealed class TransferOwnerHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<TransferOwnerCommand>
{
    public async Task Handle(TransferOwnerCommand request, CancellationToken cancellationToken)
    {
        var owner = await context.PropertyOwners.FirstOrDefaultAsync(
            item => item.Id == request.OwnerId && item.PropertyId == request.PropertyId, cancellationToken)
            ?? throw new NotFoundException("Owner was not found.");
        owner.Transfer(request.EndDate, currentUser.UserId);
        await context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record DeletePropertyCommand(Guid Id) : IRequest;

public sealed class DeletePropertyHandler(IApplicationDbContext context, ICurrentUserService currentUser) : IRequestHandler<DeletePropertyCommand>
{
    public async Task Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await context.Properties.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Property was not found.");
        property.SoftDelete(currentUser.UserId);
        await context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record AddParcelCommand(
    Guid PropertyId,
    string SurveyNumber,
    string? SubdivisionNumber,
    decimal Extent,
    ExtentUnit ExtentUnit,
    decimal OwnershipPercentage,
    string? BoundaryDescription,
    string? Notes) : IRequest<Guid>;

public sealed class AddParcelValidator : AbstractValidator<AddParcelCommand>
{
    public AddParcelValidator()
    {
        RuleFor(command => command.SurveyNumber).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Extent).GreaterThan(0);
        RuleFor(command => command.OwnershipPercentage).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public sealed class AddParcelHandler(IApplicationDbContext context, ICurrentUserService currentUser) : IRequestHandler<AddParcelCommand, Guid>
{
    public async Task<Guid> Handle(AddParcelCommand request, CancellationToken cancellationToken)
    {
        var property = await context.Properties.Include(item => item.Parcels)
            .FirstOrDefaultAsync(item => item.Id == request.PropertyId, cancellationToken)
            ?? throw new NotFoundException("Property was not found.");
        var parcel = property.AddParcel(
            request.SurveyNumber, request.Extent, request.ExtentUnit, currentUser.UserId,
            request.SubdivisionNumber, request.OwnershipPercentage);
        parcel.Update(
            request.SurveyNumber, request.SubdivisionNumber, request.Extent, request.ExtentUnit,
            request.OwnershipPercentage, request.BoundaryDescription, request.Notes, currentUser.UserId);
        await context.SaveChangesAsync(cancellationToken);
        return parcel.Id;
    }
}

public sealed record SubdivideParcelCommand(
    Guid PropertyId,
    Guid ParcelId,
    decimal SplitExtent,
    string RemainderSubdivision,
    string SplitSubdivision) : IRequest<Guid>;

public sealed class SubdivideParcelValidator : AbstractValidator<SubdivideParcelCommand>
{
    public SubdivideParcelValidator()
    {
        RuleFor(command => command.SplitExtent).GreaterThan(0);
        RuleFor(command => command.RemainderSubdivision).NotEmpty().MaximumLength(50);
        RuleFor(command => command.SplitSubdivision).NotEmpty().MaximumLength(50);
    }
}

public sealed class SubdivideParcelHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<SubdivideParcelCommand, Guid>
{
    public async Task<Guid> Handle(SubdivideParcelCommand request, CancellationToken cancellationToken)
    {
        var property = await context.Properties.Include(item => item.Parcels)
            .FirstOrDefaultAsync(item => item.Id == request.PropertyId, cancellationToken)
            ?? throw new NotFoundException("Property was not found.");
        var (_, split) = property.SubdivideParcel(
            request.ParcelId, request.SplitExtent, request.RemainderSubdivision, request.SplitSubdivision, currentUser.UserId);
        await context.SaveChangesAsync(cancellationToken);
        return split.Id;
    }
}

public sealed record AddOwnerCommand(
    Guid PropertyId,
    string OwnerName,
    decimal OwnershipPercentage,
    OwnershipType OwnershipType,
    DateOnly StartDate,
    string? Notes) : IRequest<Guid>;

public sealed class AddOwnerValidator : AbstractValidator<AddOwnerCommand>
{
    public AddOwnerValidator()
    {
        RuleFor(command => command.OwnerName).NotEmpty().MaximumLength(150);
        RuleFor(command => command.OwnershipPercentage).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public sealed class AddOwnerHandler(IApplicationDbContext context, ICurrentUserService currentUser) : IRequestHandler<AddOwnerCommand, Guid>
{
    public async Task<Guid> Handle(AddOwnerCommand request, CancellationToken cancellationToken)
    {
        var property = await context.Properties.Include(item => item.Owners)
            .FirstOrDefaultAsync(item => item.Id == request.PropertyId, cancellationToken)
            ?? throw new NotFoundException("Property was not found.");
        var owner = PropertyOwner.Create(
            property.Id, currentUser.UserId, request.OwnerName, request.OwnershipPercentage,
            request.OwnershipType, request.StartDate, currentUser.UserId, request.Notes);
        property.AddOwner(owner);
        await context.SaveChangesAsync(cancellationToken);
        return owner.Id;
    }
}

public sealed record PropertyAccountingDto(
    decimal CostBasis,
    decimal? LatestValuation,
    decimal? UnrealizedGain,
    decimal ActiveExtentAcres,
    decimal? AllocatedCostBasis,
    decimal? RealizedGain,
    decimal AcquisitionCost,
    decimal Improvements,
    decimal Maintenance,
    decimal? GuidanceValue,
    decimal? MarketValue);

public sealed record GetPropertyAccountingQuery(
    Guid PropertyId,
    decimal? ExtentSold,
    ExtentUnit? ExtentUnit,
    decimal? GrossProceeds,
    decimal? SellingExpenses) : IRequest<PropertyAccountingDto>;

public sealed class GetPropertyAccountingHandler(IApplicationDbContext context)
    : IRequestHandler<GetPropertyAccountingQuery, PropertyAccountingDto>
{
    public async Task<PropertyAccountingDto> Handle(GetPropertyAccountingQuery request, CancellationToken cancellationToken)
    {
        var property = await context.Properties.Include(item => item.Parcels).Include(item => item.Valuations)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.PropertyId, cancellationToken)
            ?? throw new NotFoundException("Property was not found.");

        var lines = await (
            from line in context.TransactionLines
            join transaction in context.Transactions on line.TransactionId equals transaction.Id
            join category in context.Categories on line.CategoryId equals category.Id into categories
            from category in categories.DefaultIfEmpty()
            where line.PropertyId == request.PropertyId && transaction.Status != TransactionStatus.Draft
            select new BasisLine(line.PropertyId, line.LineType, line.Amount, category == null ? null : category.CategoryType))
            .ToListAsync(cancellationToken);

        var summary = PropertyAccounting.Summarize(lines, request.PropertyId);
        var history = property.Valuations.Select(valuation =>
            new ValuationSnapshot(valuation.ValuationDate, valuation.EstimatedValue, valuation.ValuationSource, valuation.CreatedAt));
        var estimated = ValuationPolicy.EstimatedValue(history);
        var activeExtent = property.Parcels.Where(parcel => parcel.Status == ParcelStatus.Active).Sum(parcel => parcel.ExtentInAcres);
        decimal? soldAcres = request.ExtentSold is null
            ? null
            : new Extent(request.ExtentSold.Value, request.ExtentUnit ?? ExtentUnit.Acres).InAcres;
        decimal? allocated = soldAcres is null ? null : PropertyAccounting.AllocatedCostBasis(summary.CostBasis, soldAcres.Value, activeExtent);
        decimal? realized = allocated is null || request.GrossProceeds is null
            ? null
            : PropertyAccounting.RealizedGain(request.GrossProceeds.Value, request.SellingExpenses ?? 0m, allocated.Value);

        return new PropertyAccountingDto(
            summary.CostBasis, estimated, PropertyAccounting.UnrealizedGain(estimated, summary.CostBasis), activeExtent, allocated, realized,
            summary.Acquisition, summary.Improvements, summary.Maintenance,
            ValuationPolicy.LatestGuidance(history)?.Amount, ValuationPolicy.LatestMarket(history)?.Amount);
    }
}
