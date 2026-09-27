using FluentValidation;
using LandWealth.Application.Common.Interfaces;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Application.Features.NetWorth;

public sealed record AssetDto(Guid Id, string Name, AssetType AssetType, decimal EstimatedValue, decimal AcquisitionCost, decimal UnrealizedGain, DateOnly? AcquisitionDate);
public sealed record LiabilityDto(Guid Id, string Name, LiabilityType LiabilityType, string? Lender, decimal PrincipalAmount, decimal OutstandingBalance, decimal? InterestRate);

public sealed record CreateAssetCommand(string Name, AssetType AssetType, decimal AcquisitionCost, decimal EstimatedValue, DateOnly? AcquisitionDate, string? Notes) : IRequest<Guid>;

public sealed class CreateAssetValidator : AbstractValidator<CreateAssetCommand>
{
    public CreateAssetValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.AcquisitionCost).GreaterThanOrEqualTo(0);
        RuleFor(command => command.EstimatedValue).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateAssetHandler(IApplicationDbContext context, ICurrentUserService currentUser) : IRequestHandler<CreateAssetCommand, Guid>
{
    public async Task<Guid> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = Asset.Create(currentUser.UserId, request.Name, request.AssetType, request.AcquisitionCost, request.EstimatedValue, currentUser.UserId, request.AcquisitionDate, request.Notes);
        context.Add(asset);
        await context.SaveChangesAsync(cancellationToken);
        return asset.Id;
    }
}

public sealed record ListAssetsQuery : IRequest<IReadOnlyList<AssetDto>>;

public sealed class ListAssetsHandler(IApplicationDbContext context) : IRequestHandler<ListAssetsQuery, IReadOnlyList<AssetDto>>
{
    public async Task<IReadOnlyList<AssetDto>> Handle(ListAssetsQuery request, CancellationToken cancellationToken)
    {
        var assets = await context.Assets.AsNoTracking().OrderBy(asset => asset.Name).ToListAsync(cancellationToken);
        return assets.Select(asset => new AssetDto(asset.Id, asset.Name, asset.AssetType, asset.EstimatedValue, asset.AcquisitionCost, asset.UnrealizedGain, asset.AcquisitionDate)).ToList();
    }
}

public sealed record CreateLiabilityCommand(
    string Name, LiabilityType LiabilityType, decimal PrincipalAmount, string? Lender, decimal? InterestRate, DateOnly? StartDate, DateOnly? EndDate, string? Notes) : IRequest<Guid>;

public sealed class CreateLiabilityValidator : AbstractValidator<CreateLiabilityCommand>
{
    public CreateLiabilityValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.PrincipalAmount).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateLiabilityHandler(IApplicationDbContext context, ICurrentUserService currentUser) : IRequestHandler<CreateLiabilityCommand, Guid>
{
    public async Task<Guid> Handle(CreateLiabilityCommand request, CancellationToken cancellationToken)
    {
        var liability = Liability.Create(
            currentUser.UserId, request.Name, request.LiabilityType, request.PrincipalAmount, currentUser.UserId,
            request.Lender, request.InterestRate, request.StartDate, request.EndDate, request.Notes);
        context.Add(liability);
        await context.SaveChangesAsync(cancellationToken);
        return liability.Id;
    }
}

public sealed record ListLiabilitiesQuery : IRequest<IReadOnlyList<LiabilityDto>>;

public sealed class ListLiabilitiesHandler(IApplicationDbContext context) : IRequestHandler<ListLiabilitiesQuery, IReadOnlyList<LiabilityDto>>
{
    public async Task<IReadOnlyList<LiabilityDto>> Handle(ListLiabilitiesQuery request, CancellationToken cancellationToken)
    {
        var liabilities = await context.Liabilities.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken);
        return liabilities.Select(item => new LiabilityDto(item.Id, item.Name, item.LiabilityType, item.Lender, item.PrincipalAmount, item.OutstandingBalance, item.InterestRate)).ToList();
    }
}
