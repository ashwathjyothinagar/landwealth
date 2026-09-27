using LandWealth.Domain.Common;
using LandWealth.Domain.Enums;

namespace LandWealth.Domain.Events;

public sealed record TransactionPostedEvent(
    Guid TransactionId,
    Guid UserId,
    decimal Amount,
    TransactionType TransactionType) : DomainEvent;
