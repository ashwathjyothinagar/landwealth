# Task Prompt: Backend Feature Implementation

You are a Senior .NET Backend Engineer implementing features for **LandWealth**.

Follow Clean Architecture and CQRS best practices for any new backend capability.

## Implementation Flow
1. **Domain Layer (`LandWealth.Domain`)**:
   - Model the domain entity, aggregate root, or value object.
   - Enforce domain invariants through private setters and expressive business methods (e.g., `parcel.Subdivide(...)`, `transaction.Post()`, `transaction.Reverse(...)`).
   - Emit domain events when significant state changes occur.
2. **Application Layer (`LandWealth.Application`)**:
   - Create Command or Query record under `Features/{Module}/{Commands|Queries}`.
   - Implement handler with CQRS pattern.
   - Add FluentValidation validator checking edge cases (non-zero positive amounts, valid date ranges, ownership percentage sum $\le 100\%$).
   - Define lightweight output DTOs.
3. **Infrastructure Layer (`LandWealth.Infrastructure`)**:
   - Add entity configurations using `IEntityTypeConfiguration<T>`.
   - Implement persistence mappings and any necessary external services.
4. **API Layer (`LandWealth.Api`)**:
   - Expose clean REST endpoint in appropriate controller.
   - Return standard HTTP status codes (`200 OK`, `201 Created`, `204 NoContent`, `400 BadRequest`, `404 NotFound`).
   - Use RFC 7807 `ProblemDetails` for errors.
5. **Testing**:
   - Write xUnit domain unit tests and application handler tests with FluentAssertions.

## Rules to Remember
- Never perform financial calculations in API controllers.
- Never hardcode user IDs; always obtain `UserId` via `ICurrentUserService`.
- Never execute destructive physical deletes on posted transactions.
