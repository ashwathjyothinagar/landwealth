using FluentValidation;
using LandWealth.Application.Common.Exceptions;
using LandWealth.Application.Common.Interfaces;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Application.Features.Valuations;

public sealed record ValuationDto(Guid Id, DateOnly ValuationDate, decimal EstimatedValue, ValuationSource ValuationSource, string? Notes);

public sealed record AddValuationCommand(
    Guid PropertyId,
    DateOnly ValuationDate,
    decimal EstimatedValue,
    ValuationSource ValuationSource,
    string? Notes) : IRequest<Guid>;

public sealed class AddValuationValidator : AbstractValidator<AddValuationCommand>
{
    public AddValuationValidator()
    {
        RuleFor(command => command.EstimatedValue).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Notes).MaximumLength(2000);
    }
}

public sealed class AddValuationHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<AddValuationCommand, Guid>
{
    public async Task<Guid> Handle(AddValuationCommand request, CancellationToken cancellationToken)
    {
        var exists = await context.Properties.AnyAsync(property => property.Id == request.PropertyId, cancellationToken);
        if (!exists)
            throw new NotFoundException("Property was not found.");

        var valuation = PropertyValuation.Create(
            request.PropertyId, currentUser.UserId, request.ValuationDate, request.EstimatedValue,
            request.ValuationSource, currentUser.UserId, request.Notes);
        context.Add(valuation);
        await context.SaveChangesAsync(cancellationToken);
        return valuation.Id;
    }
}

public sealed record ListValuationsQuery(Guid PropertyId) : IRequest<IReadOnlyList<ValuationDto>>;

public sealed class ListValuationsHandler(IApplicationDbContext context) : IRequestHandler<ListValuationsQuery, IReadOnlyList<ValuationDto>>
{
    public async Task<IReadOnlyList<ValuationDto>> Handle(ListValuationsQuery request, CancellationToken cancellationToken)
    {
        var exists = await context.Properties.AnyAsync(property => property.Id == request.PropertyId, cancellationToken);
        if (!exists)
            throw new NotFoundException("Property was not found.");

        var valuations = await context.PropertyValuations.AsNoTracking()
            .Where(valuation => valuation.PropertyId == request.PropertyId)
            .OrderBy(valuation => valuation.ValuationDate)
            .ToListAsync(cancellationToken);

        return valuations.Select(valuation => new ValuationDto(
            valuation.Id, valuation.ValuationDate, valuation.EstimatedValue, valuation.ValuationSource, valuation.Notes)).ToList();
    }
}
