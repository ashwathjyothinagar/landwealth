using FluentValidation;
using LandWealth.Application.Common.Interfaces;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Application.Features.Auth;

public sealed record AuthResponse(Guid UserId, string Email, string FullName, string Token, DateTime ExpiresAt);

public sealed record RegisterUserCommand(string Email, string Password, string FullName) : IRequest<AuthResponse>;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(150);
    }
}

public sealed class RegisterUserHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator) : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await context.FindUserByEmailAsync(email, cancellationToken);
        if (existing is not null)
            throw new DomainException("An account with this email already exists.");

        var user = User.Create(email, passwordHasher.Hash(request.Password), request.FullName);
        context.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(user, tokenGenerator.Create(user));
    }

    internal static AuthResponse ToResponse(User user, AccessToken accessToken)
        => new(user.Id, user.Email, user.FullName, accessToken.Token, accessToken.ExpiresAt);
}

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Password).NotEmpty();
    }
}

public sealed class LoginHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await context.FindUserByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email or password is incorrect.");

        return RegisterUserHandler.ToResponse(user, tokenGenerator.Create(user));
    }
}

public sealed record CurrentUserDto(Guid UserId, string Email, string FullName);

public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto>;

public sealed class GetCurrentUserHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var profile = await context.Users.FirstOrDefaultAsync(candidate => candidate.Id == currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("The current user was not found.");
        return new CurrentUserDto(profile.Id, profile.Email, profile.FullName);
    }
}
