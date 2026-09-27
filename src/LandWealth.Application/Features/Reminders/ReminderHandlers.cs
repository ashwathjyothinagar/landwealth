using FluentValidation;
using LandWealth.Application.Common.Exceptions;
using LandWealth.Application.Common.Interfaces;
using LandWealth.Domain.Accounting;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Application.Features.Reminders;

public sealed record ReminderDto(
    Guid Id,
    Guid? PropertyId,
    string Title,
    string? Description,
    DateOnly DueDate,
    ReminderPriority Priority,
    ReminderStatus Status,
    DateOnly? CompletedDate);

public sealed record CreateReminderCommand(
    string Title,
    DateOnly DueDate,
    ReminderPriority Priority,
    Guid? PropertyId,
    string? Description) : IRequest<Guid>;

public sealed class CreateReminderValidator : AbstractValidator<CreateReminderCommand>
{
    public CreateReminderValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(2000);
    }
}

public sealed class CreateReminderHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CreateReminderCommand, Guid>
{
    public async Task<Guid> Handle(CreateReminderCommand request, CancellationToken cancellationToken)
    {
        if (request.PropertyId is not null &&
            !await context.Properties.AnyAsync(property => property.Id == request.PropertyId, cancellationToken))
            throw new NotFoundException("Property was not found.");

        var reminder = PropertyReminder.Create(
            currentUser.UserId, request.Title, request.DueDate, request.Priority, currentUser.UserId,
            request.PropertyId, request.Description);
        context.Add(reminder);
        await context.SaveChangesAsync(cancellationToken);
        return reminder.Id;
    }
}

public sealed record ListRemindersQuery(bool UpcomingOnly) : IRequest<IReadOnlyList<ReminderDto>>;

public sealed class ListRemindersHandler(IApplicationDbContext context, IDateTimeProvider clock)
    : IRequestHandler<ListRemindersQuery, IReadOnlyList<ReminderDto>>
{
    public async Task<IReadOnlyList<ReminderDto>> Handle(ListRemindersQuery request, CancellationToken cancellationToken)
    {
        var reminders = await context.PropertyReminders.AsNoTracking().OrderBy(reminder => reminder.DueDate).ToListAsync(cancellationToken);
        var today = clock.Today;
        var projected = reminders.Select(reminder => Map(reminder, today));
        if (request.UpcomingOnly)
            projected = projected.Where(reminder => reminder.Status is ReminderStatus.Pending or ReminderStatus.Overdue);
        return projected.ToList();
    }

    internal static ReminderDto Map(PropertyReminder reminder, DateOnly today)
    {
        var overdue = reminder.Status == ReminderStatus.Pending && reminder.DueDate < today;
        return new ReminderDto(
            reminder.Id, reminder.PropertyId, reminder.Title, reminder.Description, reminder.DueDate,
            ReminderPolicy.Escalate(reminder.Priority, overdue),
            ReminderPolicy.DisplayStatus(reminder.Status, overdue),
            reminder.CompletedDate);
    }
}

public sealed record CompleteReminderCommand(Guid Id, DateOnly CompletedDate) : IRequest;

public sealed class CompleteReminderHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CompleteReminderCommand>
{
    public async Task Handle(CompleteReminderCommand request, CancellationToken cancellationToken)
    {
        var reminder = await context.PropertyReminders.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Reminder was not found.");
        reminder.Complete(request.CompletedDate, currentUser.UserId);
        await context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record DismissReminderCommand(Guid Id) : IRequest;

public sealed class DismissReminderHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<DismissReminderCommand>
{
    public async Task Handle(DismissReminderCommand request, CancellationToken cancellationToken)
    {
        var reminder = await context.PropertyReminders.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Reminder was not found.");
        reminder.Dismiss(currentUser.UserId);
        await context.SaveChangesAsync(cancellationToken);
    }
}
