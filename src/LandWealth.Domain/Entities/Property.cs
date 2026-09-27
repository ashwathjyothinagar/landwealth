using LandWealth.Domain.Common;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.Entities;

public class Property : AuditableEntity<Guid>
{
    private readonly List<PropertyParcel> _parcels = new();
    private readonly List<PropertyOwner> _owners = new();
    private readonly List<PropertyValuation> _valuations = new();
    private readonly List<PropertyDocument> _documents = new();
    private readonly List<PropertyReminder> _reminders = new();

    private Property() { }

    public string Name { get; private set; } = default!;
    public PropertyType PropertyType { get; private set; }
    public PropertyStatus Status { get; private set; }
    public string? PrimarySurveyNumber { get; private set; }
    public DateOnly? PurchaseDate { get; private set; }
    public decimal PurchasePrice { get; private set; }
    public string? Location { get; private set; }
    public string? Village { get; private set; }
    public string? Taluk { get; private set; }
    public string? District { get; private set; }
    public string State { get; private set; } = default!;
    public string Country { get; private set; } = "India";
    public string? PostalCode { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<PropertyParcel> Parcels => _parcels.AsReadOnly();
    public IReadOnlyCollection<PropertyOwner> Owners => _owners.AsReadOnly();
    public IReadOnlyCollection<PropertyValuation> Valuations => _valuations.AsReadOnly();
    public IReadOnlyCollection<PropertyDocument> Documents => _documents.AsReadOnly();
    public IReadOnlyCollection<PropertyReminder> Reminders => _reminders.AsReadOnly();

    public static Property Create(
        Guid userId,
        string name,
        PropertyType propertyType,
        string state,
        Guid createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        return new Property
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            PropertyType = propertyType,
            State = state.Trim(),
            Country = "India",
            Status = PropertyStatus.Planned,
            PurchasePrice = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void UpdateDetails(
        string name,
        PropertyType propertyType,
        PropertyStatus status,
        string? primarySurveyNumber,
        DateOnly? purchaseDate,
        decimal purchasePrice,
        string? location,
        string? village,
        string? taluk,
        string? district,
        string state,
        string country,
        string? postalCode,
        decimal? latitude,
        decimal? longitude,
        string? notes,
        Guid modifiedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (purchasePrice < 0)
            throw new DomainException("Purchase price cannot be negative.");

        if (status != Status)
            Transition(status, modifiedBy);

        Name = name.Trim();
        PropertyType = propertyType;
        PrimarySurveyNumber = primarySurveyNumber?.Trim();
        PurchaseDate = purchaseDate;
        PurchasePrice = purchasePrice;
        Location = location?.Trim();
        Village = village?.Trim();
        Taluk = taluk?.Trim();
        District = district?.Trim();
        State = state.Trim();
        Country = string.IsNullOrWhiteSpace(country) ? "India" : country.Trim();
        PostalCode = postalCode?.Trim();
        Latitude = latitude;
        Longitude = longitude;
        Notes = notes?.Trim();
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Transition(PropertyStatus newStatus, Guid modifiedBy)
    {
        if (Status == newStatus)
            return;

        if (!AllowedTransitions[Status].Contains(newStatus))
            throw new DomainException($"Cannot move a property from {Status} to {newStatus}.");

        Status = newStatus;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public IReadOnlyList<PropertyStatus> NextStatuses() => AllowedTransitions[Status];

    public PropertyParcel AddParcel(
        string surveyNumber,
        decimal extent,
        ExtentUnit unit,
        Guid createdBy,
        string? subdivisionNumber = null,
        decimal ownershipPercentage = 100m)
    {
        var parcel = PropertyParcel.Create(Id, UserId, surveyNumber, extent, unit, createdBy, subdivisionNumber, ownershipPercentage);
        _parcels.Add(parcel);
        return parcel;
    }

    public PropertyOwner AddOwner(PropertyOwner owner)
    {
        if (owner.PropertyId != Id)
            throw new DomainException("Owner does not belong to this property.");

        var activeTotal = _owners.Where(existing => existing.IsActive).Sum(existing => existing.OwnershipPercentage);
        if (activeTotal + owner.OwnershipPercentage > 100m)
            throw new DomainException($"Active ownership would total {activeTotal + owner.OwnershipPercentage}%, which exceeds 100%.");

        _owners.Add(owner);
        return owner;
    }

    public (PropertyParcel Remainder, PropertyParcel Split) SubdivideParcel(
        Guid parcelId,
        decimal splitExtent,
        string remainderSubdivision,
        string splitSubdivision,
        Guid modifiedBy)
    {
        var parcel = _parcels.FirstOrDefault(existing => existing.Id == parcelId)
            ?? throw new DomainException("Parcel was not found on this property.");

        if (parcel.Status != ParcelStatus.Active)
            throw new DomainException("Only an active parcel can be subdivided.");
        if (splitExtent <= 0 || splitExtent >= parcel.Extent.Value)
            throw new DomainException("Split extent must be greater than zero and less than the parcel extent.");

        ArgumentException.ThrowIfNullOrWhiteSpace(remainderSubdivision);
        ArgumentException.ThrowIfNullOrWhiteSpace(splitSubdivision);

        var remainderExtent = parcel.Extent.Value - splitExtent;
        var unit = parcel.Extent.Unit;
        parcel.MarkSubdivided(modifiedBy);

        var remainder = PropertyParcel.Create(
            Id, UserId, parcel.SurveyNumber, remainderExtent, unit, modifiedBy, remainderSubdivision.Trim(), parcel.OwnershipPercentage);
        var split = PropertyParcel.Create(
            Id, UserId, parcel.SurveyNumber, splitExtent, unit, modifiedBy, splitSubdivision.Trim(), parcel.OwnershipPercentage);

        _parcels.Add(remainder);
        _parcels.Add(split);
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        return (remainder, split);
    }

    public void SoftDelete(Guid modifiedBy)
    {
        IsDeleted = true;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    private static readonly IReadOnlyDictionary<PropertyStatus, PropertyStatus[]> AllowedTransitions =
        new Dictionary<PropertyStatus, PropertyStatus[]>
        {
            [PropertyStatus.Planned] = [PropertyStatus.Purchased, PropertyStatus.Archived],
            [PropertyStatus.Purchased] = [PropertyStatus.Held, PropertyStatus.UnderDevelopment, PropertyStatus.ForSale, PropertyStatus.Sold, PropertyStatus.Archived],
            [PropertyStatus.Held] = [PropertyStatus.UnderDevelopment, PropertyStatus.ForSale, PropertyStatus.Sold, PropertyStatus.Archived],
            [PropertyStatus.UnderDevelopment] = [PropertyStatus.Held, PropertyStatus.ForSale, PropertyStatus.Sold, PropertyStatus.Archived],
            [PropertyStatus.ForSale] = [PropertyStatus.Held, PropertyStatus.Sold, PropertyStatus.Archived],
            [PropertyStatus.Sold] = [PropertyStatus.Archived],
            [PropertyStatus.Archived] = []
        };

    /// <summary>
    /// Returns the most recent estimated market value from the valuation history.
    /// Returns null if no valuations have been entered yet.
    /// </summary>
    public decimal? LatestEstimatedValue =>
        _valuations.OrderByDescending(v => v.ValuationDate).FirstOrDefault()?.EstimatedValue;
}
